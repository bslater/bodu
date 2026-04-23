// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies the Electronic Codebook (ECB) mode transformation to an underlying <see cref="IBlockCipher" />, encrypting or decrypting
/// each block independently with no chaining.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/classic-modes.svg" alt="ECB panel — each plaintext block is encrypted independently to its ciphertext block with no feedback." />
/// </para>
/// <para>
/// Encryption computes <c>Cᵢ = E(Pᵢ)</c> and decryption <c>Pᵢ = D(Cᵢ)</c>; no initialisation vector is used.
/// See <b>panel 1</b> of the diagram above: each column is entirely self-contained, so the three cells carry
/// no arrows between them.
/// </para>
/// <para>
/// That independence is exactly what makes ECB insecure for virtually all real-world messages: identical plaintext
/// blocks always yield identical ciphertext blocks, leaking structural information. Prefer CBC, CTR, or an
/// authenticated mode unless ECB is required as a primitive inside a larger construction.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/cipher-modes.html#ecb--almost-never">ECB walk-through in the cipher-modes guide</seealso>
public sealed class EcbModeTransform : IBlockCipherModeTransform
{
    private readonly IBlockCipher _cipher;

    /// <summary>
    /// Initialises a new instance of the <see cref="EcbModeTransform" /> class that wraps the specified block cipher.
    /// </summary>
    /// <param name="cipher">The block cipher over which ECB is applied.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> is <see langword="null" />.</exception>
    public EcbModeTransform(IBlockCipher _cipher)
    {
        this._cipher = _cipher ?? throw new ArgumentNullException(nameof(_cipher));
    }

    /// <inheritdoc />
    public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
    {
        int blockSize = this._cipher.BlockSize;

        ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

        for (int offset = 0; offset < input.Length; offset += blockSize)
        {
            ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
            Span<byte> outBlock = output.Slice(offset, blockSize);

            if (encrypt)
                this._cipher.Encrypt(inBlock, outBlock);
            else
                this._cipher.Decrypt(inBlock, outBlock);
        }

        return input.Length;
    }
}
