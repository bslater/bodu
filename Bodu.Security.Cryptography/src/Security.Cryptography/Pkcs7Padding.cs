// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Pkcs7Padding.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the PKCS#7 padding scheme (RFC 5652), which appends <c>N</c> bytes of value <c>N</c> to align the input
/// to the cipher block size.
/// </summary>
/// <remarks>
/// <para>
/// A full block of padding is always added when the input length is already a multiple of the block size, so that
/// <see cref="Unpad" /> can unambiguously recover the original plaintext length. Valid values of <c>N</c> lie in the
/// range <c>1..blockSize</c>.
/// </para>
/// <para>
/// <strong>When to choose PKCS7.</strong> The default for confidentiality-only block-cipher modes (CBC, ECB) — PKCS#7
/// is the padding that every mainstream cryptographic library ships as the default, and what every interoperable file
/// format expects. For ISO/EMV environments use <see cref="Iso7816_4Padding" />; when integrating with code that emits
/// trailing zeros use <see cref="ZeroPadding" />; when the surrounding mode (CTR, CTS, AEAD) provides its own alignment
/// use <see cref="NoPadding" />.
/// </para>
/// <note type="caution"> PKCS#7 unpadding under CBC is the canonical setting for the padding-oracle attack. Always pair
/// PKCS#7-padded CBC with a separate authenticator (HMAC over the ciphertext) or, preferably, replace the whole
/// construction with an AEAD mode such as <see cref="GcmModeTransform" /> or <see cref="EaxModeTransform" />. </note>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
///
/// IPaddingStrategy padding = new Pkcs7Padding();
/// byte[] padded = padding.Pad(plaintext, blockSize: 128); // 128 bits = 16 bytes
///
/// // padded.Length is a multiple of 16; the trailing N bytes each equal N.
/// byte[] recovered = padding.Unpad(padded, blockSize: 128);
///]]>
/// </code>
/// </example>
public sealed class Pkcs7Padding
    : IPaddingStrategy
{
    /// <inheritdoc />
    public bool StripsPaddingOnUnpad => true;

    /// <summary>
    /// Applies PKCS#7 padding to the input data, ensuring the total output is a multiple of the block size.
    /// </summary>
    /// <param name="input">The data to pad.</param>
    /// <param name="blockSize">The block size in bits. Must be a positive multiple of 8.</param>
    /// <returns>The padded data as a byte array.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="blockSize" /> is not a positive multiple of 8.
    /// </exception>
    public byte[] Pad(ReadOnlySpan<byte> input, int blockSize)
    {
        ThrowHelper.ThrowIfNotPositiveMultipleOf(blockSize, 8);

        var size = blockSize / 8;
        var paddingLength = size - (input.Length % size);
        if (paddingLength == 0)
            paddingLength = size;

        var result = new byte[input.Length + paddingLength];
        input.CopyTo(result);
        for (var i = input.Length; i < result.Length; i++)
            result[i] = (byte)paddingLength;

        return result;
    }

    /// <summary>
    /// Validates and removes PKCS#7 padding from the specified input data.
    /// </summary>
    /// <param name="input">The padded data.</param>
    /// <param name="blockSize">The block size in bits. Must be a positive multiple of 8.</param>
    /// <returns>The unpadded data as a byte array.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="blockSize" /> is not a positive multiple of 8.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="input" /> is empty or not aligned to the block size.
    /// </exception>
    /// <exception cref="CryptographicException">Thrown if the padding is invalid or malformed.</exception>
    public byte[] Unpad(ReadOnlySpan<byte> input, int blockSize)
    {
        ThrowHelper.ThrowIfNotPositiveMultipleOf(blockSize, 8);

        var size = blockSize / 8;

        // Constant-time padding verification to mitigate CBC padding-oracle attacks (Vaudenay 2002).
        if (input.Length == 0 || input.Length % size != 0)
            CryptoHelpers.ThrowInvalidPaddedSequence("PKCS#7", nameof(input));

        var length = input.Length;
        int padLen = input[length - 1];

        // valid = (padLen >= 1) & (padLen <= size), as a 0/1 mask, branchlessly.
        // padLen is a byte value in [0, 255]; (-padLen) is in [-255, 0] and its sign bit
        // is set iff padLen > 0, which gives geOne = 1 iff padLen >= 1.
        var geOne = ((-padLen) >> 31) & 1;                  // 1 iff padLen >= 1
        var leBlock = ((padLen - size - 1) >> 31) & 1;      // 1 iff padLen <= size
        var valid = geOne & leBlock;

        // effective = valid == 1 ? padLen : size  (branchless)
        var effective = (valid * padLen) + ((1 - valid) * size);

        // Walk the last block-size bytes unconditionally.
        var start = length - size;
        for (var i = start; i < length; i++)
        {
            // shouldBePadByte = (i >= length - effective) ? 1 : 0  (branchless)
            var diff = i - (length - effective);
            var shouldBePadByte = ((~diff) >> 31) & 1; // 1 iff diff >= 0

            // matches = (input[i] == padLen) ? 1 : 0  (branchless)
            var xor = input[i] ^ padLen;
            var matches = (((xor - 1) & ~xor) >> 31) & 1; // 1 iff xor == 0

            // Accumulate: valid &= (shouldBePadByte == 0) | (matches == 1)
            var constraint = (1 - shouldBePadByte) | matches;
            valid &= constraint;
        }

        if (valid == 0)
            CryptoHelpers.ThrowInvalidPadding("PKCS#7");

        return input[.. (length - padLen)].ToArray();
    }
}
