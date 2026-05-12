// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AesBlockCipher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exposes the BCL <see cref="Aes"/> algorithm as an <see cref="IBlockCipher"/>, providing the
/// single-block primitive that the authenticated-mode transforms (<see cref="GcmModeTransform"/>,
/// <see cref="CcmModeTransform"/>, <see cref="OcbModeTransform"/>, <see cref="SivModeTransform"/>,
/// <see cref="GcmSivModeTransform"/>) require.
/// </summary>
/// <remarks>
/// <para>
/// The adapter encrypts and decrypts exactly one 16-byte block per call, in ECB mode with no padding,
/// delegating to the BCL's hardware-accelerated <see cref="Aes"/> implementation. All key scheduling is
/// performed by the BCL on construction.
/// </para>
/// <para>
/// <see cref="AesBlockCipher"/> is not intended for direct encryption of user data. Wrap it in one of
/// the authenticated mode transforms listed above — the mode transform is responsible for chaining,
/// IV / nonce handling, associated-data authentication, and tag generation or verification.
/// </para>
/// <para>
/// Instances hold sensitive key material and must be disposed after use. Disposal releases the
/// underlying <see cref="Aes"/> instance and zeros its expanded key schedule.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// // Encrypt a single 16-byte block (typically used indirectly via an AEAD mode transform).
/// byte[] key = RandomNumberGenerator.GetBytes(16);
/// using var cipher = new AesBlockCipher(key);
///
/// Span&lt;byte&gt; block = stackalloc byte[16];
/// Span&lt;byte&gt; output = stackalloc byte[16];
/// cipher.Encrypt(block, output);
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/aead-modes.html">Using AEAD modes (guide with full encrypt / decrypt examples)</seealso>
public sealed class AesBlockCipher
    : IBlockCipher
{
    /// <summary>
    /// The fixed AES block size in bits.
    /// </summary>
    public const int BlockSizeBits = 128;

    private readonly Aes _aes;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesBlockCipher"/> class with the specified AES key.
    /// </summary>
    /// <param name="key">
    /// The AES key. Valid lengths are 16, 24, or 32 bytes (AES-128, AES-192, or AES-256). A defensive copy
    /// is taken — the caller may zero the original array immediately after construction.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException"><paramref name="key"/> length is not 16, 24, or 32 bytes.</exception>
    public AesBlockCipher(byte[] key)
    {
        ThrowHelper.ThrowIfNull(key);

        var aes = Aes.Create();
        try
        {
            aes.Key = key; // BCL validates length and throws CryptographicException on mismatch.
        }
        catch
        {
            aes.Dispose();
            throw;
        }

        this._aes = aes;
    }

    /// <inheritdoc />
    /// <value>Always 128 (bits), the fixed AES block size.</value>
    public int BlockSize => BlockSizeBits;

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="input"/> or <paramref name="output"/> is not exactly <see cref="BlockSize"/> / 8 bytes.
    /// </exception>
    public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockSizeBits / 8);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockSizeBits / 8);
        this.ThrowIfDisposed();

        this._aes.EncryptEcb(input, output, PaddingMode.None);
    }

    /// <summary>
    /// Releases the underlying <see cref="Aes"/> instance, zeroing its expanded key schedule.
    /// Subsequent calls to <see cref="Encrypt"/> or <see cref="Decrypt"/> throw
    /// <see cref="ObjectDisposedException"/>.
    /// </summary>
    public void Dispose()
    {
        if (!this._disposed)
        {
            this._aes.Dispose();
            this._disposed = true;
        }
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="input"/> or <paramref name="output"/> is not exactly <see cref="BlockSize"/> / 8 bytes.
    /// </exception>
    public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockSizeBits / 8);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockSizeBits / 8);
        this.ThrowIfDisposed();

        this._aes.DecryptEcb(input, output, PaddingMode.None);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(this.GetType().Name);
#endif
    }
}
