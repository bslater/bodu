// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base36Encoding.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using Bodu.Text.Encoding;

namespace Bodu.Samples.Text.Encoding.CustomEncoding;

/// <summary>
/// A Base36 binary encoding (digits <c>0-9</c> then <c>A-Z</c>) implementing
/// <see cref="IBinaryEncoding" /> — the alphabet used by license keys, short URLs, and other
/// identifiers meant to be read aloud. Like Base58, the payload is treated as one big-endian
/// integer, so the encoding has no padding and no alignment requirement; each leading zero byte
/// is preserved as a leading <c>'0'</c> character.
/// </summary>
/// <remarks>
/// The implementation favours clarity over throughput (it round-trips through
/// <see cref="BigInteger" />). A production codec would divide in place over the byte buffer the
/// way the library's own Base58 does — the contract it must satisfy is the same either way,
/// which is what the accompanying contract-test project verifies.
/// </remarks>
public sealed class Base36Encoding
    : IBinaryEncoding
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <inheritdoc />
    public string Name => "base36";

    /// <inheritdoc />
    public string Description => "Base36 (0-9, A-Z; big-endian integer form, leading zero bytes preserved as '0').";

    /// <inheritdoc />
    public string Encode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
            return string.Empty;

        // Count leading zero bytes - they vanish from the integer and must be re-emitted.
        var leadingZeros = 0;
        while (leadingZeros < bytes.Length && bytes[leadingZeros] == 0)
            leadingZeros++;

        // Interpret the payload as one unsigned big-endian integer.
        var value = new BigInteger(bytes, isUnsigned: true, isBigEndian: true);

        var digits = new List<char>();
        while (value > 0)
        {
            value = BigInteger.DivRem(value, 36, out var remainder);
            digits.Add(Alphabet[(int)remainder]);
        }

        digits.Reverse();
        return new string('0', leadingZeros) + new string(digits.ToArray());
    }

    /// <inheritdoc />
    public byte[] Decode(ReadOnlySpan<char> chars)
    {
        if (chars.IsEmpty)
            return [];

        // Mirror of the encode side: each leading '0' character restores one leading zero byte.
        var leadingZeros = 0;
        while (leadingZeros < chars.Length && chars[leadingZeros] == '0')
            leadingZeros++;

        BigInteger value = 0;
        foreach (var c in chars[leadingZeros..])
        {
            var digit = DigitValue(c);
            if (digit < 0)
                throw new FormatException($"'{c}' is not a Base36 digit (expected 0-9 or A-Z).");

            value = (value * 36) + digit;
        }

        // Rebuild the payload: the preserved zero bytes first, then the integer's big-endian magnitude.
        var magnitude = value.IsZero ? [] : value.ToByteArray(isUnsigned: true, isBigEndian: true);

        var result = new byte[leadingZeros + magnitude.Length];
        magnitude.CopyTo(result, leadingZeros);
        return result;
    }

    /// <inheritdoc />
    public bool IsValid(ReadOnlySpan<char> source)
    {
        foreach (var c in source)
        {
            if (DigitValue(c) < 0)
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public int GetMaxEncodedLength(int byteCount) =>
        // log(256) / log(36) = 1.5494... characters per byte, rounded up.
        checked((int)Math.Ceiling(byteCount * 1.5495)) + 1;

    /// <inheritdoc />
    public int GetMaxDecodedLength(int charCount) =>
        // log(36) / log(256) = 0.6454... bytes per character, rounded up.
        checked((int)Math.Ceiling(charCount * 0.6455)) + 1;

    /// <inheritdoc />
    public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten)
    {
        // The Try surface reports an undersized destination with false instead of throwing, so callers
        // can budget with GetMaxEncodedLength and retry.
        var text = Encode(source);
        if (text.Length > destination.Length)
        {
            charsWritten = 0;
            return false;
        }

        text.CopyTo(destination);
        charsWritten = text.Length;
        return true;
    }

    /// <inheritdoc />
    public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten)
    {
        // Validate up front so malformed text yields false here rather than the FormatException Decode throws.
        if (!IsValid(source))
        {
            bytesWritten = 0;
            return false;
        }

        var bytes = Decode(source);
        if (bytes.Length > destination.Length)
        {
            bytesWritten = 0;
            return false;
        }

        bytes.CopyTo(destination);
        bytesWritten = bytes.Length;
        return true;
    }

    private static int DigitValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'A' and <= 'Z' => c - 'A' + 10,
        _ => -1,
    };
}
