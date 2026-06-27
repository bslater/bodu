// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuotedPrintable.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class QuotedPrintable
{
    /// <summary>
    /// Decodes a Quoted-Printable character span into a byte array using the supplied options.
    /// </summary>
    /// <param name="source">The Quoted-Printable input.</param>
    /// <param name="options">The decoding options.</param>
    /// <returns>The decoded byte array. Returns an empty array for empty input.</returns>
    /// <exception cref="FormatException">Thrown when the input is not well-formed Quoted-Printable.</exception>
    public static byte[] Decode(ReadOnlySpan<char> source, QuotedPrintableDecodingOptions options = QuotedPrintableDecodingOptions.None)
    {
        if (source.IsEmpty)
            return [];

        byte[] buffer = new byte[GetMaxDecodedLength(source.Length)];

        if (!TryDecodeCore(source, options, buffer, measureOnly: false, requireCanonical: false, out int written))
            throw new FormatException(EncodingResourceStrings.Format_Invalid_QuotedPrintable);

        if (written == buffer.Length)
            return buffer;

        byte[] trimmed = new byte[written];
        Buffer.BlockCopy(buffer, 0, trimmed, 0, written);
        return trimmed;
    }

    /// <summary>
    /// Attempts to decode a Quoted-Printable character span into a destination byte span.
    /// </summary>
    /// <param name="source">The Quoted-Printable input.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="options">The decoding options.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the input is malformed or the destination is
    /// too small.
    /// </returns>
    public static bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten, QuotedPrintableDecodingOptions options = QuotedPrintableDecodingOptions.None) =>
        TryDecodeCore(source, options, destination, measureOnly: false, requireCanonical: false, out bytesWritten);

    /// <summary>
    /// Decodes <paramref name="chars" /> into <paramref name="destination" />, or measures the decoded byte count when
    /// <paramref name="measureOnly" /> is set, validating the input on both paths.
    /// </summary>
    /// <param name="chars">The Quoted-Printable input.</param>
    /// <param name="options">The decoding options.</param>
    /// <param name="destination">The destination span (ignored when <paramref name="measureOnly" /> is set).</param>
    /// <param name="measureOnly">
    /// When <see langword="true" />, only the byte count is computed and no output is written.
    /// </param>
    /// <param name="requireCanonical">
    /// When <see langword="true" />, enforce the RFC 2045 76-character encoded-line limit (canonical validation); when
    /// <see langword="false" />, line length is ignored (lenient recovery).
    /// </param>
    /// <param name="bytesWritten">When this method returns, contains the byte count, or <c>0</c> on failure.</param>
    /// <returns>
    /// <see langword="true" /> when the input is well-formed and (when writing) fits; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryDecodeCore(ReadOnlySpan<char> chars, QuotedPrintableDecodingOptions options, Span<byte> destination, bool measureOnly, bool requireCanonical, out int bytesWritten)
    {
        bool allowLowercaseHex = (options & QuotedPrintableDecodingOptions.AllowLowercaseHex) != 0;
        bool ignoreTrailingWhitespace = (options & QuotedPrintableDecodingOptions.IgnoreTrailingWhitespace) != 0;
        bool allowBareLineFeed = (options & QuotedPrintableDecodingOptions.AllowBareLineFeed) != 0;

        int pos = 0;
        bool overflow = false;

        // Encoded-line length is tracked only for canonical validation (the RFC 2045 76-character limit, with the
        // soft-break '=' counted and the CRLF terminator not). Lenient decode ignores it entirely.
        int lineLength = 0;

        int n = chars.Length;
        int i = 0;
        while (i < n)
        {
            char c = chars[i];

            if (c == '=')
            {
                if (i + 1 >= n)
                {
                    bytesWritten = 0;
                    return false;
                }

                char next = chars[i + 1];

                if (next == '\r')
                {
                    if (i + 2 < n && chars[i + 2] == '\n')
                    {
                        if (requireCanonical && ++lineLength > MaxCanonicalLineLength)
                        {
                            bytesWritten = 0;
                            return false;
                        }

                        lineLength = 0;
                        i += 3;
                        continue;
                    }

                    bytesWritten = 0;
                    return false;
                }

                if (next == '\n')
                {
                    if (allowBareLineFeed)
                    {
                        if (requireCanonical && ++lineLength > MaxCanonicalLineLength)
                        {
                            bytesWritten = 0;
                            return false;
                        }

                        lineLength = 0;
                        i += 2;
                        continue;
                    }

                    bytesWritten = 0;
                    return false;
                }

                if (i + 2 >= n
                    || !TryHexValue(next, allowLowercaseHex, out int high)
                    || !TryHexValue(chars[i + 2], allowLowercaseHex, out int low))
                {
                    bytesWritten = 0;
                    return false;
                }

                if (requireCanonical && (lineLength += 3) > MaxCanonicalLineLength)
                {
                    bytesWritten = 0;
                    return false;
                }

                EmitByte(destination, measureOnly, (byte)((high << 4) | low), ref pos, ref overflow);
                i += 3;
                continue;
            }

            if (c == '\r')
            {
                if (i + 1 < n && chars[i + 1] == '\n')
                {
                    EmitByte(destination, measureOnly, 0x0D, ref pos, ref overflow);
                    EmitByte(destination, measureOnly, 0x0A, ref pos, ref overflow);
                    lineLength = 0;
                    i += 2;
                    continue;
                }

                bytesWritten = 0;
                return false;
            }

            if (c == '\n')
            {
                if (allowBareLineFeed)
                {
                    EmitByte(destination, measureOnly, 0x0A, ref pos, ref overflow);
                    lineLength = 0;
                    i++;
                    continue;
                }

                bytesWritten = 0;
                return false;
            }

            if (c == ' ' || c == '\t')
            {
                int j = i;
                while (j < n && (chars[j] == ' ' || chars[j] == '\t'))
                    j++;

                if (requireCanonical && (lineLength += j - i) > MaxCanonicalLineLength)
                {
                    bytesWritten = 0;
                    return false;
                }

                if (IsTrailingAfterWhitespace(chars, j, allowBareLineFeed))
                {
                    if (!ignoreTrailingWhitespace)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    i = j;
                    continue;
                }

                for (int k = i; k < j; k++)
                    EmitByte(destination, measureOnly, (byte)chars[k], ref pos, ref overflow);

                i = j;
                continue;
            }

            if (c >= 0x21 && c <= 0x7E)
            {
                if (requireCanonical && ++lineLength > MaxCanonicalLineLength)
                {
                    bytesWritten = 0;
                    return false;
                }

                EmitByte(destination, measureOnly, (byte)c, ref pos, ref overflow);
                i++;
                continue;
            }

            // Non-ASCII or a stray control character.
            bytesWritten = 0;
            return false;
        }

        if (overflow)
        {
            bytesWritten = 0;
            return false;
        }

        bytesWritten = pos;
        return true;
    }

    /// <summary>
    /// Indicates whether the character at <paramref name="index" /> (the byte following a whitespace run) marks the run
    /// as trailing whitespace at a line end.
    /// </summary>
    /// <param name="chars">The input characters.</param>
    /// <param name="index">The index of the first character after the whitespace run.</param>
    /// <param name="allowBareLineFeed">Whether a bare LF is accepted as a line break.</param>
    /// <returns><see langword="true" /> when the run precedes a line break or the end of input.</returns>
    private static bool IsTrailingAfterWhitespace(ReadOnlySpan<char> chars, int index, bool allowBareLineFeed)
    {
        int n = chars.Length;
        if (index >= n)
            return true;

        char c = chars[index];
        if (c == '\r' || c == '\n')
            return true;

        if (c == '=')
        {
            if (index + 2 < n && chars[index + 1] == '\r' && chars[index + 2] == '\n')
                return true;

            return allowBareLineFeed && index + 1 < n && chars[index + 1] == '\n';
        }

        return false;
    }
}
