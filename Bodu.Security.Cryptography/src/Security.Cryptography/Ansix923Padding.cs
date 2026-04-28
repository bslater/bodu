// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ansix923Padding.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the ANSI X.923 padding scheme, which appends <c>N - 1</c> bytes of value
/// <c>0x00</c> followed by a trailing byte holding the padding length <c>N</c>.
/// </summary>
/// <remarks>
/// A full block of padding is always added when the input is already block-aligned so
/// that <see cref="Unpad" /> can unambiguously recover the original length. Valid values
/// of <c>N</c> lie in the range <c>1..blockSize</c>. <see cref="Unpad" /> validates in
/// constant time to resist padding-oracle side channels.
/// </remarks>
public sealed class Ansix923Padding : IPaddingStrategy
{
    /// <inheritdoc />
    public bool StripsPaddingOnUnpad => true;

    /// <summary>
    /// Applies ANSI X.923 padding to the input data, ensuring the total output is a
    /// multiple of the block size.
    /// </summary>
    /// <param name="input">The data to pad.</param>
    /// <param name="blockSize">The block size in bytes.</param>
    /// <returns>The padded data as a byte array.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="blockSize" /> is less than or equal to zero.</exception>
    public byte[] Pad(ReadOnlySpan<byte> input, int blockSize)
    {
        if (blockSize <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(blockSize),
                string.Format(ResourceStrings.ArgumentOutOfRangeException_BlockSizeMustBeGreaterThan, 0));

        int paddingLength = blockSize - (input.Length % blockSize);
        if (paddingLength == 0)
            paddingLength = blockSize;

        byte[] result = new byte[input.Length + paddingLength];
        input.CopyTo(result);

        // Remaining pad bytes are already 0x00 from array allocation; only the trailing
        // length byte needs to be written.
        result[result.Length - 1] = (byte)paddingLength;

        return result;
    }

    /// <summary>
    /// Validates and removes ANSI X.923 padding from the specified input data.
    /// </summary>
    /// <param name="input">The padded data.</param>
    /// <param name="blockSize">The block size in bytes.</param>
    /// <returns>The unpadded data as a byte array.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="input" /> is empty or not aligned to the block size.</exception>
    /// <exception cref="CryptographicException">Thrown if the padding is invalid or malformed.</exception>
    public byte[] Unpad(ReadOnlySpan<byte> input, int blockSize)
    {
        // Constant-time verification to mitigate CBC padding-oracle attacks.
        if (input.Length == 0 || input.Length % blockSize != 0)
            throw new ArgumentException("Input is not a valid ANSI X.923 padded block sequence.", nameof(input));

        int length = input.Length;
        int padLen = input[length - 1];

        int geOne = ((-padLen) >> 31) & 1;                  // 1 iff padLen >= 1
        int leBlock = ((padLen - blockSize - 1) >> 31) & 1; // 1 iff padLen <= blockSize
        int valid = geOne & leBlock;

        // effective = valid == 1 ? padLen : blockSize (branchless)
        int effective = (valid * padLen) + ((1 - valid) * blockSize);

        // Walk the last blockSize bytes unconditionally. Every byte in the padding
        // region, other than the final length byte, must be 0x00.
        int start = length - blockSize;
        int lastIndex = length - 1;
        for (int i = start; i < length; i++)
        {
            int diff = i - (length - effective);
            int shouldBePadByte = ((~diff) >> 31) & 1; // 1 iff i >= length - effective

            // isLastByte = (i == length - 1) ? 1 : 0 (branchless)
            int xorLast = i ^ lastIndex;
            int isLastByte = (((xorLast - 1) & ~xorLast) >> 31) & 1;

            // Expected value in padding region: 0x00 for interior bytes, padLen for the last byte.
            int expected = isLastByte * padLen;
            int xorExpected = input[i] ^ expected;
            int matches = (((xorExpected - 1) & ~xorExpected) >> 31) & 1;

            int constraint = (1 - shouldBePadByte) | matches;
            valid &= constraint;
        }

        if (valid == 0)
            throw new CryptographicException("Invalid ANSI X.923 padding.");

        return input.Slice(0, length - padLen).ToArray();
    }
}
