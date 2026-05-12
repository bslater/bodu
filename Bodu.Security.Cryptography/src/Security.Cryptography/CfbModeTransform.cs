// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CfbModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies the Cipher Feedback (CFB) mode transformation to an underlying <see cref="IBlockCipher"/>, turning it into a
/// self-synchronising stream cipher.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/classic-modes.svg" alt="CFB panel — the previous ciphertext is fed back as the cipher input; its encryption produces a keystream XORed with plaintext."/>
/// </para>
/// <para>
/// Both directions use the cipher's encryption primitive: encryption computes <c>Cᵢ = Pᵢ ⊕ E(IVᵢ)</c> and decryption
/// <c>Pᵢ = Cᵢ ⊕ E(IVᵢ)</c>, with <c>IV₀</c> supplied by the caller and <c>IVᵢ₊₁ = Cᵢ</c> for subsequent blocks.
/// See <b>panel 3</b> of the diagram above: the dashed feedback lines carry ciphertext blocks back into the next cipher
/// input — the cipher runs the same direction (encrypt) for both encryption and decryption, and the plaintext simply XORs
/// into or out of the resulting keystream.
/// </para>
/// <para>
/// The initialisation vector must equal the cipher block size in length and should be unique and unpredictable per message under a given key.
/// </para>
/// <para>
/// <strong>When to use CFB.</strong> Pick CFB only for interoperability with legacy formats — it was the
/// stream-cipher mode of choice in PGP / OpenPGP and certain disk-encryption layouts. CFB removes the padding
/// requirement that <see cref="CbcModeTransform"/> imposes, but inherits the same lack of authentication and
/// adds bit-flip propagation across multiple blocks. For new code prefer <see cref="CtrModeTransform"/> for
/// stream-cipher behaviour, or an AEAD mode (<see cref="GcmModeTransform"/>, <see cref="EaxModeTransform"/>)
/// for authenticated encryption.
/// </para>
/// <para>
/// CFB is sequential at the block level: each ciphertext block must be produced before the next can be
/// computed, so the mode does not parallelise within a message.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
///
/// // Most callers should set SymmetricAlgorithm.Mode = CipherBlockMode.CFB instead of using this directly.
/// using IBlockCipher cipher = new AesBlockCipher(key);
/// byte[] iv = RandomNumberGenerator.GetBytes(cipher.BlockSize / 8);
/// IBlockCipherModeTransform cfb = new CfbModeTransform(cipher, iv);
///
/// byte[] ciphertext = new byte[plaintext.Length];
/// int written = cfb.Transform(plaintext, ciphertext, encrypt: true);
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/cipher-modes.html#cfb--self-synchronising-stream-cipher">CFB walk-through in the cipher-modes guide</seealso>
public sealed class CfbModeTransform : IBlockCipherModeTransform
{
    private readonly IBlockCipher _cipher;
    private readonly byte[] _currentIv;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CfbModeTransform"/> class with the specified cipher and initialisation vector.
    /// </summary>
    /// <param name="cipher">The block cipher over which CFB is applied.</param>
    /// <param name="iv">The initialisation vector used as the feedback register for the first block. A defensive copy is taken.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher"/> or <paramref name="iv"/> is <see langword="null"/>.</exception>
    public CfbModeTransform(IBlockCipher cipher, byte[] iv)
    {
        ThrowHelper.ThrowIfNull(cipher);
        CryptoHelpers.ThrowIfIvLengthInvalid(iv, cipher.BlockSize);

        this._cipher = cipher;
        this._currentIv = (byte[])iv.Clone();
    }

    /// <inheritdoc />
    public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
    {
        var blockSize = this._cipher.BlockSize / 8;

        // Empty input is a no-op, consistent with CbcModeTransform.
        CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize, throwIfZero: false);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

        Span<byte> feedback = stackalloc byte[blockSize];

        for (var offset = 0; offset < input.Length; offset += blockSize)
        {
            ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
            Span<byte> outBlock = output.Slice(offset, blockSize);

            // Encrypt the current IV (used as feedback input)
            this._cipher.Encrypt(this._currentIv, feedback);

            if (encrypt)
            {
                // XOR plaintext with encrypted feedback to produce ciphertext
                for (var i = 0; i < blockSize; i++)
                    outBlock[i] = (byte)(inBlock[i] ^ feedback[i]);

                // Update IV to current ciphertext block
                outBlock.CopyTo(this._currentIv);
            }
            else
            {
                // XOR ciphertext with encrypted feedback to produce plaintext
                for (var i = 0; i < blockSize; i++)
                    outBlock[i] = (byte)(inBlock[i] ^ feedback[i]);

                // Update IV to current ciphertext block
                inBlock.CopyTo(this._currentIv);
            }
        }

        return input.Length;
    }

    /// <summary>
    /// Releases the resources used by this instance and zeroes the running feedback register so
    /// that key-equivalent state does not linger in memory after disposal. The underlying
    /// <see cref="IBlockCipher"/> is not disposed by this type — ownership remains with the caller.
    /// </summary>
    /// <remarks>Idempotent.</remarks>
    public void Dispose()
    {
        if (this._disposed)
            return;

        CryptoHelpers.Clear(this._currentIv);
        this._disposed = true;
        GC.SuppressFinalize(this);
    }
}
