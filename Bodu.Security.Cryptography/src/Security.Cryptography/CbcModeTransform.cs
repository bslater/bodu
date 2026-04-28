// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CbcModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Bodu.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies the Cipher Block Chaining (CBC) mode transformation to an underlying <see cref="IBlockCipher" />.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/classic-modes.svg" alt="CBC panel — each plaintext block is XORed with the previous ciphertext before encryption; the first block uses the IV." />
/// </para>
/// <para>
/// Encryption computes <c>Cᵢ = E(Pᵢ ⊕ Cᵢ₋₁)</c> with <c>C₋₁ = IV</c>, and decryption inverts this as <c>Pᵢ = D(Cᵢ) ⊕ Cᵢ₋₁</c>.
/// See <b>panel 2</b> of the diagram above: the dashed feedback line feeds each ciphertext block forward into the
/// XOR that precedes the next encryption, and the IV supplies that feedback for the very first block.
/// </para>
/// <para>
/// The initialisation vector must equal the cipher block size in length and should be unpredictable for each message; repeating an IV
/// under the same key weakens confidentiality. The instance retains the most recent ciphertext block as the chaining value, so
/// successive calls to <see cref="Transform" /> continue the stream.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/cipher-modes.html#cbc--the-default">CBC walk-through in the cipher-modes guide</seealso>
public sealed class CbcModeTransform
    : IBlockCipherModeTransform
{
    private readonly IBlockCipher _cipher;
    private readonly byte[] _currentIv;

    /// <summary>
    /// Initializes a new instance of the <see cref="CbcModeTransform" /> class with the specified cipher and initialisation vector.
    /// </summary>
    /// <param name="cipher">The block cipher over which CBC is applied.</param>
    /// <param name="iv">The initialisation vector used as the chaining value for the first block. A defensive copy is taken.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
    public CbcModeTransform(IBlockCipher cipher, byte[] iv)
    {
        this._cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
        if (iv is null)
            throw new ArgumentNullException(nameof(iv));
        if (iv.Length != cipher.BlockSize)
            throw new ArgumentException(
                $"IV length ({iv.Length}) must equal the cipher block size ({cipher.BlockSize}).",
                nameof(iv));
        this._currentIv = (byte[])iv.Clone(); // Used to track the evolving IV during transformation
    }

    /// <inheritdoc />
    public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
    {
        int blockSize = this._cipher.BlockSize;

        ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize, throwIfZero: false);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

        Span<byte> tempBlock = stackalloc byte[blockSize];

        for (int offset = 0; offset < input.Length; offset += blockSize)
        {
            ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
            Span<byte> outBlock = output.Slice(offset, blockSize);

            if (encrypt)
            {
                // Encrypt: XOR input with IV, then encrypt
                for (int i = 0; i < blockSize; i++)
                    tempBlock[i] = (byte)(inBlock[i] ^ this._currentIv[i]);

                this._cipher.Encrypt(tempBlock, outBlock);

                // Update IV to the current ciphertext block
                outBlock.CopyTo(this._currentIv);
            }
            else
            {
                // Decrypt: store current ciphertext block, decrypt, then XOR with IV
                inBlock.CopyTo(tempBlock);

                this._cipher.Decrypt(inBlock, outBlock);

                for (int i = 0; i < blockSize; i++)
                    outBlock[i] ^= this._currentIv[i];

                // Update IV to original ciphertext block
                tempBlock.CopyTo(this._currentIv);
            }
        }

        return input.Length;
    }
}
