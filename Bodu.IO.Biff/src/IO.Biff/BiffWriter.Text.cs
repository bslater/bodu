// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriter.Text.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Bodu.IO.Biff;

public ref partial struct BiffWriter
{
    /// <summary>The offset of the counts within an SST payload.</summary>
    private const int SstCountsLength = 8;

    /// <summary>
    /// Writes a <c>LABEL</c> record: an inline text cell.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="xfIndex">The extended-format index of the cell.</param>
    /// <param name="text">The cell text.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="row" /> or <paramref name="column" /> is outside the 16-bit range, or the encoded
    /// text does not fit the record.
    /// </exception>
    public void WriteLabel(int row, int column, ushort xfIndex, scoped ReadOnlySpan<char> text)
    {
        RequireCellIndex(row, nameof(row));
        RequireCellIndex(column, nameof(column));

        int textLength = MeasureText(text, wideLength: true);
        Span<byte> payload = Begin((ushort)BiffRecordType.Label, 6 + textLength, textLength, nameof(text));
        WriteCellPrefix(payload, row, column, xfIndex);
        EncodeText(payload.Slice(6), text, wideLength: true);
        Commit(payload.Length);
    }

    /// <summary>
    /// Writes a <c>LABELSST</c> record: a text cell referencing the shared string table (BIFF8 only).
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="xfIndex">The extended-format index of the cell.</param>
    /// <param name="sstIndex">The zero-based index of the text in the shared string table.</param>
    /// <exception cref="InvalidOperationException">Thrown when the writer emits BIFF5.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="row" /> or <paramref name="column" /> is outside the 16-bit range.
    /// </exception>
    public void WriteLabelSst(int row, int column, ushort xfIndex, uint sstIndex)
    {
        RequireBiff8(BiffRecordType.LabelSst);
        RequireCellIndex(row, nameof(row));
        RequireCellIndex(column, nameof(column));

        Span<byte> payload = Begin((ushort)BiffRecordType.LabelSst, BiffLabelSstRecord.Length);
        WriteCellPrefix(payload, row, column, xfIndex);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.Slice(6), sstIndex);
        Commit(BiffLabelSstRecord.Length);
    }

    /// <summary>
    /// Writes a <c>STRING</c> record: the cached text result of the preceding formula.
    /// </summary>
    /// <param name="text">The cached text.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded text does not fit the record.</exception>
    public void WriteString(scoped ReadOnlySpan<char> text)
    {
        int textLength = MeasureText(text, wideLength: true);
        Span<byte> payload = Begin((ushort)BiffRecordType.String, textLength, textLength, nameof(text));
        EncodeText(payload, text, wideLength: true);
        Commit(payload.Length);
    }

    /// <summary>
    /// Writes a <c>BOUNDSHEET</c> record: one entry of the sheet directory.
    /// </summary>
    /// <param name="streamOffset">
    /// The absolute offset of the sheet's <c>BOF</c> record within the workbook stream.
    /// </param>
    /// <param name="state">The sheet's visibility.</param>
    /// <param name="sheetType">The kind of sheet.</param>
    /// <param name="name">The sheet name.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the encoded name is longer than 255 bytes or characters.
    /// </exception>
    public void WriteBoundSheet(uint streamOffset, BiffSheetState state, BiffSheetType sheetType, scoped ReadOnlySpan<char> name)
    {
        int nameLength = MeasureText(name, wideLength: false);
        Span<byte> payload = Begin((ushort)BiffRecordType.BoundSheet, 6 + nameLength, nameLength, nameof(name));
        BinaryPrimitives.WriteUInt32LittleEndian(payload, streamOffset);
        payload[4] = (byte)((byte)state & 0x03);
        payload[5] = (byte)sheetType;
        EncodeText(payload.Slice(6), name, wideLength: false);
        Commit(payload.Length);
    }

    /// <summary>
    /// Writes a <c>FORMAT</c> record: a number-format index and its code.
    /// </summary>
    /// <param name="formatIndex">The number-format index.</param>
    /// <param name="code">The format code.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded code does not fit the record.</exception>
    public void WriteFormat(ushort formatIndex, scoped ReadOnlySpan<char> code)
    {
        bool wideLength = _version == BiffVersion.Biff8;
        int codeLength = MeasureText(code, wideLength);
        Span<byte> payload = Begin((ushort)BiffRecordType.Format, 2 + codeLength, codeLength, nameof(code));
        BinaryPrimitives.WriteUInt16LittleEndian(payload, formatIndex);
        EncodeText(payload.Slice(2), code, wideLength);
        Commit(payload.Length);
    }

    /// <summary>
    /// Writes a <c>FONT</c> record.
    /// </summary>
    /// <param name="height">The font height in twips.</param>
    /// <param name="attributes">
    /// The attribute flags: italic (bit 1), strikeout (bit 3), outline (bit 4), shadow (bit 5).
    /// </param>
    /// <param name="colorIndex">The palette color index; <c>0x7FFF</c> for automatic.</param>
    /// <param name="weight">The weight: 400 normal, 700 bold.</param>
    /// <param name="escapement">The escapement: 0 none, 1 superscript, 2 subscript.</param>
    /// <param name="underline">The underline type.</param>
    /// <param name="family">The font family.</param>
    /// <param name="characterSet">The character set.</param>
    /// <param name="name">The face name.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the encoded name is longer than 255 bytes or characters.
    /// </exception>
    public void WriteFont(ushort height, ushort attributes, ushort colorIndex, ushort weight, ushort escapement, byte underline, byte family, byte characterSet, scoped ReadOnlySpan<char> name)
    {
        int nameLength = MeasureText(name, wideLength: false);
        Span<byte> payload = Begin((ushort)BiffRecordType.Font, 14 + nameLength, nameLength, nameof(name));
        payload.Slice(0, 14).Clear();
        BinaryPrimitives.WriteUInt16LittleEndian(payload, height);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(2), attributes);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(4), colorIndex);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(6), weight);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(8), escapement);
        payload[10] = underline;
        payload[11] = family;
        payload[12] = characterSet;
        EncodeText(payload.Slice(14), name, wideLength: false);
        Commit(payload.Length);
    }

    /// <summary>
    /// Writes a shared string table (<c>SST</c>) from the supplied unique strings, splitting it into <c>CONTINUE</c>
    /// records as the format requires (BIFF8 only).
    /// </summary>
    /// <param name="strings">The unique strings, in the order cells reference them.</param>
    /// <exception cref="InvalidOperationException">Thrown when the writer emits BIFF5.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a string is longer than 65,535 characters.</exception>
    /// <remarks>
    /// The total reference count is recorded as the number of strings; use
    /// <see cref="WriteSst(ReadOnlySpan{string}, uint)" /> to record the actual number of <c>LABELSST</c> cells.
    /// </remarks>
    public void WriteSst(scoped ReadOnlySpan<string> strings) =>
        WriteSst(strings, (uint)strings.Length);

    /// <summary>
    /// Writes a shared string table (<c>SST</c>) from the supplied unique strings and reference count, splitting it
    /// into <c>CONTINUE</c> records as the format requires (BIFF8 only).
    /// </summary>
    /// <param name="strings">The unique strings, in the order cells reference them.</param>
    /// <param name="totalReferenceCount">The number of <c>LABELSST</c> cells that reference the table.</param>
    /// <exception cref="InvalidOperationException">Thrown when the writer emits BIFF5.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a string is longer than 65,535 characters.</exception>
    /// <remarks>
    /// A string's header is never split across records; its characters may be, at a character boundary, and each
    /// continued run restarts with its own flags byte — the rules <see cref="BiffSstReader" /> decodes by.
    /// </remarks>
    public void WriteSst(scoped ReadOnlySpan<string> strings, uint totalReferenceCount)
    {
        RequireBiff8(BiffRecordType.Sst);

        byte[] block = ArrayPool<byte>.Shared.Rent(_maxPayloadLength);
        try
        {
            var table = new SstBlockWriter(block.AsSpan(0, _maxPayloadLength));
            BinaryPrimitives.WriteUInt32LittleEndian(table.Block, totalReferenceCount);
            BinaryPrimitives.WriteUInt32LittleEndian(table.Block.Slice(4), (uint)strings.Length);
            table.Used = SstCountsLength;

            foreach (string value in strings)
            {
                ThrowHelper.ThrowIfNull(value);
                if (value.Length > ushort.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(strings),
                        value.Length,
                        string.Format(CultureInfo.CurrentCulture, BiffResourceStrings.Arg_OutOfRange_BiffStringTooLong, value.Length, ushort.MaxValue));
                }

                WriteSstString(ref table, value);
            }

            FlushSstBlock(ref table);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(block);
        }
    }

    /// <summary>
    /// Appends one string to the table, flushing and continuing blocks as its header and characters require.
    /// </summary>
    /// <param name="table">The block state.</param>
    /// <param name="value">The string.</param>
    private void WriteSstString(ref SstBlockWriter table, string value)
    {
        bool wide = NeedsWideCharacters(value);
        int charSize = wide ? 2 : 1;

        // The header (length + flags) must not straddle a boundary.
        if (table.Remaining < 3)
            FlushSstBlock(ref table);

        BinaryPrimitives.WriteUInt16LittleEndian(table.Block.Slice(table.Used), (ushort)value.Length);
        table.Block[table.Used + 2] = (byte)(wide ? 0x01 : 0x00);
        table.Used += 3;

        int written = 0;
        while (written < value.Length)
        {
            int available = table.Remaining / charSize;
            if (available == 0)
            {
                // Continue in a new record, which restarts with its own flags byte.
                FlushSstBlock(ref table);
                table.Block[table.Used] = (byte)(wide ? 0x01 : 0x00);
                table.Used += 1;
                available = table.Remaining / charSize;
            }

            int take = Math.Min(available, value.Length - written);
            Span<byte> destination = table.Block.Slice(table.Used, take * charSize);
            if (wide)
            {
                Encoding.Unicode.GetBytes(value.AsSpan(written, take), destination);
            }
            else
            {
                for (int i = 0; i < take; i++)
                    destination[i] = (byte)value[written + i];
            }

            table.Used += take * charSize;
            written += take;
        }
    }

    /// <summary>
    /// Emits the current block as the <c>SST</c> record (first) or a <c>CONTINUE</c> record and starts a new block.
    /// </summary>
    /// <param name="table">The block state.</param>
    private void FlushSstBlock(ref SstBlockWriter table)
    {
        WriteRecord(table.IsFirst ? BiffRecordType.Sst : BiffRecordType.Continue, table.Block.Slice(0, table.Used));
        table.IsFirst = false;
        table.Used = 0;
    }

    /// <summary>
    /// Determines whether any character of the text falls outside the compressed (8-bit) range.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns><see langword="true" /> when 16-bit characters are needed.</returns>
    private static bool NeedsWideCharacters(ReadOnlySpan<char> text)
    {
        foreach (char c in text)
        {
            if (c > 0xFF)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Computes the encoded size of a string structure — length prefix, flags, and characters — under the version being
    /// emitted.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="wideLength">Whether the length prefix is 16 bits (<see langword="true" />) or 8 bits.</param>
    /// <returns>The encoded size in bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the character count (BIFF8) or byte count (BIFF5) exceeds what the length prefix can express.
    /// </exception>
    private readonly int MeasureText(ReadOnlySpan<char> text, bool wideLength)
    {
        int max = wideLength ? ushort.MaxValue : byte.MaxValue;
        int units;
        int bytes;
        if (_version == BiffVersion.Biff8)
        {
            units = text.Length;
            bytes = NeedsWideCharacters(text) ? text.Length * 2 : text.Length;
            bytes += 1;
        }
        else
        {
            units = BiffTextEncoding.GetEncoding(_codePage).GetByteCount(text);
            bytes = units;
        }

        if (units > max)
        {
            throw new ArgumentOutOfRangeException(
                nameof(text),
                units,
                string.Format(CultureInfo.CurrentCulture, BiffResourceStrings.Arg_OutOfRange_BiffStringTooLong, units, max));
        }

        return (wideLength ? 2 : 1) + bytes;
    }

    /// <summary>
    /// Encodes a string structure into the destination, in the layout <see cref="MeasureText" /> sized.
    /// </summary>
    /// <param name="destination">The buffer sized by <see cref="MeasureText" />.</param>
    /// <param name="text">The text.</param>
    /// <param name="wideLength">Whether the length prefix is 16 bits.</param>
    private readonly void EncodeText(Span<byte> destination, ReadOnlySpan<char> text, bool wideLength)
    {
        int cursor;
        if (_version == BiffVersion.Biff8)
        {
            cursor = WriteLengthPrefix(destination, text.Length, wideLength);
            bool wide = NeedsWideCharacters(text);
            destination[cursor++] = (byte)(wide ? 0x01 : 0x00);
            if (wide)
            {
                Encoding.Unicode.GetBytes(text, destination.Slice(cursor));
            }
            else
            {
                for (int i = 0; i < text.Length; i++)
                    destination[cursor + i] = (byte)text[i];
            }

            return;
        }

        Encoding encoding = BiffTextEncoding.GetEncoding(_codePage);
        int byteCount = encoding.GetByteCount(text);
        cursor = WriteLengthPrefix(destination, byteCount, wideLength);
        encoding.GetBytes(text, destination.Slice(cursor));
    }

    /// <summary>
    /// Writes an 8- or 16-bit length prefix.
    /// </summary>
    /// <param name="destination">The buffer.</param>
    /// <param name="length">The length to record.</param>
    /// <param name="wideLength">Whether the prefix is 16 bits.</param>
    /// <returns>The number of bytes written.</returns>
    private static int WriteLengthPrefix(Span<byte> destination, int length, bool wideLength)
    {
        if (wideLength)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(destination, (ushort)length);
            return 2;
        }

        destination[0] = (byte)length;
        return 1;
    }

    /// <summary>
    /// Reserves a record whose variable part must fit the version's maximum payload.
    /// </summary>
    /// <param name="recordId">The record identifier.</param>
    /// <param name="payloadLength">The total payload length.</param>
    /// <param name="variableLength">The length of the variable part, reported when the record is too long.</param>
    /// <param name="paramName">The name of the parameter that carried the variable part.</param>
    /// <returns>The span that receives the payload.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the payload exceeds the maximum.</exception>
    private readonly Span<byte> Begin(ushort recordId, int payloadLength, int variableLength, string paramName)
    {
        if (payloadLength > _maxPayloadLength)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                variableLength,
                string.Format(CultureInfo.CurrentCulture, BiffResourceStrings.Arg_OutOfRange_BiffPayloadTooLong, payloadLength, _version, _maxPayloadLength));
        }

        return Begin(recordId, payloadLength);
    }

    /// <summary>
    /// Tracks the block being filled while a shared string table is written.
    /// </summary>
    private ref struct SstBlockWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SstBlockWriter" /> struct.
        /// </summary>
        /// <param name="block">The buffer holding one record's payload.</param>
        public SstBlockWriter(Span<byte> block)
        {
            Block = block;
            Used = 0;
            IsFirst = true;
        }

        /// <summary>
        /// Gets the buffer holding one record's payload.
        /// </summary>
        public Span<byte> Block { get; }

        /// <summary>
        /// Gets or sets the number of bytes used in the block.
        /// </summary>
        public int Used { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the block will be emitted as the SST record itself.
        /// </summary>
        public bool IsFirst { get; set; }

        /// <summary>
        /// Gets the number of free bytes in the block.
        /// </summary>
        public readonly int Remaining => Block.Length - Used;
    }
}
