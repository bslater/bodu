// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8WorkbookGlobals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Formats.Excel.Biff8;

/// <summary>
/// Parses the workbook globals substream of a BIFF8 workbook: the beginning-of-file marker, the date system, the bound
/// sheets and their stream offsets, the shared string table, and the extended-format and number-format records.
/// </summary>
/// <remarks>
/// The globals substream is read once when a workbook is opened. It validates that the stream is an unencrypted BIFF8
/// workbook and produces the workbook-wide tables every sheet read depends on.
/// </remarks>
internal sealed class Biff8WorkbookGlobals
{
    /// <summary>The BIFF version stored in a BIFF8 beginning-of-file record.</summary>
    private const ushort Biff8Version = 0x0600;

    /// <summary>The beginning-of-file substream type that marks the workbook globals.</summary>
    private const ushort BofTypeGlobals = 0x0005;

    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8WorkbookGlobals" /> class.
    /// </summary>
    /// <param name="sheets">The bound sheets, in workbook order.</param>
    /// <param name="sharedStrings">The decoded shared string table.</param>
    /// <param name="formats">The workbook format table.</param>
    /// <param name="dateSystem">The workbook's declared date system.</param>
    private Biff8WorkbookGlobals(
        IReadOnlyList<Biff8SheetDirectoryEntry> sheets,
        string[] sharedStrings,
        Biff8FormatTable formats,
        ExcelDateSystem dateSystem)
    {
        Sheets = sheets;
        SharedStrings = sharedStrings;
        Formats = formats;
        DateSystem = dateSystem;
    }

    /// <summary>
    /// Gets the bound sheets declared by the workbook, in workbook order.
    /// </summary>
    /// <returns>The sheet directory entries, each carrying its substream offset, type, and visibility.</returns>
    public IReadOnlyList<Biff8SheetDirectoryEntry> Sheets { get; }

    /// <summary>
    /// Gets the decoded shared string table.
    /// </summary>
    /// <returns>The workbook's deduplicated text values, indexed as cells reference them.</returns>
    public string[] SharedStrings { get; }

    /// <summary>
    /// Gets the workbook format table.
    /// </summary>
    /// <returns>The table that resolves a cell's extended-format index to a number format.</returns>
    public Biff8FormatTable Formats { get; }

    /// <summary>
    /// Gets the workbook's declared date system.
    /// </summary>
    /// <returns>The date system, or <see cref="ExcelDateSystem.Excel1900" /> when none is declared.</returns>
    public ExcelDateSystem DateSystem { get; }

    /// <summary>
    /// Parses the workbook globals substream.
    /// </summary>
    /// <param name="globals">The bytes of the globals substream, from its BOF record through its EOF record.</param>
    /// <param name="options">The reader options governing date-format detection.</param>
    /// <returns>The parsed workbook globals.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the globals substream is malformed.</exception>
    /// <exception cref="ExcelBinaryUnsupportedException">Thrown when the workbook is not BIFF8.</exception>
    /// <exception cref="ExcelBinaryEncryptedWorkbookException">Thrown when the workbook is encrypted.</exception>
    public static Biff8WorkbookGlobals Parse(byte[] globals, ExcelBinaryReaderOptions options)
    {
        List<(Biff8RecordType Type, int PayloadStart, int PayloadLength)> records = IndexRecords(globals);
        if (records.Count == 0 || records[0].Type != Biff8RecordType.Bof)
            throw new ExcelBinaryFormatException(ExcelBinaryResourceStrings.Format_Invalid_Biff8Structure);

        ValidateGlobalsBof(Payload(globals, records[0]));

        List<Biff8SheetDirectoryEntry> sheets = new();
        List<ushort> xfFormatIndex = new();
        Dictionary<ushort, string> formatCodes = new();
        ExcelDateSystem dateSystem = ExcelDateSystem.Excel1900;
        string[] sharedStrings = [];

        for (int i = 1; i < records.Count; i++)
        {
            ReadOnlySpan<byte> payload = Payload(globals, records[i]);
            switch (records[i].Type)
            {
                case Biff8RecordType.FilePass:
                    throw new ExcelBinaryEncryptedWorkbookException(ExcelBinaryResourceStrings.Op_NotSupported_Biff8Encrypted);

                case Biff8RecordType.BoundSheet:
                    sheets.Add(ReadBoundSheet(payload));
                    break;

                case Biff8RecordType.DateMode:
                    dateSystem = Biff8Payload.ReadUInt16(payload, 0, Biff8RecordType.DateMode) != 0
                        ? ExcelDateSystem.Excel1904
                        : ExcelDateSystem.Excel1900;
                    break;

                case Biff8RecordType.Xf when payload.Length >= 4:
                    xfFormatIndex.Add(Biff8Payload.ReadUInt16(payload, 2, Biff8RecordType.Xf));
                    break;

                case Biff8RecordType.Format:
                    ReadFormatRecord(payload, formatCodes);
                    break;

                case Biff8RecordType.Sst:
                    sharedStrings = ReadSharedStrings(globals, records, ref i);
                    break;

                default:
                    break;
            }
        }

        var formats = new Biff8FormatTable([.. xfFormatIndex], formatCodes, options.DetectDateFormats);
        return new Biff8WorkbookGlobals(sheets, sharedStrings, formats, dateSystem);
    }

