// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtrModeTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies Counter (CTR) mode to an underlying <see cref="IBlockCipher" />, turning it into a synchronous stream
/// cipher. The counter is incremented in big-endian order (rightmost byte first), matching NIST SP 800-38A Section 6.5.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/classic-modes.svg" alt="CTR panel - independent counter blocks are encrypted to form a keystream, then XORed with plaintext."/>
/// </para>
/// <para>
/// CTR is self-inverse: the same <see cref="Transform" /> operation is applied for both encryption and decryption. The
/// cipher's <em>encrypt</em> primitive is always used; the decrypt primitive is never called. See <b>panel 5</b> of the
/// diagram above: each cell has its own counter block <c>CTRᵢ</c> and no arrows connect one cell to the next - meaning
/// the keystream is trivially parallelisable and supports random-access seeking into the middle of a message.
/// </para>
/// <para>
/// That independence is also where the sharpest pitfall lives. To protect against keystream reuse, the transform tracks
/// counter wrap-around: once the block-width counter rolls over its full 2^n value space back to zero, the next call to
/// <see cref="Transform" /> throws <see cref="CryptographicException" />. Reusing a <c>(key, nonce)</c> pair across
/// messages is catastrophic - the XOR of two ciphertexts recovers the XOR of the two plaintexts - so callers must
/// ensure each counter value is used at most once per key.
/// </para>
/// <para>
/// <strong>When to use CTR.</strong> The right confidentiality-only mode for new code that needs random access,
/// parallelisable encryption, or a stream-cipher shape - disk encryption layers without authentication, network
/// protocols where authentication is provided separately, and anywhere a precomputed keystream is useful. CTR is also
/// the keystream layer of the major AEAD modes; if you need authentication as well, reach for
/// <see cref="GcmModeTransform" /> (CTR + GHASH) or <see cref="EaxModeTransform" /> (CTR + OMAC) directly rather than
/// building it on top of bare CTR.
/// </para>
/// <para>
/// The counter increment is parallelisable: every block's keystream can be produced independently, so throughput scales
/// with available cores or SIMD width.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
///
/// // Most callers should set SymmetricAlgorithm.Mode = CipherBlockMode.CTR instead of using this directly.
/// using IBlockCipher cipher = new AesBlockCipher(key);
///
/// // Initial counter is typically `nonce || zero-counter`; the nonce must never repeat under one key.
/// byte[] initialCounter = BuildInitialCounter(nonce);
/// IBlockCipherModeTransform ctr = new CtrModeTransform(cipher, initialCounter);
/// byte[] ciphertext = new byte[plaintext.Length];
/// int written = ctr.Transform(plaintext, ciphertext, encrypt: true);
///
/// // The same call shape decrypts: encrypt: true / encrypt: false produce identical results.
///]]>
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/cipher-modes.html#ctr---parallel-seekable-stream-shaped">CTR walk-through in
/// the cipher-modes guide</seealso>
public sealed class CtrModeTransform
    : IBlockCipherModeTransform
{
    /// <summary>The underlying block cipher whose encrypt primitive generates the keystream.</summary>
    private readonly IBlockCipher _cipher;

    /// <summary>The current counter block, incremented after each keystream block is produced.</summary>
    private readonly byte[] _counter;

    /// <summary>Indicates whether the block-width counter has rolled over its full 2^n value space back to zero.</summary>
    private bool _counterWrapped;

    /// <summary>Indicates whether this instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CtrModeTransform" /> class.
    /// </summary>
    /// <param name="cipher">The block cipher whose encrypt primitive generates the keystream.</param>
    /// <param name="initialCounter">
    /// The starting counter block. Must equal the cipher block size. A defensive copy is taken.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="cipher" /> or <paramref name="initialCounter" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="initialCounter" /> length does not equal the cipher block size.
    /// </exception>
    public CtrModeTransform(IBlockCipher cipher, byte[] initialCounter)
    {
        ThrowHelper.ThrowIfNull(cipher);
        CryptographyThrowHelper.ThrowIfIvLengthInvalid(initialCounter, cipher.BlockSize);

        _cipher = cipher;
        _counter = (byte[])initialCounter.Clone();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Each call consumes one counter block per whole or partial block of input; the unused keystream of a final
    /// partial block is discarded rather than carried into the next call. Counter blocks are encrypted a run at a time
    /// through <see cref="IBlockCipher.EncryptBlocks" />, except under <see cref="Serpent128Cipher" />, whose kernels
    /// form the counter blocks in registers and combine their keystream with the input as they store it.
    /// </remarks>
    [SkipLocalsInit]
    public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);
        CryptographyThrowHelper.ThrowIfInvalidOverlap(input, output);

        if (_cipher is ICounterModeBlockCipher counterCipher)
            return TransformInCipher(counterCipher, input, output);

        int blockSize = _cipher.BlockSize / 8;
        int batchLength = CounterKeystream.BatchLength(blockSize);
        Span<byte> counters = batchLength <= CounterKeystream.BatchBytes ? stackalloc byte[batchLength] : new byte[batchLength];
        Span<byte> keystream = batchLength <= CounterKeystream.BatchBytes ? stackalloc byte[batchLength] : new byte[batchLength];
        int used = 0;

        try
        {
            int offset = 0;
            while (offset < input.Length)
            {
                // Lay out counter blocks until the run is full, the input is covered, or the counter has wrapped - the
                // block after a wrap is the one the per-block formulation refuses to produce.
                int limit = Math.Min(batchLength, input.Length - offset);
                int filled = blockSize == 16 ? LayOutCounters16(counters, limit) : LayOutCounters(counters, limit, blockSize);

                if (filled > 0)
                {
                    // Widen the extent to clear before the cipher writes keystream, so a throwing cipher leaves none
                    // behind.
                    int length = Math.Min(filled, input.Length - offset);
                    used = Math.Max(used, filled);
                    CounterKeystream.Apply(_cipher, counters[..filled], keystream, input.Slice(offset, length), output.Slice(offset, length));
                    offset += length;
                }

                if (offset < input.Length && _counterWrapped)
                    throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_CtrCounterWrapped);
            }

            return input.Length;
        }
        finally
        {
            CryptographyHelper.Clear(keystream[..used]);
        }
    }

    /// <summary>
    /// Releases the resources used by this instance and zeroes the retained counter state so that key-equivalent
    /// counter values do not linger in memory after disposal. The underlying <see cref="IBlockCipher" /> is not
    /// disposed by this type - ownership remains with the caller.
    /// </summary>
    /// <remarks>
    /// Idempotent.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        CryptographyHelper.Clear(_counter);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies the keystream through a 128-bit block cipher that forms its counter blocks itself, stopping after the
    /// block that takes the counter's last value, as the run of counter blocks does.
    /// </summary>
    /// <param name="cipher">The cipher.</param>
    /// <param name="input">The input.</param>
    /// <param name="output">The destination, at least as long as <paramref name="input" />.</param>
    /// <returns>The number of bytes written: the length of <paramref name="input" />.</returns>
    /// <exception cref="CryptographicException">
    /// The counter had wrapped, or wrapped before the input was covered; the blocks before the wrap are written first.
    /// </exception>
    private int TransformInCipher(ICounterModeBlockCipher cipher, ReadOnlySpan<byte> input, Span<byte> output)
    {
        if (input.IsEmpty)
            return 0;

        if (_counterWrapped)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_CtrCounterWrapped);

        ulong high = BinaryPrimitives.ReadUInt64BigEndian(_counter);
        ulong low = BinaryPrimitives.ReadUInt64BigEndian(_counter.AsSpan(8));
        int blocks = (int)(((uint)input.Length + 15u) / 16u);

        // The counter values left, 2^128 minus the counter, number no more than the input's blocks only when the high
        // half is all ones and the low half is not zero; there they are 2^64 minus the low half.
        ulong left = 0UL - low;
        bool wraps = high == ulong.MaxValue && low != 0 && left <= (ulong)blocks;
        int length = wraps ? (int)Math.Min(input.Length, (long)left * 16) : input.Length;

        cipher.XorCounterKeystream(ref high, ref low, input[..length], output);
        BinaryPrimitives.WriteUInt64BigEndian(_counter, high);
        BinaryPrimitives.WriteUInt64BigEndian(_counter.AsSpan(8), low);

        if (wraps)
        {
            _counterWrapped = true;
            if (length < input.Length)
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_CtrCounterWrapped);
        }

        return input.Length;
    }

    /// <summary>
    /// Writes successive counter blocks into <paramref name="destination" /> until at least <paramref name="limit" />
    /// bytes are covered or the counter wraps, advancing the counter past each block written.
    /// </summary>
    /// <param name="destination">The run of counter blocks to fill.</param>
    /// <param name="limit">
    /// The number of input bytes the run must cover; the last block may cover only part of it.
    /// </param>
    /// <param name="blockSize">The cipher's block size, in bytes.</param>
    /// <returns>The number of bytes of counter blocks written: a whole number of blocks.</returns>
    private int LayOutCounters(Span<byte> destination, int limit, int blockSize)
    {
        int filled = 0;
        while (filled < limit && !_counterWrapped)
        {
            _counter.CopyTo(destination[filled..]);
            IncrementCounter();
            filled += blockSize;
        }

        return filled;
    }

    /// <summary>
    /// Writes successive 16-byte counter blocks into <paramref name="destination" /> as <see cref="LayOutCounters" />
    /// does, holding the counter as two big-endian 64-bit words so each block costs two stores and an add.
    /// </summary>
    /// <param name="destination">The run of counter blocks to fill.</param>
    /// <param name="limit">
    /// The number of input bytes the run must cover; the last block may cover only part of it.
    /// </param>
    /// <returns>The number of bytes of counter blocks written: a whole number of blocks.</returns>
    private int LayOutCounters16(Span<byte> destination, int limit)
    {
        ulong high = BinaryPrimitives.ReadUInt64BigEndian(_counter);
        ulong low = BinaryPrimitives.ReadUInt64BigEndian(_counter.AsSpan(8));
        int filled = 0;

        while (filled < limit && !_counterWrapped)
        {
            BinaryPrimitives.WriteUInt64BigEndian(destination[filled..], high);
            BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(filled + 8), low);
            filled += 16;

            // Big-endian increment across both words; carrying out of the high word is the full 2^128 rollover.
            if (++low == 0 && ++high == 0)
                _counterWrapped = true;
        }

        BinaryPrimitives.WriteUInt64BigEndian(_counter, high);
        BinaryPrimitives.WriteUInt64BigEndian(_counter.AsSpan(8), low);
        return filled;
    }

    /// <summary>
    /// Increments the counter in big-endian (rightmost-byte-first) order, matching NIST SP 800-38A, and latches the
    /// wrap flag when the increment carries out past the most significant byte (a full 2^n rollover to zero).
    /// </summary>
    /// <remarks>
    /// The counter spans the entire cipher block, so a carry propagating off the top byte means all 2^n counter values
    /// have been consumed and the counter has returned to zero. Latching on that carry-out is O(1) and detects the true
    /// keystream-reuse boundary; it fires at or before any return to the (possibly non-zero) initial value, so it never
    /// permits reuse.
    /// </remarks>
    private void IncrementCounter()
    {
        int i = _counter.Length - 1;
        while (i >= 0 && ++_counter[i] == 0)
            i--;

        // Carry propagated past the most significant byte: the counter has consumed all 2^n block values and rolled
        // over to zero. Latch to prevent keystream reuse.
        if (i < 0)
            _counterWrapped = true;
    }
}
