// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16.Decode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class Base16
{
    /// <summary>
    /// The maximum scratch-buffer size that may be allocated on the stack during lenient decoding. Inputs larger than
    /// this fall back to a heap allocation to avoid stack overflow.
    /// </summary>
    private const int StackallocThreshold = 256;

    /// <summary>
    /// Decodes a hexadecimal-encoded string into a byte array.
    /// </summary>
    /// <param name="s">The string containing Base16 (hex) characters.</param>
    /// <param name="style">Parsing styles that allow optional prefix and whitespace tolerance.</param>
    /// <returns>A new byte array representing the decoded binary data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="s" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">
    /// Thrown when the input contains non-hexadecimal characters, or when it has an odd number of hex digits after
    /// applying <paramref name="style" />.
    /// </exception>
    public static byte[] Decode(string s, BaseFormatStyles style = BaseFormatStyles.None)
    {
        ThrowHelper.ThrowIfNull(s);
        return Decode(s.AsSpan(), style);
    }

    /// <summary>
    /// Decodes a portion of a character array containing hexadecimal-encoded data into a byte array.
    /// </summary>
    /// <param name="chars">The character array containing Base16 (hex) characters.</param>
    /// <param name="offset">The zero-based starting position within <paramref name="chars" />.</param>
    /// <param name="count">The number of characters to decode.</param>
    /// <param name="style">Parsing styles that allow optional prefix and whitespace tolerance.</param>
    /// <returns>A new byte array representing the decoded binary data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="chars" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="count" /> is negative, or either exceeds the bounds
    /// of <paramref name="chars" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the segment defined by <paramref name="offset" /> and <paramref name="count" /> exceeds the
    /// available range of <paramref name="chars" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the input contains non-hexadecimal characters, or when it has an odd number of hex digits after
    /// applying <paramref name="style" />.
    /// </exception>
    public static byte[] Decode(char[] chars, int offset, int count, BaseFormatStyles style = BaseFormatStyles.None)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(chars, offset, count);
        return Decode(chars.AsSpan(offset, count), style);
    }

    /// <summary>
    /// Decodes a read-only span of hexadecimal characters into a byte array.
    /// </summary>
    /// <param name="chars">The span containing Base16 (hex) characters.</param>
    /// <param name="style">Parsing styles that allow optional prefix and whitespace tolerance.</param>
    /// <returns>A new byte array representing the decoded binary data. Returns <see cref="Array.Empty{T}" /> when
    /// <paramref name="chars" /> is empty.</returns>
    /// <exception cref="FormatException">
    /// Thrown when the input contains non-hexadecimal characters, or when it has an odd number of hex digits after
    /// applying <paramref name="style" />.
    /// </exception>
    public static byte[] Decode(ReadOnlySpan<char> chars, BaseFormatStyles style = BaseFormatStyles.None)
    {
        if (chars.IsEmpty)
            return Array.Empty<byte>();

        if (style == BaseFormatStyles.None)
        {
            if ((chars.Length & 1) != 0)
                throw new FormatException("Hex digit count is not even.");

            byte[] result = new byte[chars.Length / 2];
            if (!DecodeHexPairs(chars, result))
                throw new FormatException("Input contains non-hexadecimal characters.");

            return result;
        }

        Span<char> scratch = chars.Length <= StackallocThreshold
            ? stackalloc char[chars.Length]
            : new char[chars.Length];

        int digitCount = StripDecorationsInto(chars, style, scratch);
        if ((digitCount & 1) != 0)
            throw new FormatException("Hex digit count is not even after removing decorations.");

        byte[] decoded = new byte[digitCount / 2];
        if (!DecodeHexPairs(scratch.Slice(0, digitCount), decoded))
            throw new FormatException("Input contains non-hexadecimal characters.");

        return decoded;
    }

    /// <summary>
    /// Attempts to decode hexadecimal characters into bytes using the provided destination span.
    /// </summary>
    /// <param name="chars">The span of characters to decode.</param>
    /// <param name="destination">The span that receives the decoded bytes.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written, or <c>0</c> when
    /// decoding failed.</param>
    /// <param name="style">Parsing styles that allow optional prefix and whitespace tolerance.</param>
    /// <returns><see langword="true" /> when decoding succeeded; <see langword="false" /> when the input is malformed
    /// or <paramref name="destination" /> is too small.</returns>
    /// <remarks>
    /// This method never throws for malformed input — it returns <see langword="false" /> instead. Callers that
    /// require exception semantics should use <see cref="Decode(ReadOnlySpan{char}, BaseFormatStyles)" />.
    /// </remarks>
    public static bool TryDecode(ReadOnlySpan<char> chars, Span<byte> destination, out int bytesWritten, BaseFormatStyles style = BaseFormatStyles.None)
    {
        bytesWritten = 0;

        if (chars.IsEmpty)
            return true;

        if (style == BaseFormatStyles.None)
        {
            if ((chars.Length & 1) != 0)
                return false;

            int need = chars.Length / 2;
            if (destination.Length < need)
                return false;

            if (!DecodeHexPairs(chars, destination))
                return false;

            bytesWritten = need;
            return true;
        }

        Span<char> scratch = chars.Length <= StackallocThreshold
            ? stackalloc char[chars.Length]
            : new char[chars.Length];

        int digitCount = StripDecorationsInto(chars, style, scratch);
        if ((digitCount & 1) != 0)
            return false;

        int needBytes = digitCount / 2;
        if (destination.Length < needBytes)
            return false;

        if (!DecodeHexPairs(scratch.Slice(0, digitCount), destination))
            return false;

        bytesWritten = needBytes;
        return true;
    }

    /// <summary>
    /// Decodes paired hexadecimal characters into bytes using a fast nibble-conversion table.
    /// </summary>
    /// <param name="chars">The character span containing hex digits. The length must be a multiple of two.</param>
    /// <param name="bytes">The destination span. Must be at least <c>chars.Length / 2</c> bytes in size.</param>
    /// <returns><see langword="true" /> when every character is a valid hex digit; <see langword="false" /> when an
    /// invalid character is encountered.</returns>
    /// <remarks>This method assumes the caller has already validated the input length.</remarks>
    private static bool DecodeHexPairs(ReadOnlySpan<char> chars, Span<byte> bytes)
    {
        int bi = 0;
        for (int i = 0; i < chars.Length; i += 2)
        {
            int hi = Nibble(chars[i]);
            int lo = Nibble(chars[i + 1]);
            if ((hi | lo) < 0)
                return false;

            bytes[bi++] = (byte)((hi << 4) | lo);
        }

        return true;

        static int Nibble(char c) => c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'A' and <= 'F' => c - 'A' + 10,
            >= 'a' and <= 'f' => c - 'a' + 10,
            _ => -1,
        };
    }

    /// <summary>
    /// Copies <paramref name="source" /> into <paramref name="scratch" /> with decoration characters removed.
    /// </summary>
    /// <param name="source">The decorated input span.</param>
    /// <param name="style">The decoration tolerance flags.</param>
    /// <param name="scratch">The scratch buffer that receives the stripped digits. Must be at least as large as
    /// <paramref name="source" />.</param>
    /// <returns>The number of digit characters written to <paramref name="scratch" />.</returns>
    /// <remarks>
    /// <para>
    /// When <see cref="BaseFormatStyles.AllowPrefix" /> is set, a leading <c>0x</c> or <c>0X</c> sequence is skipped.
    /// The prefix is detected as the literal two-character sequence; bare <c>'0'</c> digits at the start of the input
    /// are preserved.
    /// </para>
    /// <para>
    /// When <see cref="BaseFormatStyles.IgnoreWhitespace" /> is set, ASCII whitespace ( <c>' '</c>, <c>'\t'</c>,
    /// <c>'\r'</c>, <c>'\n'</c>) is filtered out at any position.
    /// </para>
    /// </remarks>
    private static int StripDecorationsInto(ReadOnlySpan<char> source, BaseFormatStyles style, Span<char> scratch)
    {
        int j = 0;
        int i = 0;

        if (style.HasFlag(BaseFormatStyles.AllowPrefix) &&
            source.Length >= Prefix.Length &&
            source.StartsWith(Prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            i = Prefix.Length;
        }

        bool ignoreWhitespace = style.HasFlag(BaseFormatStyles.IgnoreWhitespace);
        for (; i < source.Length; i++)
        {
            char c = source[i];
            if (ignoreWhitespace && c is ' ' or '\t' or '\r' or '\n')
                continue;

            scratch[j++] = c;
        }

        return j;
    }
}
