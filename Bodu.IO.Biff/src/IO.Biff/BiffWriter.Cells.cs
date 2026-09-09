// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriter.Cells.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Biff;

public ref partial struct BiffWriter
{
    /// <summary>
    /// Writes a <c>NUMBER</c> record: a cell holding a double-precision value.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="xfIndex">The extended-format index of the cell.</param>
    /// <param name="value">The cell value.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="row" /> or <paramref name="column" /> is outside the 16-bit range.
    /// </exception>
    public void WriteNumber(int row, int column, ushort xfIndex, double value)
    {
        RequireCellIndex(row, nameof(row));
        RequireCellIndex(column, nameof(column));

        Span<byte> payload = Begin((ushort)BiffRecordType.Number, BiffNumberRecord.Length);
        WriteCellPrefix(payload, row, column, xfIndex);
        BinaryPrimitives.WriteDoubleLittleEndian(payload.Slice(6), value);
        Commit(BiffNumberRecord.Length);
    }

    /// <summary>
    /// Writes an <c>RK</c> record: a cell holding an RK-encoded number.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="xfIndex">The extended-format index of the cell.</param>
    /// <param name="rk">The RK value, as produced by <see cref="BiffRk.TryEncode" />.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="row" /> or <paramref name="column" /> is outside the 16-bit range.
    /// </exception>
    public void WriteRk(int row, int column, ushort xfIndex, uint rk)
    {
        RequireCellIndex(row, nameof(row));
        RequireCellIndex(column, nameof(column));

        Span<byte> payload = Begin((ushort)BiffRecordType.Rk, BiffRkRecord.Length);
        WriteCellPrefix(payload, row, column, xfIndex);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.Slice(6), rk);
        Commit(BiffRkRecord.Length);
    }

    /// <summary>
    /// Writes a <c>MULRK</c> record: a run of adjacent RK-encoded cells in one row.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="firstColumn">The zero-based column index of the first cell.</param>
    /// <param name="cells">The cells, in column order.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the row or a column index is outside the 16-bit range, or the run does not fit the record.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="cells" /> is empty.</exception>
    public void WriteMulRk(int row, int firstColumn, scoped ReadOnlySpan<BiffRkCell> cells)
    {
        RequireCellIndex(row, nameof(row));
        RequireCellIndex(firstColumn, nameof(firstColumn));
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(cells, 1);
        RequireCellIndex(firstColumn + cells.Length - 1, nameof(cells));

        int length = 6 + (cells.Length * BiffRkCell.Length);
        if (length > _maxPayloadLength)
            throw PayloadTooLong(length, nameof(cells));

        Span<byte> payload = Begin((ushort)BiffRecordType.MulRk, length);
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)row);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(2), (ushort)firstColumn);
        for (int i = 0; i < cells.Length; i++)
        {
            int offset = 4 + (i * BiffRkCell.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(offset), cells[i].XfIndex);
            BinaryPrimitives.WriteUInt32LittleEndian(payload.Slice(offset + 2), cells[i].RawValue);
        }

        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(length - 2), (ushort)(firstColumn + cells.Length - 1));
        Commit(length);
    }

    /// <summary>
    /// Writes a <c>BLANK</c> record: a formatted cell carrying no value.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="xfIndex">The extended-format index of the cell.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="row" /> or <paramref name="column" /> is outside the 16-bit range.
    /// </exception>
    public void WriteBlank(int row, int column, ushort xfIndex)
    {
        RequireCellIndex(row, nameof(row));
        RequireCellIndex(column, nameof(column));

        Span<byte> payload = Begin((ushort)BiffRecordType.Blank, BiffBlankRecord.Length);
        WriteCellPrefix(payload, row, column, xfIndex);
        Commit(BiffBlankRecord.Length);
    }

    /// <summary>
    /// Writes a <c>MULBLANK</c> record: a run of adjacent blank cells in one row.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="firstColumn">The zero-based column index of the first cell.</param>
    /// <param name="xfIndices">The extended-format index of each cell, in column order.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the row or a column index is outside the 16-bit range, or the run does not fit the record.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="xfIndices" /> is empty.</exception>
    public void WriteMulBlank(int row, int firstColumn, scoped ReadOnlySpan<ushort> xfIndices)
    {
        RequireCellIndex(row, nameof(row));
        RequireCellIndex(firstColumn, nameof(firstColumn));
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(xfIndices, 1);
        RequireCellIndex(firstColumn + xfIndices.Length - 1, nameof(xfIndices));

        int length = 6 + (xfIndices.Length * 2);
        if (length > _maxPayloadLength)
            throw PayloadTooLong(length, nameof(xfIndices));

        Span<byte> payload = Begin((ushort)BiffRecordType.MulBlank, length);
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)row);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(2), (ushort)firstColumn);
        for (int i = 0; i < xfIndices.Length; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(4 + (i * 2)), xfIndices[i]);

        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(length - 2), (ushort)(firstColumn + xfIndices.Length - 1));
        Commit(length);
    }

    /// <summary>
    /// Writes a <c>BOOLERR</c> record holding a boolean.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="xfIndex">The extended-format index of the cell.</param>
    /// <param name="value">The boolean value.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="row" /> or <paramref name="column" /> is outside the 16-bit range.
    /// </exception>
    public void WriteBoolean(int row, int column, ushort xfIndex, bool value) =>
        WriteBoolErr(row, column, xfIndex, (byte)(value ? 1 : 0), isError: false);

    /// <summary>
    /// Writes a <c>BOOLERR</c> record holding an error code.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="xfIndex">The extended-format index of the cell.</param>
    /// <param name="errorCode">The BIFF error code.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="row" /> or <paramref name="column" /> is outside the 16-bit range.
    /// </exception>
    public void WriteError(int row, int column, ushort xfIndex, byte errorCode) =>
        WriteBoolErr(row, column, xfIndex, errorCode, isError: true);

    /// <summary>
    /// Writes a <c>BOOLERR</c> record from its raw value byte and kind flag.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="xfIndex">The extended-format index of the cell.</param>
    /// <param name="rawValue">The value byte.</param>
    /// <param name="isError">Whether the value byte is an error code.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="row" /> or <paramref name="column" /> is outside the 16-bit range.
    /// </exception>
    public void WriteBoolErr(int row, int column, ushort xfIndex, byte rawValue, bool isError)
    {
        RequireCellIndex(row, nameof(row));
        RequireCellIndex(column, nameof(column));

        Span<byte> payload = Begin((ushort)BiffRecordType.BoolErr, BiffBoolErrRecord.Length);
        WriteCellPrefix(payload, row, column, xfIndex);
        payload[6] = rawValue;
        payload[7] = (byte)(isError ? 1 : 0);
        Commit(BiffBoolErrRecord.Length);
    }

    /// <summary>
    /// Writes a <c>FORMULA</c> record whose cached result is a number.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="xfIndex">The extended-format index of the cell.</param>
    /// <param name="cachedValue">The cached numeric result.</param>
    /// <param name="tokens">The parsed-expression tokens (<c>rgce</c>), written verbatim.</param>
    /// <param name="flags">The option flags (<c>grbit</c>).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="row" /> or <paramref name="column" /> is outside the 16-bit range, or the tokens do
    /// not fit the record.
    /// </exception>
    public void WriteFormula(int row, int column, ushort xfIndex, double cachedValue, scoped ReadOnlySpan<byte> tokens, ushort flags = 0)
    {
        Span<byte> result = stackalloc byte[8];
        BinaryPrimitives.WriteDoubleLittleEndian(result, cachedValue);
        WriteFormulaCore(row, column, xfIndex, result, tokens, flags);
    }

    /// <summary>
    /// Writes a <c>FORMULA</c> record whose cached result is a string, boolean, error, or empty marker.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="xfIndex">The extended-format index of the cell.</param>
    /// <param name="cachedResultKind">
    /// The kind of cached result; a string result is carried by a following <c>STRING</c> record.
    /// </param>
    /// <param name="cachedValue">The boolean (zero or one) or error code byte; ignored for other kinds.</param>
    /// <param name="tokens">The parsed-expression tokens (<c>rgce</c>), written verbatim.</param>
    /// <param name="flags">The option flags (<c>grbit</c>).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="cachedResultKind" /> is <see cref="BiffCachedResultKind.Number" /> or undefined,
    /// <paramref name="row" /> or <paramref name="column" /> is outside the 16-bit range, or the tokens do not fit the
    /// record.
    /// </exception>
    public void WriteFormula(int row, int column, ushort xfIndex, BiffCachedResultKind cachedResultKind, byte cachedValue, scoped ReadOnlySpan<byte> tokens, ushort flags = 0)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(cachedResultKind);
        if (cachedResultKind == BiffCachedResultKind.Number)
            throw new ArgumentOutOfRangeException(nameof(cachedResultKind), cachedResultKind, null);

        Span<byte> result = stackalloc byte[8];
        result.Clear();
        result[0] = cachedResultKind switch
        {
            BiffCachedResultKind.String => 0,
            BiffCachedResultKind.Boolean => 1,
            BiffCachedResultKind.Error => 2,
            _ => 3,
        };
        result[2] = cachedResultKind is BiffCachedResultKind.Boolean or BiffCachedResultKind.Error ? cachedValue : (byte)0;
        result[6] = 0xFF;
        result[7] = 0xFF;
        WriteFormulaCore(row, column, xfIndex, result, tokens, flags);
    }

    /// <summary>
    /// Writes a <c>ROW</c> record.
    /// </summary>
    /// <param name="record">The row description.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the row or column fields are outside the 16-bit range.
    /// </exception>
    public void WriteRow(in BiffRowRecord record)
    {
        RequireCellIndex(record.Row, nameof(record));
        RequireCellIndex(record.FirstColumn, nameof(record));
        RequireCellIndex(record.LastColumnExclusive, nameof(record));

        Span<byte> payload = Begin((ushort)BiffRecordType.Row, BiffRowRecord.Length);
        payload.Clear();
        BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)record.Row);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(2), (ushort)record.FirstColumn);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(4), (ushort)record.LastColumnExclusive);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(6), record.HeightField);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(12), record.Options);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(14), record.XfField);
        Commit(BiffRowRecord.Length);
    }

    /// <summary>
    /// Writes a <c>DIMENSIONS</c> record in the layout of the version being emitted.
    /// </summary>
    /// <param name="record">The used range.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a field is negative, a column exceeds the 16-bit range, or under BIFF5 a row exceeds the 16-bit
    /// range.
    /// </exception>
    public void WriteDimensions(in BiffDimensionsRecord record)
    {
        ThrowHelper.ThrowIfNegative(record.FirstRow);
        ThrowHelper.ThrowIfNegative(record.LastRowExclusive);
        RequireCellIndex(record.FirstColumn, nameof(record));
        RequireCellIndex(record.LastColumnExclusive, nameof(record));

        if (_version == BiffVersion.Biff8)
        {
            Span<byte> payload = Begin((ushort)BiffRecordType.Dimensions, BiffDimensionsRecord.Biff8Length);
            payload.Clear();
            BinaryPrimitives.WriteUInt32LittleEndian(payload, (uint)record.FirstRow);
            BinaryPrimitives.WriteUInt32LittleEndian(payload.Slice(4), (uint)record.LastRowExclusive);
            BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(8), (ushort)record.FirstColumn);
            BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(10), (ushort)record.LastColumnExclusive);
            Commit(BiffDimensionsRecord.Biff8Length);
            return;
        }

        RequireCellIndex(record.FirstRow, nameof(record));
        RequireCellIndex(record.LastRowExclusive, nameof(record));

        Span<byte> narrow = Begin((ushort)BiffRecordType.Dimensions, BiffDimensionsRecord.Biff5Length);
        narrow.Clear();
        BinaryPrimitives.WriteUInt16LittleEndian(narrow, (ushort)record.FirstRow);
        BinaryPrimitives.WriteUInt16LittleEndian(narrow.Slice(2), (ushort)record.LastRowExclusive);
        BinaryPrimitives.WriteUInt16LittleEndian(narrow.Slice(4), (ushort)record.FirstColumn);
        BinaryPrimitives.WriteUInt16LittleEndian(narrow.Slice(6), (ushort)record.LastColumnExclusive);
        Commit(BiffDimensionsRecord.Biff5Length);
    }

    /// <summary>
    /// Writes an <c>XF</c> record carrying the font, format, and type fields; the alignment, border, and fill fields
    /// that follow are written as zeros.
    /// </summary>
    /// <param name="record">The extended format.</param>
    public void WriteXf(in BiffXfRecord record)
    {
        int length = _version == BiffVersion.Biff8 ? BiffXfRecord.Biff8Length : BiffXfRecord.Biff5Length;
        Span<byte> payload = Begin((ushort)BiffRecordType.Xf, length);
        payload.Clear();
        BinaryPrimitives.WriteUInt16LittleEndian(payload, record.FontIndex);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(2), record.FormatIndex);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(4), record.TypeField);
        Commit(length);
    }

    /// <summary>
    /// Writes a <c>FORMULA</c> record from its eight-byte cached result.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="xfIndex">The extended-format index.</param>
    /// <param name="result">The eight-byte cached result.</param>
    /// <param name="tokens">The parsed-expression tokens.</param>
    /// <param name="flags">The option flags.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when an index is outside the 16-bit range or the tokens do not fit the record.
    /// </exception>
    private void WriteFormulaCore(int row, int column, ushort xfIndex, scoped ReadOnlySpan<byte> result, scoped ReadOnlySpan<byte> tokens, ushort flags)
    {
        RequireCellIndex(row, nameof(row));
        RequireCellIndex(column, nameof(column));

        int length = 22 + tokens.Length;
        if (length > _maxPayloadLength || tokens.Length > ushort.MaxValue)
            throw PayloadTooLong(length, nameof(tokens));

        Span<byte> payload = Begin((ushort)BiffRecordType.Formula, length);
        payload.Clear();
        WriteCellPrefix(payload, row, column, xfIndex);
        result.CopyTo(payload.Slice(6));
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(14), flags);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(20), (ushort)tokens.Length);
        tokens.CopyTo(payload.Slice(22));
        Commit(length);
    }
}
