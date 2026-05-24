// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32.Decode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class Base32
{
    /// <summary>
    /// Decodes a Base32 string into a byte array using the supplied variant.
    /// </summary>
    /// <param name="s">The Base32 input string.</param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="style">Parsing styles that tolerate whitespace and missing padding.</param>
    /// <returns>A new byte array representing the decoded binary data.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="variant" /> is not a defined value.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the input contains characters outside the variant alphabet, has invalid padding, or has an invalid
    /// length for the selected variant.
    /// </exception>
    public static byte[] Decode(string s, Base32Variant variant = Base32Variant.Standard, BaseFormatStyles style = BaseFormatStyles.None)
    {
        ThrowHelper.ThrowIfNull(s);
        return Decode(s.AsSpan(), variant, style);
    }

    /// <summary>
    /// Decodes a Base32 character span into a byte array.
    /// </summary>
    /// <param name="chars">The character span.</param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="style">Parsing styles.</param>
    /// <returns>The decoded byte array. Returns <see cref="Array.Empty{T}" /> when the input is empty.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="variant" /> is not a defined value.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the input is not valid Base32 for the chosen variant and parsing styles.
    /// </exception>
    public static byte[] Decode(ReadOnlySpan<char> chars, Base32Variant variant = Base32Variant.Standard, BaseFormatStyles style = BaseFormatStyles.None)
    {
        if (chars.IsEmpty)
            return Array.Empty<byte>();

        (_, var lookup) = GetVariantConfig(variant);
        var ignoreWhitespace = style.HasFlag(BaseFormatStyles.IgnoreWhitespace);
        var padIsRequired = ShouldRequireExactPadding(variant, style);
        var strictQuantum = variant is Base32Variant.Standard or Base32Variant.HexExtended;
        var requireCanonical = style.HasFlag(BaseFormatStyles.RequireCanonicalEncoding);

        var buffer = new byte[GetMaxDecodedLength(chars.Length)];

        if (!DecodeCore(chars, lookup, ignoreWhitespace, padIsRequired, strictQuantum, requireCanonical, buffer, out var written, out var error))
            throw new FormatException(error);

        if (written == buffer.Length)
            return buffer;

        var trimmed = new byte[written];
        Buffer.BlockCopy(buffer, 0, trimmed, 0, written);
        return trimmed;
    }

    /// <summary>
    /// Decodes a portion of a character array into a byte array using the supplied variant.
    /// </summary>
    /// <param name="chars">The character array.</param>
    /// <param name="offset">The zero-based starting offset.</param>
    /// <param name="count">The number of characters to decode.</param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="style">Parsing styles.</param>
    /// <returns>A new byte array representing the decoded binary data.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="chars" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="count" /> is out of range, or when
    /// <paramref name="variant" /> is not a defined value.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the segment defined by <paramref name="offset" /> and <paramref name="count" /> exceeds the
    /// available range of <paramref name="chars" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the input is not valid Base32 for the chosen variant and parsing styles.
    /// </exception>
    public static byte[] Decode(char[] chars, int offset, int count, Base32Variant variant = Base32Variant.Standard, BaseFormatStyles style = BaseFormatStyles.None)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(chars, offset, count);
        return Decode(chars.AsSpan(offset, count), variant, style);
    }

    /// <summary>
    /// Attempts to decode Base32 characters into bytes using the provided destination span.
    /// </summary>
    /// <param name="chars">The input characters.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="bytesWritten">
    /// When this method returns, contains the number of bytes written, or <c>0</c> on failure.
    /// </param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="style">Parsing styles.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the input is malformed or the destination is
    /// too small.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="variant" /> is not a defined value.
    /// </exception>
    public static bool TryDecode(ReadOnlySpan<char> chars, Span<byte> destination, out int bytesWritten, Base32Variant variant = Base32Variant.Standard, BaseFormatStyles style = BaseFormatStyles.None)
    {
        bytesWritten = 0;

        if (chars.IsEmpty)
            return true;

        (_, var lookup) = GetVariantConfig(variant);
        var ignoreWhitespace = style.HasFlag(BaseFormatStyles.IgnoreWhitespace);
        var padIsRequired = ShouldRequireExactPadding(variant, style);
        var strictQuantum = variant is Base32Variant.Standard or Base32Variant.HexExtended;
        var requireCanonical = style.HasFlag(BaseFormatStyles.RequireCanonicalEncoding);

        return DecodeCore(chars, lookup, ignoreWhitespace, padIsRequired, strictQuantum, requireCanonical, destination, out bytesWritten, out _);
    }

    /// <summary>
    /// Core decoder that walks the input characters once, validating and accumulating into the destination span.
    /// </summary>
    /// <param name="chars">The input characters.</param>
    /// <param name="lookup">The variant lookup table.</param>
    /// <param name="ignoreWhitespace">Whether to skip ASCII whitespace.</param>
    /// <param name="requireExactPadding">
    /// Whether the input must be a multiple of eight non-whitespace characters (including <c>=</c>).
    /// </param>
    /// <param name="strictQuantum">
    /// Whether the input must match an RFC 4648 §6 terminal-quantum length (2, 4, 5, 7, or 8 data characters). Only the
    /// Standard and HexExtended variants enforce this; Crockford and Z-Base32 omit the check.
    /// </param>
    /// <param name="requireCanonical">
    /// Whether the input must be in canonical form — the unused bits of the final partial group must be zero.
    /// </param>
    /// <param name="destination">The destination span.</param>
    /// <param name="bytesWritten">
    /// When this method returns, contains the number of bytes written, or <c>0</c> on failure.
    /// </param>
    /// <param name="error">
    /// When this method returns <see langword="false" />, contains the failure reason, or <see langword="null" /> on
    /// success.
    /// </param>
    /// <returns><see langword="true" /> on success; <see langword="false" /> on any validation failure.</returns>
    private static bool DecodeCore(ReadOnlySpan<char> chars, sbyte[] lookup, bool ignoreWhitespace, bool requireExactPadding, bool strictQuantum, bool requireCanonical, Span<byte> destination, out int bytesWritten, out string? error)
    {
        bytesWritten = 0;
        error = null;

        var accumulator = 0;
        var bitsAccumulated = 0;
        var symbolsConsumed = 0;
        var paddingSeen = 0;

        foreach (var c in chars)
        {
            if (ignoreWhitespace && c is ' ' or '\t' or '\r' or '\n')
                continue;

            if (c == PaddingChar)
            {
                paddingSeen++;
                continue;
            }

            if (paddingSeen > 0)
            {
                error = EncodingResourceStrings.Format_Invalid_Base32PaddingNotAtEnd;
                bytesWritten = 0;
                return false;
            }

            if ((uint)c >= (uint)lookup.Length)
            {
                error = string.Format(System.Globalization.CultureInfo.InvariantCulture, EncodingResourceStrings.Format_Invalid_Base32CharacterNotInAlphabet, c);
                bytesWritten = 0;
                return false;
            }

            int value = lookup[c];
            if (value < 0)
            {
                error = string.Format(System.Globalization.CultureInfo.InvariantCulture, EncodingResourceStrings.Format_Invalid_Base32CharacterNotInAlphabet, c);
                bytesWritten = 0;
                return false;
            }

            accumulator = (accumulator << 5) | value;
            bitsAccumulated += 5;
            symbolsConsumed++;

            if (bitsAccumulated >= 8)
            {
                bitsAccumulated -= 8;
                if (bytesWritten >= destination.Length)
                {
                    bytesWritten = 0;
                    return false;
                }

                destination[bytesWritten++] = (byte)((accumulator >> bitsAccumulated) & 0xFF);
            }
        }

        var totalSymbols = symbolsConsumed + paddingSeen;
        if (requireExactPadding && (totalSymbols % 8) != 0)
        {
            error = EncodingResourceStrings.Format_Invalid_Base32LengthNotMultipleOfEight;
            bytesWritten = 0;
            return false;
        }

        if (requireExactPadding)
        {
            var expectedPadding = (8 - (symbolsConsumed % 8)) % 8;
            if (paddingSeen != expectedPadding)
            {
                error = string.Format(System.Globalization.CultureInfo.InvariantCulture, EncodingResourceStrings.Format_Invalid_Base32PaddingCount, expectedPadding, paddingSeen);
                bytesWritten = 0;
                return false;
            }
        }

        // Per RFC 4648 §6 the valid terminal-quantum data character counts are 2, 4, 5, 7, or 8. Crockford and
        // Z-Base32 do not impose this rule, so the check is gated by the strictQuantum flag.
        var dataMod = symbolsConsumed % 8;
        if (strictQuantum && dataMod is 1 or 3 or 6)
        {
            error = EncodingResourceStrings.Format_Invalid_Base32TerminalQuantum;
            bytesWritten = 0;
            return false;
        }

        // Canonical encoding requires the unused low bits of the final partial group to be zero. Without this check
        // multiple distinct inputs decode to the same bytes (RFC 4648 §3.5).
        if (requireCanonical && bitsAccumulated > 0)
        {
            var leftoverMask = (1 << bitsAccumulated) - 1;
            if ((accumulator & leftoverMask) != 0)
            {
                error = EncodingResourceStrings.Format_Invalid_Base32NonCanonical;
                bytesWritten = 0;
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the decoder must require strict <c>=</c> padding alignment for the supplied variant and
    /// parsing style.
    /// </summary>
    /// <param name="variant">The variant.</param>
    /// <param name="style">The parsing style.</param>
    /// <returns><see langword="true" /> when padding alignment is required.</returns>
    private static bool ShouldRequireExactPadding(Base32Variant variant, BaseFormatStyles style)
    {
        return !style.HasFlag(BaseFormatStyles.AllowMissingPadding) && variant switch
        {
            Base32Variant.Standard => true,
            Base32Variant.HexExtended => true,
            Base32Variant.Crockford => false,
            Base32Variant.ZBase32 => false,
            _ => false,
        };
    }
}
