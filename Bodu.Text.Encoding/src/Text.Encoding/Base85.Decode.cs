// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85.Decode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class Base85
{

    /// <summary>
    /// Decodes a Base85 string into a byte array using the supplied variant.
    /// </summary>
    /// <param name="s">The Base85 input.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="style">Parsing styles. Only <see cref="BaseFormatStyles.IgnoreWhitespace" /> has effect.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="s" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="FormatException">Thrown when the input is not valid Base85 for the chosen variant.</exception>
    public static byte[] Decode(string s, Base85Variant variant = Base85Variant.Ascii85, BaseFormatStyles style = BaseFormatStyles.None)
    {
        ThrowHelper.ThrowIfNull(s);
        return Decode(s.AsSpan(), variant, style);
    }

    /// <summary>
    /// Decodes a Base85 character span into a byte array.
    /// </summary>
    /// <param name="chars">The character span.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="style">Parsing styles.</param>
    /// <returns>The decoded byte array. Returns <see cref="Array.Empty{T}" /> for empty input.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="FormatException">Thrown when the input is not valid Base85.</exception>
    public static byte[] Decode(ReadOnlySpan<char> chars, Base85Variant variant = Base85Variant.Ascii85, BaseFormatStyles style = BaseFormatStyles.None)
    {
        EnsureValidVariant(variant);

        if (chars.IsEmpty)
            return Array.Empty<byte>();

        var buffer = new byte[GetMaxDecodedLength(chars.Length)];

        if (!TryDecodeCore(chars, variant, style, buffer, out var written, out var error))
            throw new FormatException(error);

        if (written == buffer.Length)
            return buffer;

        var trimmed = new byte[written];
        Buffer.BlockCopy(buffer, 0, trimmed, 0, written);
        return trimmed;
    }

    /// <summary>
    /// Decodes a portion of a character array.
    /// </summary>
    /// <param name="chars">The character array.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="count">The number of characters.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="style">Parsing styles.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="chars" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" />, <paramref name="count" />, or <paramref name="variant" /> is out of
    /// range.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the segment defined by <paramref name="offset" /> and <paramref name="count" /> exceeds the
    /// available range of <paramref name="chars" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the input is not valid Base85.</exception>
    public static byte[] Decode(char[] chars, int offset, int count, Base85Variant variant = Base85Variant.Ascii85, BaseFormatStyles style = BaseFormatStyles.None)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(chars, offset, count);
        return Decode(chars.AsSpan(offset, count), variant, style);
    }

    /// <summary>
    /// Attempts to decode Base85 characters into a destination byte span.
    /// </summary>
    /// <param name="chars">The Base85 character span.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="style">Parsing styles.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the input is malformed or the destination is
    /// too small.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool TryDecode(ReadOnlySpan<char> chars, Span<byte> destination, out int bytesWritten, Base85Variant variant = Base85Variant.Ascii85, BaseFormatStyles style = BaseFormatStyles.None)
    {
        EnsureValidVariant(variant);
        bytesWritten = 0;

        if (chars.IsEmpty)
            return true;

        var buffer = new byte[GetMaxDecodedLength(chars.Length)];
        if (!TryDecodeCore(chars, variant, style, buffer, out var written, out _))
            return false;

        if (destination.Length < written)
            return false;

        buffer.AsSpan(0, written).CopyTo(destination);
        bytesWritten = written;
        return true;
    }

    /// <summary>
    /// Core decoder. Walks the input span, validating characters and accumulating bytes into <paramref name="buffer" />
    /// .
    /// </summary>
    /// <param name="chars">The input characters.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="style">The parsing styles.</param>
    /// <param name="buffer">The output buffer (must be at least <see cref="GetMaxDecodedLength" /> in size).</param>
    /// <param name="written">When this method returns, contains the number of bytes written.</param>
    /// <param name="error">When this method returns <see langword="false" />, contains the failure reason.</param>
    /// <returns><see langword="true" /> on success; <see langword="false" /> on failure.</returns>
    private static bool TryDecodeCore(ReadOnlySpan<char> chars, Base85Variant variant, BaseFormatStyles style, byte[] buffer, out int written, out string? error)
    {
        var lookup = GetLookup(variant);
        var ignoreWhitespace = style.HasFlag(BaseFormatStyles.IgnoreWhitespace);
        var allowShortcut = variant == Base85Variant.Ascii85;
        var requireFullGroups = variant == Base85Variant.Z85;

        if (allowShortcut && style.HasFlag(BaseFormatStyles.AllowPrefix))
            chars = StripAscii85Delimiters(chars, ignoreWhitespace);

        written = 0;
        error = null;

        uint accumulator = 0;
        var charsAccumulated = 0;

        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (ignoreWhitespace && c is ' ' or '\t' or '\r' or '\n')
                continue;

            if (allowShortcut && c == ZeroShortcut)
            {
                if (charsAccumulated != 0)
                {
                    error = "The 'z' shortcut may only appear at the start of a five-character group.";
                    written = 0;
                    return false;
                }

                buffer[written++] = 0;
                buffer[written++] = 0;
                buffer[written++] = 0;
                buffer[written++] = 0;
                continue;
            }

            if (c >= lookup.Length || lookup[c] < 0)
            {
                error = $"Input contains a character outside the Base85 variant alphabet: '{c}'.";
                written = 0;
                return false;
            }

            int symbolValue = lookup[c];

            // Detect overflow before it occurs: max accumulator after four digits is 85^4 = 52200625; after the fifth
            // (when symbolValue is added) we need (accumulator * 85 + symbolValue) <= uint.MaxValue.
            var next = ((ulong)accumulator * 85) + (uint)symbolValue;
            if (next > uint.MaxValue)
            {
                error = "Base85 group exceeds the maximum 32-bit value.";
                written = 0;
                return false;
            }

            accumulator = (uint)next;
            charsAccumulated++;

            if (charsAccumulated == 5)
            {
                buffer[written++] = (byte)((accumulator >> 24) & 0xFF);
                buffer[written++] = (byte)((accumulator >> 16) & 0xFF);
                buffer[written++] = (byte)((accumulator >> 8) & 0xFF);
                buffer[written++] = (byte)(accumulator & 0xFF);
                accumulator = 0;
                charsAccumulated = 0;
            }
        }

        if (charsAccumulated == 0)
            return true;

        if (requireFullGroups)
        {
            error = "Z85 input length must be a multiple of five characters.";
            written = 0;
            return false;
        }

        if (charsAccumulated == 1)
        {
            error = "A single trailing Base85 character cannot decode to a complete byte.";
            written = 0;
            return false;
        }

        // Adobe Ascii85 tail handling: pad with 'u' (digit 84) to fill the group, then emit charsAccumulated - 1 bytes.
        for (var i = charsAccumulated; i < 5; i++)
        {
            var next = ((ulong)accumulator * 85) + 84;
            if (next > uint.MaxValue)
            {
                error = "Trailing Base85 group overflows the 32-bit accumulator after padding.";
                written = 0;
                return false;
            }

            accumulator = (uint)next;
        }

        var bytesFromTail = charsAccumulated - 1;
        for (var i = 0; i < bytesFromTail; i++)
        {
            buffer[written++] = (byte)((accumulator >> ((3 - i) * 8)) & 0xFF);
        }

        return true;
    }
}
