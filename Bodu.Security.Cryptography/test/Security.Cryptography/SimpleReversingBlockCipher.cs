// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingBlockCipher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// A diagnostic block cipher engine that reverses the byte order of each block, with optional tweak support.
/// </summary>
/// <remarks>
/// <para>
/// This class is intended exclusively for use in test harnesses and diagnostic scenarios. It provides a
/// deterministic, inspectable transform that makes it straightforward to verify block alignment, padding
/// behaviour, mode-of-operation chaining, tweak propagation, and streaming correctness without
/// cryptographic complexity.
/// </para>
/// <para>
/// When no tweak is provided, encryption and decryption are identical operations — reversing a reversed
/// block restores the original input.
/// </para>
/// <para>
/// When a tweak is provided the operations differ so that the transform is still self-consistent:
/// <list type="bullet">
/// <item><description><b>Encrypt:</b> XOR the plaintext with the tweak (cycling), then reverse the result.</description></item>
/// <item><description><b>Decrypt:</b> Reverse the ciphertext, then XOR the result with the tweak (cycling).</description></item>
/// </list>
/// This guarantees <c>Decrypt(Encrypt(pt, tweak), tweak) == pt</c> for any tweak.
/// </para>
/// <para>
/// The tweak is cycled byte-by-byte across the block if it is shorter than the block size, and truncated
/// if longer. All calls to <see cref="Encrypt" /> and <see cref="Decrypt" /> are recorded in
/// <see cref="Diagnostics" /> for post-hoc inspection in tests.
/// </para>
/// <note type="warning">This cipher provides no cryptographic security and must not be used in production code.</note>
/// </remarks>
internal sealed class SimpleReversingBlockCipher
    : IBlockCipher
{
    private bool disposed;
    private byte[]? tweak;

    /// <summary>
    /// Initialises a new instance of the <see cref="SimpleReversingBlockCipher" /> class without a tweak.
    /// </summary>
    /// <param name="key">The key bytes. Must not be <see langword="null" />.</param>
    /// <param name="blockSizeBytes">The block size in bytes. Must be positive.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="blockSizeBytes" /> is not positive.</exception>
    public SimpleReversingBlockCipher(byte[] key, int blockSizeBytes)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));
        if (blockSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(blockSizeBytes), "Block size must be positive.");

        BlockSize = blockSizeBytes;
        Key = (byte[])key.Clone();
        tweak = null;
        Diagnostics = new SimpleReversingDiagnostics();
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="SimpleReversingBlockCipher" /> class with an optional tweak.
    /// </summary>
    /// <param name="key">The key bytes. Must not be <see langword="null" />.</param>
    /// <param name="blockSizeBytes">The block size in bytes. Must be positive.</param>
    /// <param name="tweak">
    /// An optional tweak value. When non-<see langword="null" />, it is XORed into the block before reversal
    /// on encrypt and after reversal on decrypt, cycling byte-by-byte if shorter than the block.
    /// Pass <see langword="null" /> to operate without a tweak, equivalent to using the two-parameter constructor.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="blockSizeBytes" /> is not positive.</exception>
    public SimpleReversingBlockCipher(byte[] key, int blockSizeBytes, byte[]? tweak)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));
        if (blockSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(blockSizeBytes), "Block size must be positive.");

        BlockSize = blockSizeBytes;
        Key = (byte[])key.Clone();
        this.tweak = tweak is null ? null : (byte[])tweak.Clone();
        Diagnostics = new SimpleReversingDiagnostics();
    }

    /// <inheritdoc />
    public int BlockSize { get; }

    /// <summary>Gets the key that was provided at construction time.</summary>
    public byte[] Key { get; private set; }

    /// <summary>
    /// Gets the tweak that was provided at construction time, or <see langword="null" /> if no tweak was supplied.
    /// </summary>
    public byte[]? Tweak => tweak is null ? null : (byte[])tweak.Clone();

    /// <summary>Gets the diagnostic recorder for this cipher instance.</summary>
    public SimpleReversingDiagnostics Diagnostics { get; }

    /// <summary>
    /// Encrypts one block by XORing with the tweak (if set) then reversing the result.
    /// When no tweak is set, this is equivalent to a plain byte reversal.
    /// </summary>
    /// <param name="input">The plaintext block. Must be at least <see cref="BlockSize" /> bytes.</param>
    /// <param name="output">The output span. Must be at least <see cref="BlockSize" /> bytes.</param>
    /// <exception cref="ArgumentException">Either span is shorter than <see cref="BlockSize" /> bytes.</exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        ValidateSpans(input, output);

        ReadOnlySpan<byte> block = input.Slice(0, BlockSize);
        Span<byte> dest = output.Slice(0, BlockSize);

        block.CopyTo(dest);

        // XOR with tweak before reversing (encrypt direction).
        if (tweak is { Length: > 0 })
            ApplyTweak(dest, tweak);

        dest.Reverse();

        Diagnostics.RecordEncrypt(block, dest);
    }

    /// <summary>
    /// Decrypts one block by reversing the bytes then XORing with the tweak (if set).
    /// When no tweak is set, this is equivalent to a plain byte reversal.
    /// </summary>
    /// <param name="input">The ciphertext block. Must be at least <see cref="BlockSize" /> bytes.</param>
    /// <param name="output">The output span. Must be at least <see cref="BlockSize" /> bytes.</param>
    /// <exception cref="ArgumentException">Either span is shorter than <see cref="BlockSize" /> bytes.</exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        ValidateSpans(input, output);

        ReadOnlySpan<byte> block = input.Slice(0, BlockSize);
        Span<byte> dest = output.Slice(0, BlockSize);

        block.CopyTo(dest);
        dest.Reverse();

        // XOR with tweak after reversing (decrypt direction).
        if (tweak is { Length: > 0 })
            ApplyTweak(dest, tweak);

        Diagnostics.RecordDecrypt(block, dest);
    }

    /// <summary>
    /// Releases the key and tweak material held by this instance.
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;

        CryptoHelpers.Clear(this.Key);
        CryptographicOperations.ZeroMemory(Key);
        Key = Array.Empty<byte>();

        if (tweak is not null)
        {
            CryptographicOperations.ZeroMemory(tweak);
            tweak = null;
        }

        disposed = true;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// XORs <paramref name="block" /> in-place with <paramref name="tweakBytes" />, cycling the tweak
    /// if it is shorter than the block.
    /// </summary>
    private static void ApplyTweak(Span<byte> block, byte[] tweakBytes)
    {
        int tweakLen = tweakBytes.Length;
        for (int i = 0; i < block.Length; i++)
            block[i] ^= tweakBytes[i % tweakLen];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(disposed, this);
#else
        if (this.disposed) throw new ObjectDisposedException(nameof(SimpleReversingBlockCipher));
#endif
    }

    private void ValidateSpans(ReadOnlySpan<byte> input, Span<byte> output)
    {
        if (input.Length != BlockSize)
            throw new ArgumentException(
                $"Input span must be exactly {BlockSize} bytes but was {input.Length}.", nameof(input));
        if (output.Length != BlockSize)
            throw new ArgumentException(
                $"Output span must be exactly {BlockSize} bytes but was {output.Length}.", nameof(output));
    }
}