    /// <summary>
    /// Walks the globals substream once, recording each record's type and the offset and length of its payload.
    /// </summary>
    /// <param name="globals">The globals substream bytes.</param>
    /// <returns>The ordered record index.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when a record is malformed.</exception>
    private static List<(Biff8RecordType Type, int PayloadStart, int PayloadLength)> IndexRecords(byte[] globals)
    {
        List<(Biff8RecordType, int, int)> records = new();
        var cursor = new Biff8RecordCursor(globals);
        int headerPos = cursor.Position;
        while (cursor.TryRead(out Biff8Record record))
        {
            records.Add((record.Type, headerPos + 4, record.Payload.Length));
            headerPos = cursor.Position;
        }

        return records;
    }

    /// <summary>
    /// Returns the payload span of an indexed record.
    /// </summary>
    /// <param name="globals">The globals substream bytes.</param>
    /// <param name="record">The indexed record.</param>
    /// <returns>The record's payload span.</returns>
    private static ReadOnlySpan<byte> Payload(byte[] globals, (Biff8RecordType Type, int PayloadStart, int PayloadLength) record) =>
        globals.AsSpan(record.PayloadStart, record.PayloadLength);

    /// <summary>
    /// Validates that the leading beginning-of-file record declares an unencrypted BIFF8 workbook globals substream.
    /// </summary>
    /// <param name="payload">The BOF record payload.</param>
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when the BOF record is malformed or not a globals substream.
    /// </exception>
    /// <exception cref="ExcelBinaryUnsupportedException">
    /// Thrown when the declared BIFF version is not BIFF8.
    /// </exception>
    private static void ValidateGlobalsBof(ReadOnlySpan<byte> payload)
    {
        ushort version = Biff8Payload.ReadUInt16(payload, 0, Biff8RecordType.Bof);
        if (version != Biff8Version)
        {
            throw new ExcelBinaryUnsupportedException(
                string.Format(CultureInfo.CurrentCulture, ExcelBinaryResourceStrings.Op_NotSupported_Biff8Version, version));
        }

        ushort type = Biff8Payload.ReadUInt16(payload, 2, Biff8RecordType.Bof);
        if (type != BofTypeGlobals)
        {
            throw new ExcelBinaryFormatException(
                string.Format(CultureInfo.CurrentCulture, ExcelBinaryResourceStrings.Format_Invalid_Biff8SubstreamType, type));
        }
    }

