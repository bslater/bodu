// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using Bodu.Formats.Excel.Biff8;

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
/// </remarks>
public sealed class ExcelWorksheetReader
    : IDisposable
{
    /// <summary>The worksheet substream bytes, from its BOF record through its EOF record.</summary>
    private readonly byte[] _data;

    /// <summary>The workbook shared string table.</summary>
    private readonly string[] _sharedStrings;

    /// <summary>The workbook format table used to resolve each cell's number format.</summary>
    private readonly Biff8FormatTable _formats;

    /// <summary>Cells pending emission from an expanded <c>MULRK</c> record.</summary>
    private ExcelCell[] _pending = [];

    /// <summary>The index of the next pending cell to emit.</summary>
    private int _pendingIndex;

    /// <summary>The current read position within the substream.</summary>
    private int _position;

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
    internal ExcelWorksheetReader(ExcelWorksheetInfo worksheet, byte[] substream, string[] sharedStrings, Biff8FormatTable formats)
    {
        Worksheet = worksheet;
        _data = substream;
        _sharedStrings = sharedStrings;
        _formats = formats;
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

        while (!_ended && TryStep(out Biff8RecordType type, out int payloadStart, out int payloadLength))
        {
            ReadOnlySpan<byte> payload = _data.AsSpan(payloadStart, payloadLength);
            switch (type)
            {
                case Biff8RecordType.Eof:
                    _ended = true;
                    break;

                case Biff8RecordType.LabelSst:
                    cell = Biff8CellDecoder.ReadLabelSst(payload, _sharedStrings, _formats);
                    return true;

                case Biff8RecordType.Label:
                    cell = Biff8CellDecoder.ReadLabel(payload, _formats);
                    return true;

                case Biff8RecordType.Number:
                    cell = Biff8CellDecoder.ReadNumber(payload, _formats);
                    return true;

                case Biff8RecordType.Rk:
                    cell = Biff8CellDecoder.ReadRk(payload, _formats);
                    return true;

                case Biff8RecordType.BoolErr:
                    cell = Biff8CellDecoder.ReadBoolErr(payload, _formats);
                    return true;

                case Biff8RecordType.MulRk:
                    _pending = Biff8CellDecoder.ReadMulRk(payload, _formats);
                    _pendingIndex = 0;
                    if (_pendingIndex < _pending.Length)
                    {
                        cell = _pending[_pendingIndex++];
                        return true;
                    }

                    break;

                case Biff8RecordType.Formula:
                    cell = ReadFormula(payload);
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
    /// advances. BIFF8 writes cells in row-major order, so this groups a producer's rows without buffering the whole
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
    /// Decodes a formula cell, consuming a following <c>STRING</c> record when the cached result is text.
    /// </summary>
    /// <param name="payload">The formula record payload.</param>
    /// <returns>The decoded cell carrying the cached result.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the formula or string record is malformed.</exception>
    private ExcelCell ReadFormula(ReadOnlySpan<byte> payload)
    {
        ExcelCell cell = Biff8CellDecoder.ReadFormula(payload, _formats, out bool expectsString);
        if (!expectsString)
            return cell;

        int savedPosition = _position;
        if (TryStep(out Biff8RecordType type, out int stringStart, out int stringLength) && type == Biff8RecordType.String)
        {
            string text = Biff8CellDecoder.ReadCachedString(_data.AsSpan(stringStart, stringLength));
            return ExcelCell.Text(cell.RowIndex, cell.ColumnIndex, text, cell.FormatIndex);
        }

        // The following record was not the expected STRING result; leave it for the next read.
        _position = savedPosition;
        return cell;
    }

    /// <summary>
    /// Advances past the next record in the substream, reporting its type and payload location.
    /// </summary>
    /// <param name="type">When this method returns, the record's type.</param>
    /// <param name="payloadStart">When this method returns, the byte offset of the record's payload.</param>
    /// <param name="payloadLength">When this method returns, the length of the record's payload.</param>
    /// <returns>
    /// <see langword="true" /> when a record was read; <see langword="false" /> at the end of the substream.
    /// </returns>
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when a record header or payload runs past the substream.
    /// </exception>
    private bool TryStep(out Biff8RecordType type, out int payloadStart, out int payloadLength)
    {
        int remaining = _data.Length - _position;
        if (remaining == 0)
        {
            type = default;
            payloadStart = 0;
            payloadLength = 0;
            return false;
        }

        if (remaining < 4)
        {
            throw new ExcelBinaryFormatException(
                string.Format(CultureInfo.CurrentCulture, ExcelBinaryResourceStrings.Format_Invalid_Biff8TrailingBytes, remaining));
        }

        ushort id = BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(_position));
        int length = BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(_position + 2));
        payloadStart = _position + 4;
        if (payloadStart + length > _data.Length)
            throw new ExcelBinaryFormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8Structure);

        type = (Biff8RecordType)id;
        payloadLength = length;
        _position = payloadStart + length;
        return true;
    }
}
