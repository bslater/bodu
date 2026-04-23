// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies Galois/Counter Mode (GCM) to an underlying <see cref="IBlockCipher" />, providing single-pass
/// authenticated encryption with associated data (AEAD).
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/aead-mode.svg" alt="Generic AEAD data flow — a CTR-style keystream produces ciphertext from plaintext, and a polynomial MAC computed over nonce, associated data, and ciphertext yields the authentication tag." />
/// </para>
/// <para>
/// GCM is the canonical instance of the generic AEAD shape shown above: the top pipeline is the
/// <b>Keystream Generator (CTR)</b> panel, and the bottom pipeline is the <b>MAC / polynomial hash</b>
/// panel specialised to GHASH. The nonce feeds both pipelines (<c>J0</c> on the top for the counter base
/// and implicitly on the bottom as the tag-finalising key), and the resulting <em>ciphertext</em> output is
/// routed into the MAC alongside the <em>associated data</em>.
/// </para>
/// <para>
/// GCM combines CTR-mode encryption with GHASH authentication over GF(2^128):
/// <list type="bullet">
/// <item><description>Hash key: H = E(0^128)</description></item>
/// <item><description>Initial counter J0 derived from the IV.</description></item>
/// <item><description>Ciphertext: C_i = P_i ⊕ E(counter_i), counter incremented per block.</description></item>
/// <item><description>Tag: T = GHASH(H, AAD, C) ⊕ E(J0).</description></item>
/// </list>
/// </para>
/// <para>
/// GF(2^128) multiplication uses the irreducible polynomial x^128 + x^7 + x^2 + x + 1 with
/// big-endian bit ordering, and the reduction constant 0xE1 in the most-significant byte.
/// </para>
/// <para>
/// The tag size is fixed at 16 bytes (the full GHASH output). The IV is used directly as the initial
/// counter J0; for standard GCM with 96-bit nonces, the caller should pad to blockSize.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/aead-modes.html#gcm--the-workhorse">GCM walk-through in the AEAD-modes guide</seealso>
/// <seealso cref="AesBlockCipher" />
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions" />
public sealed class GcmModeTransform : IAeadBlockCipherModeTransform
{
    private const int DefaultTagSize = 16;

    private readonly IBlockCipher _cipher;
    private readonly byte[] _h;          // hash key = E(0^128)
    private readonly byte[] _j0;         // initial counter (base for tag and first keystream)
    private readonly byte[] _counter;    // running CTR counter (incremented per block)
    private byte[]? _aad;
    private bool _aadProcessed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GcmModeTransform" /> class.
    /// </summary>
    /// <param name="cipher">The block cipher. Must have a 16-byte (128-bit) block size.</param>
    /// <param name="iv">
    /// The initialisation vector used as the base counter J0. Must equal the cipher block size.
    /// A defensive copy is taken.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="iv" /> length does not equal the cipher block size.</exception>
    public GcmModeTransform(IBlockCipher cipher, byte[] iv)
    {
        this._cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
        if (iv is null) throw new ArgumentNullException(nameof(iv));
        if (iv.Length != cipher.BlockSize)
            throw new ArgumentException(
                $"IV length ({iv.Length}) must equal the cipher block size ({cipher.BlockSize}).",
                nameof(iv));

        // H = E_K(0^blockSize) — the GHASH hash key.
        this._h = new byte[cipher.BlockSize];
        cipher.Encrypt(new byte[cipher.BlockSize], this._h);

        // J0 is the base counter for the tag; counter starts at J0 + 1 for data.
        this._j0 = (byte[])iv.Clone();
        this._counter = (byte[])iv.Clone();
        IncrementCounter32(this._counter); // counter for first data block = J0 + 1
    }

    /// <inheritdoc />
    public int TagSize => DefaultTagSize;

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
            throw new ArgumentException(
                $"Output buffer must be at least {required} bytes (plaintext + tag).",
                nameof(output));

        EnsureAadProcessed();

        // Encrypt plaintext with CTR → ciphertext.
        Span<byte> ciphertext = output.Slice(0, plaintext.Length);
        ApplyCtr(plaintext, ciphertext);

