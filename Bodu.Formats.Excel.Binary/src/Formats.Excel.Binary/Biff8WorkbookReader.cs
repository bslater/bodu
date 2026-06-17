// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8WorkbookReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Bodu.IO.Compound;

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Reads an Excel 97-2003 binary workbook (BIFF8 / <c>.xls</c>), exposing its worksheets and the raw cell values of
/// each.
/// </summary>
/// <remarks>
/// <para>
/// The reader opens the workbook stream from its compound-file container, decodes the shared string table, and surfaces
/// each worksheet's populated cells through <see cref="ReadSheetCells(int)" /> and
/// <see cref="ReadSheetCells(string)" />. It targets BIFF8 only and interprets nothing beyond raw cell values — there
/// is no formula evaluation, styling, or date detection.
/// </para>
/// <para>
/// The workbook content is buffered in memory when the reader is opened, so the source stream need not remain open.
/// </para>
/// </remarks>
public sealed class Biff8WorkbookReader
{
    /// <summary>The BIFF version stored in a BIFF8 beginning-of-file record.</summary>
    private const ushort Biff8Version = 0x0600;

    /// <summary>The complete ordered record list of the workbook stream.</summary>
    private readonly List<Biff8Record> _records;

    /// <summary>The decoded shared string table.</summary>
    private readonly string[] _sharedStrings;

    /// <summary>The worksheets, in workbook order.</summary>
    private readonly List<Biff8SheetInfo> _sheets;

    /// <summary>The record-index ranges of each worksheet substream, parallel to <see cref="_sheets" />.</summary>
    private readonly List<(int Start, int EndExclusive)> _sheetRanges;

    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8WorkbookReader" /> class from the workbook stream bytes.
    /// </summary>
    /// <param name="workbook">The raw bytes of the compound file's workbook stream.</param>
    /// <exception cref="Biff8FormatException">Thrown when the workbook stream is not valid BIFF.</exception>
    /// <exception cref="Biff8UnsupportedRecordException">Thrown when the workbook is not BIFF8.</exception>
    private Biff8WorkbookReader(ReadOnlyMemory<byte> workbook)
    {
        _records = new Biff8RecordReader(workbook).ReadAll();
        List<(int Start, int EndExclusive)> substreams = SplitSubstreams(_records);
        ValidateVersion(_records);

        _sharedStrings = ReadSharedStrings(_records, substreams);
        (_sheets, _sheetRanges) = ReadSheets(_records, substreams);
    }

    /// <summary>
    /// Gets the worksheets contained in the workbook, in workbook order.
    /// </summary>
    /// <returns>A read-only list of <see cref="Biff8SheetInfo" /> describing each worksheet.</returns>
    public IReadOnlyList<Biff8SheetInfo> Sheets => _sheets;

    /// <summary>
    /// Opens a BIFF8 workbook over the supplied stream.
    /// </summary>
    /// <param name="stream">
    /// The stream containing the <c>.xls</c> file; read from its current position to the end.
    /// </param>
    /// <returns>An open <see cref="Biff8WorkbookReader" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">Thrown when the stream is not a valid compound file.</exception>
    /// <exception cref="Biff8WorkbookStreamNotFoundException">
    /// Thrown when the compound file has no workbook stream.
    /// </exception>
    /// <exception cref="Biff8FormatException">Thrown when the workbook stream is not valid BIFF.</exception>
    /// <exception cref="Biff8UnsupportedRecordException">Thrown when the workbook is not BIFF8.</exception>
    public static Biff8WorkbookReader Open(Stream stream)
    {
        ThrowHelper.ThrowIfNull(stream);

        using var compound = CompoundBinaryFile.Open(stream);
        if (!compound.TryGetStream("Workbook", out CompoundStream? workbook) &&
            !compound.TryGetStream("Book", out workbook))
        {
            throw new Biff8WorkbookStreamNotFoundException(ExcelBinaryResourceStrings.IO_KeyNotFound_Biff8Workbook);
        }

        return new Biff8WorkbookReader(workbook!.AsMemory());
    }

    /// <summary>
    /// Reads the populated cells of the worksheet at the specified index.
    /// </summary>
    /// <param name="sheetIndex">The zero-based worksheet index.</param>
    /// <returns>The populated cells of the worksheet, in record order.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="sheetIndex" /> is out of range.
    /// </exception>
    /// <exception cref="Biff8FormatException">Thrown when a cell record is malformed.</exception>
    public IEnumerable<ExcelCell> ReadSheetCells(int sheetIndex)
    {
        if ((uint)sheetIndex >= (uint)_sheets.Count)
            throw new ArgumentOutOfRangeException(nameof(sheetIndex));

        (int start, int endExclusive) = _sheetRanges[sheetIndex];
        return Biff8WorksheetReader.ReadCells(_records, start, endExclusive, _sharedStrings);
    }

    /// <summary>
    /// Reads the populated cells of the worksheet with the specified name.
    /// </summary>
    /// <param name="sheetName">The worksheet name, compared using ordinal equality.</param>
    /// <returns>The populated cells of the worksheet, in record order.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sheetName" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown when no worksheet with the given name exists.</exception>
    /// <exception cref="Biff8FormatException">Thrown when a cell record is malformed.</exception>
    public IEnumerable<ExcelCell> ReadSheetCells(string sheetName)
    {
        ThrowHelper.ThrowIfNull(sheetName);

        for (int i = 0; i < _sheets.Count; i++)
        {
            if (string.Equals(_sheets[i].Name, sheetName, StringComparison.Ordinal))
                return ReadSheetCells(i);
        }

        throw new KeyNotFoundException(
            string.Format(CultureInfo.CurrentCulture, ExcelBinaryResourceStrings.IO_KeyNotFound_Biff8Sheet, sheetName));
    }

