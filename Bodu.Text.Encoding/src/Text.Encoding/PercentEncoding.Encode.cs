// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PercentEncoding.Encode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class PercentEncoding
{
    /// <summary>
    /// Encodes <paramref name="source" /> into a percent-encoded string using the supplied component mode.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="mode">The component mode.</param>
    /// <returns>A percent-encoded string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="mode" /> is undefined.</exception>
    public static string Encode(ReadOnlySpan<byte> source, PercentEncodingMode mode = PercentEncodingMode.UriComponent)
    {
        EnsureValidMode(mode);

        if (source.IsEmpty)
            return string.Empty;

        EncodeCore(source, mode, Span<char>.Empty, measureOnly: true, out int length);

        char[] buffer = new char[length];
        EncodeCore(source, mode, buffer, measureOnly: false, out int written);
        return new string(buffer, 0, written);
    }

    /// <summary>
    /// Encodes a .NET string by first converting it to bytes with <paramref name="textEncoding" /> (UTF-8 by default),
    /// then percent-encoding those bytes.
    /// </summary>
    /// <param name="value">The string to encode.</param>
    /// <param name="textEncoding">The text encoding used to obtain bytes, or <see langword="null" /> for UTF-8.</param>
    /// <param name="mode">The component mode.</param>
    /// <returns>A percent-encoded string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="mode" /> is undefined.</exception>
    public static string EncodeString(string value, System.Text.Encoding? textEncoding = null, PercentEncodingMode mode = PercentEncodingMode.UriComponent)
    {
        ThrowHelper.ThrowIfNull(value);
        EnsureValidMode(mode);

        byte[] bytes = (textEncoding ?? System.Text.Encoding.UTF8).GetBytes(value);
        return Encode(bytes, mode);
    }

    /// <summary>
    /// Returns the exact number of characters that <see cref="Encode(ReadOnlySpan{byte}, PercentEncodingMode)" />
    /// produces for the supplied data and mode.
    /// </summary>
    /// <param name="source">The input bytes.</param>
    /// <param name="mode">The component mode.</param>
    /// <returns>The exact encoded character count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="mode" /> is undefined.</exception>
    public static int GetEncodedLength(ReadOnlySpan<byte> source, PercentEncodingMode mode = PercentEncodingMode.UriComponent)
    {
        EnsureValidMode(mode);

        EncodeCore(source, mode, Span<char>.Empty, measureOnly: true, out int charCount);
        return charCount;
    }

    /// <summary>
    /// Attempts to encode <paramref name="source" /> into a destination character span.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <param name="mode">The component mode.</param>
    /// <returns>
    /// <see langword="true" /> when the mode is defined and the destination is large enough; otherwise
    /// <see langword="false" />.
    /// </returns>
    public static bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten, PercentEncodingMode mode = PercentEncodingMode.UriComponent)
    {
        if (!IsDefinedMode(mode))
        {
            charsWritten = 0;
            return false;
        }

        return EncodeCore(source, mode, destination, measureOnly: false, out charsWritten);
    }

    /// <summary>
    /// Writes the percent-encoding of <paramref name="source" /> into <paramref name="destination" />, or measures its
    /// length when <paramref name="measureOnly" /> is set.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="mode">The component mode.</param>
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
    private static bool EncodeCore(ReadOnlySpan<byte> source, PercentEncodingMode mode, Span<char> destination, bool measureOnly, out int charsWritten)
    {
        bool[] table = s_passThrough[(int)mode];
        bool form = mode == PercentEncodingMode.FormUrlEncoded;

        int pos = 0;
        bool overflow = false;

        foreach (byte b in source)
        {
            if (form && b == 0x20)
            {
                EmitChar(destination, measureOnly, '+', ref pos, ref overflow);
            }
            else if (table[b])
            {
                EmitChar(destination, measureOnly, (char)b, ref pos, ref overflow);
            }
            else
            {
                EmitChar(destination, measureOnly, '%', ref pos, ref overflow);
                EmitChar(destination, measureOnly, ToHexDigit(b >> 4), ref pos, ref overflow);
                EmitChar(destination, measureOnly, ToHexDigit(b & 0x0F), ref pos, ref overflow);
            }
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
