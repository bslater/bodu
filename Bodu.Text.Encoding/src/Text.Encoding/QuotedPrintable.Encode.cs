// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuotedPrintable.Encode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class QuotedPrintable
{
    /// <summary>
    /// Encodes <paramref name="source" /> into a Quoted-Printable string using the supplied options.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="options">The encoding options.</param>
    /// <returns>A Quoted-Printable string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="QuotedPrintableEncodingOptions.Mode" /> is undefined or the normalized
    /// <see cref="QuotedPrintableEncodingOptions.MaxLineLength" /> is less than four.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="QuotedPrintableEncodingOptions.NewLine" /> is not <c>"\r\n"</c> or <c>"\n"</c>.
    /// </exception>
    public static string Encode(ReadOnlySpan<byte> source, QuotedPrintableEncodingOptions options = default)
    {
        QuotedPrintableEncodingOptions normalized = NormalizeAndValidate(options);

        if (source.IsEmpty)
            return string.Empty;

        EncodeCore(source, normalized, Span<char>.Empty, measureOnly: true, out int length);

        char[] buffer = new char[length];
        EncodeCore(source, normalized, buffer, measureOnly: false, out int written);
        return new string(buffer, 0, written);
    }

    /// <summary>
    /// Attempts to encode <paramref name="source" /> into a destination character span.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <param name="options">The encoding options.</param>
    /// <returns>
    /// <see langword="true" /> when the options are valid and the destination is large enough; otherwise
    /// <see langword="false" />.
    /// </returns>
    public static bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten, QuotedPrintableEncodingOptions options = default)
    {
        if (!TryNormalize(options, out QuotedPrintableEncodingOptions normalized, out _))
        {
            charsWritten = 0;
            return false;
        }

        if (source.IsEmpty)
        {
            charsWritten = 0;
            return true;
        }

        return EncodeCore(source, normalized, destination, measureOnly: false, out charsWritten);
    }

    /// <summary>
    /// Returns the worst-case Quoted-Printable token width of a single source byte, treating space and tab as their
    /// escaped width because the encoder may protect them.
    /// </summary>
    /// <param name="value">The source byte.</param>
    /// <returns><c>1</c> for a byte that may pass through literally; otherwise <c>3</c>.</returns>
    private static int WorstCaseWidth(byte value)
    {
        if (value == 0x20 || value == 0x09 || value == 0x3D)
            return 3;

        return (value >= 0x21 && value <= 0x3C) || (value >= 0x3E && value <= 0x7E) ? 1 : 3;
    }

    /// <summary>
    /// Writes the Quoted-Printable encoding of <paramref name="source" /> into <paramref name="destination" />, or
    /// measures its length when <paramref name="measureOnly" /> is set.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="options">The normalized encoding options.</param>
    /// <param name="destination">The destination span (ignored when <paramref name="measureOnly" /> is set).</param>
    /// <param name="measureOnly">
    /// When <see langword="true" />, only the length is computed and no output is written.
    /// </param>
    /// <param name="charsWritten">
    /// When this method returns, contains the character count, or <c>0</c> on overflow.
    /// </param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when <paramref name="destination" /> is too small.
    /// </returns>
    private static bool EncodeCore(ReadOnlySpan<byte> source, QuotedPrintableEncodingOptions options, Span<char> destination, bool measureOnly, out int charsWritten)
    {
        int pos = 0;
        bool overflow = false;

        ReadOnlySpan<char> newLine = options.NewLine.AsSpan();
        bool textMode = options.Mode == QuotedPrintableEncodingMode.Text;
        int maxContent = options.MaxLineLength - 1;
        int lineLen = 0;
        int n = source.Length;

        int i = 0;
        while (i < n)
        {
            byte b = source[i];

            // Text-mode hard line break: a canonical CRLF pair is emitted as the configured newline.
            if (textMode && b == 0x0D && i + 1 < n && source[i + 1] == 0x0A)
            {
                foreach (char c in newLine)
                    EmitChar(destination, measureOnly, c, ref pos, ref overflow);

                lineLen = 0;
                i += 2;
                continue;
            }

            char t0;
            char t1 = '\0';
            char t2 = '\0';
            int width;

            if (b == 0x20 || b == 0x09)
            {
                // Protect space/tab from becoming the last character on its line, where a decoder may delete it.
                bool atEnd = i + 1 == n;
                bool beforeHardBreak = textMode && i + 1 < n && source[i + 1] == 0x0D && i + 2 < n && source[i + 2] == 0x0A;
                bool protect;
                if (atEnd || beforeHardBreak)
                {
                    protect = true;
                }
                else
                {
                    int nextWidth = WorstCaseWidth(source[i + 1]);
                    protect = (lineLen + 1 <= maxContent) && (lineLen + 1 + nextWidth > maxContent);
                }

                if (protect)
                {
                    t0 = '=';
                    t1 = HexHigh(b);
                    t2 = HexLow(b);
                    width = 3;
                }
                else
                {
                    t0 = (char)b;
                    width = 1;
                }
            }
            else if (b == 0x3D)
            {
                t0 = '=';
                t1 = '3';
                t2 = 'D';
                width = 3;
            }
            else if ((b >= 0x21 && b <= 0x3C) || (b >= 0x3E && b <= 0x7E))
            {
                t0 = (char)b;
                width = 1;
            }
            else
            {
                t0 = '=';
                t1 = HexHigh(b);
                t2 = HexLow(b);
                width = 3;
            }

            // Soft-wrap before the token when it would not leave room for a trailing soft-break marker.
            if (lineLen + width > maxContent)
            {
                EmitChar(destination, measureOnly, '=', ref pos, ref overflow);
                foreach (char c in newLine)
                    EmitChar(destination, measureOnly, c, ref pos, ref overflow);

                lineLen = 0;
            }

            EmitChar(destination, measureOnly, t0, ref pos, ref overflow);
            if (width == 3)
            {
                EmitChar(destination, measureOnly, t1, ref pos, ref overflow);
                EmitChar(destination, measureOnly, t2, ref pos, ref overflow);
            }

            lineLen += width;
            i++;
        }

        if (overflow)
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = pos;
        return true;
    }
}
