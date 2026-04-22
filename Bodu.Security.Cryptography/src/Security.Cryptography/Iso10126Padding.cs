// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso10126Padding.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the ISO 10126 padding scheme, which appends <c>N - 1</c> cryptographically
/// random bytes followed by a trailing byte holding the padding length <c>N</c>.
/// </summary>
/// <remarks>
/// A full block of padding is always added when the input is already block-aligned so
/// that <see cref="Unpad" /> can unambiguously recover the original length. The interior
/// pad bytes are not reconstructable on decryption, so only the trailing length byte is
/// validated. ISO 10126 was withdrawn by ISO in 2007; it is supported for interoperability
/// with existing ciphertexts.
/// </remarks>
public sealed class Iso10126Padding : IPaddingStrategy
{
    /// <inheritdoc />
    public bool StripsPaddingOnUnpad => true;

    /// <summary>
    /// Applies ISO 10126 padding to the input data, ensuring the total output is a
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

        // Fill the interior pad region with random bytes, then overwrite the final byte
        // with the pad length. paddingLength - 1 can be zero (when paddingLength == 1),
        // in which case the interior region is empty and only the length byte is written.
        int interiorLength = paddingLength - 1;
        if (interiorLength > 0)
        {
            Span<byte> interior = result.AsSpan(input.Length, interiorLength);
            RandomNumberGenerator.Fill(interior);
        }

        result[result.Length - 1] = (byte)paddingLength;

        return result;
    }

    /// <summary>
    /// Validates and removes ISO 10126 padding from the specified input data.
    /// </summary>
    /// <param name="input">The padded data.</param>
    /// <param name="blockSize">The block size in bytes.</param>
    /// <returns>The unpadded data as a byte array.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="input" /> is empty or not aligned to the block size.</exception>
    /// <exception cref="CryptographicException">Thrown if the trailing length byte is out of range.</exception>
    public byte[] Unpad(ReadOnlySpan<byte> input, int blockSize)
    {
        if (input.Length == 0 || input.Length % blockSize != 0)
            throw new ArgumentException("Input is not a valid ISO 10126 padded block sequence.", nameof(input));

        int length = input.Length;
        int padLen = input[length - 1];

        // Only the trailing length byte can be validated; interior pad bytes are random.
        if (padLen < 1 || padLen > blockSize)
            throw new CryptographicException("Invalid ISO 10126 padding.");

        return input.Slice(0, length - padLen).ToArray();
    }
}
