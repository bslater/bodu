// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtrModeTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies Counter (CTR) mode to an underlying <see cref="IBlockCipher" />, turning it into a synchronous stream
/// cipher. The counter is incremented in big-endian order (rightmost byte first), matching NIST SP 800-38A Section 6.5.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/classic-modes.svg" alt="CTR panel — independent counter blocks are encrypted to form a keystream, then XORed with plaintext."/>
/// </para>
/// <para>
/// CTR is self-inverse: the same <see cref="Transform" /> operation is applied for both encryption and decryption. The
/// cipher's <em>encrypt</em> primitive is always used; the decrypt primitive is never called. See <b>panel 5</b> of the
/// diagram above: each cell has its own counter block <c>CTRᵢ</c> and no arrows connect one cell to the next — meaning
/// the keystream is trivially parallelisable and supports random-access seeking into the middle of a message.
/// </para>
/// <para>
/// That independence is also where the sharpest pitfall lives. To protect against keystream reuse, the transform tracks
/// counter wrap-around: if the counter increments back to its initial value, the next call to <see cref="Transform" />
/// throws <see cref="CryptographicException" />. Reusing a <c>(key, nonce)</c> pair across messages is catastrophic —
/// the XOR of two ciphertexts recovers the XOR of the two plaintexts — so callers must ensure each counter value is
/// used at most once per key.
/// </para>
/// <para>
/// <strong>When to use CTR.</strong> The right confidentiality-only mode for new code that needs random access,
/// parallelisable encryption, or a stream-cipher shape — disk encryption layers without authentication, network
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
/// <seealso href="../guides/cryptography/cipher-modes.html#ctr--parallel-seekable-stream-shaped">CTR walk-through in
/// the cipher-modes guide</seealso>
public sealed class CtrModeTransform
    : IBlockCipherModeTransform
{
    private readonly IBlockCipher _cipher;
    private readonly byte[] _initialCounter;
    private readonly byte[] _counter;
    private bool _counterWrapped;
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
        _initialCounter = (byte[])initialCounter.Clone();
        _counter = (byte[])initialCounter.Clone();
    }

    /// <inheritdoc />
    public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
    {
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);
        CryptographyThrowHelper.ThrowIfInvalidOverlap(input, output);

        int blockSize = _cipher.BlockSize / 8;
        Span<byte> keystream = stackalloc byte[blockSize];

        for (int offset = 0; offset < input.Length; offset += blockSize)
        {
            if (_counterWrapped)
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_CtrCounterWrapped);

            _cipher.Encrypt(_counter, keystream);
            IncrementCounter();

            int len = Math.Min(blockSize, input.Length - offset);
            for (int i = 0; i < len; i++)
                output[offset + i] = (byte)(input[offset + i] ^ keystream[i]);
        }

        return input.Length;
    }

    /// <summary>
    /// Releases the resources used by this instance and zeroes the retained counter state so that key-equivalent
    /// counter values do not linger in memory after disposal. The underlying <see cref="IBlockCipher" /> is not
    /// disposed by this type — ownership remains with the caller.
    /// </summary>
    /// <remarks>
    /// Idempotent.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        CryptographyHelper.Clear(_counter);
        CryptographyHelper.Clear(_initialCounter);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Increments the counter in big-endian (rightmost-byte-first) order, matching NIST SP 800-38A, then detects
    /// wrap-around.
    /// </summary>
    private void IncrementCounter()
    {
        for (int i = _counter.Length - 1; i >= 0; i--)
            if (++_counter[i] != 0) break;

        // Wrap detected: counter has returned to its initial value.
        if (_counter.AsSpan().SequenceEqual(_initialCounter))
            _counterWrapped = true;
    }
}
