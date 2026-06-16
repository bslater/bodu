// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bech32.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Bech32 (BIP-173) and Bech32m (BIP-350) encoding and decoding — a checksummed base-32 format consisting of a
/// human-readable part (HRP), the <c>1</c> separator, a data part of 5-bit groups, and a six-symbol error-detecting
/// checksum.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/encoding-special-purpose.svg" alt="Special-purpose encodings. Base45 (RFC 9285) packs each pair of bytes into three characters from the QR-code Alphanumeric-mode alphabet with no padding. Base62 uses the GMP-style alphabet 0-9 A-Z a-z and big-integer division by 62. Bech32 and Bech32m (BIP 173 / 350) are not plain binary-to-text encodings: an encoded string is a human-readable part, the 1 separator, 5-bit data groups, and a six-symbol error-detecting checksum."/>
/// </para>
/// <para>
/// Bech32 is not a plain binary-to-text encoding: every encoded string carries an HRP prefix and a checksum, so the
/// type is modelled on <see cref="Base58Check" /> rather than the <see cref="IBinaryEncoding" /> family. The core
/// surface operates on 5-bit data groups (each value <c>0</c>–<c>31</c>); use <see cref="ConvertBits" /> — or the
/// <c>FromBytes</c> / <c>ToBytes</c> convenience members — to translate between 8-bit bytes and 5-bit groups.
/// </para>
/// <para>
/// Encoded output is always lower case (the canonical form). Decoding accepts an all-lower-case or all-upper-case
/// string and rejects mixed case per BIP-173; the returned HRP is normalized to lower case. The decoder reports which
/// scheme validated the checksum through the <see cref="Bech32Encoding" /> out-parameter.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Encode 5-bit data groups under a human-readable prefix.
/// byte[] data = { 0x00, 0x01, 0x02 };                 // already 5-bit values
/// string encoded = Bech32.Encode("abc", data);        // "abc1..." with a Bech32 checksum
///
/// // Decode and recover the parts.
/// Bech32.Decode(encoded, out string hrp, out byte[] groups, out Bech32Encoding scheme);
///]]>
/// </example>
public static partial class Bech32
{
    /// <summary>
    /// The BIP-173 data alphabet, indexed by 5-bit symbol value.
    /// </summary>
    private const string Charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";

    /// <summary>
    /// The separator that divides the human-readable part from the data part.
    /// </summary>
    private const char Separator = '1';

    /// <summary>
    /// The number of checksum symbols appended to the data part.
    /// </summary>
    private const int ChecksumSymbols = 6;

    /// <summary>
    /// The maximum overall length of a Bech32 string defined by BIP-173.
    /// </summary>
    private const int MaxEncodedLength = 90;

    /// <summary>
    /// The lowest US-ASCII code point permitted in a human-readable part.
    /// </summary>
    private const int MinHrpChar = 33;

    /// <summary>
    /// The highest US-ASCII code point permitted in a human-readable part.
    /// </summary>
    private const int MaxHrpChar = 126;

    /// <summary>
    /// The checksum constant for the original Bech32 scheme (BIP-173).
    /// </summary>
    private const uint Bech32Constant = 1;

    /// <summary>
    /// The checksum constant for the Bech32m scheme (BIP-350).
    /// </summary>
    private const uint Bech32mConstant = 0x2bc830a3;

    /// <summary>
    /// The five generator coefficients used by the checksum polynomial.
    /// </summary>
    private static readonly uint[] s_generators = [0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3];

    /// <summary>
    /// Maps a US-ASCII code point to its 5-bit data value, or <c>-1</c> when the character is not in the alphabet.
    /// </summary>
    private static readonly sbyte[] s_charsetReverse = BuildReverse(Charset);

