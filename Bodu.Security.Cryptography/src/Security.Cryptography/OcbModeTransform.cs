// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies Offset CodeBook mode version 3 (OCB3) to an underlying <see cref="IBlockCipher" />,
/// providing single-pass authenticated encryption with associated data per RFC 7253.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/aead-mode.svg" alt="Generic AEAD data flow — OCB3 realises both the keystream and the MAC pipelines as a single offset-driven pass over each block." />
/// </para>
/// <para>
/// OCB3 collapses the two pipelines of the generic AEAD shape above into a <em>single pass</em>: the
/// keystream and the MAC chain share the same per-block offset Δ<sub>i</sub>, so each block is touched
/// by the cipher exactly once. In the diagram, this corresponds to merging the top and bottom arrows
/// that reach the MAC — the ciphertext output is simultaneously the next input to the authentication
/// accumulator.
/// </para>
/// <para>
/// The nonce is derived from the first 12 bytes of the IV supplied to the constructor.
/// The tag length defaults to 16 bytes (TAGLEN = 128) and may be set to any value from
/// 1 to 16 bytes via the <paramref name="tagLen" /> constructor parameter.
/// Supported RFC 7253 values are 8, 12, and 16 bytes (64, 96, and 128 bits).
/// </para>
/// <para>
/// Offset initialisation uses the RFC 7253 §2.4 K_top stretch:
/// <code>
///   Nonce  = num2str(TAGLEN mod 128, 7) || zeros(120-bitlen(N)) || 1 || N
///   bottom = str2num(Nonce[123..128])
///   K_top  = ENCIPHER(K, Nonce[1..122] || zeros(6))
///   Stretch = K_top || (K_top[1..64] XOR K_top[9..72])   -- adjacent-byte XOR
///   Offset_0 = Stretch[1+bottom..128+bottom]
/// </code>
/// </para>
/// <para>
/// The L array uses GF(2^128) doubling with polynomial x^128 + x^7 + x^2 + x + 1 (big-endian).
/// </para>
/// <para>
/// <strong>Lifecycle.</strong> Each instance encrypts or decrypts exactly one message. A second call
/// to <see cref="Encrypt" /> or <see cref="Decrypt" /> — including after a tag-mismatch failure —
/// throws <see cref="InvalidOperationException" />. The supplied <see cref="IBlockCipher" /> is
/// not disposed by this type; ownership remains with the caller. <see cref="Dispose" /> clears the
/// retained nonce, OCB offset constants, and cached associated-data state.
/// </para>
/// <para>
/// <strong>When to use OCB3.</strong> Pick OCB3 when you want a single-pass AEAD mode without GCM's
/// catastrophic-on-nonce-reuse profile — OCB still requires nonces to be unique per key, but the failure
/// mode is graceful (only that one message's confidentiality is lost; the GHASH-key-leak amplification
/// does not apply). OCB historically had patent encumbrances that limited adoption; the patents have since
/// been placed into the public domain, but <see cref="GcmModeTransform"/> remains the more widely deployed
/// choice in practice. For nonce-misuse resistance prefer <see cref="GcmSivModeTransform"/> or
/// <see cref="SivModeTransform"/>; for constrained environments prefer <see cref="CcmModeTransform"/>.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using IBlockCipher cipher = new AesBlockCipher(key);
/// byte[] iv = BuildOcbIv(nonce); // first 12 bytes of the IV are the nonce
/// using IAeadBlockCipherModeTransform ocb = new OcbModeTransform(cipher, iv, tagLen: 16);
///
/// byte[] sealed_ = ocb.Encrypt(plaintext, associatedData: header);
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/aead-modes.html#ocb3--single-pass-rfc-7253">OCB3 walk-through in the AEAD-modes guide</seealso>
/// <seealso cref="AesBlockCipher" />
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions" />
public sealed class OcbModeTransform
    : IAeadBlockCipherModeTransform
    , IDisposable
{
    private const int BlockSizeBytes = 16;
    private const int NonceLengthBytes = 12;
    private const int MaxLValues = 32; // enough for 2^32 blocks

    private readonly IBlockCipher _cipher;
    private readonly byte[] _nonce;        // 12-byte nonce
    private readonly int _tagLen;          // TAGLEN in bytes (1–blockSize)
    private readonly byte[] _lStar;        // L_* = E(0^128)
    private readonly byte[] _lDollar;      // L_$ = double(L_*)
    private readonly byte[][] _lArray;     // L[0] = double(L_$), L[1] = double(L[0]), …
    private byte[]? _aad;
    private bool _aadProcessed;
    private bool _completed;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcbModeTransform" /> class.
    /// </summary>
    /// <param name="cipher">The block cipher. Must have a 16-byte block size.</param>
    /// <param name="iv">
    /// The initialisation vector. The first 12 bytes are used as the OCB3 nonce.
    /// Must equal the cipher block size. A defensive copy is taken.
    /// </param>
    /// <param name="tagLen">
    /// Tag length in bytes. Must be between 1 and the cipher block size.
    /// RFC 7253 defines recommended values of 8, 12, and 16 bytes.
    /// Defaults to 16 (TAGLEN = 128 bits).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="cipher" /> does not have a 16-byte block size, <paramref name="iv" /> length does not
    /// equal the cipher block size, or <paramref name="tagLen" /> is outside the range [1, block size].
    /// </exception>
    public OcbModeTransform(IBlockCipher cipher, byte[] iv, int tagLen = 16)
    {
        this._cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));

        if (iv is null)
            throw new ArgumentNullException(nameof(iv));

        if (cipher.BlockSize != BlockSizeBytes)
        {
            throw new ArgumentException(
                $"OCB requires a block cipher with a {BlockSizeBytes}-byte block size.",
                nameof(cipher));
        }

        if (iv.Length != cipher.BlockSize)
        {
            throw new ArgumentException(
                $"IV length ({iv.Length}) must equal the cipher block size ({cipher.BlockSize}).",
                nameof(iv));
        }

        if (tagLen < 1 || tagLen > cipher.BlockSize)
        {
            throw new ArgumentException(
                $"Tag length ({tagLen}) must be between 1 and the cipher block size ({cipher.BlockSize}).",
                nameof(tagLen));
        }

        this._nonce = new byte[NonceLengthBytes];
        iv.AsSpan(0, NonceLengthBytes).CopyTo(this._nonce);

        this._tagLen = tagLen;

        int blockSize = cipher.BlockSize;

        // RFC 7253 §2.1 — Key-dependent constants derived once per key.
        // L_* = ENCIPHER(K, zeros(128)).
        byte[] zeroBlock = new byte[blockSize];
        this._lStar = new byte[blockSize];

        try
        {
            cipher.Encrypt(zeroBlock, this._lStar);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(zeroBlock);
        }

        // L_$ = double(L_*).
        this._lDollar = GfDouble(this._lStar);

        // L[i] = double(L[i-1]), L[0] = double(L_$).
        this._lArray = new byte[MaxLValues][];
        this._lArray[0] = GfDouble(this._lDollar);

        for (int i = 1; i < MaxLValues; i++)
            this._lArray[i] = GfDouble(this._lArray[i - 1]);
    }

    /// <inheritdoc />
    public int TagSize => this._tagLen;

    /// <inheritdoc />
    public void ProcessAssociatedData(ReadOnlySpan<byte> associatedData)
    {
        this.ThrowIfDisposed();

        if (this._aadProcessed)
            throw new InvalidOperationException("AssociatedData has already been processed.");

        this._aad = associatedData.ToArray();
        this._aadProcessed = true;
    }

    /// <inheritdoc />
    public int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        this.ThrowIfDisposed();
        this.ThrowIfCompleted();

        int required = plaintext.Length + TagSize;
        if (output.Length < required)
            throw new ArgumentException($"Output must be at least {required} bytes.", nameof(output));

        EnsureAadProcessed();

        int blockSize = this._cipher.BlockSize;

        byte[]? offset = null;
        byte[]? checksum = null;
        byte[]? block = null;
        byte[]? tagInput = null;
        byte[]? hashResult = null;

        try
        {
            offset = ComputeInitialOffset();
            checksum = new byte[blockSize];
            block = new byte[blockSize];

            int m = (plaintext.Length + blockSize - 1) / blockSize;

            for (int blockIdx = 1; blockIdx <= m - 1; blockIdx++)
            {
                int src = (blockIdx - 1) * blockSize;

                Xor(offset, this._lArray[Ntz(blockIdx)], offset);

                plaintext.Slice(src, blockSize).CopyTo(block);
                Xor(block, offset, block);
                this._cipher.Encrypt(block, block);
                Xor(block, offset, block);
                block.CopyTo(output.Slice(src));

                Xor(checksum, plaintext.Slice(src, blockSize), checksum);
            }

            if (plaintext.Length > 0)
            {
                int lastSrc = (m - 1) * blockSize;
                int lastLen = plaintext.Length - lastSrc;

                if (lastLen == blockSize)
                {
                    Xor(offset, this._lArray[Ntz(m)], offset);

                    plaintext.Slice(lastSrc, blockSize).CopyTo(block);
                    Xor(block, offset, block);
                    this._cipher.Encrypt(block, block);
                    Xor(block, offset, block);
                    block.CopyTo(output.Slice(lastSrc));

                    Xor(checksum, plaintext.Slice(lastSrc, blockSize), checksum);
                }
                else
                {
                    byte[] pad = new byte[blockSize];
                    byte[] padBlock = new byte[blockSize];

                    try
                    {
                        Xor(offset, this._lStar, offset);

                        this._cipher.Encrypt(offset, pad);

                        for (int i = 0; i < lastLen; i++)
                            output[lastSrc + i] = (byte)(plaintext[lastSrc + i] ^ pad[i]);

                        plaintext.Slice(lastSrc, lastLen).CopyTo(padBlock);
                        padBlock[lastLen] = 0x80;

                        Xor(checksum, padBlock, checksum);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(padBlock);
                        CryptographicOperations.ZeroMemory(pad);
                    }
                }
            }

            tagInput = new byte[blockSize];

            Xor(this._lDollar, offset, tagInput);
            Xor(tagInput, checksum, tagInput);
            this._cipher.Encrypt(tagInput, tagInput);

            hashResult = ComputeHash(this._aad!);
            Xor(tagInput, hashResult, tagInput);

            tagInput.AsSpan(0, this._tagLen).CopyTo(output.Slice(plaintext.Length));

            return required;
        }
        finally
        {
            ClearIfNotNull(hashResult);
            ClearIfNotNull(tagInput);
            ClearIfNotNull(block);
            ClearIfNotNull(checksum);
            ClearIfNotNull(offset);
            this._completed = true;
        }
    }

    /// <inheritdoc />
    public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        this.ThrowIfDisposed();
        this.ThrowIfCompleted();

        if (ciphertextWithTag.Length < TagSize)
            throw new ArgumentException($"Input must be at least {TagSize} bytes.", nameof(ciphertextWithTag));

        int plaintextLength = ciphertextWithTag.Length - TagSize;
        if (output.Length < plaintextLength)
            throw new ArgumentException($"Output must be at least {plaintextLength} bytes.", nameof(output));

        EnsureAadProcessed();

        ReadOnlySpan<byte> ciphertext = ciphertextWithTag.Slice(0, plaintextLength);
        ReadOnlySpan<byte> receivedTag = ciphertextWithTag.Slice(plaintextLength);

        int blockSize = this._cipher.BlockSize;

        byte[]? offset = null;
        byte[]? checksum = null;
        byte[]? block = null;
        byte[]? tagInput = null;
        byte[]? hashResult = null;

        try
        {
            offset = ComputeInitialOffset();
            checksum = new byte[blockSize];
            block = new byte[blockSize];

            int m = (plaintextLength + blockSize - 1) / blockSize;

            for (int blockIdx = 1; blockIdx <= m - 1; blockIdx++)
            {
                int src = (blockIdx - 1) * blockSize;

                Xor(offset, this._lArray[Ntz(blockIdx)], offset);

                ciphertext.Slice(src, blockSize).CopyTo(block);
                Xor(block, offset, block);
                this._cipher.Decrypt(block, block);
                Xor(block, offset, block);
                block.CopyTo(output.Slice(src));

                Xor(checksum, output.Slice(src, blockSize), checksum);
            }

            if (plaintextLength > 0)
            {
                int lastSrc = (m - 1) * blockSize;
                int lastLen = plaintextLength - lastSrc;

                if (lastLen == blockSize)
                {
                    Xor(offset, this._lArray[Ntz(m)], offset);

                    ciphertext.Slice(lastSrc, blockSize).CopyTo(block);
                    Xor(block, offset, block);
                    this._cipher.Decrypt(block, block);
                    Xor(block, offset, block);
                    block.CopyTo(output.Slice(lastSrc));

                    Xor(checksum, output.Slice(lastSrc, blockSize), checksum);
                }
                else
                {
                    byte[] pad = new byte[blockSize];
                    byte[] padBlock = new byte[blockSize];

                    try
                    {
                        Xor(offset, this._lStar, offset);

                        this._cipher.Encrypt(offset, pad);

                        for (int i = 0; i < lastLen; i++)
                            output[lastSrc + i] = (byte)(ciphertext[lastSrc + i] ^ pad[i]);

                        output.Slice(lastSrc, lastLen).CopyTo(padBlock);
                        padBlock[lastLen] = 0x80;

                        Xor(checksum, padBlock, checksum);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(padBlock);
                        CryptographicOperations.ZeroMemory(pad);
                    }
                }
            }

            tagInput = new byte[blockSize];

            Xor(this._lDollar, offset, tagInput);
            Xor(tagInput, checksum, tagInput);
            this._cipher.Encrypt(tagInput, tagInput);

            hashResult = ComputeHash(this._aad!);
            Xor(tagInput, hashResult, tagInput);

            if (!CryptographicOperations.FixedTimeEquals(tagInput.AsSpan(0, this._tagLen), receivedTag))
            {
                CryptographicOperations.ZeroMemory(output.Slice(0, plaintextLength));
                throw new CryptographicException("OCB authentication tag verification failed.");
            }

            return plaintextLength;
        }
        finally
        {
            ClearIfNotNull(hashResult);
            ClearIfNotNull(tagInput);
            ClearIfNotNull(block);
            ClearIfNotNull(checksum);
            ClearIfNotNull(offset);
            this._completed = true;
        }
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if this transform has already encrypted or
    /// decrypted a message. OCB transforms are single-use; create a fresh instance per message.
    /// </summary>
    private void ThrowIfCompleted()
    {
        if (this._completed)
            throw new InvalidOperationException(
                "This OCB transform has already completed and cannot be reused. Create a new instance per message.");
    }

    /// <summary>
    /// Releases the resources used by this instance and clears retained nonce, OCB offset constants,
    /// and associated-data state from memory.
    /// </summary>
    /// <remarks>
    /// The supplied <see cref="IBlockCipher" /> is not disposed by this type. Ownership remains with the caller.
    /// </remarks>
    public void Dispose()
    {
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release managed resources; <see langword="false" /> to release unmanaged resources only.
    /// </param>
    private void Dispose(bool disposing)
    {
        if (this._disposed)
            return;

        if (disposing)
        {
            CryptographicOperations.ZeroMemory(this._nonce);
            CryptographicOperations.ZeroMemory(this._lStar);
            CryptographicOperations.ZeroMemory(this._lDollar);

            foreach (byte[] value in this._lArray)
                CryptographicOperations.ZeroMemory(value);

            if (this._aad is not null)
            {
                CryptographicOperations.ZeroMemory(this._aad);
                this._aad = null;
            }

            this._aadProcessed = false;
        }

        this._disposed = true;
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the associated-data authentication contribution has been initialised before payload processing.
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
    /// Computes the initial offset Offset_0 using the RFC 7253 §2.4 K_top stretch.
    /// Supports a 12-byte nonce.
    /// </summary>
    /// <returns>The initial <c>Offset_0</c> value derived from the nonce per RFC 7253.</returns>
    private byte[] ComputeInitialOffset()
    {
        int blockSize = this._cipher.BlockSize;

        byte[] nonceWord = new byte[blockSize];
        byte[]? ktopInput = null;
        byte[]? ktop = null;
        byte[]? stretch = null;

        try
        {
            nonceWord[0] = (byte)((this._tagLen * 8 % 128) << 1);
            nonceWord[3] = 0x01;
            this._nonce.CopyTo(nonceWord, 4);

            int bottom = nonceWord[blockSize - 1] & 0x3F;

            ktopInput = (byte[])nonceWord.Clone();
            ktopInput[blockSize - 1] &= 0xC0;

            ktop = new byte[blockSize];
            this._cipher.Encrypt(ktopInput, ktop);

            stretch = new byte[blockSize + blockSize / 2];
            ktop.CopyTo(stretch, 0);

            for (int i = 0; i < blockSize / 2; i++)
                stretch[blockSize + i] = (byte)(ktop[i] ^ ktop[i + 1]);

            byte[] offset = new byte[blockSize];

            int byteOffset = bottom / 8;
            int bitOffset = bottom % 8;

            if (bitOffset == 0)
            {
                stretch.AsSpan(byteOffset, blockSize).CopyTo(offset);
            }
            else
            {
                for (int i = 0; i < blockSize; i++)
                {
                    offset[i] = (byte)(
                        (stretch[byteOffset + i] << bitOffset) |
                        (stretch[byteOffset + i + 1] >> (8 - bitOffset)));
                }
            }

            return offset;
        }
        finally
        {
            ClearIfNotNull(stretch);
            ClearIfNotNull(ktop);
            ClearIfNotNull(ktopInput);
            CryptographicOperations.ZeroMemory(nonceWord);
        }
    }

    /// <summary>
    /// Computes HASH(K, A), the OCB3 authentication of associated data per RFC 7253.
    /// </summary>
    /// <param name="aad">The associated authenticated data.</param>
    /// <returns>The HASH value of <paramref name="aad" /> per RFC 7253.</returns>
    private byte[] ComputeHash(ReadOnlySpan<byte> aad)
    {
        int blockSize = this._cipher.BlockSize;

        byte[] sum = new byte[blockSize];

        if (aad.Length == 0)
            return sum;

        byte[] offsetHash = new byte[blockSize];
        byte[] block = new byte[blockSize];

        try
        {
            int m = (aad.Length + blockSize - 1) / blockSize;

            for (int blockIdx = 1; blockIdx <= m; blockIdx++)
            {
                int src = (blockIdx - 1) * blockSize;
                int blockLen = Math.Min(blockSize, aad.Length - src);
                bool full = blockLen == blockSize;

                if (full)
                {
                    Xor(offsetHash, this._lArray[Ntz(blockIdx)], offsetHash);

                    aad.Slice(src, blockSize).CopyTo(block);
                    Xor(block, offsetHash, block);
                    this._cipher.Encrypt(block, block);
                    Xor(sum, block, sum);
                }
                else
                {
                    byte[] padBlock = new byte[blockSize];

                    try
                    {
                        Xor(offsetHash, this._lStar, offsetHash);

                        aad.Slice(src, blockLen).CopyTo(padBlock);
                        padBlock[blockLen] = 0x80;

                        Xor(padBlock, offsetHash, padBlock);

                        this._cipher.Encrypt(padBlock, padBlock);
                        Xor(sum, padBlock, sum);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(padBlock);
                    }
                }
            }

            return sum;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(sum);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(block);
            CryptographicOperations.ZeroMemory(offsetHash);
        }
    }

    /// <summary>
    /// Multiplies <paramref name="x" /> by α in GF(2^128) with big-endian bit order and
    /// polynomial x^128 + x^7 + x^2 + x + 1.
    /// </summary>
    /// <param name="x">The 16-byte input block.</param>
    /// <returns>The GF(2<sup>128</sup>) doubling of <paramref name="x" />.</returns>
    private static byte[] GfDouble(byte[] x)
    {
        byte[] result = new byte[x.Length];
        bool msb = (x[0] & 0x80) != 0;

        for (int i = 0; i < x.Length - 1; i++)
            result[i] = (byte)((x[i] << 1) | (x[i + 1] >> 7));

        result[x.Length - 1] = (byte)(x[x.Length - 1] << 1);

        if (msb)
            result[x.Length - 1] ^= 0x87;

        return result;
    }

    /// <summary>
    /// Writes the byte-wise XOR of <paramref name="a" /> and <paramref name="b" /> into <paramref name="result" />.
    /// </summary>
    /// <param name="a">The first operand span.</param>
    /// <param name="b">The second operand span.</param>
    /// <param name="result">The destination span.</param>
    private static void Xor(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> result)
    {
        for (int i = 0; i < result.Length; i++)
            result[i] = (byte)(a[i] ^ b[i]);
    }

    /// <summary>
    /// Returns the number of trailing zero bits of <paramref name="n" />.
    /// </summary>
    /// <param name="n">A positive block index.</param>
    /// <returns>The number of trailing zero bits in <paramref name="n" />.</returns>
    private static int Ntz(int n)
    {
        if (n == 0)
            return 32;

        int count = 0;

        while ((n & 1) == 0)
        {
            n >>= 1;
            count++;
        }

        return count;
    }

    /// <summary>
    /// Clears <paramref name="value" /> when it is not <see langword="null" />.
    /// </summary>
    /// <param name="value">The byte array to clear.</param>
    private static void ClearIfNotNull(byte[]? value)
    {
        if (value is not null)
            CryptographicOperations.ZeroMemory(value);
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException" /> if this instance has been disposed.
    /// </summary>
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(this._disposed, this);
}
