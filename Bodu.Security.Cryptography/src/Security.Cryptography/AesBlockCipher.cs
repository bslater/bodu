// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AesBlockCipher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exposes the BCL <see cref="Aes" /> algorithm as an <see cref="IBlockCipher" />, providing the single-block primitive
/// that the authenticated-mode transforms (<see cref="GcmModeTransform" />, <see cref="CcmModeTransform" />,
/// <see cref="OcbModeTransform" />, <see cref="SivModeTransform" />, <see cref="GcmSivModeTransform" />) require.
/// </summary>
/// <remarks>
/// <para>
/// The adapter encrypts and decrypts exactly one 16-byte block per call, in ECB mode with no padding, delegating to the
/// BCL's hardware-accelerated <see cref="Aes" /> implementation. Key scheduling is performed once on construction and
/// the resulting ECB transforms are cached, so per-block calls reuse the expanded key schedule rather than rebuilding a
/// cipher context each time.
/// </para>
/// <para>
/// <see cref="AesBlockCipher" /> is not intended for direct encryption of user data. Wrap it in one of the
/// authenticated mode transforms listed above — the mode transform is responsible for chaining, IV / nonce handling,
/// associated-data authentication, and tag generation or verification.
/// </para>
/// <para>
/// Instances hold sensitive key material and must be disposed after use. Disposal releases the underlying
/// <see cref="Aes" /> instance and zeros its expanded key schedule.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Encrypt a single 16-byte block (typically used indirectly via an AEAD mode transform).
/// byte[] key = RandomNumberGenerator.GetBytes(16);
/// using var cipher = new AesBlockCipher(key);
/// Span<byte> block = stackalloc byte[16];
/// Span<byte> output = stackalloc byte[16];
/// cipher.Encrypt(block, output);
///]]>
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/aead-modes.html">Using AEAD modes (guide with full encrypt / decrypt examples)
/// </seealso>
public sealed class AesBlockCipher
    : IBlockCipher
{
    /// <summary>Length of the AES block is 128 bits (16 bytes). Internal constant kept for span-length validation; callers should read <see cref="BlockSize" /> instead.</summary>
    private const int BlockSizeBits = 128;

    /// <summary>The underlying BCL <see cref="Aes" /> instance that owns the expanded key schedule.</summary>
    private readonly Aes _aes;

    /// <summary>The cached ECB encryptor, created once so its key schedule is reused across every single-block call. Nulled on disposal.</summary>
    private ICryptoTransform? _encryptor;

    /// <summary>The cached ECB decryptor, created once so its key schedule is reused across every single-block call. Nulled on disposal.</summary>
    private ICryptoTransform? _decryptor;

    /// <summary>Reusable single-block scratch buffer for the byte-array-based <see cref="ICryptoTransform" /> surface.</summary>
    private readonly byte[] _scratchIn = new byte[BlockSizeBits / 8];

    /// <summary>Reusable single-block scratch buffer for the byte-array-based <see cref="ICryptoTransform" /> surface.</summary>
    private readonly byte[] _scratchOut = new byte[BlockSizeBits / 8];

    /// <summary>Indicates whether the instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesBlockCipher" /> class with the specified AES key.
    /// </summary>
    /// <param name="key">
    /// The AES key. Valid lengths are 16, 24, or 32 bytes (AES-128, AES-192, or AES-256). A defensive copy is taken —
    /// the caller may zero the original array immediately after construction.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException"><paramref name="key" /> length is not 16, 24, or 32 bytes.</exception>
    public AesBlockCipher(byte[] key)
    {
        ThrowHelper.ThrowIfNull(key);

        var aes = Aes.Create();
        try
        {
            aes.Key = key; // BCL validates length and throws CryptographicException on mismatch.
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            // Create the ECB transforms once. ECB is stateless between blocks, so a single cached transform can
            // process every subsequent single-block call without re-deriving the key schedule per call (the cost the
            // one-shot EncryptEcb/DecryptEcb API pays on every invocation).
            _encryptor = aes.CreateEncryptor();
            _decryptor = aes.CreateDecryptor();
        }
        catch
        {
            aes.Dispose();
            throw;
        }

        _aes = aes;
    }

    /// <inheritdoc />
    /// <value>Length of the AES block is 128 bits (16 bytes).</value>
    public int BlockSize => BlockSizeBits;

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="input" /> or <paramref name="output" /> is not exactly <see cref="BlockSize" /> / 8 bytes.
    /// </exception>
    public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockSizeBits / 8);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockSizeBits / 8);
        ThrowIfDisposed();

        TransformSingleBlock(_encryptor!, input, output);
    }

    /// <summary>
    /// Releases the underlying <see cref="Aes" /> instance, zeroing its expanded key schedule. Subsequent calls to
    /// <see cref="Encrypt" /> or <see cref="Decrypt" /> throw <see cref="ObjectDisposedException" />.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _encryptor?.Dispose();
            _decryptor?.Dispose();
            _aes.Dispose();
            _encryptor = null;
            _decryptor = null;
            CryptographyHelper.Clear(_scratchIn);
            CryptographyHelper.Clear(_scratchOut);
            _disposed = true;
        }
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="input" /> or <paramref name="output" /> is not exactly <see cref="BlockSize" /> / 8 bytes.
    /// </exception>
    public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockSizeBits / 8);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockSizeBits / 8);
        ThrowIfDisposed();

        TransformSingleBlock(_decryptor!, input, output);
    }

    /// <summary>
    /// Runs a single 16-byte block through the supplied cached ECB transform via reusable scratch buffers, bridging
    /// the span-based <see cref="IBlockCipher" /> surface to the byte-array-based <see cref="ICryptoTransform" /> API.
    /// </summary>
    /// <param name="transform">The cached ECB encryptor or decryptor.</param>
    /// <param name="input">The single input block.</param>
    /// <param name="output">The destination for the transformed block.</param>
    private void TransformSingleBlock(ICryptoTransform transform, ReadOnlySpan<byte> input, Span<byte> output)
    {
        input.CopyTo(_scratchIn);
        transform.TransformBlock(_scratchIn, 0, _scratchIn.Length, _scratchOut, 0);
        _scratchOut.AsSpan().CopyTo(output);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}