    /// <summary>
    /// Decodes a <c>BOUNDSHEET8</c> record into a sheet directory entry.
    /// </summary>
    /// <param name="payload">The bound-sheet record payload.</param>
    /// <returns>The decoded directory entry.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the record is too short.</exception>
    private static Biff8SheetDirectoryEntry ReadBoundSheet(ReadOnlySpan<byte> payload)
    {
        Biff8Payload.RequireLength(payload, 8, Biff8RecordType.BoundSheet);
        long streamOffset = Biff8Payload.ReadUInt32(payload, 0, Biff8RecordType.BoundSheet);
        byte hsState = (byte)(payload[4] & 0x03);
        byte sheetType = payload[5];
        int charCount = payload[6];
        bool highByte = (payload[7] & 0x01) != 0;

        string name = Biff8StringReader.Decode(payload, 8, charCount, highByte, Biff8RecordType.BoundSheet);
        return new Biff8SheetDirectoryEntry(streamOffset, MapSheetType(sheetType), MapVisibility(hsState), name);
    }

    /// <summary>
    /// Maps a bound-sheet hidden-state value to an <see cref="ExcelSheetVisibility" />.
    /// </summary>
    /// <param name="hsState">The two-bit hidden-state value.</param>
    /// <returns>The corresponding visibility.</returns>
    private static ExcelSheetVisibility MapVisibility(byte hsState) =>
        hsState switch
        {
            0x00 => ExcelSheetVisibility.Visible,
            0x01 => ExcelSheetVisibility.Hidden,
            _ => ExcelSheetVisibility.VeryHidden,
        };

    /// <summary>
    /// Maps a bound-sheet sheet-type value to an <see cref="ExcelSheetType" />.
    /// </summary>
    /// <param name="sheetType">The sheet-type byte.</param>
    /// <returns>The corresponding sheet type.</returns>
    private static ExcelSheetType MapSheetType(byte sheetType) =>
        sheetType switch
        {
            0x00 => ExcelSheetType.Worksheet,
            0x01 => ExcelSheetType.MacroSheet,
            0x02 => ExcelSheetType.Chart,
            0x06 => ExcelSheetType.VbaModule,
            _ => ExcelSheetType.Unknown,
        };

    /// <summary>
    /// Collects the SST payload and the payloads of its trailing continuation records, then decodes the table.
    /// </summary>
    /// <param name="globals">The globals substream bytes.</param>
    /// <param name="records">The indexed records of the globals substream.</param>
    /// <param name="index">
    /// The index of the SST record; advanced past the consumed continuation records on return.
    /// </param>
    /// <returns>The decoded shared strings.</returns>
    private static string[] ReadSharedStrings(
        byte[] globals,
        List<(Biff8RecordType Type, int PayloadStart, int PayloadLength)> records,
        ref int index)
    {
        List<ReadOnlyMemory<byte>> blocks = [globals.AsMemory(records[index].PayloadStart, records[index].PayloadLength)];

        while (index + 1 < records.Count && records[index + 1].Type == Biff8RecordType.Continue)
        {
            index++;
            blocks.Add(globals.AsMemory(records[index].PayloadStart, records[index].PayloadLength));
        }

        return Biff8SharedStringTable.Parse(blocks);
    }

    /// <summary>
    /// Decodes a FORMAT record and stores its format code under the record's number-format index.
    /// </summary>
    /// <param name="payload">The FORMAT record payload.</param>
    /// <param name="formatCodes">The map to populate.</param>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the record is too short.</exception>
    private static void ReadFormatRecord(ReadOnlySpan<byte> payload, Dictionary<ushort, string> formatCodes)
    {
        Biff8Payload.RequireLength(payload, 5, Biff8RecordType.Format);
        ushort formatIndex = Biff8Payload.ReadUInt16(payload, 0, Biff8RecordType.Format);
        int charCount = Biff8Payload.ReadUInt16(payload, 2, Biff8RecordType.Format);
        bool highByte = (payload[4] & 0x01) != 0;

        formatCodes[formatIndex] = Biff8StringReader.Decode(payload, 5, charCount, highByte, Biff8RecordType.Format);
    }
}
