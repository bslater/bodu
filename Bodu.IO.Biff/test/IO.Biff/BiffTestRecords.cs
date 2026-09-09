// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffTestRecords.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

namespace Bodu.IO.Biff;

/// <summary>
/// Builds BIFF records byte by byte from the MS-XLS record layouts, independently of <see cref="BiffWriter" />, so
/// the reader tests cannot share an encoding mistake with the writer.
/// </summary>
internal static class BiffTestRecords
{
    /// <summary>The BIFF5 <c>BOF</c> version marker.</summary>
    public const ushort Biff5Marker = 0x0500;

    /// <summary>The BIFF8 <c>BOF</c> version marker.</summary>
    public const ushort Biff8Marker = 0x0600;

    /// <summary>
    /// Frames a payload as a record.
    /// </summary>
    /// <param name="id">The record identifier.</param>
    /// <param name="payload">The payload.</param>
    /// <returns>The header followed by the payload.</returns>
    public static byte[] Record(ushort id, ReadOnlySpan<byte> payload)
    {
        byte[] record = new byte[4 + payload.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(record, id);
        BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(2), (ushort)payload.Length);
        payload.CopyTo(record.AsSpan(4));
        return record;
    }

    /// <summary>
    /// Frames a payload as a record of a named type.
    /// </summary>
    /// <param name="type">The record type.</param>
    /// <param name="payload">The payload.</param>
    /// <returns>The header followed by the payload.</returns>
    public static byte[] Record(BiffRecordType type, ReadOnlySpan<byte> payload) =>
        Record((ushort)type, payload);

    /// <summary>
    /// Concatenates records into one stream.
    /// </summary>
    /// <param name="records">The records.</param>
    /// <returns>The stream bytes.</returns>
    public static byte[] Stream(params byte[][] records)
    {
        using MemoryStream stream = new();
        foreach (byte[] record in records)
            stream.Write(record);

        return stream.ToArray();
    }

