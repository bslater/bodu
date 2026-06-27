// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PercentEncoding.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class PercentEncoding
{
    /// <summary>
    /// Decodes percent-encoded characters into a byte array using the supplied mode and options.
    /// </summary>
    /// <param name="source">The percent-encoded input.</param>
    /// <param name="mode">The component mode.</param>
    /// <param name="options">The decoding options.</param>
    /// <returns>The decoded byte array. Returns an empty array for empty input.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="mode" /> is undefined.</exception>
    /// <exception cref="FormatException">
    /// Thrown when the input contains a non-ASCII character or a malformed percent sequence (and
    /// <see cref="PercentDecodingOptions.AllowInvalidPercentLiterals" /> is not set).
    /// </exception>
    public static byte[] Decode(ReadOnlySpan<char> source, PercentEncodingMode mode = PercentEncodingMode.UriComponent, PercentDecodingOptions options = PercentDecodingOptions.None)
    {
        EnsureValidMode(mode);

        if (source.IsEmpty)
            return [];

        byte[] buffer = new byte[GetMaxDecodedLength(source.Length)];

        if (!TryDecodeCore(source, mode, options, buffer, measureOnly: false, requireCanonical: false, out int written))
            throw new FormatException(EncodingResourceStrings.Format_Invalid_PercentEncoding);

        if (written == buffer.Length)
            return buffer;

        byte[] trimmed = new byte[written];
        Buffer.BlockCopy(buffer, 0, trimmed, 0, written);
        return trimmed;
    }

    /// <summary>
    /// Decodes percent-encoded characters to bytes, then converts those bytes to a string with
    /// <paramref name="textEncoding" /> (UTF-8 by default).
    /// </summary>
    /// <param name="source">The percent-encoded input.</param>
    /// <param name="textEncoding">
    /// The text encoding used to build the string, or <see langword="null" /> for UTF-8.
    /// </param>
    /// <param name="mode">The component mode.</param>
    /// <param name="options">The decoding options.</param>
    /// <returns>The decoded string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="mode" /> is undefined.</exception>
    /// <exception cref="FormatException">Thrown when the percent-encoded input is malformed.</exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when the decoded bytes are invalid for <paramref name="textEncoding" /> and it uses a throwing fallback.
    /// </exception>
    /// <remarks>
    /// The default <see cref="System.Text.Encoding.UTF8" /> uses a <em>replacement</em> decoder fallback, so invalid
    /// UTF-8 byte sequences become the U+FFFD replacement character rather than failing. Pass a throwing encoding (for
    /// example <c>new UTF8Encoding(false, throwOnInvalidBytes: true)</c>) to reject invalid sequences instead.
    /// </remarks>
    public static string DecodeString(ReadOnlySpan<char> source, System.Text.Encoding? textEncoding = null, PercentEncodingMode mode = PercentEncodingMode.UriComponent, PercentDecodingOptions options = PercentDecodingOptions.None)
    {
        byte[] bytes = Decode(source, mode, options);
        return (textEncoding ?? System.Text.Encoding.UTF8).GetString(bytes);
    }

    /// <summary>
    /// Attempts to decode percent-encoded characters into a destination byte span.
    /// </summary>
    /// <param name="source">The percent-encoded input.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="mode">The component mode.</param>
    /// <param name="options">The decoding options.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the mode is undefined, the input is malformed,
    /// or the destination is too small.
    /// </returns>
    public static bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten, PercentEncodingMode mode = PercentEncodingMode.UriComponent, PercentDecodingOptions options = PercentDecodingOptions.None)
    {
        if (!IsDefinedMode(mode))
        {
            bytesWritten = 0;
            return false;
        }

        return TryDecodeCore(source, mode, options, destination, measureOnly: false, requireCanonical: false, out bytesWritten);
    }

    /// <summary>
    /// Decodes <paramref name="chars" /> into <paramref name="destination" />, or measures the decoded byte count when
    /// <paramref name="measureOnly" /> is set, validating the input on both paths.
    /// </summary>
    /// <param name="chars">The percent-encoded input.</param>
    /// <param name="mode">The component mode.</param>
    /// <param name="options">The decoding options.</param>
    /// <param name="destination">The destination span (ignored when <paramref name="measureOnly" /> is set).</param>
    /// <param name="measureOnly">
    /// When <see langword="true" />, only the byte count is computed and no output is written.
    /// </param>
    /// <param name="requireCanonical">
    /// When <see langword="true" />, literal characters the mode would percent-encode are rejected (canonical
    /// validation); when <see langword="false" />, any ASCII literal is accepted (lenient recovery).
    /// </param>
    /// <param name="bytesWritten">When this method returns, contains the byte count, or <c>0</c> on failure.</param>
    /// <returns>
    /// <see langword="true" /> when the input is well-formed and (when writing) fits; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryDecodeCore(ReadOnlySpan<char> chars, PercentEncodingMode mode, PercentDecodingOptions options, Span<byte> destination, bool measureOnly, bool requireCanonical, out int bytesWritten)
    {
        bool allowInvalid = (options & PercentDecodingOptions.AllowInvalidPercentLiterals) != 0;
        bool form = mode == PercentEncodingMode.FormUrlEncoded;
        bool[] table = s_passThrough[(int)mode];

        int pos = 0;
        bool overflow = false;

        int n = chars.Length;
        int i = 0;
        while (i < n)
        {
            char c = chars[i];

            if (c > 0x7F)
            {
                // The byte-oriented surface rejects non-ASCII characters; Unicode belongs in DecodeString.
                bytesWritten = 0;
                return false;
            }

            if (c == '%')
            {
                if (i + 2 < n && TryHexValue(chars[i + 1], out int high) && TryHexValue(chars[i + 2], out int low))
                {
                    EmitByte(destination, measureOnly, (byte)((high << 4) | low), ref pos, ref overflow);
                    i += 3;
                    continue;
                }

                if (allowInvalid)
                {
                    EmitByte(destination, measureOnly, (byte)'%', ref pos, ref overflow);
                    i++;
                    continue;
                }

                bytesWritten = 0;
                return false;
            }

            if (form && c == '+')
            {
                EmitByte(destination, measureOnly, 0x20, ref pos, ref overflow);
                i++;
                continue;
            }

            // Canonical validation rejects literal characters the selected mode would have percent-encoded. A
            // percent-escaped octet (handled above) is always accepted; only bare literals are constrained.
            if (requireCanonical && !table[c])
            {
                bytesWritten = 0;
                return false;
            }

            EmitByte(destination, measureOnly, (byte)c, ref pos, ref overflow);
            i++;
        }

        if (overflow)
        {
            bytesWritten = 0;
            return false;
        }

        bytesWritten = pos;
        return true;
    }
}
