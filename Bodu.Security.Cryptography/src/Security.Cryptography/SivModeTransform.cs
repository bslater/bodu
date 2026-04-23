// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SivModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies Synthetic Initialisation Vector (SIV) mode to two underlying <see cref="IBlockCipher" />
/// instances, providing deterministic authenticated encryption per RFC 5297 (AES-SIV).
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/aead-mode.svg" alt="Generic AEAD data flow — SIV inverts the usual order by running the MAC pipeline first (S2V) to derive a synthetic IV, which is then used as the CTR counter." />
/// </para>
/// <para>
/// SIV <em>inverts</em> the order shown in the generic AEAD diagram: the bottom pipeline runs <b>first</b>
/// — S2V/CMAC over the associated data and plaintext produces the synthetic IV (which is both the tag and
/// the CTR counter base) — and only then does the top pipeline encrypt the plaintext under that derived
/// counter. That reversal is what makes SIV misuse-resistant: re-encrypting the same message yields the
/// same ciphertext, but confidentiality is not lost beyond confirming message equality.
/// </para>
/// <para>
/// SIV requires two independent ciphers keyed with different material (each half of a doubled key):
/// <list type="bullet">
/// <item><description><c>s2vCipher</c> (K₁) — used by CMAC and S2V to derive the synthetic IV.</description></item>
/// <item><description><c>ctrCipher</c> (K₂) — used by AES-CTR to encrypt the plaintext.</description></item>
/// </list>
/// </para>
/// <para>
/// The S2V algorithm (RFC 5297 Section 2.4) accumulates all associated data blocks and the
/// plaintext into a single 128-bit tag using CMAC:
/// <code>
/// D ← CMAC(K₁, 0^128)
/// for each AD block Sᵢ (i &lt; n): D ← dbl(D) ⊕ CMAC(K₁, Sᵢ)
/// if |Sₙ| ≥ 128: T ← CMAC(K₁, xorend(Sₙ, D))
/// else:            T ← CMAC(K₁, dbl(D) ⊕ pad(Sₙ))
/// SIV ← T
/// </code>
/// </para>
/// <para>
/// Ciphertext is output as <c>C || SIV</c> (ciphertext then tag), consistent with the
/// <see cref="IAeadBlockCipherModeTransform" /> convention.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/aead-modes.html#siv--misuse-resistant">SIV walk-through in the AEAD-modes guide</seealso>
/// <seealso cref="AesBlockCipher" />
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions" />
public sealed class SivModeTransform : IAeadBlockCipherModeTransform
{
    private const int TagLengthBytes = 16;

    private readonly IBlockCipher _s2vCipher;  // K1 — CMAC / S2V
    private readonly IBlockCipher _ctrCipher;  // K2 — CTR encryption
    private byte[]? _aad;
    private bool _aadProcessed;

    /// <summary>
    /// Initialises a new instance of the <see cref="SivModeTransform" /> class.
    /// </summary>
    /// <param name="s2vCipher">The cipher keyed with K₁, used for CMAC and S2V computation.</param>
    /// <param name="ctrCipher">The cipher keyed with K₂, used for CTR encryption.</param>
    /// <param name="iv">Accepted for interface compatibility; not used by SIV (the synthetic IV is derived from the data).</param>
    /// <exception cref="ArgumentNullException"><paramref name="s2vCipher" />, <paramref name="ctrCipher" />, or <paramref name="iv" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="iv" /> length does not equal the cipher block size.</exception>
    public SivModeTransform(IBlockCipher _s2vCipher, IBlockCipher _ctrCipher, byte[] iv)
    {
        this._s2vCipher = _s2vCipher ?? throw new ArgumentNullException(nameof(_s2vCipher));
        this._ctrCipher = _ctrCipher ?? throw new ArgumentNullException(nameof(_ctrCipher));
        if (iv is null) throw new ArgumentNullException(nameof(iv));
        if (iv.Length != _s2vCipher.BlockSize)
            throw new ArgumentException(
                $"IV length ({iv.Length}) must equal the cipher block size ({_s2vCipher.BlockSize}).", nameof(iv));
        // iv is intentionally unused — SIV derives its own synthetic IV.
    }

    /// <inheritdoc />
    public int TagSize => TagLengthBytes;

    /// <inheritdoc />
    public void ProcessAssociatedData(ReadOnlySpan<byte> associatedData)
    {
        if (this._aadProcessed)
            throw new InvalidOperationException("AssociatedData has already been processed.");
        this._aad = associatedData.ToArray();
        this._aadProcessed = true;
    }

    /// <inheritdoc />
    public int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        int required = plaintext.Length + TagSize;
        if (output.Length < required)
            throw new ArgumentException($"Output must be at least {required} bytes.", nameof(output));
        EnsureAadProcessed();

