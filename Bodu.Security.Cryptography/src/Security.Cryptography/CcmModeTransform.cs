// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CcmModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies Counter with CBC-MAC (CCM) mode to an underlying <see cref="IBlockCipher" />,
/// providing authenticated encryption with associated data (AEAD) per NIST SP 800-38C.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/aead-mode.svg" alt="Generic AEAD data flow — a CTR-style keystream produces ciphertext and a MAC over nonce, associated data, and ciphertext produces the tag. In CCM the MAC is CBC-MAC." />
/// </para>
/// <para>
/// CCM is the <b>CTR + CBC-MAC</b> instantiation of the generic AEAD shape above: the top pipeline is the
/// plain CTR keystream generator (panel labelled <em>Keystream Generator (CTR)</em>), and the bottom
/// pipeline is a CBC-MAC chain over the formatted nonce, associated data, and ciphertext that produces
/// the tag.
/// </para>
/// <para>
/// Fixed parameters (matching the most common deployment profile):
/// <list type="bullet">
/// <item><description>Nonce (Nlen): 12 bytes — first 12 bytes of the IV.</description></item>
/// <item><description>Length field (q): 3 bytes — messages up to 2^24 − 1 bytes.</description></item>
/// <item><description>Tag (T): 16 bytes.</description></item>
/// </list>
/// </para>
/// <para>
/// Formatting follows NIST SP 800-38C Section 6.3.
/// Flag byte B0: bit 6 = Adata, bits 5–3 = M' = (T−2)/2 = 7, bits 2–0 = L' = q−1 = 2.
/// Counter block A_i: byte 0 = 0x02, bytes 1–12 = nonce, bytes 13–15 = counter (big-endian).
/// AAD length is encoded as a 2-byte big-endian prefix (supports up to 65 279 bytes).
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/aead-modes.html#ccm--a-two-pass-alternative">CCM walk-through in the AEAD-modes guide</seealso>
/// <seealso cref="AesBlockCipher" />
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions" />
public sealed class CcmModeTransform : IAeadBlockCipherModeTransform
{
    private const int NonceLengthBytes = 12;
    private const int TagLengthBytes = 16;
    private const byte CounterFlagByte = 0x02; // L' = q-1 = 2
    private const byte BaseB0NoAad = 0x3A;  // 0_111_010
    private const byte BaseB0WithAad = 0x7A;  // 1_111_010

    private readonly IBlockCipher _cipher;
    private readonly byte[] _nonce;
    private byte[]? _aad;
    private bool _aadProcessed;

