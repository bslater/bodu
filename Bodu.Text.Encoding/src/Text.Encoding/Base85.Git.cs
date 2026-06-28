// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85.Git.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class Base85
{
    /// <summary>
    /// Encodes <paramref name="source" /> using the exact Git binary-patch Base85 primitive, emitting five characters
    /// for every one-to-four-byte group.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The Git-padded Base85 string. Returns <see cref="string.Empty" /> for empty input.</returns>
    /// <remarks>
    /// Unlike the compact <see cref="Base85Variant.GitCompact" /> mode used by
    /// <see cref="Encode(ReadOnlySpan{byte}, Base85Variant, BaseFormattingOptions)" />, this primitive never trims the
    /// trailing group: a final remainder of one, two, or three bytes is zero-padded internally and still produces five
    /// characters. The output is therefore not self-delimiting — decoding requires the original byte count, which Git
    /// carries in its binary-patch line prefix. Use
    /// <see cref="DecodeGitPadded(ReadOnlySpan{char}, int, BaseFormatStyles)" /> with that length to recover the bytes.
    /// </remarks>
    public static string EncodeGitPadded(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty)
            return string.Empty;

        int encodedLength = GetGitPaddedEncodedLength(source.Length);
        char[] buffer = new char[encodedLength];
        EncodeGitPaddedIntoBuffer(source, buffer);
        return new string(buffer);
    }

    /// <summary>
    /// Attempts to encode <paramref name="source" /> using the Git binary-patch Base85 primitive into a destination
    /// character span.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <returns>
    /// <see langword="true" /> when the destination is large enough; otherwise <see langword="false" />.
    /// </returns>
    public static bool TryEncodeGitPadded(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten)
    {
        if (source.IsEmpty)
        {
            charsWritten = 0;
            return true;
        }

        int encodedLength = GetGitPaddedEncodedLength(source.Length);
        if (destination.Length < encodedLength)
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = EncodeGitPaddedIntoBuffer(source, destination);
        return true;
    }

    /// <summary>
    /// Decodes a Git-padded Base85 character span, returning the leading <paramref name="decodedLength" /> bytes.
    /// </summary>
    /// <param name="source">The Git-padded Base85 input.</param>
    /// <param name="decodedLength">The number of bytes the original payload contained.</param>
    /// <param name="styles">Parsing styles. Only <see cref="BaseFormatStyles.IgnoreWhitespace" /> has effect.</param>
    /// <returns>The decoded byte array of length <paramref name="decodedLength" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="decodedLength" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the retained input length is not a multiple of five characters, or when
    /// <paramref name="decodedLength" /> is inconsistent with the number of encoded groups.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the input contains a character outside the Git Base85 alphabet or a group overflows a 32-bit value.
    /// </exception>
    public static byte[] DecodeGitPadded(ReadOnlySpan<char> source, int decodedLength, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        ThrowHelper.ThrowIfNegative(decodedLength);

        if (!TryCountGitPaddedSymbols(source, styles, out int symbolCount, out char badChar))
            throw new FormatException(string.Format(System.Globalization.CultureInfo.CurrentCulture, EncodingResourceStrings.Format_Invalid_Base85CharacterNotInAlphabet, badChar));

        if ((symbolCount % 5) != 0)
            throw new ArgumentException(EncodingResourceStrings.Arg_Invalid_GitBase85InputLength, nameof(source));

        int groupCount = symbolCount / 5;
        if (!IsGitPaddedDecodedLengthValid(decodedLength, groupCount))
            throw new ArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture, EncodingResourceStrings.Arg_Invalid_GitBase85DecodedLength, decodedLength), nameof(decodedLength));

        byte[] result = decodedLength == 0 ? [] : new byte[decodedLength];
        if (!TryDecodeGitPaddedGroups(source, styles, decodedLength, result, out _, out _))
            throw new FormatException(EncodingResourceStrings.Format_Invalid_Base85GroupOverflow);

        return result;
    }

    /// <summary>
    /// Attempts to decode a Git-padded Base85 character span, returning the leading <paramref name="decodedLength" />
    /// bytes.
    /// </summary>
    /// <param name="source">The Git-padded Base85 input.</param>
    /// <param name="decodedLength">The number of bytes the original payload contained.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="styles">Parsing styles. Only <see cref="BaseFormatStyles.IgnoreWhitespace" /> has effect.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when <paramref name="decodedLength" /> is negative,
    /// the input is malformed, the framing is inconsistent, or the destination is too small.
    /// </returns>
    public static bool TryDecodeGitPadded(ReadOnlySpan<char> source, int decodedLength, Span<byte> destination, out int bytesWritten, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        bytesWritten = 0;

        if (decodedLength < 0)
            return false;

        if (!TryCountGitPaddedSymbols(source, styles, out int symbolCount, out _))
            return false;

        if ((symbolCount % 5) != 0)
            return false;

        int groupCount = symbolCount / 5;
        if (!IsGitPaddedDecodedLengthValid(decodedLength, groupCount))
            return false;

        if (destination.Length < decodedLength)
            return false;

        if (!TryDecodeGitPaddedGroups(source, styles, decodedLength, destination, out int written, out _))
            return false;

        bytesWritten = written;
        return true;
    }

    /// <summary>
    /// Returns the exact number of characters that <see cref="EncodeGitPadded(ReadOnlySpan{byte})" /> produces for the
    /// supplied byte count.
    /// </summary>
    /// <param name="byteCount">The input byte count. Must be non-negative.</param>
    /// <returns>The encoded character count: <c>((byteCount + 3) / 4) * 5</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteCount" /> is negative.</exception>
    public static int GetGitPaddedEncodedLength(int byteCount)
    {
        ThrowHelper.ThrowIfNegative(byteCount);
        checked
        {
            return ((byteCount + 3) / 4) * 5;
        }
    }

    /// <summary>
    /// Encodes <paramref name="source" /> into <paramref name="destination" /> using the Git binary-patch Base85
    /// primitive (always five characters per group).
    /// </summary>
    /// <param name="source">The input bytes.</param>
    /// <param name="destination">
    /// The destination span. Must be at least <see cref="GetGitPaddedEncodedLength" /> characters.
    /// </param>
    /// <returns>The number of characters written.</returns>
    private static int EncodeGitPaddedIntoBuffer(ReadOnlySpan<byte> source, Span<char> destination)
    {
        int position = 0;
        int sourcePos = 0;

        Span<char> group = stackalloc char[5];
        while (sourcePos < source.Length)
        {
            int remaining = Math.Min(4, source.Length - sourcePos);
            uint value = 0;
            for (int i = 0; i < 4; i++)
            {
                value <<= 8;
                if (i < remaining)
                    value |= source[sourcePos + i];
            }

            for (int i = 4; i >= 0; i--)
            {
                group[i] = GitBase85Alphabet[(int)(value % 85)];
                value /= 85;
            }

            group.CopyTo(destination[position..]);
            position += 5;
            sourcePos += 4;
        }

        return position;
    }

    /// <summary>
    /// Counts the retained (non-whitespace) symbols in <paramref name="source" />, validating that each is in the Git
    /// Base85 alphabet.
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <param name="styles">The parsing styles.</param>
    /// <param name="symbolCount">When this method returns, contains the retained symbol count.</param>
    /// <param name="badChar">
    /// When this method returns <see langword="false" />, contains the offending character.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every retained character is in the alphabet; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryCountGitPaddedSymbols(ReadOnlySpan<char> source, BaseFormatStyles styles, out int symbolCount, out char badChar)
    {
        bool ignoreWhitespace = styles.HasFlag(BaseFormatStyles.IgnoreWhitespace);
        symbolCount = 0;
        badChar = '\0';

        foreach (char c in source)
        {
            if (ignoreWhitespace && c is ' ' or '\t' or '\r' or '\n')
                continue;

            if (c >= s_gitBase85Lookup.Length || s_gitBase85Lookup[c] < 0)
            {
                badChar = c;
                return false;
            }

            symbolCount++;
        }

        return true;
    }

    /// <summary>
    /// Indicates whether <paramref name="decodedLength" /> is consistent with <paramref name="groupCount" /> encoded
    /// Git-padded groups.
    /// </summary>
    /// <param name="decodedLength">The caller-supplied decoded byte count.</param>
    /// <param name="groupCount">The number of five-character groups in the input.</param>
    /// <returns><see langword="true" /> when the lengths agree.</returns>
    private static bool IsGitPaddedDecodedLengthValid(int decodedLength, int groupCount)
    {
        if (decodedLength == 0 && groupCount == 0)
            return true;

        if (decodedLength > groupCount * 4)
            return false;

        return groupCount == ((decodedLength + 3) / 4);
    }

    /// <summary>
    /// Decodes the Git-padded groups in <paramref name="source" />, writing at most <paramref name="decodedLength" />
    /// bytes into <paramref name="destination" />.
    /// </summary>
    /// <param name="source">The input characters (already validated for alphabet membership and framing).</param>
    /// <param name="styles">The parsing styles.</param>
    /// <param name="decodedLength">The number of bytes to retain.</param>
    /// <param name="destination">
    /// The destination span (must be at least <paramref name="decodedLength" /> bytes).
    /// </param>
    /// <param name="written">When this method returns, contains the number of bytes written.</param>
    /// <param name="overflow">
    /// When this method returns <see langword="false" />, indicates a group overflowed 32 bits.
    /// </param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when a group overflows a 32-bit value.
    /// </returns>
    private static bool TryDecodeGitPaddedGroups(ReadOnlySpan<char> source, BaseFormatStyles styles, int decodedLength, Span<byte> destination, out int written, out bool overflow)
    {
        bool ignoreWhitespace = styles.HasFlag(BaseFormatStyles.IgnoreWhitespace);
        written = 0;
        overflow = false;

        uint accumulator = 0;
        int charsAccumulated = 0;

        foreach (char c in source)
        {
            if (ignoreWhitespace && c is ' ' or '\t' or '\r' or '\n')
                continue;

            ulong next = ((ulong)accumulator * 85) + (uint)s_gitBase85Lookup[c];
            if (next > uint.MaxValue)
            {
                overflow = true;
                written = 0;
                return false;
            }

            accumulator = (uint)next;
            charsAccumulated++;

            if (charsAccumulated == 5)
            {
                for (int i = 0; i < 4 && written < decodedLength; i++)
                    destination[written++] = (byte)((accumulator >> ((3 - i) * 8)) & 0xFF);

                accumulator = 0;
                charsAccumulated = 0;
            }
        }

        return true;
    }
}
