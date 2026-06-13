// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base45.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class Base45
{
    /// <summary>
    /// Decodes a Base45 string into a byte array.
    /// </summary>
    /// <param name="s">The Base45 input.</param>
    /// <param name="styles">
    /// Parsing styles. Only <see cref="BaseFormatStyles.IgnoreWhitespace" /> has effect, and it skips tab, carriage
    /// return, and line feed but never the space symbol.
    /// </param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the input is not valid Base45.</exception>
    public static byte[] Decode(string s, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        ThrowHelper.ThrowIfNull(s);
        return Decode(s.AsSpan(), styles);
    }

    /// <summary>
    /// Decodes a Base45 character span into a byte array.
    /// </summary>
    /// <param name="chars">The Base45 character span.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>The decoded byte array. Returns <see cref="Array.Empty{T}" /> for empty input.</returns>
    /// <exception cref="FormatException">
    /// Thrown when the input contains a character outside the alphabet, has an invalid length, or contains a group that
    /// decodes to a value outside the valid byte range.
    /// </exception>
    public static byte[] Decode(ReadOnlySpan<char> chars, BaseFormatStyles styles = BaseFormatStyles.None) =>
        chars.IsEmpty
            ? []
            : TryDecodeCore(chars, styles, out var result, out var error)
                ? result!
                : throw new FormatException(error);

    /// <summary>
    /// Decodes a portion of a character array into a byte array.
    /// </summary>
    /// <param name="chars">The character array.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="count">The number of characters.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="chars" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="count" /> is out of range.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the segment defined by <paramref name="offset" /> and <paramref name="count" /> exceeds the
    /// available range of <paramref name="chars" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the input is not valid Base45.</exception>
    public static byte[] Decode(char[] chars, int offset, int count, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(chars, offset, count);
        return Decode(chars.AsSpan(offset, count), styles);
    }

    /// <summary>
    /// Attempts to decode a Base45 character span into a destination byte span without throwing.
    /// </summary>
    /// <param name="chars">The Base45 character span.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="bytesWritten">
    /// When this method returns, contains the number of bytes written, or <c>0</c> on failure.
    /// </param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the input is malformed or the destination is
    /// too small.
    /// </returns>
    public static bool TryDecode(ReadOnlySpan<char> chars, Span<byte> destination, out int bytesWritten, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        bytesWritten = 0;

        if (chars.IsEmpty)
            return true;

        if (!TryDecodeCore(chars, styles, out var result, out _))
            return false;

        if (destination.Length < result!.Length)
            return false;

        result.AsSpan().CopyTo(destination);
        bytesWritten = result.Length;
        return true;
    }

    /// <summary>
    /// Core decoder that produces a byte array (or reports the failure reason).
    /// </summary>
    /// <param name="chars">The input span.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <param name="result">When this method returns, contains the decoded byte array on success.</param>
    /// <param name="error">
    /// When this method returns, contains a failure reason or <see langword="null" /> on success.
    /// </param>
    /// <returns><see langword="true" /> on success; <see langword="false" /> on failure.</returns>
    private static bool TryDecodeCore(ReadOnlySpan<char> chars, BaseFormatStyles styles, out byte[]? result, out string? error)
    {
        result = null;
        error = null;

        var ignoreWhitespace = styles.HasFlag(BaseFormatStyles.IgnoreWhitespace);

        var upperBytes = GetMaxDecodedLength(chars.Length);
        var output = upperBytes == 0 ? [] : new byte[upperBytes];
        var byteCount = 0;

        Span<int> group = stackalloc int[3];
        var groupLength = 0;

        foreach (var c in chars)
        {
            if (ignoreWhitespace && IsIgnorableWhitespace(c))
                continue;

            if (c >= s_lookup.Length || s_lookup[c] < 0)
            {
                error = string.Format(System.Globalization.CultureInfo.CurrentCulture, EncodingResourceStrings.Format_Invalid_Base45CharacterNotInAlphabet, c);
                return false;
            }

            group[groupLength++] = s_lookup[c];

            if (groupLength != 3)
                continue;

            var pair = group[0] + (group[1] * Radix) + (group[2] * Radix * Radix);
            if (pair > MaxPairValue)
            {
                error = EncodingResourceStrings.Format_Invalid_Base45GroupOverflow;
                return false;
            }

            output[byteCount++] = (byte)(pair >> 8);
            output[byteCount++] = (byte)(pair & 0xFF);
            groupLength = 0;
        }

        if (groupLength == 1)
        {
            error = EncodingResourceStrings.Format_Invalid_Base45InvalidLength;
            return false;
        }

        if (groupLength == 2)
        {
            var single = group[0] + (group[1] * Radix);
            if (single > MaxSingleValue)
            {
                error = EncodingResourceStrings.Format_Invalid_Base45GroupOverflow;
                return false;
            }

            output[byteCount++] = (byte)single;
        }

        result = byteCount == output.Length ? output : output[..byteCount];
        return true;
    }
}
