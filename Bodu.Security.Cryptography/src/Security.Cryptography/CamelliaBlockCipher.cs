// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaBlockCipher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the core Camellia block cipher engine, implementing low-level encryption and decryption of individual
/// 128-bit blocks.
/// </summary>
/// <remarks>
/// <para>
/// This type represents the raw Camellia block primitive. Camellia uses a 128-bit block size and accepts 128-bit,
/// 192-bit, and 256-bit keys.
/// </para>
/// <para>
/// Most callers should prefer the higher-level <see cref="Camellia" /> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> contract.
/// </para>
/// </remarks>
/// <seealso cref="Camellia" />
public sealed class CamelliaBlockCipher
    : IBlockCipher
{
    private const int BlockSizeInBytes = 16;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CamelliaBlockCipher" /> class using the specified key.
    /// </summary>
    /// <param name="key">
    /// The encryption key. Must be 16, 24, or 32 bytes in length. Must not be <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is not 16, 24, or 32 bytes in length.
    /// </exception>
    public CamelliaBlockCipher(ReadOnlySpan<byte> key)
    {
        if (key.Length is not (16 or 24 or 32))
            throw new ArgumentException("The Camellia key must be 16, 24, or 32 bytes in length.", nameof(key));

        // TODO:
        // Implement RFC 3713 key schedule:
        // - derive KA and KB
        // - generate whitening keys kw
        // - generate round keys k
        // - generate FL / FLINV keys ke
        //
        // Do not ship until validated against published Camellia known-answer tests.
    }

    /// <inheritdoc />
    public int BlockSize => BlockSizeInBytes;

    /// <inheritdoc />
    public void Dispose()
    {
        if (!this._disposed)
        {
            // TODO:
            // Clear expanded subkeys once added.

            this._disposed = true;
        }
    }

    /// <summary>
    /// Decrypts a single 128-bit block using the Camellia cipher.
    /// </summary>
    /// <param name="input">The ciphertext block to decrypt.</param>
    /// <param name="output">The plaintext output buffer.</param>
    public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockSizeInBytes);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockSizeInBytes);

        throw new NotImplementedException("Camellia block decryption has not yet been implemented.");
    }

    /// <summary>
    /// Encrypts a single 128-bit block using the Camellia cipher.
    /// </summary>
    /// <param name="input">The plaintext block to encrypt.</param>
    /// <param name="output">The ciphertext output buffer.</param>
    public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockSizeInBytes);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockSizeInBytes);

        throw new NotImplementedException("Camellia block encryption has not yet been implemented.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(nameof(CamelliaBlockCipher));
#endif
    }
}