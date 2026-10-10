// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AesBlockCipher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Diagnostics;
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
/// BCL's hardware-accelerated <see cref="Aes" /> implementation. The ECB encryptor is created on construction and the
/// decryptor on the first decryption, and both are cached, so per-block calls reuse the expanded key schedule rather
/// than rebuilding a cipher context each time, and an instance that only encrypts never builds a decryptor.
/// </para>
/// <para>
/// <see cref="AesBlockCipher" /> is not intended for direct encryption of user data. Wrap it in one of the
/// authenticated mode transforms listed above - the mode transform is responsible for chaining, IV / nonce handling,
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
    : IBlockCipher, ICbcBlockCipher
{
    /// <summary>Length of the AES block is 128 bits (16 bytes). Internal constant kept for span-length validation; callers should read <see cref="BlockSize" /> instead.</summary>
    private const int BlockSizeBits = 128;

    /// <summary>The largest chunk, in bytes, a multi-block run moves through the cached transforms in one call.</summary>
    private const int BulkChunkBytes = 4096;

    /// <summary>The run length, in bytes, from which a multi-block run goes through the BCL's one-shot ECB call instead: long enough that the call's per-invocation cipher-context setup is amortized, and the chunk copies are avoided.</summary>
    private const int OneShotThresholdBytes = 64 * 1024;

    /// <summary>The underlying BCL <see cref="Aes" /> instance that owns the expanded key schedule.</summary>
    private readonly Aes _aes;

    /// <summary>Reusable single-block scratch buffer for the byte-array-based <see cref="ICryptoTransform" /> surface.</summary>
    private readonly byte[] _scratchIn = new byte[BlockSizeBits / 8];

    /// <summary>Reusable single-block scratch buffer for the byte-array-based <see cref="ICryptoTransform" /> surface.</summary>
    private readonly byte[] _scratchOut = new byte[BlockSizeBits / 8];

    /// <summary>The cached ECB encryptor, created once so its key schedule is reused across every single-block call. Nulled on disposal.</summary>
    private ICryptoTransform? _encryptor;

    /// <summary>The cached CBC encryptor with a zero IV, created on the first chained encryption: each chain folds the caller's chaining value into its first block and resets the transform when it ends. Nulled on disposal.</summary>
    private ICryptoTransform? _cbcEncryptor;

    /// <summary>The cached ECB decryptor, created on the first decryption so an instance that only encrypts - as the cipher of every counter-based mode does - never pays for one. Nulled on disposal.</summary>
    private ICryptoTransform? _decryptor;

    /// <summary>Indicates whether the instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesBlockCipher" /> class with the specified AES key.
    /// </summary>
    /// <param name="key">
    /// The AES key. Valid lengths are 16, 24, or 32 bytes (AES-128, AES-192, or AES-256). A defensive copy is taken -
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

            // Create the ECB encryptor once. ECB is stateless between blocks, so a single cached transform can process
            // every subsequent call without re-deriving the key schedule per call (the cost the one-shot
            // EncryptEcb/DecryptEcb API pays on every invocation). The decryptor is created on first use.
            _encryptor = aes.CreateEncryptor();
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

    /// <summary>
    /// Gets the size, in bits, of the key this instance was created with: 128, 192, or 256.
    /// </summary>
    internal int KeySize => _aes.KeySize;

    /// <summary>
    /// Gets the cached ECB decryptor, creating it on first use.
    /// </summary>
    private ICryptoTransform Decryptor =>
        _decryptor ??= _aes.CreateDecryptor();

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
            _cbcEncryptor?.Dispose();
            _aes.Dispose();
            _encryptor = null;
            _decryptor = null;
            _cbcEncryptor = null;
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

        TransformSingleBlock(Decryptor, input, output);
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="input" /> is not a whole number of blocks, or <paramref name="output" /> is shorter than
    /// <paramref name="input" />.
    /// </exception>
    /// <remarks>
    /// Moves the run through the cached ECB encryptor in chunks of up to 4 KiB, one platform call per chunk. The cached
    /// transform keeps its key schedule, whereas the BCL's one-shot ECB methods rebuild a cipher context on every
    /// call - which costs about as much as encrypting a kilobyte, so a counter mode handing over a few kilobytes at a
    /// time would spend most of its time there. <paramref name="output" /> may be the same memory as
    /// <paramref name="input" />.
    /// </remarks>
    public void EncryptBlocks(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, BlockSizeBits / 8, throwIfZero: false);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, input.Length);

        TransformBlocks(encrypt: true, input, output);
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="input" /> is not a whole number of blocks, or <paramref name="output" /> is shorter than
    /// <paramref name="input" />.
    /// </exception>
    /// <remarks>
    /// Moves the run through the cached ECB decryptor, or one one-shot call from 64 KiB, as
    /// <see cref="EncryptBlocks" /> does. <paramref name="output" /> may be the same memory as
    /// <paramref name="input" />.
    /// </remarks>
    public void DecryptBlocks(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, BlockSizeBits / 8, throwIfZero: false);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, input.Length);

        TransformBlocks(encrypt: false, input, output);
    }

    /// <summary>
    /// Runs a whole number of blocks through the cached ECB encryptor or decryptor, a chunk of up to
    /// <see cref="BulkChunkBytes" /> at a time through scratch borrowed from the shared array pool for the call.
    /// </summary>
    /// <param name="encrypt">
    /// <see langword="true" /> to encrypt the blocks; <see langword="false" /> to decrypt them.
    /// </param>
    /// <param name="input">The blocks to transform.</param>
    /// <param name="output">The destination; at least as long as <paramref name="input" />, and may alias it.</param>
    private void TransformBlocks(bool encrypt, ReadOnlySpan<byte> input, Span<byte> output)
    {
        // A long run amortizes the one-shot call's cipher-context setup and needs no copies, so it goes straight to the
        // platform; shorter runs go through the cached transform, whose setup is already paid.
        if (input.Length >= OneShotThresholdBytes)
        {
            if (encrypt)
                _aes.EncryptEcb(input, output[..input.Length], PaddingMode.None);
            else
                _aes.DecryptEcb(input, output[..input.Length], PaddingMode.None);

            return;
        }

        if (input.Length == 0)
            return;

        ICryptoTransform transform = encrypt ? _encryptor! : Decryptor;

        // Borrow the scratch for this call only, so an instance created per message allocates none and no instance
        // keeps a run's plaintext, ciphertext, or keystream between calls. The first half takes each chunk in, the
        // second receives it transformed.
        int chunk = Math.Min(BulkChunkBytes, input.Length);
        byte[] scratch = ArrayPool<byte>.Shared.Rent(2 * chunk);

        try
        {
            for (int offset = 0; offset < input.Length; offset += BulkChunkBytes)
            {
                int length = Math.Min(BulkChunkBytes, input.Length - offset);
                input.Slice(offset, length).CopyTo(scratch);
                transform.TransformBlock(scratch, 0, length, scratch, chunk);
                scratch.AsSpan(chunk, length).CopyTo(output.Slice(offset, length));
            }
        }
        finally
        {
            CryptographyHelper.Clear(scratch.AsSpan(0, 2 * chunk));
            ArrayPool<byte>.Shared.Return(scratch);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// The chain runs through a cached CBC encryptor in chunks of up to 4 KiB, which the platform chains block to block
    /// itself: one platform call per chunk rather than one per block. <paramref name="output" /> may be the same memory
    /// as <paramref name="input" />.
    /// </remarks>
    void ICbcBlockCipher.EncryptCbc(ReadOnlySpan<byte> input, Span<byte> output, Span<byte> chainingValue)
    {
        ThrowIfDisposed();
        Debug.Assert(input.Length % (BlockSizeBits / 8) == 0, "CBC chains whole blocks.");
        Debug.Assert(chainingValue.Length == BlockSizeBits / 8, "The chaining value is one block.");

        if (input.IsEmpty)
            return;

        const int BlockBytes = BlockSizeBits / 8;
        ICryptoTransform cbc = _cbcEncryptor ??= CreateCbcEncryptor();
        int chunk = Math.Min(BulkChunkBytes, input.Length);
        byte[] scratch = ArrayPool<byte>.Shared.Rent(2 * chunk);

        try
        {
            for (int offset = 0; offset < input.Length; offset += BulkChunkBytes)
            {
                int length = Math.Min(BulkChunkBytes, input.Length - offset);
                Span<byte> source = scratch.AsSpan(0, length);
                input.Slice(offset, length).CopyTo(source);

                // The transform starts each chain from a zero IV, so the caller's chaining value is folded into the
                // chain's first block.
                if (offset == 0)
                    CryptographyHelper.Xor(source[..BlockBytes], chainingValue, source[..BlockBytes]);

                cbc.TransformBlock(scratch, 0, length, scratch, chunk);
                if (!output.IsEmpty)
                    scratch.AsSpan(chunk, length).CopyTo(output.Slice(offset, length));
            }

            int lastChunk = ((input.Length - 1) % BulkChunkBytes) + 1;
            scratch.AsSpan(chunk + lastChunk - BlockBytes, BlockBytes).CopyTo(chainingValue);
        }
        finally
        {
            // Reset the transform to its zero IV for the next chain.
            cbc.TransformFinalBlock(scratch, 0, 0);
            CryptographyHelper.Clear(scratch.AsSpan(0, 2 * chunk));
            ArrayPool<byte>.Shared.Return(scratch);
        }
    }

    /// <summary>
    /// Creates the CBC encryptor with a zero IV that chained encryption runs through.
    /// </summary>
    /// <returns>The encryptor.</returns>
    private ICryptoTransform CreateCbcEncryptor()
    {
        // The algorithm object's mode and IV apply to the transforms created from it. Switch them for this one, then
        // restore the mode so a decryptor created later is still an ECB one.
        _aes.Mode = CipherMode.CBC;
        try
        {
            _aes.IV = new byte[BlockSizeBits / 8];
            return _aes.CreateEncryptor();
        }
        finally
        {
            _aes.Mode = CipherMode.ECB;
        }
    }

    /// <summary>
    /// Runs a single 16-byte block through the supplied cached ECB transform via reusable scratch buffers, bridging the
    /// span-based <see cref="IBlockCipher" /> surface to the byte-array-based <see cref="ICryptoTransform" /> API.
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