    /// <summary>
    /// Initialises a new instance. The first 12 bytes of <paramref name="iv" /> are used as the CCM nonce.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="iv" /> length does not equal the cipher block size.</exception>
    public CcmModeTransform(IBlockCipher _cipher, byte[] iv)
    {
        this._cipher = _cipher ?? throw new ArgumentNullException(nameof(_cipher));
        if (iv is null) throw new ArgumentNullException(nameof(iv));
        if (iv.Length != _cipher.BlockSize)
            throw new ArgumentException(
                $"IV length ({iv.Length}) must equal the _cipher block size ({_cipher.BlockSize}).", nameof(iv));

        this._nonce = new byte[NonceLengthBytes];
        iv.AsSpan(0, NonceLengthBytes).CopyTo(this._nonce);
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

        byte[] mac = ComputeCbcMac(this._aad.AsSpan(), plaintext);
        byte[] encTag = XorWithCtrBlock(mac, counterIndex: 0);

        EncryptCtr(plaintext, output.Slice(0, plaintext.Length), startIndex: 1);
        encTag.AsSpan(0, TagSize).CopyTo(output.Slice(plaintext.Length));
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
        ReadOnlySpan<byte> receivedTag = ciphertextWithTag.Slice(plaintextLength);

        EncryptCtr(ciphertext, output.Slice(0, plaintextLength), startIndex: 1);
        byte[] mac = ComputeCbcMac(this._aad.AsSpan(), output.Slice(0, plaintextLength));
        byte[] encTag = XorWithCtrBlock(mac, counterIndex: 0);

        if (!CryptographicOperations.FixedTimeEquals(encTag.AsSpan(0, TagSize), receivedTag))
        {
            CryptographicOperations.ZeroMemory(output.Slice(0, plaintextLength));
            throw new CryptographicException("CCM authentication tag verification failed.");
        }
        return plaintextLength;
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the associated-data (AAD) MAC contribution has been finalised exactly once before
    /// payload bytes are processed; no-op on subsequent invocations.
    /// </summary>
    private void EnsureAadProcessed()
    {
        if (!this._aadProcessed) { this._aad = Array.Empty<byte>(); this._aadProcessed = true; }
    }

    /// <summary>
    /// Computes CBC-MAC over the NIST SP 800-38C formatted input (B0 + AAD encoding + plaintext).
    /// </summary>
    /// <param name="aad">The associated authenticated data.</param>
    /// <param name="plaintext">The plaintext bytes whose MAC is being computed.</param>
    /// <returns>The computed CBC-MAC tag, truncated to the configured tag length.</returns>
    private byte[] ComputeCbcMac(ReadOnlySpan<byte> _aad, ReadOnlySpan<byte> plaintext)
    {
        int blockSize = this._cipher.BlockSize;
        byte[] mac = new byte[blockSize];

        // Block B0.
        bool hasAad = _aad.Length > 0;
        byte[] b0 = new byte[blockSize];
        b0[0] = hasAad ? BaseB0WithAad : BaseB0NoAad;
        this._nonce.CopyTo(b0, 1);
        // Message length in last 3 bytes (big-endian, q=3).
        uint len = (uint)plaintext.Length;
        b0[15] = (byte)len;
        b0[14] = (byte)(len >> 8);
        b0[13] = (byte)(len >> 16);
        CbcMacUpdate(mac, b0);

        // AAD: 2-byte length prefix then AAD bytes, zero-padded to block boundary.
        if (hasAad)
        {
            if (_aad.Length >= 0xFF00)
                throw new NotSupportedException("AAD must be shorter than 65280 bytes for 2-byte length encoding.");
            // Encode: 2-byte length + _aad + zero-padding to block multiple.
            int encodedLen = 2 + _aad.Length;
            int padded = ((encodedLen + blockSize - 1) / blockSize) * blockSize;
            byte[] aadEncoded = new byte[padded];
            aadEncoded[0] = (byte)(_aad.Length >> 8);
            aadEncoded[1] = (byte)_aad.Length;
            _aad.CopyTo(aadEncoded.AsSpan(2));
            for (int i = 0; i < aadEncoded.Length; i += blockSize)
                CbcMacUpdate(mac, aadEncoded.AsSpan(i, blockSize));
        }

        // Plaintext blocks (zero-padded last block).
        for (int i = 0; i < plaintext.Length; i += blockSize)
        {
            byte[] block = new byte[blockSize];
            plaintext.Slice(i, Math.Min(blockSize, plaintext.Length - i)).CopyTo(block);
            CbcMacUpdate(mac, block);
        }

        return mac;
    }

    /// <summary>
    /// XORs <paramref name="block" /> into <paramref name="mac" /> and runs a single AES block
    /// through the underlying cipher to advance the CBC-MAC state.
    /// </summary>
    /// <param name="mac">The CBC-MAC accumulator (16 bytes); updated in place.</param>
    /// <param name="block">The next input block; must be 16 bytes.</param>
    private void CbcMacUpdate(byte[] mac, ReadOnlySpan<byte> block)
    {
        Span<byte> xored = stackalloc byte[mac.Length];
        for (int i = 0; i < mac.Length; i++) xored[i] = (byte)(mac[i] ^ block[i]);
        this._cipher.Encrypt(xored, mac);
    }

    /// <summary>Builds counter block A_i (flags | nonce | counter), encrypts it, and XORs with input.</summary>
    /// <param name="input">The input bytes to XOR with the keystream.</param>
    /// <param name="counterIndex">The CTR block index that produces the keystream block.</param>
    /// <returns>A fresh array holding <c>input XOR keystream</c>.</returns>
    private byte[] XorWithCtrBlock(ReadOnlySpan<byte> input, int counterIndex)
    {
        int blockSize = this._cipher.BlockSize;
        byte[] ctr = new byte[blockSize];
        ctr[0] = CounterFlagByte;
        this._nonce.CopyTo(ctr, 1);
        ctr[15] = (byte)counterIndex;
        ctr[14] = (byte)(counterIndex >> 8);
        ctr[13] = (byte)(counterIndex >> 16);

        byte[] ks = new byte[blockSize];
        this._cipher.Encrypt(ctr, ks);
        for (int i = 0; i < Math.Min(input.Length, blockSize); i++)
            ks[i] ^= input[i];
        return ks;
    }

    /// <summary>
    /// Applies CTR-mode encryption starting from counter-block index
    /// <paramref name="startIndex" />, writing <c>input XOR keystream</c> into
    /// <paramref name="output" />.
    /// </summary>
    /// <param name="input">The plaintext (or ciphertext) bytes to XOR with the keystream.</param>
    /// <param name="output">The destination span; must be at least <paramref name="input" />.Length bytes.</param>
    /// <param name="startIndex">The starting counter-block index in the CTR sequence.</param>
    private void EncryptCtr(ReadOnlySpan<byte> input, Span<byte> output, int startIndex)
    {
        int blockSize = this._cipher.BlockSize;
        Span<byte> ks = stackalloc byte[blockSize];
        for (int offset = 0; offset < input.Length; offset += blockSize)
        {
            int idx = startIndex + offset / blockSize;
            byte[] ctr = new byte[blockSize];
            ctr[0] = CounterFlagByte;
            this._nonce.CopyTo(ctr, 1);
            ctr[15] = (byte)idx;
            ctr[14] = (byte)(idx >> 8);
            ctr[13] = (byte)(idx >> 16);
            this._cipher.Encrypt(ctr, ks);
            int rem = Math.Min(blockSize, input.Length - offset);
            for (int i = 0; i < rem; i++)
                output[offset + i] = (byte)(input[offset + i] ^ ks[i]);
        }
    }
}
