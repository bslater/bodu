// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base62.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Base62 encoding and decoding of binary data using the GMP-style alphabet <c>0-9 A-Z a-z</c>.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/encoding-special-purpose.svg" alt="Special-purpose encodings. Base45 (RFC 9285) packs each pair of bytes into three characters from the QR-code Alphanumeric-mode alphabet with no padding. Base62 uses the GMP-style alphabet 0-9 A-Z a-z and big-integer division by 62, preserving leading zero bytes as leading zero characters. Bech32 and Bech32m comprise a human-readable part, the 1 separator, 5-bit data groups, and a six-symbol checksum."/>
/// </para>
/// <para>
/// Base62 is a non-power-of-two radix (radix 62) commonly used for compact, URL-safe identifiers and short links.
/// Because the radix is not a power of two, encoding is performed with big-integer arithmetic rather than the
/// bit-stream technique used by Base16, Base32, and Base64.
/// </para>
/// <para>
/// The alphabet orders digits first, then upper-case letters, then lower-case letters — the convention used by the GNU
/// Multiple Precision library and most <c>base-x</c> implementations. Leading zero bytes in the input are encoded as
/// leading <c>0</c> characters so that the byte-level and character-level forms preserve a meaningful prefix.
/// </para>
/// <para>
/// Base62 has no padding character and no standard decorations. Only <see cref="BaseFormatStyles.IgnoreWhitespace" />
/// affects decoding.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// byte[] data = { 0x00, 0xDE, 0xAD, 0xBE, 0xEF };
/// string encoded = Base62.Encode(data);          // leading zero byte -> leading '0'
/// byte[] roundtrip = Base62.Decode(encoded);
///]]>
/// </code>
/// </example>
public static partial class Base62
{
    /// <summary>The GMP-style Base62 alphabet, indexed by symbol value.</summary>
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>The encoding radix.</summary>
    private const int Radix = 62;

    /// <summary>Maps a US-ASCII code point to its Base62 value, or <c>-1</c> when the character is not in the alphabet.</summary>
    private static readonly sbyte[] s_lookup = BuildLookup(Alphabet);

    /// <summary>
    /// Returns an upper bound on the number of bytes that decoding <paramref name="charCount" /> characters can
    /// produce.
    /// </summary>
    /// <param name="charCount">The input character count.</param>
    /// <returns>An upper bound on the decoded byte count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="charCount" /> is negative.</exception>
    public static int GetMaxDecodedLength(int charCount)
    {
        ThrowHelper.ThrowIfNegative(charCount);
        if (charCount == 0)
            return 0;
        checked
        {
            return ((charCount * 745) / 1000) + 1;
        }
    }

    /// <summary>
    /// Returns an upper bound on the number of characters required to encode <paramref name="byteCount" /> bytes.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <returns>An upper bound on the encoded character count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteCount" /> is negative.</exception>
    /// <remarks>
    /// Because Base62 is a non-power-of-two radix and the exact encoded length depends on the leading-zero count of the
    /// input, this overload returns a worst-case upper bound suitable for buffer sizing. The actual output length is
    /// the result of <see cref="Encode(ReadOnlySpan{byte})" /> on the specific data.
    /// </remarks>
    public static int GetMaxEncodedLength(int byteCount)
    {
        ThrowHelper.ThrowIfNegative(byteCount);
        if (byteCount == 0)
            return 0;
        checked
        {
            return ((byteCount * 138) / 100) + 1;
        }
    }

    /// <summary>
    /// Indicates whether <paramref name="value" /> is a valid Base62 digit.
    /// </summary>
    /// <param name="value">The character to test.</param>
    /// <returns><see langword="true" /> when the character belongs to the Base62 alphabet.</returns>
    public static bool IsBase62Digit(char value) =>
        value < s_lookup.Length && s_lookup[value] >= 0;

    /// <summary>
    /// Indicates whether <paramref name="source" /> is a valid Base62 input.
    /// </summary>
    /// <param name="source">The character span.</param>
    /// <param name="styles">Parsing styles. Only <see cref="BaseFormatStyles.IgnoreWhitespace" /> has effect.</param>
    /// <returns>
    /// <see langword="true" /> when every retained character is in the alphabet; otherwise <see langword="false" />.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> source, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        bool ignoreWhitespace = styles.HasFlag(BaseFormatStyles.IgnoreWhitespace);

        foreach (char c in source)
        {
            if (ignoreWhitespace && c is ' ' or '\t' or '\r' or '\n')
                continue;

            if (c >= s_lookup.Length || s_lookup[c] < 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Builds a 128-entry lookup table mapping alphabet characters to their numeric value.
    /// </summary>
    /// <param name="alphabet">The alphabet.</param>
    /// <returns>The lookup table, with <c>-1</c> for every non-alphabet code point.</returns>
    private static sbyte[] BuildLookup(string alphabet)
    {
        sbyte[] table = new sbyte[128];
        Array.Fill(table, (sbyte)-1);

        for (int i = 0; i < alphabet.Length; i++)
            table[alphabet[i]] = (sbyte)i;

        return table;
    }
}
