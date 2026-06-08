// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base62.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Text.Encoding;

public static partial class Base62
{
    /// <summary>
    /// Decodes a Base62 string into a byte array.
    /// </summary>
    /// <param name="s">The Base62 input.</param>
    /// <param name="style">Parsing styles. Only <see cref="BaseFormatStyles.IgnoreWhitespace" /> has effect.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the input contains characters outside the Base62 alphabet.
    /// </exception>
    public static byte[] Decode(string s, BaseFormatStyles style = BaseFormatStyles.None)
    {
        ThrowHelper.ThrowIfNull(s);
        return Decode(s.AsSpan(), style);
    }

    /// <summary>
    /// Decodes a Base62 character span into a byte array.
    /// </summary>
    /// <param name="chars">The Base62 character span.</param>
    /// <param name="style">Parsing styles.</param>
    /// <returns>The decoded byte array. Returns <see cref="Array.Empty{T}" /> for empty input.</returns>
    /// <exception cref="FormatException">Thrown when the input is not valid Base62.</exception>
    public static byte[] Decode(ReadOnlySpan<char> chars, BaseFormatStyles style = BaseFormatStyles.None) =>
        chars.IsEmpty
            ? []
            : TryDecodeCore(chars, style, out var result, out var error)
                ? result!
                : throw new FormatException(error);

    /// <summary>
    /// Decodes a portion of a character array into a byte array.
    /// </summary>
    /// <param name="chars">The character array.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="count">The number of characters.</param>
    /// <param name="style">Parsing styles.</param>
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
    /// <exception cref="FormatException">Thrown when the input is not valid Base62.</exception>
    public static byte[] Decode(char[] chars, int offset, int count, BaseFormatStyles style = BaseFormatStyles.None)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(chars, offset, count);
        return Decode(chars.AsSpan(offset, count), style);
    }

    /// <summary>
    /// Attempts to decode a Base62 character span into a destination byte span.
    /// </summary>
    /// <param name="chars">The Base62 character span.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="bytesWritten">
    /// When this method returns, contains the number of bytes written, or <c>0</c> on failure.
    /// </param>
    /// <param name="style">Parsing styles.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the input is malformed or the destination is
    /// too small.
    /// </returns>
    public static bool TryDecode(ReadOnlySpan<char> chars, Span<byte> destination, out int bytesWritten, BaseFormatStyles style = BaseFormatStyles.None)
    {
        bytesWritten = 0;

        if (chars.IsEmpty)
            return true;

        if (!TryDecodeCore(chars, style, out var result, out _))
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
    /// <param name="style">Parsing styles.</param>
    /// <param name="result">When this method returns, contains the decoded byte array on success.</param>
    /// <param name="error">
    /// When this method returns, contains a failure reason or <see langword="null" /> on success.
    /// </param>
    /// <returns><see langword="true" /> on success; <see langword="false" /> on failure.</returns>
    private static bool TryDecodeCore(ReadOnlySpan<char> chars, BaseFormatStyles style, out byte[]? result, out string? error)
    {
        result = null;
        error = null;

        var ignoreWhitespace = style.HasFlag(BaseFormatStyles.IgnoreWhitespace);

        var leadingZeros = 0;
        BigInteger value = BigInteger.Zero;
        var seenNonZero = false;

        foreach (var c in chars)
        {
            if (ignoreWhitespace && c is ' ' or '\t' or '\r' or '\n')
                continue;

            if (c >= s_lookup.Length || s_lookup[c] < 0)
            {
                error = string.Format(System.Globalization.CultureInfo.CurrentCulture, EncodingResourceStrings.Format_Invalid_Base62CharacterNotInAlphabet, c);
                return false;
            }

            int symbolValue = s_lookup[c];
            if (!seenNonZero && symbolValue == 0)
            {
                leadingZeros++;
                continue;
            }

            seenNonZero = true;
            value = (value * Radix) + symbolValue;
        }

        var decoded = value.IsZero
            ? []
            : value.ToByteArray(isUnsigned: true, isBigEndian: true);

        if (leadingZeros == 0)
        {
            result = decoded;
            return true;
        }

        var combined = new byte[leadingZeros + decoded.Length];
        Buffer.BlockCopy(decoded, 0, combined, leadingZeros, decoded.Length);
        result = combined;
        return true;
    }
}
