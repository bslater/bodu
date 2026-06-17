// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the <see cref="ICryptoTransform" /> implementation for additive stream ciphers, XOR-ing data with the
/// keystream produced by an <see cref="IStreamCipher" /> engine. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// This is the stream-cipher counterpart to <see cref="BlockCipherTransform" />. It turns the low-level
/// <see cref="IStreamCipher" /> primitive into the <see cref="ICryptoTransform" /> contract that
/// <see cref="CryptoStream" /> and <c>SymmetricStreamAlgorithm.CreateEncryptor()</c> expect, and centralizes the two
/// concerns shared by every additive stream cipher:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <strong>Self-inverse XOR.</strong> Encryption and decryption are identical — both XOR the plaintext or ciphertext
/// with the keystream — so the same code path serves both directions and the transform direction is irrelevant.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>Partial-block carry.</strong> Callers (notably <see cref="CryptoStream" />) submit arbitrary,
/// non-block-aligned lengths. The transform retains the unused tail of the current keystream block between calls so the
/// keystream advances by exactly the number of bytes consumed, never re-using or skipping keystream.
/// </description>
/// </item>
/// </list>
/// <para>
/// Keystream sequencing and any keystream-exhaustion guard live in the engine, which owns its counter or internal
/// state; the transform simply pulls the next block via <see cref="IStreamCipher.NextKeystreamBlock(Span{byte})" />
/// when the carried block is consumed.
/// </para>
/// <para>
/// Because <see cref="InputBlockSize" /> and <see cref="OutputBlockSize" /> are both one byte, the BCL imposes no
/// alignment requirement on callers and <see cref="TransformBlock" /> accepts any length.
/// </para>
/// <para>
/// A <see cref="StreamCipherTransform" /> represents a single one-shot operation. After
/// <see cref="TransformFinalBlock" /> completes the instance is finalized; <see cref="CanReuseTransform" /> is
/// <see langword="false" /> and further transform calls throw.
/// </para>
/// </remarks>
/// <seealso cref="IStreamCipher" /> <seealso cref="BlockCipherTransform" />
internal sealed class StreamCipherTransform
    : ICryptoTransform
{
    /// <summary>
    /// The configured stream cipher engine that produces keystream blocks.
    /// </summary>
    private readonly IStreamCipher _cipher;

    /// <summary>
    /// The current keystream block carried between transform calls.
    /// </summary>
    private readonly byte[] _keystream;

    /// <summary>
    /// The offset of the next unused byte within the carried keystream block.
    /// </summary>
    private int _keystreamOffset;

    /// <summary>
    /// Indicates whether this instance has been disposed and its keystream cleared.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Indicates whether the final block has been transformed and the instance finalized.
    /// </summary>
    private bool _finalized;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamCipherTransform" /> class.
    /// </summary>
    /// <param name="cipher">
    /// The configured <see cref="IStreamCipher" /> engine, with its key and nonce already bound. Must not be
    /// <see langword="null" />. Ownership transfers to this transform, which disposes the engine.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
    internal StreamCipherTransform(IStreamCipher cipher)
    {
        ThrowHelper.ThrowIfNull(cipher);

        _cipher = cipher;
        _keystream = new byte[cipher.BlockSize];

        // Force the first Apply call to pull a fresh keystream block from the engine.
        _keystreamOffset = cipher.BlockSize;
    }

    /// <inheritdoc />
    public bool CanReuseTransform => false;

    /// <inheritdoc />
    public bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    /// <remarks>
    /// An additive stream cipher imposes no alignment on its input, so the block size is one byte.
    /// </remarks>
    public int InputBlockSize => 1;

    /// <inheritdoc />
    /// <remarks>
    /// An additive stream cipher imposes no alignment on its output, so the block size is one byte.
    /// </remarks>
    public int OutputBlockSize => 1;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        CryptographyHelper.Clear(_keystream);
        _cipher.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Transforms a region of the input byte array and writes the result to the output buffer.
    /// </summary>
    /// <param name="inputBuffer">The input data buffer.</param>
    /// <param name="inputOffset">
    /// The byte offset within <paramref name="inputBuffer" /> at which to begin reading.
    /// </param>
    /// <param name="inputCount">The number of bytes to process.</param>
    /// <param name="outputBuffer">The buffer to write the transformed data into.</param>
    /// <param name="outputOffset">
    /// The byte offset within <paramref name="outputBuffer" /> at which to begin writing.
    /// </param>
    /// <returns>The number of bytes written, equal to <paramref name="inputCount" />.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// This transform has already been finalized and cannot be reused.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="inputBuffer" /> or <paramref name="outputBuffer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// The block counter would overflow, which would reuse keystream; or <paramref name="inputBuffer" /> and
    /// <paramref name="outputBuffer" /> are the same array and the read and write ranges partially overlap.
    /// </exception>
    /// <remarks>
    /// When <paramref name="inputBuffer" /> and <paramref name="outputBuffer" /> are the same array, only exact
    /// in-place transformation (identical offsets) or fully disjoint ranges are supported. A partial overlap is
    /// rejected because the keystream is applied forward byte by byte, so a write into not-yet-read input would corrupt
    /// the result.
    /// </remarks>
    public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
    {
        ThrowIfDisposed();
        ThrowIfFinalized();

        ThrowHelper.ThrowIfNull(inputBuffer);
        ThrowHelper.ThrowIfNull(outputBuffer);
        CryptographyThrowHelper.ThrowIfArrayOffsetOrCountInvalid(inputBuffer, inputOffset, inputCount);
        CryptographyThrowHelper.ThrowIfArrayOffsetOrCountInvalid(outputBuffer, outputOffset, inputCount);

        ReadOnlySpan<byte> input = inputBuffer.AsSpan(inputOffset, inputCount);
        Span<byte> output = outputBuffer.AsSpan(outputOffset, inputCount);

        // Forward byte-by-byte XOR is safe for exact in-place (input and output cover the same memory) and for
        // fully disjoint ranges, but a partial overlap would let an earlier write clobber input that has not yet
        // been read.
        CryptographyThrowHelper.ThrowIfInvalidOverlap(input, output);

        Apply(input, output);
        return inputCount;
    }

    /// <summary>
    /// Transforms the final region of data and returns the result, then finalizes the transform.
    /// </summary>
    /// <param name="inputBuffer">The final input data buffer.</param>
    /// <param name="inputOffset">
    /// The byte offset within <paramref name="inputBuffer" /> at which to begin reading.
    /// </param>
    /// <param name="inputCount">The number of bytes to process from <paramref name="inputBuffer" />.</param>
    /// <returns>A new byte array of length <paramref name="inputCount" /> with the transformed final region.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// This transform has already been finalized and cannot be reused.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="inputBuffer" /> is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">
    /// The block counter would overflow, which would reuse keystream.
    /// </exception>
    public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
    {
        ThrowIfDisposed();
        ThrowIfFinalized();

        ThrowHelper.ThrowIfNull(inputBuffer);
        CryptographyThrowHelper.ThrowIfArrayOffsetOrCountInvalid(inputBuffer, inputOffset, inputCount);

        // No overlap check is needed: a fresh output array is allocated here, so it can never alias the input buffer.
        byte[] output = new byte[inputCount];

        try
        {
            Apply(inputBuffer.AsSpan(inputOffset, inputCount), output);
            return output;
        }
        finally
        {
            _finalized = true;
        }
    }

    /// <summary>
    /// XORs <paramref name="input" /> with the cipher keystream into <paramref name="output" />, pulling and carrying
    /// keystream blocks from the engine as needed.
    /// </summary>
    /// <param name="input">The bytes to transform.</param>
    /// <param name="output">The destination span, the same length as <paramref name="input" />.</param>
    /// <exception cref="CryptographicException">
    /// The engine's keystream is exhausted (for example, a fixed-width block counter would wrap and reuse keystream).
    /// </exception>
    /// <remarks>
    /// Any keystream left over from a previous call is consumed first so that the keystream advances by exactly the
    /// number of bytes processed. A new block is pulled from the engine only when the carried block is exhausted.
    /// </remarks>
    private void Apply(ReadOnlySpan<byte> input, Span<byte> output)
    {
        int blockSize = _cipher.BlockSize;

        for (int i = 0; i < input.Length; i++)
        {
            if (_keystreamOffset == blockSize)
            {
                _cipher.NextKeystreamBlock(_keystream);
                _keystreamOffset = 0;
            }

            output[i] = (byte)(input[i] ^ _keystream[_keystreamOffset++]);
        }
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if this transform has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The transform has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>
    /// Throws if this transform has already completed its final block operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// This transform has already been finalized and cannot be reused.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfFinalized()
    {
        if (_finalized)
            throw new InvalidOperationException(CryptoResourceStrings.Op_Invalid_TransformAlreadyFinalized);
    }
}