        // Compute tag: GHASH(H, AAD, C) XOR E(J0).
        byte[] tag = ComputeTag(this._aad.AsSpan(), ciphertext);
        tag.AsSpan().CopyTo(output.Slice(plaintext.Length));

        return required;
    }

    /// <inheritdoc />
    public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        if (ciphertextWithTag.Length < TagSize)
            throw new ArgumentException(
                $"Input must be at least {TagSize} bytes (tag only with no ciphertext).",
                nameof(ciphertextWithTag));

        int plaintextLength = ciphertextWithTag.Length - TagSize;
        if (output.Length < plaintextLength)
            throw new ArgumentException(
                $"Output buffer must be at least {plaintextLength} bytes.",
                nameof(output));

        EnsureAadProcessed();

        ReadOnlySpan<byte> ciphertext = ciphertextWithTag.Slice(0, plaintextLength);
        ReadOnlySpan<byte> receivedTag = ciphertextWithTag.Slice(plaintextLength);

        // Verify tag before decrypting (constant-time comparison).
        byte[] expectedTag = ComputeTag(this._aad.AsSpan(), ciphertext);
        if (!CryptographicOperations.FixedTimeEquals(expectedTag, receivedTag))
            throw new CryptographicException("GCM authentication tag verification failed.");

        // Decrypt with CTR.
        ApplyCtr(ciphertext, output.Slice(0, plaintextLength));

        return plaintextLength;
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the associated-data (AAD) GHASH contribution has been finalised exactly once
    /// before payload bytes are processed; no-op on subsequent invocations.
    /// </summary>
    private void EnsureAadProcessed()
    {
        if (!this._aadProcessed)
        {
            this._aad = Array.Empty<byte>();
            this._aadProcessed = true;
        }
    }

    /// <summary>
    /// Applies CTR mode: for each block, computes keystream = E(counter), XORs with input,
    /// increments the counter.
    /// </summary>
    /// <param name="input">The input bytes to XOR with the CTR keystream.</param>
    /// <param name="output">The destination span; must be at least <paramref name="input" />.Length bytes.</param>
    private void ApplyCtr(ReadOnlySpan<byte> input, Span<byte> output)
    {
        int blockSize = this._cipher.BlockSize;
        Span<byte> keystream = stackalloc byte[blockSize];

        for (int offset = 0; offset < input.Length; offset += blockSize)
        {
            this._cipher.Encrypt(this._counter, keystream);
            IncrementCounter32(this._counter);

            int remaining = Math.Min(blockSize, input.Length - offset);
            for (int i = 0; i < remaining; i++)
                output[offset + i] = (byte)(input[offset + i] ^ keystream[i]);
        }
    }

    /// <summary>
    /// Computes the GCM authentication tag: T = GHASH(H, AAD, C) ⊕ E(J0).
    /// </summary>
    /// <param name="aad">The associated authenticated data.</param>
    /// <param name="ciphertext">The ciphertext bytes authenticated by the tag.</param>
    /// <returns>The computed 128-bit GCM authentication tag.</returns>
    private byte[] ComputeTag(ReadOnlySpan<byte> aad, ReadOnlySpan<byte> ciphertext)
    {
        int blockSize = this._cipher.BlockSize;
        byte[] y = new byte[blockSize]; // GHASH accumulator

        // GHASH over AAD.
        GhashUpdate(y, aad);

        // GHASH over ciphertext.
        GhashUpdate(y, ciphertext);

        // GHASH length block: [len(AAD)]_64 || [len(C)]_64 in bits, big-endian.
        byte[] lenBlock = new byte[blockSize];
        WriteBigEndian64((ulong)aad.Length * 8, lenBlock, 0);
        WriteBigEndian64((ulong)ciphertext.Length * 8, lenBlock, 8);
        GhashBlock(y, lenBlock);

        // T = y ⊕ E(J0).
        byte[] eJ0 = new byte[blockSize];
        this._cipher.Encrypt(this._j0, eJ0);
        for (int i = 0; i < blockSize; i++)
            y[i] ^= eJ0[i];

        return y;
    }

    /// <summary>Feeds <paramref name="data" /> into the GHASH accumulator <paramref name="y" /> block by block.</summary>
    /// <param name="y">The running GHASH accumulator (16 bytes); updated in place.</param>
    /// <param name="data">The input bytes to fold into the GHASH state.</param>
    private void GhashUpdate(byte[] y, ReadOnlySpan<byte> data)
    {
        int blockSize = this._cipher.BlockSize;
        for (int offset = 0; offset < data.Length; offset += blockSize)
        {
            int remaining = Math.Min(blockSize, data.Length - offset);
            byte[] block = new byte[blockSize];
            data.Slice(offset, remaining).CopyTo(block);
            GhashBlock(y, block);
        }
    }

    /// <summary>Processes one 16-byte block through GHASH: y = (y ⊕ block) * H.</summary>
    /// <param name="y">The running GHASH accumulator (16 bytes); updated in place.</param>
    /// <param name="block">A single 16-byte block to fold into <paramref name="y" />.</param>
    private void GhashBlock(byte[] y, byte[] block)
    {
        for (int i = 0; i < y.Length; i++)
            y[i] ^= block[i];
        GhashMultiply(y, this._h, y);
    }

    /// <summary>
    /// Multiplies <paramref name="x" /> by <paramref name="h" /> in GF(2^128) using the GCM
    /// irreducible polynomial x^128 + x^7 + x^2 + x + 1, with big-endian bit ordering.
    /// Result is written into <paramref name="result" /> (may alias <paramref name="x" />).
    /// </summary>
    /// <param name="x">The left operand block (16 bytes).</param>
    /// <param name="h">The hash subkey <c>H</c> (16 bytes).</param>
    /// <param name="result">The destination span (16 bytes); receives <c>x · H</c> in GF(2<sup>128</sup>).</param>
    private static void GhashMultiply(ReadOnlySpan<byte> x, ReadOnlySpan<byte> h, Span<byte> result)
    {
        Span<byte> z = stackalloc byte[16]; // accumulator
        Span<byte> v = stackalloc byte[16];
        h.CopyTo(v);

        for (int i = 0; i < 128; i++)
        {
            // If bit i of x is set (bit 0 = MSB of byte 0).
            if ((x[i >> 3] & (0x80 >> (i & 7))) != 0)
                for (int j = 0; j < 16; j++) z[j] ^= v[j];

            // Right-shift v by 1 bit (big-endian).
            bool lsb = (v[15] & 1) != 0;
            for (int j = 15; j > 0; j--)
                v[j] = (byte)((v[j] >> 1) | ((v[j - 1] & 1) << 7));
            v[0] >>= 1;

            // If LSB was set, reduce by R = 0xE1 || 0...0 (represents x^7 + x^2 + x + 1).
            if (lsb) v[0] ^= 0xE1;
        }

        z.CopyTo(result);
    }

    /// <summary>
    /// Increments the 32-bit big-endian counter in the last 4 bytes of <paramref name="counter" />.
    /// </summary>
    /// <param name="counter">The 16-byte CTR block; its low 32 bits are incremented in place per NIST SP 800-38D.</param>
    private static void IncrementCounter32(byte[] counter)
    {
        for (int i = counter.Length - 1; i >= counter.Length - 4; i--)
            if (++counter[i] != 0) break;
    }

    /// <summary>
    /// Writes <paramref name="value" /> into <paramref name="buffer" /> in big-endian byte order
    /// starting at <paramref name="offset" />.
    /// </summary>
    /// <param name="value">The 64-bit value to serialise.</param>
    /// <param name="buffer">The destination buffer; must contain at least 8 bytes at
    /// <paramref name="offset" />.</param>
    /// <param name="offset">The byte offset at which to begin writing.</param>
    private static void WriteBigEndian64(ulong value, byte[] buffer, int offset)
    {
        for (int i = 7; i >= 0; i--)
        {
            buffer[offset + i] = (byte)(value & 0xFF);
            value >>= 8;
        }
    }
}