    /// <summary>
    /// Partitions the record list into substreams delimited by beginning-of-file and end-of-file records.
    /// </summary>
    /// <param name="records">The ordered record list.</param>
    /// <returns>The record-index range of each substream, the first of which is the workbook globals.</returns>
    private static List<(int Start, int EndExclusive)> SplitSubstreams(List<Biff8Record> records)
    {
        List<(int Start, int EndExclusive)> ranges = new();
        int start = -1;

        for (int i = 0; i < records.Count; i++)
        {
            switch (records[i].Type)
            {
                case Biff8RecordType.Bof:
                    start = i;
                    break;

                case Biff8RecordType.Eof when start >= 0:
                    ranges.Add((start, i + 1));
                    start = -1;
                    break;

                default:
                    break;
            }
        }

        return ranges;
    }

    /// <summary>
    /// Validates that the workbook is BIFF8 by inspecting the leading beginning-of-file record.
    /// </summary>
    /// <param name="records">The ordered record list.</param>
    /// <exception cref="Biff8FormatException">
    /// Thrown when the stream does not begin with a beginning-of-file record.
    /// </exception>
    /// <exception cref="Biff8UnsupportedRecordException">
    /// Thrown when the declared BIFF version is not BIFF8.
    /// </exception>
    private static void ValidateVersion(List<Biff8Record> records)
    {
        if (records.Count == 0 || records[0].Type != Biff8RecordType.Bof || records[0].Payload.Length < 2)
            throw new Biff8FormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8Structure);

        ushort version = BinaryPrimitives.ReadUInt16LittleEndian(records[0].Payload.Span);
        if (version != Biff8Version)
        {
            throw new Biff8UnsupportedRecordException(
                string.Format(CultureInfo.CurrentCulture, ExcelBinaryResourceStrings.Op_NotSupported_Biff8Version, version));
        }
    }

    /// <summary>
    /// Decodes the shared string table from the workbook globals substream.
    /// </summary>
    /// <param name="records">The ordered record list.</param>
    /// <param name="substreams">The substream ranges.</param>
    /// <returns>The decoded shared strings, or an empty array when the workbook has no shared string table.</returns>
    private static string[] ReadSharedStrings(List<Biff8Record> records, List<(int Start, int EndExclusive)> substreams)
    {
        if (substreams.Count == 0)
            return [];

        (int start, int endExclusive) = substreams[0];
        int sstIndex = -1;
        for (int i = start; i < endExclusive; i++)
        {
            if (records[i].Type == Biff8RecordType.Sst)
            {
                sstIndex = i;
                break;
            }
        }

        if (sstIndex < 0)
            return [];

        List<ReadOnlyMemory<byte>> blocks = [records[sstIndex].Payload];
        for (int i = sstIndex + 1; i < endExclusive && records[i].Type == Biff8RecordType.Continue; i++)
            blocks.Add(records[i].Payload);

        return Biff8SharedStringTable.Parse(blocks);
    }

    /// <summary>
    /// Reads the bound-sheet records from the workbook globals and pairs each with its worksheet substream, in order.
    /// </summary>
    /// <param name="records">The ordered record list.</param>
    /// <param name="substreams">The substream ranges; the first is the globals, the rest are sheets in order.</param>
    /// <returns>The worksheet descriptors and their parallel substream ranges.</returns>
    private static (List<Biff8SheetInfo> Sheets, List<(int Start, int EndExclusive)> Ranges) ReadSheets(
        List<Biff8Record> records,
        List<(int Start, int EndExclusive)> substreams)
    {
        List<Biff8SheetInfo> sheets = new();
        List<(int Start, int EndExclusive)> ranges = new();
        if (substreams.Count == 0)
            return (sheets, ranges);

        (int globalsStart, int globalsEnd) = substreams[0];
        List<(string Name, bool Visible)> boundSheets = new();

        for (int i = globalsStart; i < globalsEnd; i++)
        {
            if (records[i].Type != Biff8RecordType.BoundSheet)
                continue;

            ReadOnlySpan<byte> payload = records[i].Payload.Span;
            ushort grbit = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4));
            bool visible = (grbit & 0x0003) == 0;
            int charCount = payload[6];
            bool highByte = (payload[7] & 0x01) != 0;

            boundSheets.Add((ReadShortString(payload, 8, charCount, highByte), visible));
        }

        int sheetCount = Math.Min(boundSheets.Count, substreams.Count - 1);
        for (int i = 0; i < sheetCount; i++)
        {
            sheets.Add(new Biff8SheetInfo(boundSheets[i].Name, i, boundSheets[i].Visible));
            ranges.Add(substreams[i + 1]);
        }

        return (sheets, ranges);
    }

    /// <summary>
    /// Decodes a BIFF8 short string (8-bit length prefix already consumed by the caller).
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="offset">The byte offset at which the character data begins.</param>
    /// <param name="charCount">The number of characters.</param>
    /// <param name="highByte">Whether the characters are 16-bit (<see langword="true" />) or compressed 8-bit.</param>
    /// <returns>The decoded string.</returns>
    private static string ReadShortString(ReadOnlySpan<byte> payload, int offset, int charCount, bool highByte)
    {
        if (highByte)
            return Encoding.Unicode.GetString(payload.Slice(offset, charCount * 2));

        char[] characters = new char[charCount];
        for (int i = 0; i < charCount; i++)
            characters[i] = (char)payload[offset + i];

        return new string(characters);
    }
}
