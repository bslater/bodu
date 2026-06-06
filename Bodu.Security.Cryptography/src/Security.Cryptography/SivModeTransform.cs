// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SivModeTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies Synthetic Initialization Vector (SIV) mode to two underlying <see cref="IBlockCipher" /> instances,
/// providing deterministic authenticated encryption per RFC 5297 (AES-SIV).
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/aead-mode.svg" alt="Generic AEAD data flow — SIV inverts the usual order by running the MAC pipeline first (S2V) to derive a synthetic IV, which is then used as the CTR counter."/>
/// </para>
/// <para>
/// SIV <em>inverts</em> the order shown in the generic AEAD diagram: the bottom pipeline runs <b>first</b> — S2V/CMAC
/// over the associated data and plaintext produces the synthetic IV, which is both the tag and the CTR counter base —
/// and only then does the top pipeline encrypt the plaintext under that derived counter. That reversal is what makes
/// SIV misuse-resistant: re-encrypting the same message yields the same ciphertext, but confidentiality is not lost
/// beyond confirming message equality.
/// </para>
/// <para>
/// SIV requires two independent ciphers keyed with different material:
/// <list type="bullet">
/// <item>
/// <description>
/// <c>s2vCipher</c> (K₁) — used by CMAC and S2V to derive the synthetic IV.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>ctrCipher</c> (K₂) — used by AES-CTR to encrypt the plaintext.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// The S2V algorithm (RFC 5297 Section 2.4) accumulates all associated data blocks and the plaintext into a single
/// 128-bit tag using CMAC: <code>
///<![CDATA[
/// D ← CMAC(K₁, 0^128)
/// for each AD block Sᵢ (i < n): D ← dbl(D) ⊕ CMAC(K₁, Sᵢ)
/// if |Sₙ| ≥ 128: T ← CMAC(K₁, xorend(Sₙ, D))
/// else:            T ← CMAC(K₁, dbl(D) ⊕ pad(Sₙ))
/// SIV ← T
///]]>
/// </code>
/// </para>
/// <para>
/// Ciphertext is output as <c>C || SIV</c> (ciphertext then tag), consistent with the
/// <see cref="IAeadBlockCipherModeTransform" /> convention.
/// </para>
/// <para>
/// <strong>When to use SIV.</strong> Pick AES-SIV when deterministic authenticated encryption is wanted — key wrapping
/// (RFC 5297 §6 / RFC 5649), envelope encryption schemes that need stable ciphertext for deduplication, or any context
/// that cannot maintain a per-message nonce. SIV is two-pass and slower than <see cref="GcmModeTransform" /> on
/// commodity hardware, but it has the strongest misuse-resistance profile in this library: re-encrypting the same
/// <c>(plaintext, AAD)</c> tuple produces the same ciphertext, but distinct messages remain confidential and authentic.
/// <see cref="GcmSivModeTransform" /> is the RFC 8452 alternative — same misuse-resistance category, different MAC
/// (POLYVAL) and key schedule, typically faster on AES-NI/PCLMULQDQ hardware.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// SIV uses a doubled key: first half drives the MAC, second half drives CTR encryption.
/// using IBlockCipher s2v = new AesBlockCipher(macKey);
/// using IBlockCipher ctr = new AesBlockCipher(encKey);
/// byte[] iv = new byte[s2v.BlockSize / 8]; // ignored by SIV — present for interface compatibility
/// using IAeadBlockCipherModeTransform siv = new SivModeTransform(s2v, ctr, iv);
///
/// byte[] sealed_ = siv.Encrypt(plaintext, associatedData: header);
///]]>
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/aead-modes.html#siv--misuse-resistant">SIV walk-through in the AEAD-modes
/// guide</seealso> <seealso cref="AesBlockCipher"/>
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions"/>
public sealed class SivModeTransform
    : IAeadBlockCipherModeTransform, IDisposable
{
    /// <summary>
    /// Length of the SIV cipher block is 128 bits (16 bytes). Byte length derived inline via
    /// <see cref="BlockSizeBits" /> / 8.
    /// </summary>
    private const int BlockSizeBits = 128;

    /// <summary>
    /// Length of the SIV authentication tag is 128 bits (16 bytes). Byte length derived inline via
    /// <see cref="TagSizeBits" /> / 8.
    /// </summary>
    private const int TagSizeBits = 128;

    private readonly IBlockCipher _s2vCipher;
    private readonly IBlockCipher _ctrCipher;
    private byte[]? _aad;
    private bool _aadProcessed;
    private bool _completed;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SivModeTransform" /> class.
    /// </summary>
    /// <param name="s2vCipher">The cipher keyed with K₁, used for CMAC and S2V computation.</param>
    /// <param name="ctrCipher">The cipher keyed with K₂, used for CTR encryption.</param>
    /// <param name="iv">
    /// Accepted for interface compatibility; not used by SIV because the synthetic IV is derived from the data.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="s2vCipher" />, <paramref name="ctrCipher" />, or <paramref name="iv" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Either cipher does not have a 16-byte block size, or <paramref name="iv" /> length does not equal the S2V cipher
    /// block size.
    /// </exception>
    public SivModeTransform(IBlockCipher s2vCipher, IBlockCipher ctrCipher, byte[] iv)
    {
        ThrowHelper.ThrowIfNull(s2vCipher);
        ThrowHelper.ThrowIfNull(ctrCipher);
        CryptographyThrowHelper.ThrowIfIvLengthInvalid(iv, s2vCipher.BlockSize);

        CryptographyThrowHelper.ThrowIfBlockSizeNotEqualTo(s2vCipher, BlockSizeBits, "SIV S2V", nameof(s2vCipher));

        CryptographyThrowHelper.ThrowIfBlockSizeNotEqualTo(ctrCipher, BlockSizeBits, "SIV CTR", nameof(ctrCipher));

        _s2vCipher = s2vCipher;
        _ctrCipher = ctrCipher;

        // iv is intentionally unused — SIV derives its own synthetic IV.
    }

    /// <inheritdoc />
    /// <value>Length of the SIV authentication tag is 128 bits (16 bytes).</value>
    public int TagSize => TagSizeBits;

    /// <inheritdoc />
    public void ProcessAssociatedData(ReadOnlySpan<byte> associatedData)
    {
        ThrowIfDisposed();

        CryptographyThrowHelper.ThrowIfAssociatedDataAlreadyProcessed(_aadProcessed);

        _aad = associatedData.ToArray();
        _aadProcessed = true;
    }

    /// <inheritdoc />
    public int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();

        var required = plaintext.Length + (TagSizeBits / 8);
        CryptographyThrowHelper.ThrowIfOutputBufferTooSmall(output, required);

        EnsureAadProcessed();

        byte[]? siv = null;
        byte[]? ctrSeed = null;

        try
        {
            // SIV = S2V(K1, AAD, plaintext).
            siv = S2V(_aad!, plaintext);

            // Encrypt plaintext with CTR (K2) seeded from SIV with bits 31 and 63 cleared.
            ctrSeed = (byte[])siv.Clone();
            ctrSeed[8] &= 0x7F;
            ctrSeed[12] &= 0x7F;

            CtrEncrypt(plaintext, output[..plaintext.Length], ctrSeed);

            // Output: ciphertext || SIV tag.
            siv.CopyTo(output[plaintext.Length..]);

            return required;
        }
        finally
        {
            CryptographyHelper.Clear(ctrSeed);
            CryptographyHelper.Clear(siv);
            _completed = true;
        }
    }

    /// <inheritdoc />
    public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();

        CryptographyThrowHelper.ThrowIfCiphertextTooShort(ciphertextWithTag, TagSizeBits / 8);

        var plaintextLength = ciphertextWithTag.Length - (TagSizeBits / 8);
        CryptographyThrowHelper.ThrowIfOutputBufferTooSmall(output, plaintextLength);

        EnsureAadProcessed();

        ReadOnlySpan<byte> ciphertext = ciphertextWithTag[..plaintextLength];
        ReadOnlySpan<byte> receivedSiv = ciphertextWithTag[plaintextLength..];

        byte[]? ctrSeed = null;
        byte[]? expectedSiv = null;

        try
        {
            // Decrypt with CTR seeded from received SIV.
            ctrSeed = receivedSiv.ToArray();
            ctrSeed[8] &= 0x7F;
            ctrSeed[12] &= 0x7F;

            CtrEncrypt(ciphertext, output[..plaintextLength], ctrSeed);

            // Verify SIV.
            expectedSiv = S2V(_aad!, output[..plaintextLength]);
            if (!CryptographicOperations.FixedTimeEquals(expectedSiv, receivedSiv))
            {
                CryptographyHelper.Clear(output[..plaintextLength]);
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AuthenticationTagMismatch);
            }

            return plaintextLength;
        }
        finally
        {
            CryptographyHelper.Clear(expectedSiv);
            CryptographyHelper.Clear(ctrSeed);
            _completed = true;
        }
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if this transform has already encrypted or decrypted a message.
    /// SIV transforms are single-use; create a fresh instance per message.
    /// </summary>
    private void ThrowIfCompleted() =>
        CryptographyThrowHelper.ThrowIfAlreadyCompleted(_completed);

    /// <summary>
    /// Releases the resources used by this instance and clears retained associated-data state from memory.
    /// </summary>
    /// <remarks>
    /// The supplied <see cref="IBlockCipher" /> instances are not disposed by this type. Ownership remains with the
    /// caller.
    /// </remarks>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release managed resources; <see langword="false" /> to release unmanaged resources
    /// only.
    /// </param>
    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            CryptographyHelper.ClearAndNullify(ref _aad);
            _aadProcessed = false;
        }

        _disposed = true;
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the associated-data S2V contribution has been initialized before payload processing.
    /// </summary>
    private void EnsureAadProcessed()
    {
        if (!_aadProcessed)
        {
            _aad = [];
            _aadProcessed = true;
        }
    }

    /// <summary>
    /// Computes the Synthetic IV using S2V per RFC 5297 Section 2.4. S2V(K, S₁, …, Sₙ) where S₁ = AAD and Sₙ =
    /// plaintext.
    /// </summary>
    /// <param name="aad">The associated authenticated data.</param>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <returns>The S2V synthetic initialization vector.</returns>
    private byte[] S2V(ReadOnlySpan<byte> aad, ReadOnlySpan<byte> plaintext)
    {
        var blockSize = _s2vCipher.BlockSize / 8;

        var zeroBlock = new byte[blockSize];
        byte[]? d = null;
        byte[]? mac = null;
        byte[]? t = null;
        byte[]? padded = null;

        try
        {
            // D = CMAC(K1, 0^128).
            d = ComputeCmac(zeroBlock);

            // For each component before the last: D = dbl(D) XOR CMAC(K1, component).
            if (aad.Length > 0)
            {
                Dbl(d);

                mac = ComputeCmac(aad);
                Xor(d, mac, d);
            }

            // Last component = plaintext.
            if (plaintext.Length == 0)
            {
                Dbl(d);
                d[blockSize - 1] ^= 0x01;

                return ComputeCmac(d);
            }

            if (plaintext.Length >= blockSize)
            {
                t = plaintext.ToArray();

                var offset = t.Length - blockSize;
                for (var i = 0; i < blockSize; i++)
                    t[offset + i] ^= d[i];

                return ComputeCmac(t);
            }

            Dbl(d);

            padded = new byte[blockSize];
            plaintext.CopyTo(padded);
            padded[plaintext.Length] = 0x80;

            Xor(d, padded, padded);

            return ComputeCmac(padded);
        }
        finally
        {
            CryptographyHelper.Clear(padded);
            CryptographyHelper.Clear(t);
            CryptographyHelper.Clear(mac);
            CryptographyHelper.Clear(d);
            CryptographyHelper.Clear(zeroBlock);
        }
    }

    /// <summary>
    /// Computes AES-CMAC of <paramref name="message" /> using <c>s2vCipher</c> per RFC 4493.
    /// </summary>
    /// <param name="message">The message to MAC.</param>
    /// <returns>The 16-byte CMAC tag.</returns>
    private byte[] ComputeCmac(ReadOnlySpan<byte> message)
    {
        var blockSize = _s2vCipher.BlockSize / 8;

        var zeroBlock = new byte[blockSize];
        var l = new byte[blockSize];
        var k1 = new byte[blockSize];
        var k2 = new byte[blockSize];
        var mac = new byte[blockSize];
        var lastBlock = new byte[blockSize];

        try
        {
            _s2vCipher.Encrypt(zeroBlock, l);

            l.CopyTo(k1, 0);
            Dbl(k1);

            k1.CopyTo(k2, 0);
            Dbl(k2);

            var totalBlocks = (message.Length + blockSize - 1) / blockSize;
            var lastIsFull = message.Length > 0 && message.Length % blockSize == 0;

            if (message.Length == 0)
            {
                totalBlocks = 1;
                lastIsFull = false;
            }

            for (var blockIdx = 0; blockIdx < totalBlocks - 1; blockIdx++)
            {
                var block = new byte[blockSize];

                try
                {
                    message.Slice(blockIdx * blockSize, blockSize).CopyTo(block);

                    Xor(mac, block, mac);
                    _s2vCipher.Encrypt(mac, mac);
                }
                finally
                {
                    CryptographyHelper.Clear(block);
                }
            }

            if (message.Length > 0)
            {
                var lastOffset = (totalBlocks - 1) * blockSize;
                var lastLen = message.Length - lastOffset;

                message.Slice(lastOffset, lastLen).CopyTo(lastBlock);

                if (!lastIsFull)
                    lastBlock[lastLen] = 0x80;
            }
            else
            {
                lastBlock[0] = 0x80;
            }

            var subkey = lastIsFull ? k1 : k2;

            Xor(lastBlock, subkey, lastBlock);
            Xor(mac, lastBlock, mac);
            _s2vCipher.Encrypt(mac, mac);

            return mac;
        }
        catch
        {
            CryptographyHelper.Clear(mac);
            throw;
        }
        finally
        {
            CryptographyHelper.Clear(lastBlock);
            CryptographyHelper.Clear(k2);
            CryptographyHelper.Clear(k1);
            CryptographyHelper.Clear(l);
            CryptographyHelper.Clear(zeroBlock);
        }
    }

    /// <summary>
    /// Applies AES-CTR encryption using <paramref name="counter" /> as the initial counter block, producing
    /// <c>input XOR keystream</c> in <paramref name="output" />.
    /// </summary>
    /// <param name="input">The plaintext or ciphertext bytes.</param>
    /// <param name="output">The destination span.</param>
    /// <param name="counter">The starting counter block.</param>
    private void CtrEncrypt(ReadOnlySpan<byte> input, Span<byte> output, byte[] counter)
    {
        var blockSize = _ctrCipher.BlockSize / 8;
        var ctr = (byte[])counter.Clone();
        Span<byte> ks = stackalloc byte[blockSize];

        try
        {
            for (var offset = 0; offset < input.Length; offset += blockSize)
            {
                _ctrCipher.Encrypt(ctr, ks);

                for (var i = ctr.Length - 1; i >= 0; i--)
                    if (++ctr[i] != 0) break;

                var len = Math.Min(blockSize, input.Length - offset);
                for (var i = 0; i < len; i++)
                    output[offset + i] = (byte)(input[offset + i] ^ ks[i]);
            }
        }
        finally
        {
            CryptographyHelper.Clear(ks);
            CryptographyHelper.Clear(ctr);
        }
    }

    /// <summary>
    /// Doubles <paramref name="x" /> in-place in GF(2^128) with big-endian bit order and polynomial x^128 + x^7 + x^2 +
    /// x + 1.
    /// </summary>
    /// <param name="x">The 16-byte block to double in GF(2<sup>128</sup>); updated in place.</param>
    private static void Dbl(byte[] x)
    {
        var msb = (x[0] & 0x80) != 0;

        for (var i = 0; i < x.Length - 1; i++)
            x[i] = (byte)((x[i] << 1) | (x[i + 1] >> 7));

        x[^1] <<= 1;

        if (msb)
            x[^1] ^= 0x87;
    }

    /// <summary>
    /// Writes the byte-wise XOR of <paramref name="a" /> and <paramref name="b" /> into <paramref name="result" />.
    /// </summary>
    /// <param name="a">The first operand span.</param>
    /// <param name="b">The second operand span.</param>
    /// <param name="result">The destination span.</param>
    private static void Xor(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> result)
    {
        for (var i = 0; i < result.Length; i++)
            result[i] = (byte)(a[i] ^ b[i]);
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
