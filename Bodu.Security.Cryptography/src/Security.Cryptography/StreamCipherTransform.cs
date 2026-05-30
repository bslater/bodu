// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherTransform.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a base implementation of <see cref="ICryptoTransform" /> for additive stream ciphers that produce a
/// key- and nonce-dependent keystream in fixed-size blocks via an <see cref="IStreamCipher" /> engine.
/// </summary>
/// <remarks>
/// <para>
/// This is the stream-cipher counterpart to <see cref="BlockCipherTransform" />. It turns the low-level
/// <see cref="IStreamCipher" /> primitive into the <see cref="ICryptoTransform" /> contract that
/// <see cref="CryptoStream" /> and <c>SymmetricAlgorithm.CreateEncryptor()</c> expect, and centralizes the three
/// concerns that every additive stream cipher shares:
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
/// <item>
/// <description>
/// <strong>Counter-overflow protection.</strong> The 32-bit block counter is tracked across the whole operation; if it
/// would wrap past its maximum the transform throws <see cref="CryptographicException" /> rather than silently reusing
/// earlier keystream — the same hazard guarded by <see cref="CtrModeTransform" />.
/// </description>
/// </item>
/// </list>
/// <para>
/// Because <see cref="InputBlockSize" /> and <see cref="OutputBlockSize" /> are both one byte, the BCL imposes no
/// alignment requirement on callers and <see cref="TransformBlock" /> accepts any length.
/// </para>
/// <para>
/// A <see cref="StreamCipherTransform" /> represents a single one-shot operation. After
/// <see cref="TransformFinalBlock" /> completes the instance is finalized; <see cref="CanReuseTransform" /> is
/// <see langword="false" /> and further transform calls throw. Derived classes need only supply a constructor that
/// forwards an initialized <see cref="IStreamCipher" /> engine and the initial block counter.
/// </para>
/// </remarks>
/// <seealso cref="IStreamCipher" />
/// <seealso cref="BlockCipherTransform" />
public abstract class StreamCipherTransform
    : ICryptoTransform
{
    private readonly IStreamCipher _cipher;
    private readonly byte[] _keystream;
    private readonly uint _initialCounter;

    private uint _counter;
    private int _keystreamOffset;
    private bool _counterExhausted;
    private bool _disposed;
    private bool _finalized;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamCipherTransform" /> class.
    /// </summary>
    /// <param name="cipher">
    /// The configured <see cref="IStreamCipher" /> engine, with its key and nonce already bound. Must not be
    /// <see langword="null" />. Ownership transfers to this transform, which disposes the engine.
    /// </param>
    /// <param name="initialCounter">The block counter for the first keystream block.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
    protected StreamCipherTransform(IStreamCipher cipher, uint initialCounter)
    {
        ThrowHelper.ThrowIfNull(cipher);

        _cipher = cipher;
        _initialCounter = initialCounter;
        _counter = initialCounter;
        _keystream = new byte[cipher.BlockSize];

        // Force the first Apply call to generate a fresh keystream block.
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

        CryptoHelpers.Clear(_keystream);
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
    /// The block counter would overflow, which would reuse keystream.
    /// </exception>
    public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
    {
        ThrowIfDisposed();
        ThrowIfFinalized();

        ThrowHelper.ThrowIfNull(inputBuffer);
        ThrowHelper.ThrowIfNull(outputBuffer);
        CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(inputBuffer, inputOffset, inputCount);
        CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(outputBuffer, outputOffset, inputCount);

        Apply(inputBuffer.AsSpan(inputOffset, inputCount), outputBuffer.AsSpan(outputOffset, inputCount));
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
        CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(inputBuffer, inputOffset, inputCount);

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
    /// XORs <paramref name="input" /> with the cipher keystream into <paramref name="output" />, generating and
    /// carrying keystream blocks as needed.
    /// </summary>
    /// <param name="input">The bytes to transform.</param>
    /// <param name="output">The destination span, the same length as <paramref name="input" />.</param>
    /// <exception cref="CryptographicException">
    /// The block counter would advance past <see cref="uint.MaxValue" />, which would reuse a previously emitted
    /// keystream block.
    /// </exception>
    /// <remarks>
    /// Any keystream left over from a previous call is consumed first so that the keystream advances by exactly the
    /// number of bytes processed. A new block is generated only when the carried block is exhausted.
    /// </remarks>
    private void Apply(ReadOnlySpan<byte> input, Span<byte> output)
    {
        int blockSize = _cipher.BlockSize;

        for (int i = 0; i < input.Length; i++)
        {
            if (_keystreamOffset == blockSize)
            {
                if (_counterExhausted)
                    throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_StreamCounterExhausted);

                _cipher.GenerateKeystreamBlock(_counter, _keystream);
                _keystreamOffset = 0;

                // Advance the counter, latching exhaustion once it wraps back to the initial value so the
                // next block boundary fails instead of silently reusing keystream.
                _counter++;
                if (_counter == _initialCounter)
                    _counterExhausted = true;
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
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
#endif

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
