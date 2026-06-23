// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8WorkbookReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Reads an Excel 97-2003 binary workbook (BIFF8 / <c>.xls</c>), exposing its worksheets and the raw cell values of
/// each.
/// </summary>
/// <remarks>
/// <para>
/// The reader opens the workbook stream from its compound-file container, decodes the shared string table, and surfaces
/// each worksheet's populated cells through <see cref="ReadSheetCells(int)" /> and
/// <see cref="ReadSheetCells(string)" />. It targets BIFF8 only and interprets nothing beyond raw cell values — a
/// formula cell surfaces the cached result of its last calculation, but there is no formula evaluation, styling, or
/// date detection.
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

    /// <summary>The workbook format table used to resolve each cell's number format.</summary>
    private readonly Biff8FormatTable _formats;

    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8WorkbookReader" /> class from the workbook stream bytes.
    /// </summary>
    /// <param name="workbook">The raw bytes of the compound file's workbook stream.</param>
    /// <param name="properties">The workbook document properties read from the compound file's property sets.</param>
    /// <exception cref="Biff8FormatException">Thrown when the workbook stream is not valid BIFF.</exception>
    /// <exception cref="Biff8UnsupportedRecordException">Thrown when the workbook is not BIFF8.</exception>
    /// <exception cref="Biff8EncryptedWorkbookException">Thrown when the workbook is encrypted.</exception>
    private Biff8WorkbookReader(ReadOnlyMemory<byte> workbook, Biff8WorkbookProperties properties)
    {
        _records = new Biff8RecordReader(workbook).ReadAll();
        List<(int Start, int EndExclusive)> substreams = SplitSubstreams(_records);
        ValidateVersion(_records);
        ValidateNotEncrypted(_records, substreams);

        _sharedStrings = ReadSharedStrings(_records, substreams);
        (_sheets, _sheetRanges) = ReadSheets(_records, substreams);
        _formats = substreams.Count == 0
            ? Biff8FormatTable.Empty
            : Biff8FormatTable.Build(_records, substreams[0].Start, substreams[0].EndExclusive);
        DateSystem = ReadDateSystem(_records, substreams);
        Properties = properties;
    }

    /// <summary>
    /// Gets the worksheets contained in the workbook, in workbook order.
    /// </summary>
    /// <returns>A read-only list of <see cref="Biff8SheetInfo" /> describing each worksheet.</returns>
    public IReadOnlyList<Biff8SheetInfo> Sheets => _sheets;

    /// <summary>
    /// Gets the document properties of the workbook.
    /// </summary>
    /// <returns>
    /// The workbook properties; members are <see langword="null" /> when the corresponding property set is absent.
    /// </returns>
    public Biff8WorkbookProperties Properties { get; }

    /// <summary>
    /// Gets the date system the workbook uses to interpret serial date numbers.
    /// </summary>
    /// <returns>
    /// The declared date system; <see cref="ExcelDateSystem.Excel1900" /> when the workbook declares none.
    /// </returns>
    public ExcelDateSystem DateSystem { get; }

    /// <summary>
    /// Gets the format code for a number-format index, including the well-known built-in formats.
    /// </summary>
    /// <param name="formatIndex">The number-format index, as carried by <see cref="ExcelCell.FormatIndex" />.</param>
    /// <returns>The format code, or <see langword="null" /> when none is known for the index.</returns>
    public string? GetNumberFormatCode(ushort formatIndex) =>
        _formats.GetFormatCode(formatIndex);

    /// <summary>
    /// Converts a numeric cell to a <see cref="DateTime" /> using the workbook's date system.
    /// </summary>
    /// <param name="cell">The cell to convert.</param>
    /// <returns>
    /// The date and time represented by the cell's value, or <see langword="null" /> when the cell is not numeric.
    /// </returns>
    /// <remarks>
    /// The conversion is applied to any numeric cell; inspect <see cref="ExcelCell.IsDateFormatted" /> first when only
    /// date-formatted cells should be treated as dates.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when the cell's value is outside the range of representable OLE Automation dates.
    /// </exception>
    public DateTime? GetDateTime(ExcelCell cell) =>
        cell.Kind == ExcelCellKind.Number && cell.NumberValue.HasValue
            ? ExcelSerialDate.ToDateTime(cell.NumberValue.Value, DateSystem)
            : null;

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

        using var compound = CompoundFile.Open(stream);
        if (!compound.RootStorage.TryOpenStream("Workbook", out CompoundStream? workbook) &&
            !compound.RootStorage.TryOpenStream("Book", out workbook))
        {
            throw new Biff8WorkbookStreamNotFoundException(ExcelBinaryResourceStrings.IO_KeyNotFound_Biff8Workbook);
        }

        Biff8WorkbookProperties properties = ReadProperties(compound);
        using (workbook)
            return new Biff8WorkbookReader(workbook!.ReadAllBytes(), properties);
    }

    /// <summary>
    /// Reads the workbook's document-property sets from the compound file, tolerating their absence or corruption.
    /// </summary>
    /// <param name="compound">The open compound file.</param>
    /// <returns>
    /// The workbook properties; absent or unparsable property sets yield <see langword="null" /> members.
    /// </returns>
    private static Biff8WorkbookProperties ReadProperties(CompoundFile compound)
    {
        SummaryInformation? summary = TryReadSummary(compound, SummaryInformation.StreamName, SummaryInformation.Read);
        DocumentSummaryInformation? document =
            TryReadSummary(compound, DocumentSummaryInformation.StreamName, DocumentSummaryInformation.Read);

        return new Biff8WorkbookProperties(summary, document);
    }

    /// <summary>
    /// Reads and parses a property-set stream, returning <see langword="null" /> when the stream is absent or
    /// malformed.
    /// </summary>
    /// <typeparam name="T">The property-set view type.</typeparam>
    /// <param name="compound">The open compound file.</param>
    /// <param name="streamName">The name of the property-set stream.</param>
    /// <param name="parse">The parser that materializes the view from the stream.</param>
    /// <returns>The parsed property set, or <see langword="null" /> when it is absent or cannot be parsed.</returns>
    private static T? TryReadSummary<T>(CompoundFile compound, string streamName, Func<Stream, T> parse)
        where T : class
    {
        if (!compound.RootStorage.TryOpenStream(streamName, out CompoundStream? stream))
            return null;

        using (stream)
        {
            try
            {
                // Property metadata is auxiliary; a malformed set must never prevent the workbook from loading.
                return parse(stream!);
            }
            catch (CompoundFileFormatException)
            {
                return null;
            }
        }
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
        return Biff8WorksheetReader.ReadCells(_records, start, endExclusive, _sharedStrings, _formats);
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
    /// Reads the workbook's date system from the date-mode record in the globals substream.
    /// </summary>
    /// <param name="records">The ordered record list.</param>
    /// <param name="substreams">The substream ranges; the first is the workbook globals.</param>
    /// <returns>The declared date system, or <see cref="ExcelDateSystem.Excel1900" /> when none is declared.</returns>
    private static ExcelDateSystem ReadDateSystem(List<Biff8Record> records, List<(int Start, int EndExclusive)> substreams)
    {
        if (substreams.Count == 0)
            return ExcelDateSystem.Excel1900;

        (int start, int endExclusive) = substreams[0];
        for (int i = start; i < endExclusive; i++)
        {
            if (records[i].Type != Biff8RecordType.DateMode || records[i].Payload.Length < 2)
                continue;

            bool is1904 = BinaryPrimitives.ReadUInt16LittleEndian(records[i].Payload.Span) != 0;
            return is1904 ? ExcelDateSystem.Excel1904 : ExcelDateSystem.Excel1900;
        }

        return ExcelDateSystem.Excel1900;
    }

    /// <summary>
    /// Validates that the workbook is not encrypted by checking the globals substream for a file-pass record.
    /// </summary>
    /// <param name="records">The ordered record list.</param>
    /// <param name="substreams">The substream ranges; the first is the workbook globals.</param>
    /// <exception cref="Biff8EncryptedWorkbookException">
    /// Thrown when the globals substream contains a file-pass record.
    /// </exception>
    internal static void ValidateNotEncrypted(List<Biff8Record> records, List<(int Start, int EndExclusive)> substreams)
    {
        if (substreams.Count == 0)
            return;

        (int start, int endExclusive) = substreams[0];
        for (int i = start; i < endExclusive; i++)
        {
            if (records[i].Type == Biff8RecordType.FilePass)
                throw new Biff8EncryptedWorkbookException(ExcelBinaryResourceStrings.Op_NotSupported_Biff8Encrypted);
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
            (int sheetStart, int sheetEnd) = substreams[i + 1];
            Biff8SheetDimensions dimensions = ReadDimensions(records, sheetStart, sheetEnd);

            sheets.Add(new Biff8SheetInfo(boundSheets[i].Name, i, boundSheets[i].Visible, dimensions));
            ranges.Add(substreams[i + 1]);
        }

        return (sheets, ranges);
    }

    /// <summary>
    /// Reads the used range from the first <c>DIMENSIONS</c> record of a worksheet substream.
    /// </summary>
    /// <param name="records">The ordered record list.</param>
    /// <param name="start">The inclusive record index at which the worksheet substream begins.</param>
    /// <param name="endExclusive">The exclusive record index at which the worksheet substream ends.</param>
    /// <returns>
    /// The declared used range, or the default value when the substream carries no <c>DIMENSIONS</c> record.
    /// </returns>
    private static Biff8SheetDimensions ReadDimensions(List<Biff8Record> records, int start, int endExclusive)
    {
        for (int i = start; i < endExclusive; i++)
        {
            if (records[i].Type != Biff8RecordType.Dimensions)
                continue;

            ReadOnlySpan<byte> payload = records[i].Payload.Span;
            if (payload.Length < 12)
                break;

            // BIFF8 DIMENSIONS stores the last row and column as one-past-the-end (rwMac, colMac).
            int firstRow = (int)BinaryPrimitives.ReadUInt32LittleEndian(payload);
            int rowExtent = (int)BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(4));
            int firstColumn = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(8));
            int columnExtent = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(10));

            return new Biff8SheetDimensions(firstRow, Math.Max(0, rowExtent - firstRow), firstColumn, Math.Max(0, columnExtent - firstColumn));
        }

        return default;
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