    /// <summary>
    /// Builds a <c>BOF</c> record.
    /// </summary>
    /// <param name="version">The version marker.</param>
    /// <param name="substream">The substream type.</param>
    /// <param name="build">The build number.</param>
    /// <param name="year">The build year.</param>
    /// <returns>The record.</returns>
    public static byte[] Bof(ushort version, BiffSubstreamType substream = BiffSubstreamType.WorkbookGlobals, ushort build = 0x0DBB, ushort year = 0x07CC)
    {
        byte[] payload = new byte[version == Biff8Marker ? 16 : 8];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, version);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2), (ushort)substream);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4), build);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(6), year);
        if (version == Biff8Marker)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(8), 0x000040C1);
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(12), 0x00000206);
        }

        return Record(BiffRecordType.Bof, payload);
    }

    /// <summary>
    /// Builds a BIFF8 <c>BOF</c> record.
    /// </summary>
    /// <param name="substream">The substream type.</param>
    /// <returns>The record.</returns>
    public static byte[] Bof8(BiffSubstreamType substream = BiffSubstreamType.WorkbookGlobals) =>
        Bof(Biff8Marker, substream);

    /// <summary>
    /// Builds a BIFF5 <c>BOF</c> record.
    /// </summary>
    /// <param name="substream">The substream type.</param>
    /// <returns>The record.</returns>
    public static byte[] Bof5(BiffSubstreamType substream = BiffSubstreamType.WorkbookGlobals) =>
        Bof(Biff5Marker, substream);

    /// <summary>
    /// Builds an <c>EOF</c> record.
    /// </summary>
    /// <returns>The record.</returns>
    public static byte[] Eof() =>
        Record(BiffRecordType.Eof, default);

    /// <summary>
    /// Builds a <c>CODEPAGE</c> record.
    /// </summary>
    /// <param name="codePage">The raw value.</param>
    /// <returns>The record.</returns>
    public static byte[] CodePage(ushort codePage) =>
        Record(BiffRecordType.CodePage, UInt16(codePage));

    /// <summary>
    /// Builds a <c>DATEMODE</c> record.
    /// </summary>
    /// <param name="is1904">Whether the 1904 system is selected.</param>
    /// <returns>The record.</returns>
    public static byte[] DateMode(bool is1904) =>
        Record(BiffRecordType.DateMode, UInt16((ushort)(is1904 ? 1 : 0)));

    /// <summary>
    /// Builds a <c>NUMBER</c> record.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="value">The value.</param>
    /// <param name="xf">The XF index.</param>
    /// <returns>The record.</returns>
    public static byte[] Number(int row, int column, double value, ushort xf = 0)
    {
        byte[] payload = new byte[14];
        CellHeader(payload, row, column, xf);
        BinaryPrimitives.WriteDoubleLittleEndian(payload.AsSpan(6), value);
        return Record(BiffRecordType.Number, payload);
    }

    /// <summary>
    /// Builds an <c>RK</c> record.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="rk">The RK value.</param>
    /// <param name="xf">The XF index.</param>
    /// <returns>The record.</returns>
    public static byte[] Rk(int row, int column, uint rk, ushort xf = 0)
    {
        byte[] payload = new byte[10];
        CellHeader(payload, row, column, xf);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(6), rk);
        return Record(BiffRecordType.Rk, payload);
    }

    /// <summary>
    /// Builds a <c>MULRK</c> record.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="firstColumn">The first column.</param>
    /// <param name="cells">The cells.</param>
    /// <returns>The record.</returns>
    public static byte[] MulRk(int row, int firstColumn, params (ushort Xf, uint Rk)[] cells)
    {
        byte[] payload = new byte[6 + (cells.Length * 6)];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)row);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2), (ushort)firstColumn);
        for (int i = 0; i < cells.Length; i++)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4 + (i * 6)), cells[i].Xf);
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(6 + (i * 6)), cells[i].Rk);
        }

        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(payload.Length - 2), (ushort)(firstColumn + cells.Length - 1));
        return Record(BiffRecordType.MulRk, payload);
    }

    /// <summary>
    /// Builds a <c>BLANK</c> record.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="xf">The XF index.</param>
    /// <returns>The record.</returns>
    public static byte[] Blank(int row, int column, ushort xf = 0)
    {
        byte[] payload = new byte[6];
        CellHeader(payload, row, column, xf);
        return Record(BiffRecordType.Blank, payload);
    }

    /// <summary>
    /// Builds a <c>MULBLANK</c> record.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="firstColumn">The first column.</param>
    /// <param name="xfs">The XF index of each cell.</param>
    /// <returns>The record.</returns>
    public static byte[] MulBlank(int row, int firstColumn, params ushort[] xfs)
    {
        byte[] payload = new byte[6 + (xfs.Length * 2)];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)row);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2), (ushort)firstColumn);
        for (int i = 0; i < xfs.Length; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4 + (i * 2)), xfs[i]);

        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(payload.Length - 2), (ushort)(firstColumn + xfs.Length - 1));
        return Record(BiffRecordType.MulBlank, payload);
    }

    /// <summary>
    /// Builds a <c>BOOLERR</c> record.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="value">The value byte.</param>
    /// <param name="isError">Whether the value is an error code.</param>
    /// <param name="xf">The XF index.</param>
    /// <returns>The record.</returns>
    public static byte[] BoolErr(int row, int column, byte value, bool isError, ushort xf = 0)
    {
        byte[] payload = new byte[8];
        CellHeader(payload, row, column, xf);
        payload[6] = value;
        payload[7] = (byte)(isError ? 1 : 0);
        return Record(BiffRecordType.BoolErr, payload);
    }

    /// <summary>
    /// Builds a <c>LABELSST</c> record.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="sstIndex">The shared string index.</param>
    /// <param name="xf">The XF index.</param>
    /// <returns>The record.</returns>
    public static byte[] LabelSst(int row, int column, uint sstIndex, ushort xf = 0)
    {
        byte[] payload = new byte[10];
        CellHeader(payload, row, column, xf);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(6), sstIndex);
        return Record(BiffRecordType.LabelSst, payload);
    }

    /// <summary>
    /// Builds a BIFF8 <c>LABEL</c> record.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="text">The text.</param>
    /// <param name="wide">Whether to encode the text as 16-bit characters.</param>
    /// <param name="xf">The XF index.</param>
    /// <returns>The record.</returns>
    public static byte[] Label8(int row, int column, string text, bool wide = false, ushort xf = 0)
    {
        byte[] header = new byte[6];
        CellHeader(header, row, column, xf);
        return Record(BiffRecordType.Label, [.. header, .. UnicodeString(text, wide: wide)]);
    }

    /// <summary>
    /// Builds a BIFF5 <c>LABEL</c> record.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="bytes">The code-page bytes.</param>
    /// <param name="xf">The XF index.</param>
    /// <returns>The record.</returns>
    public static byte[] Label5(int row, int column, byte[] bytes, ushort xf = 0)
    {
        byte[] header = new byte[6];
        CellHeader(header, row, column, xf);
        return Record(BiffRecordType.Label, [.. header, .. UInt16((ushort)bytes.Length), .. bytes]);
    }

    /// <summary>
    /// Builds a <c>FORMULA</c> record.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="result">The eight-byte cached result.</param>
    /// <param name="tokens">The parsed-expression tokens.</param>
    /// <param name="flags">The option flags.</param>
    /// <param name="xf">The XF index.</param>
    /// <returns>The record.</returns>
    public static byte[] Formula(int row, int column, byte[] result, byte[]? tokens = null, ushort flags = 0, ushort xf = 0)
    {
        tokens ??= [];
        byte[] payload = new byte[22 + tokens.Length];
        CellHeader(payload, row, column, xf);
        result.CopyTo(payload.AsSpan(6));
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(14), flags);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(20), (ushort)tokens.Length);
        tokens.CopyTo(payload.AsSpan(22));
        return Record(BiffRecordType.Formula, payload);
    }

    /// <summary>
    /// Builds the eight-byte cached result of a numeric formula.
    /// </summary>
    /// <param name="value">The number.</param>
    /// <returns>The result bytes.</returns>
    public static byte[] NumberResult(double value)
    {
        byte[] result = new byte[8];
        BinaryPrimitives.WriteDoubleLittleEndian(result, value);
        return result;
    }

    /// <summary>
    /// Builds the eight-byte cached result of a non-numeric formula.
    /// </summary>
    /// <param name="kind">The kind byte: 0 string, 1 boolean, 2 error, 3 empty.</param>
    /// <param name="value">The value byte.</param>
    /// <returns>The result bytes.</returns>
    public static byte[] SpecialResult(byte kind, byte value = 0) =>
        [kind, 0, value, 0, 0, 0, 0xFF, 0xFF];

    /// <summary>
    /// Builds a BIFF8 <c>STRING</c> record.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The record.</returns>
    public static byte[] String8(string text) =>
        Record(BiffRecordType.String, UnicodeString(text));

    /// <summary>
    /// Builds a BIFF5 <c>STRING</c> record.
    /// </summary>
    /// <param name="bytes">The code-page bytes.</param>
    /// <returns>The record.</returns>
    public static byte[] String5(byte[] bytes) =>
        Record(BiffRecordType.String, [.. UInt16((ushort)bytes.Length), .. bytes]);

    /// <summary>
    /// Builds a BIFF8 <c>BOUNDSHEET</c> record.
    /// </summary>
    /// <param name="offset">The substream offset.</param>
    /// <param name="state">The visibility.</param>
    /// <param name="type">The sheet type.</param>
    /// <param name="name">The name.</param>
    /// <param name="wide">Whether to encode the name as 16-bit characters.</param>
    /// <returns>The record.</returns>
    public static byte[] BoundSheet8(uint offset, BiffSheetState state, BiffSheetType type, string name, bool wide = false)
    {
        byte[] head = new byte[6];
        BinaryPrimitives.WriteUInt32LittleEndian(head, offset);
        head[4] = (byte)state;
        head[5] = (byte)type;
        return Record(BiffRecordType.BoundSheet, [.. head, .. UnicodeString(name, wide: wide, wideLength: false)]);
    }

    /// <summary>
    /// Builds a BIFF5 <c>BOUNDSHEET</c> record.
    /// </summary>
    /// <param name="offset">The substream offset.</param>
    /// <param name="state">The visibility.</param>
    /// <param name="type">The sheet type.</param>
    /// <param name="name">The code-page name bytes.</param>
    /// <returns>The record.</returns>
    public static byte[] BoundSheet5(uint offset, BiffSheetState state, BiffSheetType type, byte[] name)
    {
        byte[] head = new byte[6];
        BinaryPrimitives.WriteUInt32LittleEndian(head, offset);
        head[4] = (byte)state;
        head[5] = (byte)type;
        return Record(BiffRecordType.BoundSheet, [.. head, (byte)name.Length, .. name]);
    }

    /// <summary>
    /// Builds a BIFF8 <c>DIMENSIONS</c> record.
    /// </summary>
    /// <param name="firstRow">The first row.</param>
    /// <param name="lastRowExclusive">One past the last row.</param>
    /// <param name="firstColumn">The first column.</param>
    /// <param name="lastColumnExclusive">One past the last column.</param>
    /// <returns>The record.</returns>
    public static byte[] Dimensions8(uint firstRow, uint lastRowExclusive, ushort firstColumn, ushort lastColumnExclusive)
    {
        byte[] payload = new byte[14];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, firstRow);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4), lastRowExclusive);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(8), firstColumn);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(10), lastColumnExclusive);
        return Record(BiffRecordType.Dimensions, payload);
    }

    /// <summary>
    /// Builds a BIFF5 <c>DIMENSIONS</c> record.
    /// </summary>
    /// <param name="firstRow">The first row.</param>
    /// <param name="lastRowExclusive">One past the last row.</param>
    /// <param name="firstColumn">The first column.</param>
    /// <param name="lastColumnExclusive">One past the last column.</param>
    /// <returns>The record.</returns>
    public static byte[] Dimensions5(ushort firstRow, ushort lastRowExclusive, ushort firstColumn, ushort lastColumnExclusive)
    {
        byte[] payload = new byte[10];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, firstRow);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2), lastRowExclusive);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4), firstColumn);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(6), lastColumnExclusive);
        return Record(BiffRecordType.Dimensions, payload);
    }

    /// <summary>
    /// Builds a <c>ROW</c> record.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="firstColumn">The first column.</param>
    /// <param name="lastColumnExclusive">One past the last column.</param>
    /// <param name="height">The raw height field.</param>
    /// <param name="options">The option flags.</param>
    /// <param name="xf">The raw XF field.</param>
    /// <returns>The record.</returns>
    public static byte[] Row(ushort row, ushort firstColumn, ushort lastColumnExclusive, ushort height, ushort options, ushort xf)
    {
        byte[] payload = new byte[16];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, row);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2), firstColumn);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4), lastColumnExclusive);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(6), height);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(12), options);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(14), xf);
        return Record(BiffRecordType.Row, payload);
    }

    /// <summary>
    /// Builds an <c>XF</c> record.
    /// </summary>
    /// <param name="font">The font index.</param>
    /// <param name="format">The format index.</param>
    /// <param name="typeField">The type-and-protection word.</param>
    /// <param name="length">The record length (16 for BIFF5, 20 for BIFF8).</param>
    /// <returns>The record.</returns>
    public static byte[] Xf(ushort font, ushort format, ushort typeField, int length = 20)
    {
        byte[] payload = new byte[length];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, font);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2), format);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4), typeField);
        return Record(BiffRecordType.Xf, payload);
    }

    /// <summary>
    /// Builds a BIFF8 <c>FORMAT</c> record.
    /// </summary>
    /// <param name="index">The format index.</param>
    /// <param name="code">The format code.</param>
    /// <returns>The record.</returns>
    public static byte[] Format8(ushort index, string code) =>
        Record(BiffRecordType.Format, [.. UInt16(index), .. UnicodeString(code)]);

    /// <summary>
    /// Builds a BIFF5 <c>FORMAT</c> record.
    /// </summary>
    /// <param name="index">The format index.</param>
    /// <param name="code">The code-page code bytes.</param>
    /// <returns>The record.</returns>
    public static byte[] Format5(ushort index, byte[] code) =>
        Record(BiffRecordType.Format, [.. UInt16(index), (byte)code.Length, .. code]);

    /// <summary>
    /// Builds a <c>FONT</c> record with the name in the specified representation.
    /// </summary>
    /// <param name="height">The height.</param>
    /// <param name="attributes">The attribute flags.</param>
    /// <param name="color">The color index.</param>
    /// <param name="weight">The weight.</param>
    /// <param name="escapement">The escapement.</param>
    /// <param name="underline">The underline.</param>
    /// <param name="family">The family.</param>
    /// <param name="charSet">The character set.</param>
    /// <param name="name">The encoded name including its 8-bit length prefix.</param>
    /// <returns>The record.</returns>
    public static byte[] Font(ushort height, ushort attributes, ushort color, ushort weight, ushort escapement, byte underline, byte family, byte charSet, byte[] name)
    {
        byte[] head = new byte[14];
        BinaryPrimitives.WriteUInt16LittleEndian(head, height);
        BinaryPrimitives.WriteUInt16LittleEndian(head.AsSpan(2), attributes);
        BinaryPrimitives.WriteUInt16LittleEndian(head.AsSpan(4), color);
        BinaryPrimitives.WriteUInt16LittleEndian(head.AsSpan(6), weight);
        BinaryPrimitives.WriteUInt16LittleEndian(head.AsSpan(8), escapement);
        head[10] = underline;
        head[11] = family;
        head[12] = charSet;
        return Record(BiffRecordType.Font, [.. head, .. name]);
    }

    /// <summary>
    /// Builds an <c>SST</c> record whose payload is given verbatim after the counts.
    /// </summary>
    /// <param name="total">The total count.</param>
    /// <param name="unique">The unique count.</param>
    /// <param name="body">The string bytes.</param>
    /// <returns>The record.</returns>
    public static byte[] Sst(uint total, uint unique, params byte[][] body)
    {
        byte[] counts = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(counts, total);
        BinaryPrimitives.WriteUInt32LittleEndian(counts.AsSpan(4), unique);
        return Record(BiffRecordType.Sst, [.. counts, .. body.SelectMany(b => b)]);
    }

    /// <summary>
    /// Builds a <c>CONTINUE</c> record.
    /// </summary>
    /// <param name="payload">The payload.</param>
    /// <returns>The record.</returns>
    public static byte[] Continue(params byte[] payload) =>
        Record(BiffRecordType.Continue, payload);

    /// <summary>
    /// Encodes a BIFF8 Unicode string structure.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="wide">Whether to use 16-bit characters.</param>
    /// <param name="wideLength">Whether the length prefix is 16 bits.</param>
    /// <param name="richRuns">The number of rich-text runs to declare and pad.</param>
    /// <param name="extendedSize">The extended-data size to declare and pad.</param>
    /// <returns>The encoded bytes.</returns>
    public static byte[] UnicodeString(string text, bool wide = false, bool wideLength = true, int richRuns = 0, int extendedSize = 0)
    {
        var bytes = new List<byte>();
        if (wideLength)
            bytes.AddRange(UInt16((ushort)text.Length));
        else
            bytes.Add((byte)text.Length);

        byte flags = (byte)(wide ? 0x01 : 0x00);
        if (richRuns > 0)
            flags |= 0x08;
        if (extendedSize > 0)
            flags |= 0x04;
        bytes.Add(flags);

        if (richRuns > 0)
            bytes.AddRange(UInt16((ushort)richRuns));
        if (extendedSize > 0)
        {
            byte[] size = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(size, (uint)extendedSize);
            bytes.AddRange(size);
        }

        bytes.AddRange(wide ? Encoding.Unicode.GetBytes(text) : text.Select(c => (byte)c));
        bytes.AddRange(new byte[(richRuns * 4) + extendedSize]);
        return [.. bytes];
    }

    /// <summary>
    /// Encodes a 16-bit little-endian value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The two bytes.</returns>
    public static byte[] UInt16(ushort value)
    {
        byte[] bytes = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>
    /// Writes the row, column, and XF header shared by the cell records.
    /// </summary>
    /// <param name="payload">The payload buffer.</param>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="xf">The XF index.</param>
    private static void CellHeader(Span<byte> payload, int row, int column, ushort xf)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)row);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(2), (ushort)column);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(4), xf);
    }
}