        // SIV = S2V(K1, AAD, plaintext).
        byte[] siv = S2V(this._aad.AsSpan(), plaintext);

        // Encrypt plaintext with CTR (K2) seeded from SIV with bits 31 and 63 cleared
        // (RFC 5297 Section 2.6: "the rightmost bit is the 0th", so bit 63 = byte[8] bit 7,
        // bit 31 = byte[12] bit 7 in a 16-byte big-endian representation).
        byte[] ctrSeed = (byte[])siv.Clone();
        ctrSeed[8] &= 0x7F;   // clear bit 63 from right (byte index 15 − 63÷8 = 8)
        ctrSeed[12] &= 0x7F;   // clear bit 31 from right (byte index 15 − 31÷8 = 12)
        CtrEncrypt(plaintext, output.Slice(0, plaintext.Length), ctrSeed);

        // Output: ciphertext || SIV tag.
        siv.CopyTo(output.Slice(plaintext.Length));
        return required;
    }

    /// <inheritdoc />
    public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        if (ciphertextWithTag.Length < TagSize)
            throw new ArgumentException($"Input must be at least {TagSize} bytes.", nameof(ciphertextWithTag));
        int plaintextLength = ciphertextWithTag.Length - TagSize;
        if (output.Length < plaintextLength)
            throw new ArgumentException($"Output must be at least {plaintextLength} bytes.", nameof(output));
        EnsureAadProcessed();

        ReadOnlySpan<byte> ciphertext = ciphertextWithTag.Slice(0, plaintextLength);
        ReadOnlySpan<byte> receivedSiv = ciphertextWithTag.Slice(plaintextLength);

        // Decrypt with CTR seeded from received SIV (bits 31 and 63 cleared, same as encrypt).
        byte[] ctrSeed = receivedSiv.ToArray();
        ctrSeed[8] &= 0x7F;   // clear bit 63 from right
        ctrSeed[12] &= 0x7F;   // clear bit 31 from right
        CtrEncrypt(ciphertext, output.Slice(0, plaintextLength), ctrSeed);

        // Verify SIV.
        byte[] expectedSiv = S2V(this._aad.AsSpan(), output.Slice(0, plaintextLength));
        if (!CryptographicOperations.FixedTimeEquals(expectedSiv, receivedSiv))
        {
            CryptographicOperations.ZeroMemory(output.Slice(0, plaintextLength));
            throw new CryptographicException("SIV authentication verification failed.");
        }
        return plaintextLength;
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the associated-data (AAD) S2V contribution has been finalised exactly once
    /// before payload bytes are processed; no-op on subsequent invocations.
    /// </summary>
    private void EnsureAadProcessed()
    {
        if (!this._aadProcessed) { this._aad = Array.Empty<byte>(); this._aadProcessed = true; }
    }

    /// <summary>
    /// Computes the Synthetic IV using S2V per RFC 5297 Section 2.4.
    /// S2V(K, S₁, …, Sₙ) where S₁ = AAD and Sₙ = plaintext.
    /// </summary>
    /// <param name="aad">The associated authenticated data.</param>
    /// <param name="plaintext">The plaintext bytes.</param>
    /// <returns>The S2V synthetic initialisation vector per RFC 5297 §2.4.</returns>
    private byte[] S2V(ReadOnlySpan<byte> _aad, ReadOnlySpan<byte> plaintext)
    {
        int blockSize = this._s2vCipher.BlockSize;

        // D = CMAC(K1, 0^128)
        byte[] d = ComputeCmac(new byte[blockSize]);

        // For each component before the last: D = dbl(D) XOR CMAC(K1, component)
        if (_aad.Length > 0)
        {
            Dbl(d);
            byte[] mac = ComputeCmac(_aad);
            Xor(d, mac, d);
        }

        // Last component = plaintext.
        if (plaintext.Length == 0)
        {
            // Empty plaintext: pad D to block size and return CMAC(K1, dbl(D) XOR pad).
            Dbl(d);
            d[blockSize - 1] ^= 0x01; // XOR with the single-block pad = 0^127 || 1
            return ComputeCmac(d);
        }
        else if (plaintext.Length >= blockSize)
        {
            // xorend: XOR D with the last blockSize bytes of plaintext before CMAC.
            byte[] t = plaintext.ToArray();
            int offset = t.Length - blockSize;
            for (int i = 0; i < blockSize; i++) t[offset + i] ^= d[i];
            return ComputeCmac(t);
        }
        else
        {
            // Partial: pad plaintext with 0x80 || 0...0 then XOR with dbl(D).
            Dbl(d);
            byte[] padded = new byte[blockSize];
            plaintext.CopyTo(padded);
            padded[plaintext.Length] = 0x80;
            Xor(d, padded, padded);
            return ComputeCmac(padded);
        }
    }

    /// <summary>
    /// Computes AES-CMAC of <paramref name="message" /> using <c>s2vCipher</c> per RFC 4493.
    /// </summary>
    /// <param name="message">The message to MAC.</param>
    /// <returns>The 16-byte CMAC tag.</returns>
    private byte[] ComputeCmac(ReadOnlySpan<byte> message)
    {
        int blockSize = this._s2vCipher.BlockSize;

        // Subkey generation.
        byte[] L = new byte[blockSize];
        this._s2vCipher.Encrypt(new byte[blockSize], L);
        byte[] k1 = (byte[])L.Clone(); Dbl(k1);
        byte[] k2 = (byte[])k1.Clone(); Dbl(k2);

        byte[] mac = new byte[blockSize];
        int totalBlocks = (message.Length + blockSize - 1) / blockSize;
        bool lastIsFull = message.Length > 0 && message.Length % blockSize == 0;
        if (message.Length == 0) { totalBlocks = 1; lastIsFull = false; }

        for (int blockIdx = 0; blockIdx < totalBlocks - 1; blockIdx++)
        {
            byte[] block = new byte[blockSize];
            message.Slice(blockIdx * blockSize, blockSize).CopyTo(block);
            Xor(mac, block, mac);
            this._s2vCipher.Encrypt(mac, mac);
        }

        // Last block.
        byte[] lastBlock = new byte[blockSize];
        if (message.Length > 0)
        {
            int lastOffset = (totalBlocks - 1) * blockSize;
            int lastLen = message.Length - lastOffset;
            message.Slice(lastOffset, lastLen).CopyTo(lastBlock);
            if (!lastIsFull) lastBlock[lastLen] = 0x80;
        }
        else
        {
            lastBlock[0] = 0x80; // empty message: pad = 0x80 || 0...0
        }

        byte[] subkey = lastIsFull ? k1 : k2;
        Xor(lastBlock, subkey, lastBlock);
        Xor(mac, lastBlock, mac);
        this._s2vCipher.Encrypt(mac, mac);

        return mac;
    }

    /// <summary>
    /// Applies AES-CTR encryption using <paramref name="counter" /> as the initial counter
    /// block, producing <c>input XOR keystream</c> in <paramref name="output" />.
    /// </summary>
    /// <param name="input">The plaintext (or ciphertext) bytes.</param>
    /// <param name="output">The destination span; must be at least <paramref name="input" />.Length bytes.</param>
    /// <param name="counter">The starting counter block; the low 32 bits are incremented per
    /// block per RFC 5297.</param>
    private void CtrEncrypt(ReadOnlySpan<byte> input, Span<byte> output, byte[] counter)
    {
        int blockSize = this._ctrCipher.BlockSize;
        byte[] ctr = (byte[])counter.Clone();
        Span<byte> ks = stackalloc byte[blockSize];

        for (int offset = 0; offset < input.Length; offset += blockSize)
        {
            this._ctrCipher.Encrypt(ctr, ks);
            // Increment counter (big-endian).
            for (int i = ctr.Length - 1; i >= 0; i--)
                if (++ctr[i] != 0) break;
            int len = Math.Min(blockSize, input.Length - offset);
            for (int i = 0; i < len; i++)
                output[offset + i] = (byte)(input[offset + i] ^ ks[i]);
        }
    }

    /// <summary>
    /// Doubles <paramref name="x" /> in-place in GF(2^128) with big-endian bit order and
    /// polynomial x^128 + x^7 + x^2 + x + 1.
    /// </summary>
    /// <param name="x">The 16-byte block to double in GF(2<sup>128</sup>); updated in place.</param>
    private static void Dbl(byte[] x)
    {
        bool msb = (x[0] & 0x80) != 0;
        for (int i = 0; i < x.Length - 1; i++)
            x[i] = (byte)((x[i] << 1) | (x[i + 1] >> 7));
        x[x.Length - 1] <<= 1;
        if (msb) x[x.Length - 1] ^= 0x87;
    }

    /// <summary>
    /// Writes the byte-wise XOR of <paramref name="a" /> and <paramref name="b" /> into
    /// <paramref name="result" />.
    /// </summary>
    /// <param name="a">The first operand span.</param>
    /// <param name="b">The second operand span; must be at least <paramref name="a" />.Length bytes.</param>
    /// <param name="result">The destination span; must be at least <paramref name="a" />.Length bytes.</param>
    private static void Xor(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> result)
    {
        for (int i = 0; i < result.Length; i++) result[i] = (byte)(a[i] ^ b[i]);
    }
}
