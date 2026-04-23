// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfbModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies the Output Feedback (OFB) mode transformation to an underlying <see cref="IBlockCipher" />, turning it into a synchronous
/// stream cipher in which encryption and decryption are identical operations.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/classic-modes.svg" alt="OFB panel — the cipher's output keystream feeds forward into the next cipher input, independent of plaintext." />
/// </para>
/// <para>
/// The keystream is produced by repeatedly encrypting the feedback register: <c>Oᵢ = E(Oᵢ₋₁)</c> with <c>O₀ = IV</c>, and the output
/// is <c>Pᵢ ⊕ Oᵢ</c>.
/// See <b>panel 4</b> of the diagram above: the dashed feedback arrows run between cipher <em>output</em> and the next cipher
/// <em>input</em> — in contrast to CFB (panel 3), where feedback runs from ciphertext. That structural difference is what
/// makes OFB a synchronous stream cipher — the keystream is independent of the plaintext — and immune to bit-flip propagation.
/// </para>
/// <para>
/// The initialisation vector must equal the cipher block size in length and must never be reused under the same
/// key, otherwise keystreams collide and confidentiality is lost.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/cipher-modes.html#ofb--synchronous-stream-cipher">OFB walk-through in the cipher-modes guide</seealso>
public sealed class OfbModeTransform : IBlockCipherModeTransform
{
    private readonly IBlockCipher _cipher;
    private readonly byte[] _currentIv;

    /// <summary>
    /// Initialises a new instance of the <see cref="OfbModeTransform" /> class with the specified cipher and initialisation vector.
    /// </summary>
    /// <param name="cipher">The block cipher used to generate the keystream.</param>
    /// <param name="iv">The initialisation vector used to seed the feedback register. A defensive copy is taken.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
    public OfbModeTransform(IBlockCipher _cipher, byte[] iv)
    {
        this._cipher = _cipher ?? throw new ArgumentNullException(nameof(_cipher));
        if (iv is null)
            throw new ArgumentNullException(nameof(iv));
        if (iv.Length != _cipher.BlockSize)
            throw new ArgumentException(
                $"IV length ({iv.Length}) must equal the _cipher block size ({_cipher.BlockSize}).",
                nameof(iv));
        this._currentIv = (byte[])iv.Clone();
    }

    /// <inheritdoc />
    public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
    {
        int blockSize = this._cipher.BlockSize;

        ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

        Span<byte> keystream = stackalloc byte[blockSize];

        for (int offset = 0; offset < input.Length; offset += blockSize)
        {
            ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
            Span<byte> outBlock = output.Slice(offset, blockSize);

            // Encrypt the feedback register to generate keystream
            this._cipher.Encrypt(this._currentIv, keystream);

            // XOR keystream with plaintext or ciphertext
            for (int i = 0; i < blockSize; i++)
                outBlock[i] = (byte)(inBlock[i] ^ keystream[i]);

            // Update feedback register with generated keystream
            keystream.CopyTo(this._currentIv);
        }

        return input.Length;
    }
}
