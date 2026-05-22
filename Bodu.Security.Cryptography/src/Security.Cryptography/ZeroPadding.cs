// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ZeroPadding.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements zero-byte padding, appending <c>0x00</c> bytes until the input aligns with the cipher block size.
/// </summary>
/// <remarks>
/// <para>
/// Zero padding is not self-describing and cannot be unambiguously removed when the plaintext itself may end in zero
/// bytes. Use this strategy only when the original plaintext length is recorded out-of-band or the data is known never
/// to contain trailing zeros.
/// </para>
/// <para>
/// <strong>When to choose zero padding.</strong> Pick zero padding only when the surrounding format already records the
/// plaintext length explicitly (e.g. a length-prefixed protocol frame or fixed-size record), or when the plaintext is
/// text that cannot legitimately contain trailing <c>0x00</c> bytes. For ordinary length-recoverable padding pick
/// <see cref="Pkcs7Padding" />; for AEAD modes that handle alignment internally pick <see cref="NoPadding" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
///
/// // Caller is responsible for tracking the original plaintext length —
/// // Unpad here returns the padded buffer unchanged.
/// IPaddingStrategy padding = new ZeroPadding();
/// byte[] padded = padding.Pad(plaintext, blockSize: 128); // 128 bits = 16 bytes
///]]>
/// </code>
/// </example>
public sealed class ZeroPadding
    : IPaddingStrategy
{
    /// <inheritdoc />
    public bool StripsPaddingOnUnpad => false;

    /// <summary>
    /// Pads the input with zero bytes to align its length to a multiple of the block size.
    /// </summary>
    /// <param name="input">The input data to pad.</param>
    /// <param name="blockSize">The block size in bits. Must be a positive multiple of 8.</param>
    /// <returns>The padded input.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="blockSize" /> is not a positive multiple of 8.
    /// </exception>
    public byte[] Pad(ReadOnlySpan<byte> input, int blockSize)
    {
        ThrowHelper.ThrowIfNotPositiveMultipleOf(blockSize, 8);

        var size = blockSize / 8;
        var paddingLength = size - (input.Length % size);
        if (paddingLength == size)
            paddingLength = 0; // No padding if already aligned

        var result = new byte[input.Length + paddingLength];
        input.CopyTo(result);
        return result;
    }

    /// <summary>
    /// Returns the input as-is. Zero padding cannot be safely removed unless the original length is known.
    /// </summary>
    /// <param name="input">The padded input.</param>
    /// <param name="blockSize">The block size in bits. Must be a positive multiple of 8.</param>
    /// <returns>The original input with zero padding preserved.</returns>
    /// <remarks>
    /// The method does not remove trailing zeros because it cannot distinguish between padding and legitimate data.
    /// </remarks>
    public byte[] Unpad(ReadOnlySpan<byte> input, int blockSize) => input.ToArray();
}
