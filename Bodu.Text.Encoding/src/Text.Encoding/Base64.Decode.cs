// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base64
{
    /// <summary>
    /// Decodes a Base64 string into a byte array using the supplied variant.
    /// </summary>
    /// <param name="s">The Base64 input string.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="style">Parsing styles.</param>
    /// <returns>A new byte array containing the decoded data.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="FormatException">
    /// Thrown when the input is not valid Base64 for the chosen variant and parsing styles.
    /// </exception>
    public static byte[] Decode(string s, Base64Variant variant = Base64Variant.Standard, BaseFormatStyles style = BaseFormatStyles.None)
    {
        ThrowHelper.ThrowIfNull(s);
        return Decode(s.AsSpan(), variant, style);
    }

    /// <summary>
    /// Decodes a Base64 character span into a byte array.
    /// </summary>
    /// <param name="chars">The character span.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="style">Parsing styles.</param>
    /// <returns>
    /// A new byte array containing the decoded data. Returns <see cref="Array.Empty{T}" /> when the input is empty.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="FormatException">Thrown when the input is not valid Base64.</exception>
    public static byte[] Decode(ReadOnlySpan<char> chars, Base64Variant variant = Base64Variant.Standard, BaseFormatStyles style = BaseFormatStyles.None)
    {
        EnsureValidVariant(variant);

        if (chars.IsEmpty)
            return [];

        int rentSize = chars.Length + 4; // headroom for re-padding

        // Stackalloc the scratch buffer for small inputs; fall back to ArrayPool for large inputs. The 256-char
        // threshold matches a single decoded payload of ~192 bytes, which covers JWTs, OAuth state tokens, and
        // most short identifiers that dominate Base64 traffic in practice.
        if (rentSize <= 256)
        {
            Span<char> scratch = stackalloc char[256];
            return DecodeCore(chars, scratch, variant, style);
        }

        char[] pooled = ArrayPool<char>.Shared.Rent(rentSize);

        try
        {
            return DecodeCore(chars, pooled.AsSpan(0, rentSize), variant, style);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(pooled);
        }
    }

    /// <summary>
    /// Decodes <paramref name="chars" /> using the supplied <paramref name="scratch" /> buffer for normalisation.
    /// </summary>
    /// <param name="chars">The encoded character span.</param>
    /// <param name="scratch">
    /// The caller-allocated scratch span; must be at least <paramref name="chars" />.Length + 4 long.
    /// </param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="style">Parsing styles.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="FormatException">Thrown when the input is not valid Base64.</exception>
    private static byte[] DecodeCore(ReadOnlySpan<char> chars, Span<char> scratch, Base64Variant variant, BaseFormatStyles style)
    {
        int normalized = NormalizeForDecode(chars, scratch, variant, style);
        if (normalized < 0)
            throw new FormatException(EncodingResourceStrings.Format_Invalid_Base64Alphabet);

        byte[] buffer = new byte[GetExactDecodedLength(scratch[..normalized])];
        if (!Convert.TryFromBase64Chars(scratch[..normalized], buffer, out int bytesWritten)
            || bytesWritten != buffer.Length)
        {
            throw new FormatException(EncodingResourceStrings.Format_Invalid_Base64);
        }

        if (style.HasFlag(BaseFormatStyles.RequireCanonicalEncoding)
            && !IsCanonicalEncoding(buffer.AsSpan(0, bytesWritten), scratch[..normalized]))
        {
            throw new FormatException(EncodingResourceStrings.Format_Invalid_Base64NonCanonical);
        }

        return buffer;
    }

    /// <summary>
    /// Returns the exact number of decoded bytes that a normalised, padded Base64 character span will produce — the
    /// normalised length divided by four times three, less one byte per trailing <c>=</c> padding character.
    /// </summary>
    /// <param name="normalized">A normalised Base64 character span whose length is a multiple of four.</param>
    /// <returns>The exact decoded byte count, never negative.</returns>
    private static int GetExactDecodedLength(ReadOnlySpan<char> normalized)
    {
        int groups = normalized.Length / 4;
        int padding = 0;

        if (normalized.Length >= 1 && normalized[^1] == PaddingChar)
        {
            padding = 1;
            if (normalized.Length >= 2 && normalized[^2] == PaddingChar)
                padding = 2;
        }

        return (groups * 3) - padding;
    }

    /// <summary>
    /// Decodes a portion of a character array into a byte array.
    /// </summary>
    /// <param name="chars">The character array.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="count">The number of characters.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="style">Parsing styles.</param>
    /// <returns>A new byte array containing the decoded data.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="chars" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="count" /> is out of range, or when
    /// <paramref name="variant" /> is undefined.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the segment defined by <paramref name="offset" /> and <paramref name="count" /> exceeds the
    /// available range of <paramref name="chars" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the input is not valid Base64.</exception>
    public static byte[] Decode(char[] chars, int offset, int count, Base64Variant variant = Base64Variant.Standard, BaseFormatStyles style = BaseFormatStyles.None)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(chars, offset, count);
        return Decode(chars.AsSpan(offset, count), variant, style);
    }

    /// <summary>
    /// Attempts to decode Base64 characters into bytes using the provided destination span.
    /// </summary>
    /// <param name="chars">The input characters.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="bytesWritten">
    /// When this method returns, contains the number of bytes written, or <c>0</c> on failure.
    /// </param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="style">Parsing styles.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the input is malformed or the destination is
    /// too small.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool TryDecode(ReadOnlySpan<char> chars, Span<byte> destination, out int bytesWritten, Base64Variant variant = Base64Variant.Standard, BaseFormatStyles style = BaseFormatStyles.None)
    {
        EnsureValidVariant(variant);
        bytesWritten = 0;

        if (chars.IsEmpty)
            return true;

        int rentSize = chars.Length + 4;
        char[] scratch = ArrayPool<char>.Shared.Rent(rentSize);
        try
        {
            int normalized = NormalizeForDecode(chars, scratch, variant, style);
            if (normalized < 0)
                return false;

            if (!Convert.TryFromBase64Chars(scratch.AsSpan(0, normalized), destination, out bytesWritten))
                return false;

            if (style.HasFlag(BaseFormatStyles.RequireCanonicalEncoding)
                && !IsCanonicalEncoding(destination.Slice(0, bytesWritten), scratch.AsSpan(0, normalized)))
            {
                bytesWritten = 0;
                return false;
            }

            return true;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(scratch);
        }
    }

    /// <summary>
    /// Indicates whether <paramref name="normalisedInput" /> is the canonical Base64 encoding of
    /// <paramref name="decodedBytes" />. Re-encodes the bytes using the standard alphabet and compares the result.
    /// </summary>
    /// <param name="decodedBytes">The bytes that <paramref name="normalisedInput" /> decoded to.</param>
    /// <param name="normalisedInput">The post-normalisation Base64 input (standard alphabet, fully padded).</param>
    /// <returns>
    /// <see langword="true" /> when the input matches the canonical re-encoding; <see langword="false" /> when the
    /// input has non-zero unused bits in its final partial group.
    /// </returns>
    private static bool IsCanonicalEncoding(ReadOnlySpan<byte> decodedBytes, ReadOnlySpan<char> normalisedInput)
    {
        int expectedLength = ((decodedBytes.Length + 2) / 3) * 4;
        if (expectedLength != normalisedInput.Length)
            return false;

        Span<char> reencoded = expectedLength <= 256
            ? stackalloc char[256]
            : new char[expectedLength];
        return Convert.TryToBase64Chars(decodedBytes, reencoded, out int reLen) && normalisedInput.SequenceEqual(reencoded.Slice(0, reLen));
    }

    /// <summary>
    /// Copies <paramref name="source" /> into <paramref name="scratch" />, applying variant character swaps, optional
    /// whitespace stripping, and re-padding to align the resulting length to a multiple of four characters.
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <param name="scratch">The scratch buffer that receives the normalised characters.</param>
    /// <param name="variant">The variant in use.</param>
    /// <param name="style">The parsing styles.</param>
    /// <returns>The length of the normalised buffer, or <c>-1</c> if the input contains an illegal character.</returns>
    private static int NormalizeForDecode(ReadOnlySpan<char> source, Span<char> scratch, Base64Variant variant, BaseFormatStyles style)
    {
        bool ignoreWhitespace = style.HasFlag(BaseFormatStyles.IgnoreWhitespace) || variant == Base64Variant.Mime;
        bool urlSafe = variant == Base64Variant.UrlSafe;
        bool allowMissingPadding = style.HasFlag(BaseFormatStyles.AllowMissingPadding) || urlSafe;

        int j = 0;
        bool paddingSeen = false;

        foreach (char c in source)
        {
            if (ignoreWhitespace && c is ' ' or '\t' or '\r' or '\n')
                continue;

            if (c == PaddingChar)
            {
                paddingSeen = true;
                scratch[j++] = c;
                continue;
            }

            if (paddingSeen)
            {
                return -1;
            }

            // Per RFC 4648 §4 and §5 the alphabets are disjoint with respect to '+/' and '-_'. Reject characters that
            // belong to the opposite variant before forwarding the normalised scratch buffer to the BCL decoder.
            bool isLetterOrDigit = c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9');
            char mapped;
            if (urlSafe)
            {
                if (c == '-')
                    mapped = '+';
                else if (c == '_')
                    mapped = '/';
                else if (isLetterOrDigit)
                    mapped = c;
                else
                    return -1;
            }
            else
            {
                if (c is '+' or '/')
                    mapped = c;
                else if (isLetterOrDigit)
                    mapped = c;
                else
                    return -1;
            }

            scratch[j++] = mapped;
        }

        if (allowMissingPadding)
        {
            int remainder = j % 4;
            if (remainder == 2)
            {
                scratch[j++] = PaddingChar;
                scratch[j++] = PaddingChar;
            }
            else if (remainder == 3)
            {
                scratch[j++] = PaddingChar;
            }
            else if (remainder == 1)
            {
                return -1;
            }
        }

        return j;
    }
}