    /// <summary>
    /// Converts a sequence of integers between bit widths, as defined by the BIP-173 reference <c>convertbits</c>
    /// routine — the operation that packs 8-bit bytes into 5-bit Bech32 groups and back.
    /// </summary>
    /// <param name="data">The input values, each fitting within <paramref name="fromBits" /> bits.</param>
    /// <param name="fromBits">The bit width of each input value.</param>
    /// <param name="toBits">The bit width of each output value.</param>
    /// <param name="pad">
    /// When <see langword="true" />, the final group is zero-padded; when <see langword="false" />, a non-zero residue
    /// or an over-wide residue causes failure.
    /// </param>
    /// <returns>
    /// The converted values, or <see langword="null" /> when an input value exceeds <paramref name="fromBits" /> bits,
    /// or when <paramref name="pad" /> is <see langword="false" /> and the trailing bits are not a clean zero residue.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="fromBits" /> or <paramref name="toBits" /> is not in the range 1 to 8.
    /// </exception>
    public static byte[]? ConvertBits(ReadOnlySpan<byte> data, int fromBits, int toBits, bool pad)
    {
        ThrowHelper.ThrowIfOutOfRange(fromBits, 1, 8);
        ThrowHelper.ThrowIfOutOfRange(toBits, 1, 8);

        int acc = 0;
        int bits = 0;
        int maxValue = (1 << toBits) - 1;
        int maxAccumulator = (1 << (fromBits + toBits - 1)) - 1;

        var result = new List<byte>((data.Length * fromBits / toBits) + 1);

        foreach (byte value in data)
        {
            if ((value >> fromBits) != 0)
                return null;

            acc = ((acc << fromBits) | value) & maxAccumulator;
            bits += fromBits;
            while (bits >= toBits)
            {
                bits -= toBits;
                result.Add((byte)((acc >> bits) & maxValue));
            }
        }

        if (pad)
        {
            if (bits > 0)
                result.Add((byte)((acc << (toBits - bits)) & maxValue));
        }
        else if (bits >= fromBits || ((acc << (toBits - bits)) & maxValue) != 0)
        {
            return null;
        }

        return [.. result];
    }

    /// <summary>
    /// Indicates whether <paramref name="source" /> is a structurally valid Bech32 or Bech32m string with a verifying
    /// checksum.
    /// </summary>
    /// <param name="source">The candidate string.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="source" /> decodes without error; otherwise
    /// <see langword="false" />.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> source) =>
        TryDecode(source, out _, out _, out _);

    /// <summary>
    /// Returns the overall encoded length for a Bech32 string with the supplied part lengths.
    /// </summary>
    /// <param name="hrpLength">The number of characters in the human-readable part.</param>
    /// <param name="dataLength">The number of 5-bit data groups, excluding the checksum.</param>
    /// <returns>
    /// The total character count: <paramref name="hrpLength" /> + 1 separator + <paramref name="dataLength" /> + 6
    /// checksum symbols.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="hrpLength" /> or <paramref name="dataLength" /> is negative.
    /// </exception>
    public static int GetEncodedLength(int hrpLength, int dataLength)
    {
        ThrowHelper.ThrowIfNegative(hrpLength);
        ThrowHelper.ThrowIfNegative(dataLength);
        checked
        {
            return hrpLength + 1 + dataLength + ChecksumSymbols;
        }
    }

    /// <summary>
    /// Computes the Bech32 checksum polynomial over <paramref name="values" />.
    /// </summary>
    /// <param name="values">The expanded HRP, data, and (when verifying) checksum symbols.</param>
    /// <returns>The 30-bit polynomial residue.</returns>
    private static uint Polymod(ReadOnlySpan<byte> values)
    {
        uint chk = 1;
        foreach (byte value in values)
        {
            uint top = chk >> 25;
            chk = ((chk & 0x1ffffff) << 5) ^ value;
            for (int i = 0; i < 5; i++)
            {
                if (((top >> i) & 1) != 0)
                    chk ^= s_generators[i];
            }
        }

        return chk;
    }

