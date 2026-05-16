// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso7816_4Padding.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the ISO/IEC 7816-4 padding scheme (also known as "one-and-zeros" or bit
/// padding). The first pad byte is <c>0x80</c> and remaining pad bytes are <c>0x00</c>.
/// </summary>
/// <remarks>
/// <para>
/// A full block of padding is always added when the input is already block-aligned so
/// that <see cref="Unpad"/> can unambiguously recover the original length by locating
/// the terminator. The scheme is widely used in smart-card protocols, SHA-3/Keccak and
/// CMAC. <see cref="Unpad"/> validates in constant time over the final block to resist
/// padding-oracle side channels.
/// </para>
/// <para>
/// <strong>When to choose ISO/IEC 7816-4.</strong> Pick this when interoperating with smart-card, EMV, or
/// ISO crypto tooling — the <c>0x80</c>/<c>0x00</c> sentinel scheme is the standard there. It is also the
/// padding used by the SHA-3 / Keccak family and by CMAC. For general-purpose .NET / Java / OpenSSL interop
/// prefer <see cref="Pkcs7Padding"/>; for AEAD modes that handle their own alignment use
/// <see cref="NoPadding"/>.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using Bodu.Security.Cryptography;
///
/// IPaddingStrategy padding = new Iso7816_4Padding();
/// byte[] padded = padding.Pad(plaintext, blockSize: 128); // 128 bits = 16 bytes
/// // padded ends with 0x80 followed by zero or more 0x00 bytes.
/// byte[] recovered = padding.Unpad(padded, blockSize: 128);
/// </code>
/// </example>
public sealed class Iso7816_4Padding
    : IPaddingStrategy
{
    /// <inheritdoc />
    public bool StripsPaddingOnUnpad => true;

    /// <summary>
    /// Applies ISO/IEC 7816-4 padding to the input data, ensuring the total output is a
    /// multiple of the block size.
    /// </summary>
    /// <param name="input">The data to pad.</param>
    /// <param name="blockSize">The block size in bits. Must be a positive multiple of 8.</param>
    /// <returns>The padded data as a byte array.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="blockSize"/> is less than or equal to zero.</exception>
    public byte[] Pad(ReadOnlySpan<byte> input, int blockSize)
    {
        if (blockSize <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(blockSize),
                string.Format(CryptoResourceStrings.Arg_OutOfRange_BlockSizeMustBeGreaterThan, 0));

        var size = blockSize / 8;
        var paddingLength = size - (input.Length % size);
        if (paddingLength == 0)
            paddingLength = size;

        var result = new byte[input.Length + paddingLength];
        input.CopyTo(result);

        // First pad byte is 0x80; remaining pad bytes are 0x00 (already from allocation).
        result[input.Length] = 0x80;

        return result;
    }

    /// <summary>
    /// Validates and removes ISO/IEC 7816-4 padding from the specified input data.
    /// </summary>
    /// <param name="input">The padded data.</param>
    /// <param name="blockSize">The block size in bits. Must be a positive multiple of 8.</param>
    /// <returns>The unpadded data as a byte array.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="input"/> is empty or not aligned to the block size.</exception>
    /// <exception cref="CryptographicException">Thrown if the padding terminator is missing or malformed.</exception>
    public byte[] Unpad(ReadOnlySpan<byte> input, int blockSize)
    {
        if (blockSize <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(blockSize),
                string.Format(CryptoResourceStrings.Arg_OutOfRange_BlockSizeMustBeGreaterThan, 0));

        var size = blockSize / 8;

        if (input.Length == 0 || input.Length % size != 0)
            CryptoHelpers.ThrowInvalidPaddedSequence("ISO/IEC 7816-4", nameof(input));

        var length = input.Length;
        var start = length - size;

        // Constant-time terminator scan over the final block. Walk every byte; for each
        // position compute two masks:
        //  - terminatorHere: 1 iff the byte equals 0x80 and no terminator has been seen yet.
        //  - validTail:      1 iff the byte equals 0x00 (expected after the terminator).
        // Accumulate the terminator index and verify the tail beyond it is all zeros.
        var terminatorSeen = 0;
        var terminatorIndex = -1;
        var valid = 1;

        for (var i = length - 1; i >= start; i--)
        {
            var b = input[i];

            // is80 = (b == 0x80) ? 1 : 0 (branchless)
            var xor80 = b ^ 0x80;
            var is80 = (((xor80 - 1) & ~xor80) >> 31) & 1;

            // is00 = (b == 0x00) ? 1 : 0 (branchless)
            var is00 = (((b - 1) & ~b) >> 31) & 1;

            // First 0x80 found while walking backwards marks the terminator.
            var firstTerminatorHere = is80 & (1 - terminatorSeen);

            // Record the terminator index (branchless).
            terminatorIndex = (firstTerminatorHere * i) + ((1 - firstTerminatorHere) * terminatorIndex);
            terminatorSeen |= firstTerminatorHere;

            // Before the terminator is found, every byte must be 0x00. After the terminator
            // is found (including the terminator byte itself), no further constraint applies
            // to that iteration — the bytes further left belong to the plaintext.
            var constraint = terminatorSeen | is00;
            valid &= constraint;
        }

        if (terminatorSeen == 0 || valid == 0)
            CryptoHelpers.ThrowInvalidPadding("ISO/IEC 7816-4");

        return input[..terminatorIndex].ToArray();
    }
}
