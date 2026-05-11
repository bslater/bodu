// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CbcModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies the Cipher Block Chaining (CBC) mode transformation to an underlying <see cref="IBlockCipher"/>.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/classic-modes.svg" alt="CBC panel — each plaintext block is XORed with the previous ciphertext before encryption; the first block uses the IV."/>
/// </para>
/// <para>
/// Encryption computes <c>Cᵢ = E(Pᵢ ⊕ Cᵢ₋₁)</c> with <c>C₋₁ = IV</c>, and decryption inverts this as <c>Pᵢ = D(Cᵢ) ⊕ Cᵢ₋₁</c>.
/// See <b>panel 2</b> of the diagram above: the dashed feedback line feeds each ciphertext block forward into the
/// XOR that precedes the next encryption, and the IV supplies that feedback for the very first block.
/// </para>
/// <para>
/// The initialisation vector must equal the cipher block size in length and should be unpredictable for each message; repeating an IV
/// under the same key weakens confidentiality. The instance retains the most recent ciphertext block as the chaining value, so
/// successive calls to <see cref="Transform"/> continue the stream.
/// </para>
/// <para>
/// <strong>When to use CBC.</strong> The traditional confidentiality-only mode — the right pick when
/// interoperating with legacy protocols (TLS up to 1.2, JCE defaults, many file formats) or when an
/// authenticated mode is impractical. CBC requires plaintext to be a multiple of the block size, so it is
/// almost always paired with <see cref="Pkcs7Padding"/>. For new designs prefer <see cref="GcmModeTransform"/>
/// or <see cref="EaxModeTransform"/>, both of which authenticate as well as encrypt; CBC plus a separate
/// MAC is fragile and easy to misuse. Decryption with strippable padding is vulnerable to padding-oracle
/// attacks unless the padding check is performed in constant time and never leaks via timing or error messages.
/// </para>
/// <para>
/// CBC is sequential — neither encryption nor decryption parallelises across blocks within a single message.
/// Random access into the ciphertext is not supported.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
///
/// // Most callers should set SymmetricAlgorithm.Mode = CipherBlockMode.CBC instead of using this directly.
/// using IBlockCipher cipher = new AesBlockCipher(key);
/// byte[] iv = RandomNumberGenerator.GetBytes(cipher.BlockSize); // unique per message
/// IBlockCipherModeTransform cbc = new CbcModeTransform(cipher, iv);
///
/// byte[] ciphertext = new byte[paddedPlaintext.Length];
/// int written = cbc.Transform(paddedPlaintext, ciphertext, encrypt: true);
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/cipher-modes.html#cbc--the-default">CBC walk-through in the cipher-modes guide</seealso>
public sealed class CbcModeTransform
    : IBlockCipherModeTransform
{
    private readonly IBlockCipher _cipher;
    private readonly byte[] _currentIv;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CbcModeTransform"/> class with the specified cipher and initialisation vector.
    /// </summary>
    /// <param name="cipher">The block cipher over which CBC is applied.</param>
    /// <param name="iv">The initialisation vector used as the chaining value for the first block. A defensive copy is taken.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher"/> or <paramref name="iv"/> is <see langword="null"/>.</exception>
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
        var blockSize = this._cipher.BlockSize;

        CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize, throwIfZero: false);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

        Span<byte> tempBlock = stackalloc byte[blockSize];

        for (var offset = 0; offset < input.Length; offset += blockSize)
        {
            ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
            Span<byte> outBlock = output.Slice(offset, blockSize);

            if (encrypt)
            {
                // Encrypt: XOR input with IV, then encrypt
                for (var i = 0; i < blockSize; i++)
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

                for (var i = 0; i < blockSize; i++)
                    outBlock[i] ^= this._currentIv[i];

                // Update IV to original ciphertext block
                tempBlock.CopyTo(this._currentIv);
            }
        }

        return input.Length;
    }

    /// <summary>
    /// Releases the resources used by this instance and zeroes the running chaining vector so that
    /// key-equivalent IV state does not linger in memory after disposal. The underlying
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
