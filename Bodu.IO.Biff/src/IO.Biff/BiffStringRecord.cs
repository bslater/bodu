// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffStringRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>STRING</c> record: the cached text result of the <c>FORMULA</c> record that precedes it. The
/// text is a 16-bit-length Unicode string in BIFF8 and a 16-bit-length code-page byte string in BIFF5.
/// </summary>
/// <remarks>
/// <para>
/// The record identifier is <see cref="BiffRecordType.String" />. The payload is the string structure alone. The record
/// has no position of its own: it belongs to the <see cref="BiffFormulaRecord" /> immediately before it, whose
/// <see cref="BiffFormulaRecord.CachedResultKind" /> is <see cref="BiffCachedResultKind.String" />.
/// </para>
/// <para>
/// A long text continues in <c>CONTINUE</c> records after the <c>STRING</c> record ([MS-XLS] 2.1.7.20.6), each
/// continued segment opening with its own flags byte, as in the shared string table.
/// <see cref="BiffReader.GetString" /> decodes the <c>STRING</c> record alone, so it rejects a text whose characters
/// run past the record.
/// </para>
/// </remarks>
/// <seealso cref="BiffReader.GetString" /> <seealso cref="BiffWriter.WriteString(ReadOnlySpan{char})" />
/// <seealso cref="BiffFormulaRecord" />
public readonly ref struct BiffStringRecord
{
    /// <summary>The offset of the first character in a BIFF8 payload: the 16-bit character count and the flags byte.</summary>
    private const int Biff8CharactersOffset = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffStringRecord" /> struct.
    /// </summary>
    /// <param name="text">The cached text.</param>
    private BiffStringRecord(BiffString text)
    {
        Text = text;
    }

    /// <summary>
    /// Gets the cached text.
    /// </summary>
    /// <value>The text as a span-backed view.</value>
    /// <remarks>
    /// In BIFF8 the text is an <c>XLUnicodeString</c>: only bit 0 of its flags byte, which selects 16-bit characters,
    /// is read, and the other bits are ignored, so the view carries no rich-text runs or extended data.
    /// </remarks>
    public BiffString Text { get; }

    /// <summary>
    /// Decodes a <c>STRING</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="version">The stream version, which selects the text representation.</param>
    /// <param name="codePage">The code page for BIFF5 text.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    internal static BiffStringRecord Read(ReadOnlySpan<byte> payload, BiffVersion version, int codePage) =>
        new(BiffString.Read(payload, 0, wideLength: true, version, codePage, BiffRecordType.String));

    /// <summary>
    /// Reads the text of the <c>STRING</c> record a reader is positioned on, together with the <c>CONTINUE</c> records
    /// that carry the rest of its characters.
    /// </summary>
    /// <param name="reader">
    /// The reader, positioned on a <c>STRING</c> record; when the text continues, it is left on the last
    /// <c>CONTINUE</c> record the text spans.
    /// </param>
    /// <returns>The text.</returns>
    /// <exception cref="BiffFormatException">
    /// Thrown when the string header is truncated, the characters run past the record with no <c>CONTINUE</c> record to
    /// carry the rest, a <c>CONTINUE</c> record holds no bytes, or a 16-bit segment ends inside a code unit.
    /// </exception>
    /// <remarks>
    /// In BIFF8 each continued segment opens with its own flags byte, whose bit 0 selects the width of the characters
    /// that follow it, and a segment that holds only that flags byte contributes no characters, as in the shared string
    /// table. A text that lies within the <c>STRING</c> record, and any BIFF5 text, is read as
    /// <see cref="BiffReader.GetString" /> reads it.
    /// </remarks>
    internal static string ReadContinuedText(scoped ref BiffReader reader)
    {
        ReadOnlySpan<byte> payload = reader.ValueSpan;
        if (reader.Version != BiffVersion.Biff8 || payload.Length < Biff8CharactersOffset)
            return reader.GetString().Text.GetString();

        int length = BinaryPrimitives.ReadUInt16LittleEndian(payload);
        bool highByte = (payload[2] & 0x01) != 0;
        if ((highByte ? length * 2 : length) <= payload.Length - Biff8CharactersOffset)
            return reader.GetString().Text.GetString();

        var characters = new char[length];
        int written = 0;
        ReadOnlySpan<byte> segment = payload[Biff8CharactersOffset..];
        while (true)
        {
            int take = Math.Min(length - written, highByte ? segment.Length / 2 : segment.Length);
            if (highByte)
                _ = Encoding.Unicode.GetChars(segment[..(take * 2)], characters.AsSpan(written, take));
            else
                _ = Encoding.Latin1.GetChars(segment[..take], characters.AsSpan(written, take));

            written += take;
            if (written == length)
                return new string(characters);

            // The segment is spent and characters remain: a 16-bit segment of odd length split a code unit.
            if (highByte && segment.Length % 2 != 0)
                throw BiffPayload.Malformed(BiffRecordType.Continue);

            if (!reader.TryReadContinuation(out ReadOnlySpan<byte> next))
            {
                throw new BiffFormatException(
                    string.Format(CultureInfo.CurrentCulture, BiffResourceStrings.Format_Invalid_BiffContinueExpected, BiffRecordType.String));
            }

            // A continued segment restarts with its own flags byte, which an empty CONTINUE record cannot carry.
            if (next.IsEmpty)
                throw BiffPayload.Malformed(BiffRecordType.Continue);

            highByte = (next[0] & 0x01) != 0;
            segment = next[1..];
        }
    }
}
