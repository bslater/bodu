// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base45.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Base45 encoding and decoding of binary data as defined by RFC 9285 — the compact alphanumeric encoding used
/// to carry binary payloads inside a QR code's Alphanumeric mode.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/encoding-special-purpose.svg" alt="Special-purpose encodings. Base45 (RFC 9285) packs each pair of bytes into three characters from the QR-code Alphanumeric-mode alphabet (0-9, A-Z, and nine symbols) with no padding; a trailing odd byte becomes two characters. Base62 uses the GMP-style alphabet 0-9 A-Z a-z and big-integer division by 62, preserving leading zero bytes. Bech32 and Bech32m comprise a human-readable part, the 1 separator, 5-bit data groups, and a six-symbol checksum."/>
/// </para>
/// <para>
/// Base45 encodes each pair of input bytes as a base-45 number written in three characters, and a trailing odd byte as
/// a two-character base-45 number. Unlike Base64, Base32, and Base16 it uses no padding. The 45-character alphabet is
/// the Alphanumeric-mode subset of US-ASCII: the digits <c>0-9</c>, the upper-case letters <c>A-Z</c>, and the symbols
/// <c>space</c>, <c>$</c>, <c>%</c>, <c>*</c>, <c>+</c>, <c>-</c>, <c>.</c>, <c>/</c>, and <c>:</c>.
/// </para>
/// <para>
/// Because the space character is itself a Base45 symbol, whitespace skipping via
/// <see cref="BaseFormatStyles.IgnoreWhitespace" /> applies only to tab, carriage return, and line feed — never to the
/// space character. The decoder is strict per RFC 9285 §6: it rejects characters outside the alphabet, an encoded
/// length whose value modulo three is one, and any three-character group that decodes to a value greater than
/// <c>65535</c> (or any two-character group greater than <c>255</c>).
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Encode an arbitrary byte payload for inclusion in a QR code.
/// byte[] data = { 0x41, 0x42 };
/// string encoded = Base45.Encode(data);          // "BB8"
///
/// // Round-trip.
/// byte[] roundtrip = Base45.Decode(encoded);      // { 0x41, 0x42 }
///]]>
/// </code>
/// </example>
public static partial class Base45
{
    /// <summary>The RFC 9285 §4.2 Base45 alphabet, indexed by symbol value <c>0</c>–<c>44</c>.</summary>
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    /// <summary>The encoding radix.</summary>
    private const int Radix = 45;

    /// <summary>The maximum value a valid three-character group may decode to (a 16-bit byte pair).</summary>
    private const int MaxPairValue = 65535;

    /// <summary>The maximum value a valid two-character group may decode to (a single byte).</summary>
    private const int MaxSingleValue = 255;

    /// <summary>Maps a US-ASCII code point to its Base45 symbol value, or <c>-1</c> when the character is not in the alphabet.</summary>
    private static readonly sbyte[] s_lookup = BuildLookup(Alphabet);

    /// <summary>
    /// Returns the exact number of characters required to encode <paramref name="byteCount" /> bytes.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <returns>The encoded character count: three characters per byte pair plus two for a trailing odd byte.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteCount" /> is negative.</exception>
    public static int GetEncodedLength(int byteCount)
    {
        ThrowHelper.ThrowIfNegative(byteCount);
        checked
        {
            return ((byteCount / 2) * 3) + ((byteCount & 1) == 1 ? 2 : 0);
        }
    }

    /// <summary>
    /// Returns the number of characters required to encode <paramref name="byteCount" /> bytes. Because Base45 is a
    /// fixed-rate encoding this value is exact rather than an upper bound.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <returns>The encoded character count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteCount" /> is negative.</exception>
    public static int GetMaxEncodedLength(int byteCount) =>
        GetEncodedLength(byteCount);

    /// <summary>
    /// Returns an upper bound on the number of bytes that decoding <paramref name="charCount" /> characters can
    /// produce.
    /// </summary>
    /// <param name="charCount">The input character count.</param>
    /// <returns>An upper bound on the decoded byte count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="charCount" /> is negative.</exception>
    /// <remarks>
    /// The returned value is exact for well-formed input (two bytes per three-character group plus one for a trailing
    /// two-character group). When <paramref name="charCount" /> modulo three is one — a length the decoder rejects —
    /// the value still serves as a safe buffer size.
    /// </remarks>
    public static int GetMaxDecodedLength(int charCount)
    {
        ThrowHelper.ThrowIfNegative(charCount);
        checked
        {
            return ((charCount / 3) * 2) + (charCount % 3 == 2 ? 1 : 0);
        }
    }

    /// <summary>
    /// Indicates whether <paramref name="value" /> is a character in the Base45 alphabet.
    /// </summary>
    /// <param name="value">The character to test.</param>
    /// <returns><see langword="true" /> when the character belongs to the Base45 alphabet.</returns>
    public static bool IsBase45Digit(char value) =>
        value < s_lookup.Length && s_lookup[value] >= 0;

    /// <summary>
    /// Indicates whether <paramref name="source" /> is a valid Base45 input.
    /// </summary>
    /// <param name="source">The character span to validate.</param>
    /// <param name="styles">
    /// Parsing styles. Only <see cref="BaseFormatStyles.IgnoreWhitespace" /> has effect, and it skips tab, carriage
    /// return, and line feed but never the space symbol.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="source" /> would decode without error; otherwise
    /// <see langword="false" />.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> source, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        int upper = GetMaxDecodedLength(source.Length);
        byte[] rented = System.Buffers.ArrayPool<byte>.Shared.Rent(upper == 0 ? 1 : upper);
        try
        {
            return TryDecode(source, rented, out _, styles);
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(rented);
        }
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

    /// <summary>
    /// Indicates whether <paramref name="c" /> is an ignorable whitespace character under
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> — tab, carriage return, or line feed. The space character is a
    /// Base45 symbol and is never treated as ignorable.
    /// </summary>
    /// <param name="c">The character to test.</param>
    /// <returns><see langword="true" /> when the character is ignorable whitespace.</returns>
    private static bool IsIgnorableWhitespace(char c) =>
        c is '\t' or '\r' or '\n';
}
