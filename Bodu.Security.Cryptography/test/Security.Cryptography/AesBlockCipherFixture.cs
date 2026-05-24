// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AesBlockCipherFixture.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Wraps the BCL <see cref="Aes" /> algorithm as an <see cref="IBlockCipher" /> for use in
/// known-answer and real-cipher round-trip tests. Each instance encrypts and decrypts single
/// 16-byte blocks in ECB mode with no padding, matching the primitive expected by every mode
/// transform constructor.
/// </summary>
/// <remarks>
/// This fixture is intentionally minimal: it exposes only the single-block primitive required
/// by <see cref="IBlockCipher" /> and delegates all key scheduling to the BCL. Dispose the
/// fixture after each test to release the underlying <see cref="Aes" /> instance.
/// </remarks>
internal sealed class AesBlockCipherFixture
    : IBlockCipher, IDisposable
{
    private readonly Aes _aes;
    private bool _disposed;

    /// <summary>
    /// Initialises a new instance using <paramref name="key" /> as the AES key.
    /// Supported key lengths are 16, 24, and 32 bytes (AES-128 / 192 / 256).
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException"><paramref name="key" /> length is invalid for AES.</exception>
    public AesBlockCipherFixture(byte[] key)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));
        _aes = Aes.Create();
        _aes.Key = key;
    }

    /// <inheritdoc />
    /// <value>Length of the AES block is 128 bits (16 bytes).</value>
    public int BlockSize => _aes.BlockSize;

    /// <inheritdoc />
    public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _aes.EncryptEcb(input, output, PaddingMode.None);
    }

    /// <inheritdoc />
    public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _aes.DecryptEcb(input, output, PaddingMode.None);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _aes.Dispose();
            _disposed = true;
        }
    }
}