    /// <summary>
    /// Expands a human-readable part into the high-bit prefix, separator zero, and low-bit suffix the checksum
    /// consumes.
    /// </summary>
    /// <param name="hrp">The lower-case human-readable part.</param>
    /// <returns>An expansion of length <c>(2 × hrp.Length) + 1</c>.</returns>
    private static byte[] ExpandHrp(string hrp)
    {
        byte[] result = new byte[(hrp.Length * 2) + 1];
        for (int i = 0; i < hrp.Length; i++)
        {
            result[i] = (byte)(hrp[i] >> 5);
            result[hrp.Length + 1 + i] = (byte)(hrp[i] & 31);
        }

        result[hrp.Length] = 0;
        return result;
    }

    /// <summary>
    /// Computes the six checksum symbols for <paramref name="hrp" /> and <paramref name="data" /> under the supplied
    /// scheme, writing them into <paramref name="destination" />.
    /// </summary>
    /// <param name="hrp">The lower-case human-readable part.</param>
    /// <param name="data">The 5-bit data groups.</param>
    /// <param name="encoding">The scheme selecting the checksum constant.</param>
    /// <param name="destination">A span of exactly <see cref="ChecksumSymbols" /> bytes receiving the checksum.</param>
    private static void CreateChecksum(string hrp, ReadOnlySpan<byte> data, Bech32Encoding encoding, Span<byte> destination)
    {
        byte[] expanded = ExpandHrp(hrp);
        Span<byte> values = new byte[expanded.Length + data.Length + ChecksumSymbols];
        expanded.CopyTo(values);
        data.CopyTo(values.Slice(expanded.Length));

        uint polymod = Polymod(values) ^ ConstantFor(encoding);
        for (int i = 0; i < ChecksumSymbols; i++)
            destination[i] = (byte)((polymod >> (5 * (5 - i))) & 31);
    }

    /// <summary>
    /// Verifies the checksum that trails <paramref name="data" /> and reports which scheme validated it.
    /// </summary>
    /// <param name="hrp">The lower-case human-readable part.</param>
    /// <param name="data">The 5-bit data groups including the trailing six checksum symbols.</param>
    /// <param name="encoding">
    /// When this method returns <see langword="true" />, the scheme whose constant matched.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the checksum matches the Bech32 or Bech32m constant; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool VerifyChecksum(string hrp, ReadOnlySpan<byte> data, out Bech32Encoding encoding)
    {
        byte[] expanded = ExpandHrp(hrp);
        Span<byte> values = new byte[expanded.Length + data.Length];
        expanded.CopyTo(values);
        data.CopyTo(values.Slice(expanded.Length));

        uint polymod = Polymod(values);
        switch (polymod)
        {
            case Bech32Constant:
                encoding = Bech32Encoding.Bech32;
                return true;
            case Bech32mConstant:
                encoding = Bech32Encoding.Bech32m;
                return true;
            default:
                encoding = default;
                return false;
        }
    }

    /// <summary>
    /// Returns the checksum constant for the supplied scheme.
    /// </summary>
    /// <param name="encoding">The scheme.</param>
    /// <returns>The checksum constant.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="encoding" /> is undefined.</exception>
    private static uint ConstantFor(Bech32Encoding encoding) =>
        encoding switch
        {
            Bech32Encoding.Bech32 => Bech32Constant,
            Bech32Encoding.Bech32m => Bech32mConstant,
            _ => throw new ArgumentOutOfRangeException(nameof(encoding)),
        };

    /// <summary>
    /// Builds a 128-entry reverse lookup mapping alphabet characters to their 5-bit value.
    /// </summary>
    /// <param name="charset">The alphabet.</param>
    /// <returns>The reverse lookup table, with <c>-1</c> for every non-alphabet code point.</returns>
    private static sbyte[] BuildReverse(string charset)
    {
        sbyte[] table = new sbyte[128];
        Array.Fill(table, (sbyte)-1);

        for (int i = 0; i < charset.Length; i++)
            table[charset[i]] = (sbyte)i;

        return table;
    }
}
