// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

namespace Bodu.IO.Biff;

/// <summary>
/// Provides a span-backed view over a text value embedded in a BIFF record — a BIFF8 Unicode string (16-bit or
/// compressed 8-bit characters with option flags, rich-text runs, and extended data) or a BIFF5 code-page byte string —
/// without materializing a <see cref="string" /> until the caller asks for one.
/// </summary>
/// <remarks>
/// <para>
/// BIFF8 stores text as UTF-16LE code units or, when every character has a zero high byte, as one low byte per
/// character; a flags byte preceding the characters selects the form (<see cref="IsHighByte" />) and announces the
/// optional formatting-run and phonetic (extended) trailers. BIFF5 stores text as bytes in the code page declared by
/// the stream's <c>CODEPAGE</c> record; the view carries that code page so <see cref="GetString()" /> needs no
/// argument.
/// </para>
/// <para>
/// The view is a <see langword="ref struct" /> over the record payload and must not outlive the buffer it was read
/// from.
/// </para>
/// </remarks>
public readonly ref struct BiffString
{
    /// <summary>The raw character bytes.</summary>
    private readonly ReadOnlySpan<byte> _characters;

    /// <summary>The rich-text formatting runs that follow the characters (BIFF8 only).</summary>
    private readonly ReadOnlySpan<byte> _richRuns;

    /// <summary>The extended (phonetic) data that follows the runs (BIFF8 only).</summary>
    private readonly ReadOnlySpan<byte> _extendedData;

    /// <summary>The character count the string header declared.</summary>
    private readonly int _length;

    /// <summary>The code page used to decode a byte string.</summary>
    private readonly int _codePage;

    /// <summary>The total number of bytes the string structure occupies, header included.</summary>
    private readonly int _encodedLength;

    /// <summary>The number of rich-text runs declared by the header.</summary>
    private readonly int _richRunCount;

    /// <summary>Whether the string is in the BIFF8 Unicode form rather than a BIFF5 byte string.</summary>
    private readonly bool _isUnicode;

    /// <summary>Whether the characters are 16-bit code units rather than compressed 8-bit values.</summary>
    private readonly bool _isHighByte;

    /// <summary>Whether the header declared extended data.</summary>
    private readonly bool _hasExtendedData;

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffString" /> struct.
    /// </summary>
    /// <param name="characters">The raw character bytes.</param>
    /// <param name="length">The character count the header declared.</param>
    /// <param name="isUnicode">Whether the string is a BIFF8 Unicode string.</param>
    /// <param name="isHighByte">Whether the characters are 16-bit code units.</param>
    /// <param name="richRuns">The rich-text run bytes.</param>
    /// <param name="richRunCount">The number of rich-text runs.</param>
    /// <param name="extendedData">The extended data bytes.</param>
    /// <param name="hasExtendedData">Whether extended data was declared.</param>
    /// <param name="codePage">The code page for a byte string.</param>
    /// <param name="encodedLength">The total encoded size, header included.</param>
    private BiffString(
        ReadOnlySpan<byte> characters,
        int length,
        bool isUnicode,
        bool isHighByte,
        ReadOnlySpan<byte> richRuns,
        int richRunCount,
        ReadOnlySpan<byte> extendedData,
        bool hasExtendedData,
        int codePage,
        int encodedLength)
    {
        _characters = characters;
        _length = length;
        _isUnicode = isUnicode;
        _isHighByte = isHighByte;
        _richRuns = richRuns;
        _richRunCount = richRunCount;
        _extendedData = extendedData;
        _hasExtendedData = hasExtendedData;
        _codePage = codePage;
        _encodedLength = encodedLength;
    }

    /// <summary>
    /// Gets the character count the string header declared.
    /// </summary>
    /// <value>
    /// For a Unicode string, the number of UTF-16 code units; for a byte string, the number of bytes, which equals the
    /// number of characters only for single-byte code pages.
    /// </value>
    public int Length => _length;

    /// <summary>
    /// Gets a value indicating whether the string holds no characters.
    /// </summary>
    /// <value><see langword="true" /> when <see cref="Length" /> is zero.</value>
    public bool IsEmpty => _length == 0;

    /// <summary>
    /// Gets a value indicating whether the string is in the BIFF8 Unicode form.
    /// </summary>
    /// <value>
    /// <see langword="true" /> for a BIFF8 Unicode string; <see langword="false" /> for a BIFF5 code-page byte string.
    /// </value>
    public bool IsUnicode => _isUnicode;

    /// <summary>
    /// Gets a value indicating whether the characters are stored as 16-bit code units.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when each character occupies two bytes; <see langword="false" /> for the compressed
    /// 8-bit form and for byte strings.
    /// </value>
    public bool IsHighByte => _isHighByte;

    /// <summary>
    /// Gets a value indicating whether the string carries rich-text formatting runs.
    /// </summary>
    /// <value><see langword="true" /> when the header's rich-text flag is set.</value>
    public bool HasRichRuns => _richRunCount > 0;

    /// <summary>
    /// Gets the number of rich-text formatting runs.
    /// </summary>
    /// <value>The run count, or zero when the string is not rich text.</value>
    public int RichRunCount => _richRunCount;

    /// <summary>
    /// Gets the rich-text formatting runs as raw bytes: four bytes per run, a 16-bit character index followed by a
    /// 16-bit font index.
    /// </summary>
    /// <value>The run bytes, or an empty span.</value>
    public ReadOnlySpan<byte> RichRuns => _richRuns;

    /// <summary>
    /// Gets a value indicating whether the string carries extended (phonetic) data.
    /// </summary>
    /// <value><see langword="true" /> when the header's extended-data flag is set.</value>
    public bool HasExtendedData => _hasExtendedData;

    /// <summary>
    /// Gets the extended (phonetic) data as raw bytes.
    /// </summary>
    /// <value>The extended data, or an empty span.</value>
    public ReadOnlySpan<byte> ExtendedData => _extendedData;

    /// <summary>
    /// Gets the raw character bytes exactly as stored: UTF-16LE code units, compressed low bytes, or code-page bytes.
    /// </summary>
    /// <value>The character bytes.</value>
    public ReadOnlySpan<byte> RawCharacters => _characters;

    /// <summary>
    /// Gets the code page used to decode a byte string.
    /// </summary>
    /// <value>The Windows code page number; unused for a Unicode string.</value>
    public int CodePage => _codePage;

    /// <summary>
    /// Gets the total number of bytes the string structure occupies in its record, including its length prefix, flags,
    /// and trailers.
    /// </summary>
    /// <value>The encoded size in bytes.</value>
    public int EncodedLength => _encodedLength;

    /// <summary>
    /// Gets the number of characters <see cref="GetString()" /> produces.
    /// </summary>
    /// <returns>The decoded character count.</returns>
    /// <exception cref="BiffFormatException">Thrown when a byte string's code page cannot be resolved.</exception>
    public int GetCharCount()
    {
        if (_isUnicode)
            return _isHighByte ? _characters.Length / 2 : _characters.Length;

        return BiffTextEncoding.GetEncoding(_codePage).GetCharCount(_characters);
    }

    /// <summary>
    /// Decodes the characters into a new <see cref="string" />.
    /// </summary>
    /// <returns>The decoded text.</returns>
    /// <exception cref="BiffFormatException">Thrown when a byte string's code page cannot be resolved.</exception>
    public string GetString()
    {
        if (_characters.IsEmpty)
            return string.Empty;

        return SelectEncoding().GetString(_characters);
    }

    /// <summary>
    /// Decodes the characters into the supplied destination.
    /// </summary>
    /// <param name="destination">The buffer that receives the decoded characters.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is shorter than <see cref="GetCharCount" />.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when a byte string's code page cannot be resolved.</exception>
    public int CopyTo(Span<char> destination)
    {
        Encoding encoding = SelectEncoding();
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, encoding.GetCharCount(_characters));

        return encoding.GetChars(_characters, destination);
    }

    /// <summary>
    /// Returns the decoded text.
    /// </summary>
    /// <returns>The decoded text.</returns>
    public override string ToString() =>
        GetString();

    /// <summary>
    /// Reads a string in the representation the specified version uses.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="offset">The byte offset at which the string's length prefix begins.</param>
    /// <param name="wideLength">Whether the length prefix is 16 bits (<see langword="true" />) or 8 bits.</param>
    /// <param name="version">The BIFF version that selects the representation.</param>
    /// <param name="codePage">The code page for a BIFF5 byte string.</param>
    /// <param name="type">The record type, used to compose the failure message.</param>
    /// <returns>The string view.</returns>
    /// <exception cref="BiffFormatException">
    /// Thrown when the string header or characters run past the payload.
    /// </exception>
    internal static BiffString Read(ReadOnlySpan<byte> payload, int offset, bool wideLength, BiffVersion version, int codePage, BiffRecordType type) =>
        version == BiffVersion.Biff8
            ? ReadUnicode(payload, offset, wideLength, type)
            : ReadByteString(payload, offset, wideLength, codePage, type);

    /// <summary>
    /// Reads a BIFF8 Unicode string.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="offset">The byte offset at which the length prefix begins.</param>
    /// <param name="wideLength">Whether the length prefix is 16 bits (<see langword="true" />) or 8 bits.</param>
    /// <param name="type">The record type, used to compose the failure message.</param>
    /// <returns>The string view.</returns>
    /// <exception cref="BiffFormatException">
    /// Thrown when the string header, characters, or trailers run past the payload.
    /// </exception>
    internal static BiffString ReadUnicode(ReadOnlySpan<byte> payload, int offset, bool wideLength, BiffRecordType type)
    {
        int cursor = offset;
        int length;
        if (wideLength)
        {
            length = BiffPayload.ReadUInt16(payload, cursor, type);
            cursor += 2;
        }
        else
        {
            length = BiffPayload.ReadByte(payload, cursor, type);
            cursor += 1;
        }

        byte flags = BiffPayload.ReadByte(payload, cursor, type);
        cursor += 1;

        bool highByte = (flags & 0x01) != 0;
        bool hasExtended = (flags & 0x04) != 0;
        bool hasRich = (flags & 0x08) != 0;

        int runCount = 0;
        if (hasRich)
        {
            runCount = BiffPayload.ReadUInt16(payload, cursor, type);
            cursor += 2;
        }

        int extendedLength = 0;
        if (hasExtended)
        {
            uint declared = BiffPayload.ReadUInt32(payload, cursor, type);
            if (declared > int.MaxValue)
                throw BiffPayload.Malformed(type);

            extendedLength = (int)declared;
            cursor += 4;
        }

        int byteCount = highByte ? length * 2 : length;
        ReadOnlySpan<byte> characters = Slice(payload, cursor, byteCount, type);
        cursor += byteCount;

        ReadOnlySpan<byte> runs = Slice(payload, cursor, runCount * 4, type);
        cursor += runCount * 4;

        ReadOnlySpan<byte> extended = Slice(payload, cursor, extendedLength, type);
        cursor += extendedLength;

        return new BiffString(characters, length, isUnicode: true, highByte, runs, runCount, extended, hasExtended, BiffLimits.UnicodeCodePage, cursor - offset);
    }

    /// <summary>
    /// Reads a BIFF5 code-page byte string.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="offset">The byte offset at which the length prefix begins.</param>
    /// <param name="wideLength">Whether the length prefix is 16 bits (<see langword="true" />) or 8 bits.</param>
    /// <param name="codePage">The code page the bytes are encoded in.</param>
    /// <param name="type">The record type, used to compose the failure message.</param>
    /// <returns>The string view.</returns>
    /// <exception cref="BiffFormatException">Thrown when the length prefix or bytes run past the payload.</exception>
    internal static BiffString ReadByteString(ReadOnlySpan<byte> payload, int offset, bool wideLength, int codePage, BiffRecordType type)
    {
        int cursor = offset;
        int length;
        if (wideLength)
        {
            length = BiffPayload.ReadUInt16(payload, cursor, type);
            cursor += 2;
        }
        else
        {
            length = BiffPayload.ReadByte(payload, cursor, type);
            cursor += 1;
        }

        ReadOnlySpan<byte> characters = Slice(payload, cursor, length, type);
        cursor += length;

        return new BiffString(characters, length, isUnicode: false, isHighByte: false, default, 0, default, hasExtendedData: false, codePage, cursor - offset);
    }

    /// <summary>
    /// Creates a view over characters whose header has already been consumed, as the shared-string-table reader does.
    /// </summary>
    /// <param name="characters">The raw character bytes.</param>
    /// <param name="length">The declared character count.</param>
    /// <param name="isHighByte">Whether the characters are 16-bit code units.</param>
    /// <param name="richRuns">The rich-text run bytes.</param>
    /// <param name="richRunCount">The number of rich-text runs.</param>
    /// <param name="extendedData">The extended data bytes.</param>
    /// <param name="hasExtendedData">Whether extended data was declared.</param>
    /// <param name="encodedLength">The total encoded size, header included.</param>
    /// <returns>The Unicode string view.</returns>
    internal static BiffString FromUnicodeCharacters(
        ReadOnlySpan<byte> characters,
        int length,
        bool isHighByte,
        ReadOnlySpan<byte> richRuns,
        int richRunCount,
        ReadOnlySpan<byte> extendedData,
        bool hasExtendedData,
        int encodedLength) =>
        new(characters, length, isUnicode: true, isHighByte, richRuns, richRunCount, extendedData, hasExtendedData, BiffLimits.UnicodeCodePage, encodedLength);

    /// <summary>
    /// Slices a payload, failing with a format exception rather than an argument exception on overrun.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="offset">The start offset.</param>
    /// <param name="length">The slice length.</param>
    /// <param name="type">The record type, used to compose the failure message.</param>
    /// <returns>The slice.</returns>
    /// <exception cref="BiffFormatException">Thrown when the slice would run past the payload.</exception>
    private static ReadOnlySpan<byte> Slice(ReadOnlySpan<byte> payload, int offset, int length, BiffRecordType type)
    {
        if (length < 0 || offset < 0 || (long)offset + length > payload.Length)
            throw new BiffFormatException(BiffResourceStrings.Format_Invalid_BiffString, BiffPayload.Malformed(type));

        return payload.Slice(offset, length);
    }

    /// <summary>
    /// Selects the encoding that decodes <see cref="RawCharacters" />.
    /// </summary>
    /// <returns>The encoding.</returns>
    private Encoding SelectEncoding()
    {
        if (_isUnicode)
            return _isHighByte ? Encoding.Unicode : Encoding.Latin1;

        return BiffTextEncoding.GetEncoding(_codePage);
    }
}
