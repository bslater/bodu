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
/// <para>
/// <strong>When to use ECB.</strong> Only as a building block inside a larger, well-understood construction —
/// for example, encrypting a single fixed-length tweak inside an XTS or wide-block scheme. For protecting
/// arbitrary plaintext, use <see cref="CbcModeTransform"/> as a baseline, <see cref="CtrModeTransform"/> when
/// random access or stream-cipher behaviour is wanted, or one of the AEAD modes
/// (<see cref="GcmModeTransform"/>, <see cref="EaxModeTransform"/>, …) when authentication matters.
/// </para>
/// <para>
/// Because ECB has no chaining state, the transform is stateless — repeated <see cref="Transform"/> calls produce
/// the same result for the same input — and is trivially parallelisable.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
///
/// // Most callers should reach for SymmetricAlgorithm.Mode = ECB instead of constructing this directly.
/// // Direct use is appropriate only inside larger primitives (e.g. wide-block constructions, KDFs).
/// using IBlockCipher cipher = new AesBlockCipher(key);
/// IBlockCipherModeTransform ecb = new EcbModeTransform(cipher);
///
/// byte[] ciphertext = new byte[plaintext.Length];
/// int written = ecb.Transform(plaintext, ciphertext, encrypt: true);
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/cipher-modes.html#ecb--almost-never">ECB walk-through in the cipher-modes guide</seealso>
public sealed class EcbModeTransform : IBlockCipherModeTransform
{
    private readonly IBlockCipher _cipher;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcbModeTransform" /> class that wraps the specified block cipher.
    /// </summary>
    /// <param name="cipher">The block cipher over which ECB is applied.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> is <see langword="null" />.</exception>
    public EcbModeTransform(IBlockCipher cipher)
    {
        this._cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
    }

    /// <inheritdoc />
    public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
    {
        int blockSize = this._cipher.BlockSize;

        // Empty input is a no-op, consistent with CbcModeTransform.
        CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize, throwIfZero: false);
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
