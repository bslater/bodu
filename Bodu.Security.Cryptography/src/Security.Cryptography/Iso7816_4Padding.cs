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
/// A full block of padding is always added when the input is already block-aligned so
/// that <see cref="Unpad" /> can unambiguously recover the original length by locating
/// the terminator. The scheme is widely used in smart-card protocols, SHA-3/Keccak and
/// CMAC. <see cref="Unpad" /> validates in constant time over the final block to resist
/// padding-oracle side channels.
/// </remarks>
public sealed class Iso7816_4Padding : IPaddingStrategy
{
    /// <inheritdoc />
    public bool StripsPaddingOnUnpad => true;

    /// <summary>
    /// Applies ISO/IEC 7816-4 padding to the input data, ensuring the total output is a
    /// multiple of the block size.
    /// </summary>
    /// <param name="input">The data to pad.</param>
    /// <param name="blockSize">The block size in bytes.</param>
    /// <returns>The padded data as a byte array.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="blockSize" /> is less than or equal to zero.</exception>
    public byte[] Pad(ReadOnlySpan<byte> input, int blockSize)
    {
        if (blockSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(blockSize), "Block size must be greater than zero.");

        int paddingLength = blockSize - (input.Length % blockSize);
        if (paddingLength == 0)
            paddingLength = blockSize;

        byte[] result = new byte[input.Length + paddingLength];
        input.CopyTo(result);

        // First pad byte is 0x80; remaining pad bytes are 0x00 (already from allocation).
        result[input.Length] = 0x80;

        return result;
    }

    /// <summary>
    /// Validates and removes ISO/IEC 7816-4 padding from the specified input data.
    /// </summary>
    /// <param name="input">The padded data.</param>
    /// <param name="blockSize">The block size in bytes.</param>
    /// <returns>The unpadded data as a byte array.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="input" /> is empty or not aligned to the block size.</exception>
    /// <exception cref="CryptographicException">Thrown if the padding terminator is missing or malformed.</exception>
    public byte[] Unpad(ReadOnlySpan<byte> input, int blockSize)
    {
        if (input.Length == 0 || input.Length % blockSize != 0)
            throw new ArgumentException("Input is not a valid ISO/IEC 7816-4 padded block sequence.", nameof(input));

        int length = input.Length;
        int start = length - blockSize;

        // Constant-time terminator scan over the final block. Walk every byte; for each
        // position compute two masks:
        //  - terminatorHere: 1 iff the byte equals 0x80 and no terminator has been seen yet.
        //  - validTail:      1 iff the byte equals 0x00 (expected after the terminator).
        // Accumulate the terminator index and verify the tail beyond it is all zeros.
        int terminatorSeen = 0;
        int terminatorIndex = -1;
        int valid = 1;

        for (int i = length - 1; i >= start; i--)
        {
            byte b = input[i];

            // is80 = (b == 0x80) ? 1 : 0 (branchless)
            int xor80 = b ^ 0x80;
            int is80 = (((xor80 - 1) & ~xor80) >> 31) & 1;

            // is00 = (b == 0x00) ? 1 : 0 (branchless)
            int is00 = (((b - 1) & ~b) >> 31) & 1;

            // First 0x80 found while walking backwards marks the terminator.
            int firstTerminatorHere = is80 & (1 - terminatorSeen);

            // Record the terminator index (branchless).
            terminatorIndex = (firstTerminatorHere * i) + ((1 - firstTerminatorHere) * terminatorIndex);
            terminatorSeen |= firstTerminatorHere;

            // Before the terminator is found, every byte must be 0x00. After the terminator
            // is found (including the terminator byte itself), no further constraint applies
            // to that iteration — the bytes further left belong to the plaintext.
            int constraint = terminatorSeen | is00;
            valid &= constraint;
        }

        if (terminatorSeen == 0 || valid == 0)
            throw new CryptographicException("Invalid ISO/IEC 7816-4 padding.");

        return input.Slice(0, terminatorIndex).ToArray();
    }
}
