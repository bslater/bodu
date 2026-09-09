// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel.Biff;
using Bodu.IO.Biff;

namespace Bodu.Formats.Excel;

/// <summary>
/// Provides a forward-only, low-allocation reader over the populated cells of a single worksheet substream.
/// </summary>
/// <remarks>
/// <para>
/// This is the high-throughput surface: the worksheet's substream is read once into a buffer, and cells are decoded on
/// demand through <see cref="TryReadCell(out ExcelCell)" /> without building an intermediate record list or a
/// position-keyed map. Only value-bearing records are surfaced (text, number, boolean, and error cells, including the
/// cached result of a formula cell); blank cells and uninterpreted records are skipped, so the sequence is sparse and
/// in record order.
/// </para>
/// <para>
/// For random access by position, materialize the worksheet instead with
/// <see cref="ExcelBinaryWorkbook.ReadWorksheet(int)" />.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Formats.Excel;
///
/// using var workbook = ExcelBinaryWorkbook.OpenRead("report.xls");
/// using ExcelWorksheetReader reader = workbook.OpenWorksheet(0);
/// while (reader.TryReadCell(out ExcelCell cell))
/// {
///     string value = cell.Kind switch
///     {
///         ExcelCellKind.String => cell.StringValue ?? string.Empty,
///         ExcelCellKind.Number => cell.NumberValue?.ToString() ?? string.Empty,
///         ExcelCellKind.Boolean => cell.BooleanValue?.ToString() ?? string.Empty,
///         ExcelCellKind.Error => cell.ErrorValue?.ToString() ?? string.Empty,
///         _ => string.Empty,
///     };
///     Console.WriteLine($"R{cell.RowIndex} C{cell.ColumnIndex}: {value}");
/// }
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class ExcelWorksheetReader
    : IDisposable
{
    /// <summary>The worksheet substream bytes, from its BOF record through its EOF record.</summary>
    private readonly byte[] _data;

    /// <summary>The workbook shared string table.</summary>
    private readonly string[] _sharedStrings;

    /// <summary>The workbook format table used to resolve each cell's number format.</summary>
    private readonly BiffFormatTable _formats;

    /// <summary>Cells pending emission from an expanded <c>MULRK</c> record.</summary>
    private ExcelCell[] _pending = [];

    /// <summary>The index of the next pending cell to emit.</summary>
    private int _pendingIndex;

    /// <summary>The offset of the next record header within the substream.</summary>
    private int _position;

    /// <summary>The codec state carried between reads: the version and code page of the stream.</summary>
    private BiffReaderState _state;

    /// <summary>Whether the reader has reached the worksheet's end-of-file record.</summary>
    private bool _ended;

    /// <summary>Whether this reader has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelWorksheetReader" /> class.
    /// </summary>
    /// <param name="worksheet">The descriptor of the worksheet being read.</param>
    /// <param name="substream">The worksheet substream bytes, from its BOF record through its EOF record.</param>
    /// <param name="sharedStrings">The workbook shared string table.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <param name="options">
    /// The codec options carrying the version and code page the workbook globals established.
    /// </param>
    internal ExcelWorksheetReader(ExcelWorksheetInfo worksheet, byte[] substream, string[] sharedStrings, BiffFormatTable formats, BiffReaderOptions options)
    {
        Worksheet = worksheet;
        _data = substream;
        _sharedStrings = sharedStrings;
        _formats = formats;
        _state = new BiffReaderState(options);
    }

    /// <summary>
    /// Gets the descriptor of the worksheet being read.
    /// </summary>
    /// <value>The worksheet's name, index, visibility, type, and declared used range.</value>
    public ExcelWorksheetInfo Worksheet { get; }

    /// <summary>
    /// Attempts to read the next populated cell of the worksheet.
    /// </summary>
    /// <param name="cell">When this method returns, the next populated cell when one is available.</param>
    /// <returns>
    /// <see langword="true" /> when a cell was read; <see langword="false" /> at the end of the worksheet.
    /// </returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when a cell record is malformed.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public bool TryReadCell(out ExcelCell cell)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_pendingIndex < _pending.Length)
        {
            cell = _pending[_pendingIndex++];
            return true;
        }

        try
        {
            return TryReadCellCore(out cell);
        }
        catch (BiffFormatException ex)
        {
            throw new ExcelBinaryFormatException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Enumerates the remaining populated cells of the worksheet, in record order.
    /// </summary>
    /// <returns>A lazy sequence of the worksheet's populated cells.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when a cell record is malformed.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public IEnumerable<ExcelCell> ReadCells()
    {
        while (TryReadCell(out ExcelCell cell))
            yield return cell;
    }

    /// <summary>
    /// Enumerates the remaining populated rows of the worksheet, grouping cells by row as they are read.
    /// </summary>
    /// <returns>A lazy sequence of the worksheet's populated rows, in the order their cells appear.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when a cell record is malformed.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    /// <remarks>
    /// Cells are grouped into a row while their row index does not change; a new row is started when the row index
    /// advances. Excel writes cells in row-major order, so this groups a producer's rows without buffering the whole
    /// worksheet.
    /// </remarks>
    public IEnumerable<ExcelRow> ReadRows()
    {
        List<ExcelCell> current = new();
        int currentRow = -1;

        while (TryReadCell(out ExcelCell cell))
        {
            if (current.Count > 0 && cell.RowIndex != currentRow)
            {
                yield return new ExcelRow(currentRow, current.ToArray());
                current = new List<ExcelCell>();
            }

            currentRow = cell.RowIndex;
            current.Add(cell);
        }

        if (current.Count > 0)
            yield return new ExcelRow(currentRow, current.ToArray());
    }

    /// <inheritdoc />
    public void Dispose() =>
        _disposed = true;

    /// <summary>
    /// Walks records from the current position until a value-bearing cell is decoded or the substream ends, letting
    /// codec exceptions propagate.
    /// </summary>
    /// <param name="cell">When this method returns, the decoded cell when one was found.</param>
    /// <returns><see langword="true" /> when a cell was decoded.</returns>
    private bool TryReadCellCore(out ExcelCell cell)
    {
        while (!_ended)
        {
            var reader = new BiffReader(_data.AsSpan(_position), isFinalBlock: true, _state);
            if (!reader.Read())
                break;

            // Commit the position before decoding so a decode failure leaves the reader past the bad record.
            _position += reader.BytesConsumed;
            _state = reader.CurrentState;

            switch (reader.RecordType)
            {
                case BiffRecordType.Eof:
                    _ended = true;
                    break;

                case BiffRecordType.LabelSst:
                    cell = ExcelCellMapper.FromLabelSst(reader.GetLabelSst(), _sharedStrings, _formats);
                    return true;

                case BiffRecordType.Label:
                    cell = ExcelCellMapper.FromLabel(reader.GetLabel(), _formats);
                    return true;

                case BiffRecordType.RString:
                    cell = ExcelCellMapper.FromRString(reader.GetRString(), _formats);
                    return true;

                case BiffRecordType.Number:
                    cell = ExcelCellMapper.FromNumber(reader.GetNumber(), _formats);
                    return true;

                case BiffRecordType.Rk:
                    cell = ExcelCellMapper.FromRk(reader.GetRk(), _formats);
                    return true;

                case BiffRecordType.BoolErr:
                    cell = ExcelCellMapper.FromBoolErr(reader.GetBoolErr(), _formats);
                    return true;

                case BiffRecordType.MulRk:
                    _pending = ExcelCellMapper.FromMulRk(reader.GetMulRk(), _formats);
                    _pendingIndex = 0;
                    if (_pendingIndex < _pending.Length)
                    {
                        cell = _pending[_pendingIndex++];
                        return true;
                    }

                    break;

                case BiffRecordType.Formula:
                    cell = ReadFormula(reader.GetFormula());
                    return true;

                default:
                    // BLANK, MULBLANK, ROW, formatting, and any unrecognized records carry no value to surface.
                    break;
            }
        }

        cell = default;
        return false;
    }

    /// <summary>
    /// Maps a formula cell, consuming a following <c>STRING</c> record when the cached result is text.
    /// </summary>
    /// <param name="formula">The decoded formula record.</param>
    /// <returns>The cell carrying the cached result.</returns>
    /// <exception cref="BiffFormatException">Thrown when the string record is malformed.</exception>
    private ExcelCell ReadFormula(in BiffFormulaRecord formula)
    {
        ExcelCell cell = ExcelCellMapper.FromFormula(formula, _formats, out bool expectsString);
        if (!expectsString)
            return cell;

        // Peek at the next record; consume it only when it is the expected STRING result.
        var probe = new BiffReader(_data.AsSpan(_position), isFinalBlock: true, _state);
        if (probe.Read() && probe.RecordType == BiffRecordType.String)
        {
            string text = probe.GetString().Text.GetString();
            _position += probe.BytesConsumed;
            _state = probe.CurrentState;
            return ExcelCell.Text(cell.RowIndex, cell.ColumnIndex, text, cell.FormatIndex);
        }

        return cell;
    }
}
