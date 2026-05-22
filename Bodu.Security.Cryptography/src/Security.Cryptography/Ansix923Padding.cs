// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ansix923Padding.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the ANSI X.923 padding scheme, which appends <c>N - 1</c> bytes of value <c>0x00</c> followed by a
/// trailing byte holding the padding length <c>N</c>.
/// </summary>
/// <remarks>
/// <para>
/// A full block of padding is always added when the input is already block-aligned so that <see cref="Unpad" /> can
/// unambiguously recover the original length. Valid values of <c>N</c> lie in the range <c>1..blockSize</c>.
/// <see cref="Unpad" /> validates in constant time to resist padding-oracle side channels.
/// </para>
/// <para>
/// <strong>When to choose ANSI X.923.</strong> Pick this when interoperating with legacy financial / banking systems or
/// formats that explicitly require the X.923 layout. For all other cases use <see cref="Pkcs7Padding" /> — it is the
/// modern standard and is what every mainstream library expects by default.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
///
/// IPaddingStrategy padding = new Ansix923Padding();
/// byte[] padded = padding.Pad(plaintext, blockSize: 128); // 128 bits = 16 bytes
///
/// // padded ends with N-1 zero bytes followed by a single byte holding N.
/// byte[] recovered = padding.Unpad(padded, blockSize: 128);
///]]>
/// </code>
/// </example>
public sealed class Ansix923Padding
    : IPaddingStrategy
{
    /// <inheritdoc />
    public bool StripsPaddingOnUnpad => true;

    /// <summary>
    /// Applies ANSI X.923 padding to the input data, ensuring the total output is a multiple of the block size.
    /// </summary>
    /// <param name="input">The data to pad.</param>
    /// <param name="blockSize">The block size in bits.</param>
    /// <returns>The padded data as a byte array.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    public byte[] Pad(ReadOnlySpan<byte> input, int blockSize)
    {
        if (blockSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(blockSize),
                string.Format(CryptoResourceStrings.Arg_OutOfRange_BlockSizeMustBeGreaterThan, 0));
        }

        var size = blockSize / 8;
        var paddingLength = size - (input.Length % size);
        if (paddingLength == 0)
            paddingLength = size;

        var result = new byte[input.Length + paddingLength];
        input.CopyTo(result);

        // Remaining pad bytes are already 0x00 from array allocation; only the trailing
        // length byte needs to be written.
        result[^1] = (byte)paddingLength;

        return result;
    }

    /// <summary>
    /// Validates and removes ANSI X.923 padding from the specified input data.
    /// </summary>
    /// <param name="input">The padded data.</param>
    /// <param name="blockSize">The block size in bits.</param>
    /// <returns>The unpadded data as a byte array.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="input" /> is empty or not aligned to the block size.
    /// </exception>
    /// <exception cref="CryptographicException">Thrown if the padding is invalid or malformed.</exception>
    public byte[] Unpad(ReadOnlySpan<byte> input, int blockSize)
    {
        if (blockSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(blockSize),
                string.Format(CryptoResourceStrings.Arg_OutOfRange_BlockSizeMustBeGreaterThan, 0));
        }

        // Constant-time verification to mitigate CBC padding-oracle attacks.
        var size = blockSize / 8;
        if (input.Length == 0 || input.Length % size != 0)
            CryptoHelpers.ThrowInvalidPaddedSequence("ANSI X.923", nameof(input));

        var length = input.Length;
        int padLen = input[length - 1];

        var geOne = ((-padLen) >> 31) & 1;                  // 1 iff padLen >= 1
        var leBlock = ((padLen - size - 1) >> 31) & 1;      // 1 iff padLen <= blockSize
        var valid = geOne & leBlock;

        // effective = valid == 1 ? padLen : size (branchless)
        var effective = (valid * padLen) + ((1 - valid) * size);

        // Walk the last blockSize bytes unconditionally. Every byte in the padding
        // region, other than the final length byte, must be 0x00.
        var start = length - size;
        var lastIndex = length - 1;
        for (var i = start; i < length; i++)
        {
            var diff = i - (length - effective);
            var shouldBePadByte = ((~diff) >> 31) & 1; // 1 iff i >= length - effective

            // isLastByte = (i == length - 1) ? 1 : 0 (branchless)
            var xorLast = i ^ lastIndex;
            var isLastByte = (((xorLast - 1) & ~xorLast) >> 31) & 1;

            // Expected value in padding region: 0x00 for interior bytes, padLen for the last byte.
            var expected = isLastByte * padLen;
            var xorExpected = input[i] ^ expected;
            var matches = (((xorExpected - 1) & ~xorExpected) >> 31) & 1;

            var constraint = (1 - shouldBePadByte) | matches;
            valid &= constraint;
        }

        if (valid == 0)
            CryptoHelpers.ThrowInvalidPadding("ANSI X.923");

        return input[.. (length - padLen)].ToArray();
    }
}
