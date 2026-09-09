// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8TestWorkbook.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Binary;
using Bodu.Formats.Excel.Biff;
using Bodu.IO.Biff;
using Bodu.IO.Compound;

namespace Bodu.Formats.Excel;

/// <summary>
/// Builds synthetic BIFF8 record streams, worksheet substreams, and complete <c>.xls</c> compound files for tests,
/// covering producer variations the real-world sample fixture does not exercise. Each record is emitted through
/// <see cref="BiffWriter" /> so the Excel reader is exercised against the codec's own output.
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
    /// <param name="Visibility">The bound-sheet visibility byte (0 visible, 1 hidden, 2 very hidden).</param>
    /// <param name="Body">The records between the sheet's BOF and EOF.</param>
    public sealed record SheetSpec(string Name, byte Type, byte Visibility, IReadOnlyList<byte[]> Body);

    /// <summary>Writes records through a writer passed by reference.</summary>
    /// <param name="writer">The writer.</param>
    private delegate void WriteAction(ref BiffWriter writer);

    /// <summary>Frames a raw record.</summary>
    /// <param name="id">The record identifier.</param>
    /// <param name="payload">The payload.</param>
    /// <returns>The framed record.</returns>
    public static byte[] Record(ushort id, ReadOnlySpan<byte> payload)
    {
        byte[] copy = payload.ToArray();
        return Emit((ref BiffWriter w) => w.WriteRecord(id, copy));
    }

    /// <summary>Builds a BOF record for the given substream type.</summary>
    /// <param name="substreamType">The substream type.</param>
    /// <returns>The framed record.</returns>
    public static byte[] Bof(ushort substreamType) =>
        Emit((ref BiffWriter w) => w.WriteBof((BiffSubstreamType)substreamType));

    /// <summary>Builds an EOF record.</summary>
    /// <returns>The framed record.</returns>
    public static byte[] Eof() =>
        Record((ushort)BiffRecordType.Eof, default);

    /// <summary>Builds a DATEMODE record.</summary>
    /// <param name="is1904">Whether the 1904 date system is selected.</param>
    /// <returns>The framed record.</returns>
    public static byte[] DateMode(bool is1904) =>
        Emit((ref BiffWriter w) => w.WriteDateMode(is1904));

    /// <summary>Builds a FILEPASS record, marking the workbook encrypted.</summary>
    /// <returns>The framed record.</returns>
    public static byte[] FilePass() =>
        Record(0x002F, new byte[6]);

    /// <summary>Builds a NUMBER cell.</summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="value">The value.</param>
    /// <param name="xfIndex">The XF index.</param>
    /// <returns>The framed record.</returns>
    public static byte[] Number(int row, int column, double value, ushort xfIndex = 0) =>
        Emit((ref BiffWriter w) => w.WriteNumber(row, column, xfIndex, value));

    /// <summary>Builds an RK cell.</summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="rk">The RK value.</param>
    /// <param name="xfIndex">The XF index.</param>
    /// <returns>The framed record.</returns>
    public static byte[] Rk(int row, int column, uint rk, ushort xfIndex = 0) =>
        Emit((ref BiffWriter w) => w.WriteRk(row, column, xfIndex, rk));

    /// <summary>Builds a LABELSST cell.</summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="stringIndex">The shared string index.</param>
    /// <param name="xfIndex">The XF index.</param>
    /// <returns>The framed record.</returns>
    public static byte[] LabelSst(int row, int column, uint stringIndex, ushort xfIndex = 0) =>
        Emit((ref BiffWriter w) => w.WriteLabelSst(row, column, xfIndex, stringIndex));

    /// <summary>Builds a LABEL cell holding inline text.</summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="value">The text.</param>
    /// <param name="xfIndex">The XF index.</param>
    /// <returns>The framed record.</returns>
    public static byte[] Label(int row, int column, string value, ushort xfIndex = 0) =>
        Emit((ref BiffWriter w) => w.WriteLabel(row, column, xfIndex, value));

    /// <summary>Builds a BOOLERR cell.</summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="value">The value byte.</param>
    /// <param name="isError">Whether the value is an error code.</param>
    /// <param name="xfIndex">The XF index.</param>
    /// <returns>The framed record.</returns>
    public static byte[] BoolErr(int row, int column, byte value, bool isError, ushort xfIndex = 0) =>
        Emit((ref BiffWriter w) => w.WriteBoolErr(row, column, xfIndex, value, isError));

    /// <summary>Builds a MULRK run.</summary>
    /// <param name="row">The row.</param>
    /// <param name="firstColumn">The first column.</param>
    /// <param name="values">The XF index and RK value of each cell.</param>
    /// <returns>The framed record.</returns>
    public static byte[] MulRk(int row, int firstColumn, params (ushort XfIndex, uint Rk)[] values)
    {
        BiffRkCell[] cells = [.. values.Select(v => new BiffRkCell(v.XfIndex, v.Rk))];
        return Emit((ref BiffWriter w) => w.WriteMulRk(row, firstColumn, cells));
    }

    /// <summary>Builds a FORMULA cell from its raw eight-byte cached result.</summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="result">The eight-byte cached result.</param>
    /// <param name="xfIndex">The XF index.</param>
    /// <returns>The framed record.</returns>
    public static byte[] Formula(int row, int column, byte[] result, ushort xfIndex = 0)
    {
        // row(2) + col(2) + ixfe(2) + result(8) + grbit(2) + chn(4) + cce(2).
        byte[] payload = new byte[22];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)row);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2), (ushort)column);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4), xfIndex);
        result.CopyTo(payload.AsSpan(6));
        return Record(0x0006, payload);
    }

    /// <summary>Builds a STRING record: the cached text result of a preceding formula.</summary>
    /// <param name="value">The text.</param>
    /// <returns>The framed record.</returns>
    public static byte[] CompressedString(string value) =>
        Emit((ref BiffWriter w) => w.WriteString(value));

    /// <summary>Builds a DIMENSIONS record.</summary>
    /// <param name="firstRow">The first row.</param>
    /// <param name="rowMac">One past the last row.</param>
    /// <param name="firstColumn">The first column.</param>
    /// <param name="columnMac">One past the last column.</param>
    /// <returns>The framed record.</returns>
    public static byte[] Dimensions(int firstRow, int rowMac, int firstColumn, int columnMac) =>
        Emit((ref BiffWriter w) => w.WriteDimensions(new BiffDimensionsRecord(firstRow, rowMac, firstColumn, columnMac)));

    /// <summary>Builds a shared string table from the given strings.</summary>
    /// <param name="strings">The unique strings.</param>
    /// <returns>The framed SST record followed by any continuation records.</returns>
    public static byte[] Sst(params string[] strings) =>
        Emit((ref BiffWriter w) => w.WriteSst(strings));

    /// <summary>Builds a worksheet substream: BOF, the body records, EOF.</summary>
    /// <param name="body">The body records.</param>
    /// <returns>The substream bytes.</returns>
    public static byte[] WorksheetSubstream(params byte[][] body) =>
        Concat([Bof(BofWorksheet), .. body, Eof()]);

    /// <summary>Opens a worksheet reader over a substream built from the body records, with an empty string table.</summary>
    /// <param name="body">The body records.</param>
    /// <returns>The reader.</returns>
    public static ExcelWorksheetReader OpenWorksheetReader(params byte[][] body)
    {
        ExcelWorksheetInfo info = new("Sheet", 0, ExcelSheetVisibility.Visible, ExcelSheetType.Worksheet, default);
        return new ExcelWorksheetReader(info, WorksheetSubstream(body), [], BiffFormatTable.Empty, new BiffReaderOptions { Version = BiffVersion.Biff8 });
    }

    /// <summary>Opens a worksheet reader over raw substream bytes, with an empty string table.</summary>
    /// <param name="substream">The substream bytes.</param>
    /// <returns>The reader.</returns>
    public static ExcelWorksheetReader OpenWorksheetReaderRaw(byte[] substream)
    {
        ExcelWorksheetInfo info = new("Sheet", 0, ExcelSheetVisibility.Visible, ExcelSheetType.Worksheet, default);
        return new ExcelWorksheetReader(info, substream, [], BiffFormatTable.Empty, new BiffReaderOptions { Version = BiffVersion.Biff8 });
    }

    /// <summary>Concatenates records.</summary>
    /// <param name="records">The records.</param>
    /// <returns>The concatenated bytes.</returns>
    public static byte[] Concat(params byte[][] records)
    {
        using MemoryStream stream = new();
        foreach (byte[] record in records)
            stream.Write(record);

        return stream.ToArray();
    }

    /// <summary>Builds a workbook stream and wraps it in a compound file under the <c>Workbook</c> stream name.</summary>
    /// <param name="globalsExtra">Records to place in the globals after BOF and before the bound sheets.</param>
    /// <param name="sheets">The sheets.</param>
    /// <returns>The compound file bytes, positioned at the start.</returns>
    public static MemoryStream BuildWorkbook(IReadOnlyList<byte[]> globalsExtra, params SheetSpec[] sheets)
    {
        byte[] workbook = BuildWorkbookStream(globalsExtra, sheets);
        return WrapInCompoundFile(workbook, "Workbook");
    }

    /// <summary>Wraps a workbook stream in a compound file.</summary>
    /// <param name="workbookStream">The workbook stream bytes.</param>
    /// <param name="streamName">The compound-file stream name (<c>Workbook</c> or <c>Book</c>).</param>
    /// <returns>The compound file bytes, positioned at the start.</returns>
    public static MemoryStream WrapInCompoundFile(byte[] workbookStream, string streamName)
    {
        MemoryStream container = new();
        using (var file = CompoundFile.Create(container, leaveOpen: true))
        {
            file.RootStorage.CreateStream(streamName, workbookStream);
            file.Commit();
        }

        container.Position = 0;
        return container;
    }

    /// <summary>Builds a workbook stream: the globals substream followed by each sheet's substream.</summary>
    /// <param name="globalsExtra">Records to place in the globals after BOF and before the bound sheets.</param>
    /// <param name="sheets">The sheets.</param>
    /// <returns>The workbook stream bytes.</returns>
    public static byte[] BuildWorkbookStream(IReadOnlyList<byte[]> globalsExtra, params SheetSpec[] sheets)
    {
        // Build each sheet substream up front; their lengths fix the bound-sheet stream offsets.
        byte[][] substreams = new byte[sheets.Length][];
        for (int i = 0; i < sheets.Length; i++)
            substreams[i] = Concat([Bof(BofWorksheet), .. sheets[i].Body, Eof()]);

        // The globals length is known once the (fixed-size) bound-sheet records are sized, so compute it first.
        int globalsLength = Bof(BofGlobals).Length + Eof().Length;
        foreach (byte[] record in globalsExtra)
            globalsLength += record.Length;
        foreach (SheetSpec sheet in sheets)
            globalsLength += BoundSheet(0, sheet.Type, sheet.Visibility, sheet.Name).Length;

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

    /// <summary>Builds a BOUNDSHEET record.</summary>
    /// <param name="streamOffset">The sheet's substream offset.</param>
    /// <param name="sheetType">The sheet type byte.</param>
    /// <param name="visibility">The visibility byte.</param>
    /// <param name="name">The sheet name.</param>
    /// <returns>The framed record.</returns>
    public static byte[] BoundSheet(long streamOffset, byte sheetType, byte visibility, string name) =>
        Emit((ref BiffWriter w) => w.WriteBoundSheet((uint)streamOffset, (BiffSheetState)visibility, (BiffSheetType)sheetType, name));

    /// <summary>Runs a writer callback against a fresh BIFF8 writer and returns the emitted bytes.</summary>
    /// <param name="write">The callback.</param>
    /// <returns>The emitted bytes.</returns>
    private static byte[] Emit(WriteAction write)
    {
        var output = new ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, BiffVersion.Biff8);
        write(ref writer);
        return output.WrittenSpan.ToArray();
    }
}
