// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8TestWorkbook.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.Formats.Excel.Biff8;
using Bodu.IO.Compound;

namespace Bodu.Formats.Excel;

/// <summary>
/// Builds synthetic BIFF8 record streams, worksheet substreams, and complete <c>.xls</c> compound files for tests,
/// covering producer variations the real-world sample fixture does not exercise.
/// </summary>
internal static class Biff8TestWorkbook
{
    /// <summary>The beginning-of-file substream type for the workbook globals.</summary>
    public const ushort BofGlobals = 0x0005;

    /// <summary>The beginning-of-file substream type for a worksheet.</summary>
    public const ushort BofWorksheet = 0x0010;

    /// <summary>
    /// Describes a sheet to embed in a synthetic workbook: its name, type, visibility, and substream body records.
    /// </summary>
    /// <param name="Name">The sheet name.</param>
    /// <param name="Type">The bound-sheet sheet-type byte (0 worksheet, 1 macro, 2 chart, 6 VBA).</param>
    /// <param name="Visibility">The bound-sheet hidden-state byte (0 visible, 1 hidden, 2 very hidden).</param>
    /// <param name="Body">The records between the substream's BOF and EOF markers.</param>
    public sealed record SheetSpec(string Name, byte Type, byte Visibility, IReadOnlyList<byte[]> Body);

    /// <summary>
    /// Frames a record as a header (type and length) followed by its payload.
    /// </summary>
    /// <param name="id">The record type identifier.</param>
    /// <param name="payload">The record payload.</param>
    /// <returns>The framed record bytes.</returns>
    public static byte[] Record(ushort id, ReadOnlySpan<byte> payload)
    {
        byte[] record = new byte[4 + payload.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(record, id);
        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(2), (ushort)payload.Length);
        payload.CopyTo(record.AsSpan(4));

        return record;
    }

    /// <summary>Builds a beginning-of-file record of the given substream type.</summary>
    /// <param name="substreamType">The substream type field.</param>
    /// <returns>The framed BOF record.</returns>
    public static byte[] Bof(ushort substreamType)
    {
        byte[] payload = new byte[16];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, 0x0600);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2), substreamType);

        return Record(0x0809, payload);
    }

    /// <summary>Builds an end-of-file record.</summary>
    /// <returns>The framed EOF record.</returns>
    public static byte[] Eof() =>
        Record(0x000A, default);

    /// <summary>Builds a date-mode record.</summary>
    /// <param name="is1904">Whether the workbook uses the 1904 date system.</param>
    /// <returns>The framed DATEMODE record.</returns>
    public static byte[] DateMode(bool is1904)
    {
        byte[] payload = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)(is1904 ? 1 : 0));

        return Record(0x0022, payload);
    }

    /// <summary>Builds a file-pass record marking an encrypted workbook.</summary>
    /// <returns>The framed FILEPASS record.</returns>
    public static byte[] FilePass() =>
        Record(0x002F, new byte[6]);

    /// <summary>Builds a NUMBER cell record.</summary>
    /// <param name="row">The row index.</param>
    /// <param name="column">The column index.</param>
    /// <param name="value">The numeric value.</param>
    /// <param name="xfIndex">The extended-format index.</param>
    /// <returns>The framed NUMBER record.</returns>
    public static byte[] Number(int row, int column, double value, ushort xfIndex = 0)
    {
        byte[] payload = new byte[14];
        WriteCellHeader(payload, row, column, xfIndex);
        BinaryPrimitives.WriteDoubleLittleEndian(payload.AsSpan(6), value);

        return Record(0x0203, payload);
    }

    /// <summary>Builds an RK cell record from the raw 32-bit RK encoding.</summary>
    /// <param name="row">The row index.</param>
    /// <param name="column">The column index.</param>
    /// <param name="rk">The 32-bit RK value.</param>
    /// <param name="xfIndex">The extended-format index.</param>
    /// <returns>The framed RK record.</returns>
    public static byte[] Rk(int row, int column, uint rk, ushort xfIndex = 0)
    {
        byte[] payload = new byte[10];
        WriteCellHeader(payload, row, column, xfIndex);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(6), rk);

        return Record(0x027E, payload);
    }

    /// <summary>Builds a LABELSST cell record referencing a shared string.</summary>
    /// <param name="row">The row index.</param>
    /// <param name="column">The column index.</param>
    /// <param name="stringIndex">The shared-string index.</param>
    /// <param name="xfIndex">The extended-format index.</param>
    /// <returns>The framed LABELSST record.</returns>
    public static byte[] LabelSst(int row, int column, uint stringIndex, ushort xfIndex = 0)
    {
        byte[] payload = new byte[10];
        WriteCellHeader(payload, row, column, xfIndex);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(6), stringIndex);

        return Record(0x00FD, payload);
    }

    /// <summary>Builds a LABEL cell record carrying a compressed inline string.</summary>
    /// <param name="row">The row index.</param>
    /// <param name="column">The column index.</param>
    /// <param name="value">The inline text value.</param>
    /// <param name="xfIndex">The extended-format index.</param>
    /// <returns>The framed LABEL record.</returns>
    public static byte[] Label(int row, int column, string value, ushort xfIndex = 0)
    {
        byte[] payload = new byte[9 + value.Length];
        WriteCellHeader(payload, row, column, xfIndex);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(6), (ushort)value.Length);
        payload[8] = 0x00;
        for (int i = 0; i < value.Length; i++)
            payload[9 + i] = (byte)value[i];

        return Record(0x0204, payload);
    }

    /// <summary>Builds a BOOLERR cell record.</summary>
    /// <param name="row">The row index.</param>
    /// <param name="column">The column index.</param>
    /// <param name="value">The boolean or error byte.</param>
    /// <param name="isError">Whether the value is an error code.</param>
    /// <param name="xfIndex">The extended-format index.</param>
    /// <returns>The framed BOOLERR record.</returns>
    public static byte[] BoolErr(int row, int column, byte value, bool isError, ushort xfIndex = 0)
    {
        byte[] payload = new byte[8];
        WriteCellHeader(payload, row, column, xfIndex);
        payload[6] = value;
        payload[7] = (byte)(isError ? 1 : 0);

        return Record(0x0205, payload);
    }

    /// <summary>Builds a MULRK cell-run record.</summary>
    /// <param name="row">The row index.</param>
    /// <param name="firstColumn">The first column index.</param>
    /// <param name="values">The per-cell extended-format index and RK value pairs.</param>
    /// <returns>The framed MULRK record.</returns>
    public static byte[] MulRk(int row, int firstColumn, params (ushort XfIndex, uint Rk)[] values)
    {
        byte[] payload = new byte[6 + (values.Length * 6)];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)row);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2), (ushort)firstColumn);
        for (int i = 0; i < values.Length; i++)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4 + (i * 6)), values[i].XfIndex);
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4 + (i * 6) + 2), values[i].Rk);
        }

        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(payload.Length - 2), (ushort)(firstColumn + values.Length - 1));

        return Record(0x00BD, payload);
    }

    /// <summary>Builds a FORMULA cell record carrying the supplied eight-byte cached result.</summary>
    /// <param name="row">The row index.</param>
    /// <param name="column">The column index.</param>
    /// <param name="result">The eight-byte cached result.</param>
    /// <param name="xfIndex">The extended-format index.</param>
    /// <returns>The framed FORMULA record.</returns>
    public static byte[] Formula(int row, int column, byte[] result, ushort xfIndex = 0)
    {
        // row(2) + col(2) + ixfe(2) + result(8) + grbit(2) + chn(4) + cce(2).
        byte[] payload = new byte[22];
        WriteCellHeader(payload, row, column, xfIndex);
        result.CopyTo(payload.AsSpan(6));

        return Record(0x0006, payload);
    }

    /// <summary>Builds a STRING record carrying a compressed (8-bit) cached formula string.</summary>
    /// <param name="value">The cached string.</param>
    /// <returns>The framed STRING record.</returns>
    public static byte[] CompressedString(string value)
    {
        byte[] payload = new byte[3 + value.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)value.Length);
        payload[2] = 0x00;
        for (int i = 0; i < value.Length; i++)
            payload[3 + i] = (byte)value[i];

        return Record(0x0207, payload);
    }

    /// <summary>Builds a dimensions record.</summary>
    /// <param name="firstRow">The first used row.</param>
    /// <param name="rowMac">The one-past-the-end used row.</param>
    /// <param name="firstColumn">The first used column.</param>
    /// <param name="columnMac">The one-past-the-end used column.</param>
    /// <returns>The framed DIMENSIONS record.</returns>
    public static byte[] Dimensions(int firstRow, int rowMac, int firstColumn, int columnMac)
    {
        byte[] payload = new byte[14];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, (uint)firstRow);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4), (uint)rowMac);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(8), (ushort)firstColumn);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(10), (ushort)columnMac);

        return Record(0x0200, payload);
    }

    /// <summary>Builds a shared string table record from compressed (8-bit) strings.</summary>
    /// <param name="strings">The unique strings.</param>
    /// <returns>The framed SST record.</returns>
    public static byte[] Sst(params string[] strings)
    {
        using MemoryStream payload = new();
        Span<byte> counts = stackalloc byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(counts, (uint)strings.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(counts.Slice(4), (uint)strings.Length);
        payload.Write(counts);

        foreach (string value in strings)
        {
            Span<byte> header = stackalloc byte[3];
            BinaryPrimitives.WriteUInt16LittleEndian(header, (ushort)value.Length);
            header[2] = 0x00;
            payload.Write(header);
            foreach (char c in value)
                payload.WriteByte((byte)c);
        }

        return Record(0x00FC, payload.ToArray());
    }

    /// <summary>
    /// Assembles a worksheet substream from its body records, framed by BOF and EOF markers.
    /// </summary>
    /// <param name="body">The records between the BOF and EOF markers.</param>
    /// <returns>The worksheet substream bytes.</returns>
    public static byte[] WorksheetSubstream(params byte[][] body)
    {
        using MemoryStream stream = new();
        stream.Write(Bof(BofWorksheet));
        foreach (byte[] record in body)
            stream.Write(record);
        stream.Write(Eof());

        return stream.ToArray();
    }

    /// <summary>
    /// Opens a forward-only reader over a synthetic worksheet substream built from the given body records.
    /// </summary>
    /// <param name="body">The records between the substream's BOF and EOF markers.</param>
    /// <returns>A reader over the substream, using an empty shared string table and format table.</returns>
    public static ExcelWorksheetReader OpenWorksheetReader(params byte[][] body)
    {
        ExcelWorksheetInfo info = new("Sheet", 0, ExcelSheetVisibility.Visible, ExcelSheetType.Worksheet, default);
        return new ExcelWorksheetReader(info, WorksheetSubstream(body), [], Biff8FormatTable.Empty);
    }

    /// <summary>
    /// Opens a forward-only reader over a raw substream buffer, bypassing the BOF and EOF framing of
    /// <see cref="OpenWorksheetReader(byte[][])" />.
    /// </summary>
    /// <param name="substream">The raw substream bytes.</param>
    /// <returns>A reader over the substream, using an empty shared string table and format table.</returns>
    public static ExcelWorksheetReader OpenWorksheetReaderRaw(byte[] substream)
    {
        ExcelWorksheetInfo info = new("Sheet", 0, ExcelSheetVisibility.Visible, ExcelSheetType.Worksheet, default);
        return new ExcelWorksheetReader(info, substream, [], Biff8FormatTable.Empty);
    }

    /// <summary>
    /// Concatenates a sequence of framed records into a single buffer.
    /// </summary>
    /// <param name="records">The framed records.</param>
    /// <returns>The concatenated bytes.</returns>
    public static byte[] Concat(params byte[][] records)
    {
        using MemoryStream stream = new();
        foreach (byte[] record in records)
            stream.Write(record);

        return stream.ToArray();
    }

    /// <summary>
    /// Builds a complete <c>.xls</c> compound file with the given globals records and sheets.
    /// </summary>
    /// <param name="globalsExtra">Records placed in the globals substream before the bound sheets (for example SST, DATEMODE).</param>
    /// <param name="sheets">The sheets to embed.</param>
    /// <returns>A seekable stream positioned at the start of the workbook.</returns>
    public static MemoryStream BuildWorkbook(IReadOnlyList<byte[]> globalsExtra, params SheetSpec[] sheets)
    {
        byte[] workbook = BuildWorkbookStream(globalsExtra, sheets);
        return WrapInCompoundFile(workbook, "Workbook");
    }

    /// <summary>
    /// Wraps a workbook record stream in a compound file under the given stream name.
    /// </summary>
    /// <param name="workbookStream">The raw workbook record stream.</param>
    /// <param name="streamName">The compound-file stream name (<c>Workbook</c> or <c>Book</c>).</param>
    /// <returns>A seekable stream positioned at the start of the compound file.</returns>
    public static MemoryStream WrapInCompoundFile(byte[] workbookStream, string streamName)
    {
        MemoryStream container = new();
        using (CompoundFile file = CompoundFile.Create(container, leaveOpen: true))
        {
            file.RootStorage.CreateStream(streamName, workbookStream);
            file.Commit();
        }

        container.Position = 0;
        return container;
    }

    /// <summary>
    /// Builds a workbook record stream: a globals substream with bound sheets pointing at each appended sheet substream.
    /// </summary>
    /// <param name="globalsExtra">Records placed in the globals substream before the bound sheets.</param>
    /// <param name="sheets">The sheets to embed.</param>
    /// <returns>The workbook record stream bytes.</returns>
    public static byte[] BuildWorkbookStream(IReadOnlyList<byte[]> globalsExtra, params SheetSpec[] sheets)
    {
        // Build each sheet substream up front; their lengths fix the bound-sheet stream offsets.
        byte[][] substreams = new byte[sheets.Length][];
        for (int i = 0; i < sheets.Length; i++)
        {
            using MemoryStream sub = new();
            sub.Write(Bof(BofWorksheet));
            foreach (byte[] record in sheets[i].Body)
                sub.Write(record);
            sub.Write(Eof());
            substreams[i] = sub.ToArray();
        }

        // The globals length is known once the (fixed-size) bound-sheet records are sized, so compute it first.
        int globalsLength = Bof(BofGlobals).Length + Eof().Length;
        foreach (byte[] record in globalsExtra)
            globalsLength += record.Length;
        foreach (SheetSpec sheet in sheets)
            globalsLength += BoundSheetSize(sheet.Name);

        using MemoryStream workbook = new();
        workbook.Write(Bof(BofGlobals));
        foreach (byte[] record in globalsExtra)
            workbook.Write(record);

        long offset = globalsLength;
        for (int i = 0; i < sheets.Length; i++)
        {
            workbook.Write(BoundSheet(offset, sheets[i].Type, sheets[i].Visibility, sheets[i].Name));
            offset += substreams[i].Length;
        }

        workbook.Write(Eof());
        foreach (byte[] substream in substreams)
            workbook.Write(substream);

        return workbook.ToArray();
    }

    /// <summary>Builds a bound-sheet record.</summary>
    /// <param name="streamOffset">The substream offset (lbPlyPos).</param>
    /// <param name="sheetType">The sheet-type byte.</param>
    /// <param name="visibility">The hidden-state byte.</param>
    /// <param name="name">The sheet name.</param>
    /// <returns>The framed BOUNDSHEET8 record.</returns>
    public static byte[] BoundSheet(long streamOffset, byte sheetType, byte visibility, string name)
    {
        byte[] payload = new byte[8 + name.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, (uint)streamOffset);
        payload[4] = (byte)(visibility & 0x03);
        payload[5] = sheetType;
        payload[6] = (byte)name.Length;
        payload[7] = 0x00;
        for (int i = 0; i < name.Length; i++)
            payload[8 + i] = (byte)name[i];

        return Record(0x0085, payload);
    }

    /// <summary>Returns the framed size of a bound-sheet record for the given name.</summary>
    /// <param name="name">The sheet name.</param>
    /// <returns>The total record size in bytes.</returns>
    private static int BoundSheetSize(string name) =>
        4 + 8 + name.Length;

    /// <summary>Writes the common row, column, and extended-format header of a cell record.</summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="row">The row index.</param>
    /// <param name="column">The column index.</param>
    /// <param name="xfIndex">The extended-format index.</param>
    private static void WriteCellHeader(Span<byte> payload, int row, int column, ushort xfIndex)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)row);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(2), (ushort)column);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(4), xfIndex);
    }
}
