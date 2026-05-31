// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransform.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies Offset CodeBook mode version 3 (OCB3) to an underlying <see cref="IBlockCipher" />, providing single-pass
/// authenticated encryption with associated data per RFC 7253.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/aead-mode.svg" alt="Generic AEAD data flow — OCB3 realizes both the keystream and the MAC pipelines as a single offset-driven pass over each block."/>
/// </para>
/// <para>
/// OCB3 collapses the two pipelines of the generic AEAD shape above into a <em>single pass</em>: the keystream and the
/// MAC chain share the same per-block offset Δ<sub>i</sub>, so each block is touched by the cipher exactly once. In the
/// diagram, this corresponds to merging the top and bottom arrows that reach the MAC — the ciphertext output is
/// simultaneously the next input to the authentication accumulator.
/// </para>
/// <para>
/// The nonce is derived from the first 12 bytes of the IV supplied to the constructor. The tag size defaults to 128
/// bits (16 bytes / TAGLEN = 128) and may be set to any positive multiple of 8 bits between 8 and the cipher block size
/// via the <c>tagSize</c> constructor parameter. Supported RFC 7253 values are 64, 96, and 128 bits (8, 12, and 16
/// bytes).
/// </para>
/// <para>
/// Offset initialization uses the RFC 7253 §2.4 K_top stretch: <code>
///<![CDATA[
///   Nonce  = num2str(TAGLEN mod 128, 7) || zeros(120-bitlen(N)) || 1 || N
///   bottom = str2num(Nonce[123..128])
///   K_top  = ENCIPHER(K, Nonce[1..122] || zeros(6))
///   Stretch = K_top || (K_top[1..64] XOR K_top[9..72])   -- adjacent-byte XOR
///   Offset_0 = Stretch[1+bottom..128+bottom]
///]]>
/// </code>
/// </para>
/// <para>
/// The L array uses GF(2^128) doubling with polynomial x^128 + x^7 + x^2 + x + 1 (big-endian).
/// </para>
/// <para>
/// <strong>When to use OCB3.</strong> Pick OCB3 when you want a single-pass AEAD mode without GCM's
/// catastrophic-on-nonce-reuse profile — OCB still requires nonces to be unique per key, but the failure mode is
/// graceful (only that one message's confidentiality is lost; the GHASH-key-leak amplification does not apply). OCB
/// historically had patent encumbrances that limited adoption; the patents have since been placed into the public
/// domain, but <see cref="GcmModeTransform" /> remains the more widely deployed choice in practice. For nonce-misuse
/// resistance prefer <see cref="GcmSivModeTransform" /> or <see cref="SivModeTransform" />; for constrained
/// environments prefer <see cref="CcmModeTransform" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using IBlockCipher cipher = new AesBlockCipher(key);
/// byte[] iv = BuildOcbIv(nonce); // first 12 bytes of the IV are the nonce
/// using IAeadBlockCipherModeTransform ocb = new OcbModeTransform(cipher, iv, tagSize: 128);
///
/// byte[] sealed_ = ocb.Encrypt(plaintext, associatedData: header);
///]]>
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/aead-modes.html#ocb3--single-pass-rfc-7253">OCB3 walk-through in the
/// AEAD-modes guide</seealso> <seealso cref="AesBlockCipher"/>
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions"/>
public sealed class OcbModeTransform
    : IAeadBlockCipherModeTransform, IDisposable
{
    /// <summary>
    /// Length of the OCB cipher block is 128 bits (16 bytes). Byte length derived inline via
    /// <see cref="BlockSizeBits" /> / 8.
    /// </summary>
    private const int BlockSizeBits = 128;

    /// <summary>
    /// Length of the OCB nonce is 96 bits (12 bytes). Byte length derived inline via <see cref="NonceSizeBits" /> / 8.
    /// </summary>
    private const int NonceSizeBits = 96;

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
    /// <param name="cipher">The block cipher. Must have a 128-bit (16-byte) block size.</param>
    /// <param name="iv">
    /// The initialization vector. The first 12 bytes are used as the OCB3 nonce. Must equal the cipher block size. A
    /// defensive copy is taken.
    /// </param>
    /// <param name="tagSize">
    /// The authentication-tag size, in bits, of the OCB3 tag. Must be a positive multiple of 8 between 8 bits (1 byte)
    /// and the cipher block size. RFC 7253 defines recommended values of 64, 96, and 128 bits (8, 12, and 16 bytes).
    /// Defaults to 128 bits (16 bytes).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="cipher" /> does not have a 128-bit (16-byte) block size, <paramref name="iv" /> length does not
    /// equal the cipher block size, or <paramref name="tagSize" /> is outside the range [8 bits, cipher block size] or
    /// is not a positive multiple of 8.
    /// </exception>
    public OcbModeTransform(IBlockCipher cipher, byte[] iv, int tagSize = 128)
    {
        if (cipher is null) throw new ArgumentNullException(nameof(cipher));
        CryptoHelpers.ThrowIfIvLengthInvalid(iv, cipher.BlockSize);

        if (cipher.BlockSize != BlockSizeBits)
        {
            throw new ArgumentException(
                $"OCB requires a block cipher with a {BlockSizeBits / 8}-byte block size.",
                nameof(cipher));
        }

        if (tagSize < 8 || tagSize > cipher.BlockSize || tagSize % 8 != 0)
        {
            throw new ArgumentException(
                $"Tag size ({tagSize} bits) must be a positive multiple of 8 between 8 and the cipher block size ({cipher.BlockSize} bits).",
                nameof(tagSize));
        }

        _cipher = cipher;

        _nonce = new byte[NonceSizeBits / 8];
        iv.AsSpan(0, NonceSizeBits / 8).CopyTo(_nonce);

        _tagLen = tagSize / 8;

        var blockSize = cipher.BlockSize / 8;

        // RFC 7253 §2.1 — Key-dependent constants derived once per key.
        // L_* = ENCIPHER(K, zeros(128)).
        var zeroBlock = new byte[blockSize];
        _lStar = new byte[blockSize];

        try
        {
            cipher.Encrypt(zeroBlock, _lStar);
        }
        finally
        {
            CryptoHelpers.Clear(zeroBlock);
        }

        // L_$ = double(L_*).
        _lDollar = GfDouble(_lStar);

        // L[i] = double(L[i-1]), L[0] = double(L_$).
        _lArray = new byte[MaxLValues][];
        _lArray[0] = GfDouble(_lDollar);

        for (var i = 1; i < MaxLValues; i++)
            _lArray[i] = GfDouble(_lArray[i - 1]);
    }

    /// <inheritdoc />
    /// <value>The configured OCB authentication-tag size, in bits. Defaults to 128 bits (16 bytes).</value>
    public int TagSize => _tagLen * 8;

    /// <inheritdoc />
    public void ProcessAssociatedData(ReadOnlySpan<byte> associatedData)
    {
        ThrowIfDisposed();

        CryptoHelpers.ThrowIfAssociatedDataAlreadyProcessed(_aadProcessed);

        _aad = associatedData.ToArray();
        _aadProcessed = true;
    }

    /// <inheritdoc />
    public int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();

        var required = plaintext.Length + _tagLen;
        CryptoHelpers.ThrowIfOutputBufferTooSmall(output, required);

        EnsureAadProcessed();

        var blockSize = _cipher.BlockSize / 8;

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

            var m = (plaintext.Length + blockSize - 1) / blockSize;

            for (var blockIdx = 1; blockIdx <= m - 1; blockIdx++)
            {
                var src = (blockIdx - 1) * blockSize;

                Xor(offset, _lArray[Ntz(blockIdx)], offset);

                plaintext.Slice(src, blockSize).CopyTo(block);
                Xor(block, offset, block);
                _cipher.Encrypt(block, block);
                Xor(block, offset, block);
                block.CopyTo(output[src..]);

                Xor(checksum, plaintext.Slice(src, blockSize), checksum);
            }

            if (plaintext.Length > 0)
            {
                var lastSrc = (m - 1) * blockSize;
                var lastLen = plaintext.Length - lastSrc;

                if (lastLen == blockSize)
                {
                    Xor(offset, _lArray[Ntz(m)], offset);

                    plaintext.Slice(lastSrc, blockSize).CopyTo(block);
                    Xor(block, offset, block);
                    _cipher.Encrypt(block, block);
                    Xor(block, offset, block);
                    block.CopyTo(output[lastSrc..]);

                    Xor(checksum, plaintext.Slice(lastSrc, blockSize), checksum);
                }
                else
                {
                    var pad = new byte[blockSize];
                    var padBlock = new byte[blockSize];

                    try
                    {
                        Xor(offset, _lStar, offset);

                        _cipher.Encrypt(offset, pad);

                        for (var i = 0; i < lastLen; i++)
                            output[lastSrc + i] = (byte)(plaintext[lastSrc + i] ^ pad[i]);

                        plaintext.Slice(lastSrc, lastLen).CopyTo(padBlock);
                        padBlock[lastLen] = 0x80;

                        Xor(checksum, padBlock, checksum);
                    }
                    finally
                    {
                        CryptoHelpers.Clear(padBlock);
                        CryptoHelpers.Clear(pad);
                    }
                }
            }

            tagInput = new byte[blockSize];

            Xor(_lDollar, offset, tagInput);
            Xor(tagInput, checksum, tagInput);
            _cipher.Encrypt(tagInput, tagInput);

            hashResult = ComputeHash(_aad!);
            Xor(tagInput, hashResult, tagInput);

            tagInput.AsSpan(0, _tagLen).CopyTo(output[plaintext.Length..]);

            return required;
        }
        finally
        {
            CryptoHelpers.Clear(hashResult);
            CryptoHelpers.Clear(tagInput);
            CryptoHelpers.Clear(block);
            CryptoHelpers.Clear(checksum);
            CryptoHelpers.Clear(offset);
            _completed = true;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <strong>Authentication pattern: verify-before-release.</strong> The OCB3 tag is recomputed from the offsets,
    /// checksum, and AAD hash, then compared in constant time before the per-block decryption is applied to
    /// <paramref name="output" />; no plaintext byte is ever written when authentication fails. See
    /// <see cref="IAeadBlockCipherModeTransform.Decrypt" /> for the library-wide failure contract.
    /// </remarks>
    public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();

        CryptoHelpers.ThrowIfCiphertextTooShort(ciphertextWithTag, _tagLen);

        var plaintextLength = ciphertextWithTag.Length - _tagLen;
        CryptoHelpers.ThrowIfOutputBufferTooSmall(output, plaintextLength);

        EnsureAadProcessed();

        ReadOnlySpan<byte> ciphertext = ciphertextWithTag[..plaintextLength];
        ReadOnlySpan<byte> receivedTag = ciphertextWithTag[plaintextLength..];

        var blockSize = _cipher.BlockSize / 8;

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

            var m = (plaintextLength + blockSize - 1) / blockSize;

            for (var blockIdx = 1; blockIdx <= m - 1; blockIdx++)
            {
                var src = (blockIdx - 1) * blockSize;

                Xor(offset, _lArray[Ntz(blockIdx)], offset);

                ciphertext.Slice(src, blockSize).CopyTo(block);
                Xor(block, offset, block);
                _cipher.Decrypt(block, block);
                Xor(block, offset, block);
                block.CopyTo(output[src..]);

                Xor(checksum, output.Slice(src, blockSize), checksum);
            }

            if (plaintextLength > 0)
            {
                var lastSrc = (m - 1) * blockSize;
                var lastLen = plaintextLength - lastSrc;

                if (lastLen == blockSize)
                {
                    Xor(offset, _lArray[Ntz(m)], offset);

                    ciphertext.Slice(lastSrc, blockSize).CopyTo(block);
                    Xor(block, offset, block);
                    _cipher.Decrypt(block, block);
                    Xor(block, offset, block);
                    block.CopyTo(output[lastSrc..]);

                    Xor(checksum, output.Slice(lastSrc, blockSize), checksum);
                }
                else
                {
                    var pad = new byte[blockSize];
                    var padBlock = new byte[blockSize];

                    try
                    {
                        Xor(offset, _lStar, offset);

                        _cipher.Encrypt(offset, pad);

                        for (var i = 0; i < lastLen; i++)
                            output[lastSrc + i] = (byte)(ciphertext[lastSrc + i] ^ pad[i]);

                        output.Slice(lastSrc, lastLen).CopyTo(padBlock);
                        padBlock[lastLen] = 0x80;

                        Xor(checksum, padBlock, checksum);
                    }
                    finally
                    {
                        CryptoHelpers.Clear(padBlock);
                        CryptoHelpers.Clear(pad);
                    }
                }
            }

            tagInput = new byte[blockSize];

            Xor(_lDollar, offset, tagInput);
            Xor(tagInput, checksum, tagInput);
            _cipher.Encrypt(tagInput, tagInput);

            hashResult = ComputeHash(_aad!);
            Xor(tagInput, hashResult, tagInput);

            if (!CryptographicOperations.FixedTimeEquals(tagInput.AsSpan(0, _tagLen), receivedTag))
            {
                CryptoHelpers.Clear(output[..plaintextLength]);
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AuthenticationTagMismatch);
            }

            return plaintextLength;
        }
        finally
        {
            CryptoHelpers.Clear(hashResult);
            CryptoHelpers.Clear(tagInput);
            CryptoHelpers.Clear(block);
            CryptoHelpers.Clear(checksum);
            CryptoHelpers.Clear(offset);
            _completed = true;
        }
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if this transform has already encrypted or decrypted a message.
    /// OCB transforms are single-use; create a fresh instance per message.
    /// </summary>
    private void ThrowIfCompleted() =>
        CryptoHelpers.ThrowIfAlreadyCompleted(_completed);

    /// <summary>
    /// Releases the resources used by this instance and clears retained nonce, OCB offset constants, and
    /// associated-data state from memory.
    /// </summary>
    /// <remarks>
    /// The supplied <see cref="IBlockCipher" /> is not disposed by this type. Ownership remains with the caller.
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
            CryptoHelpers.Clear(_nonce);
            CryptoHelpers.Clear(_lStar);
            CryptoHelpers.Clear(_lDollar);

            foreach (var value in _lArray)
                CryptoHelpers.Clear(value);

            CryptoHelpers.ClearAndNullify(ref _aad);

            _aadProcessed = false;
        }

        _disposed = true;
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the associated-data authentication contribution has been initialized before payload processing.
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
    /// Computes the initial offset Offset_0 using the RFC 7253 §2.4 K_top stretch. Supports a 12-byte nonce.
    /// </summary>
    /// <returns>The initial <c>Offset_0</c> value derived from the nonce per RFC 7253.</returns>
    private byte[] ComputeInitialOffset()
    {
        var blockSize = _cipher.BlockSize / 8;

        var nonceWord = new byte[blockSize];
        byte[]? ktopInput = null;
        byte[]? ktop = null;
        byte[]? stretch = null;

        try
        {
            nonceWord[0] = (byte)(((_tagLen * 8) % 128) << 1);
            nonceWord[3] = 0x01;
            _nonce.CopyTo(nonceWord, 4);

            var bottom = nonceWord[blockSize - 1] & 0x3F;

            ktopInput = (byte[])nonceWord.Clone();
            ktopInput[blockSize - 1] &= 0xC0;

            ktop = new byte[blockSize];
            _cipher.Encrypt(ktopInput, ktop);

            stretch = new byte[blockSize + (blockSize / 2)];
            ktop.CopyTo(stretch, 0);

            for (var i = 0; i < blockSize / 2; i++)
                stretch[blockSize + i] = (byte)(ktop[i] ^ ktop[i + 1]);

            var offset = new byte[blockSize];

            var byteOffset = bottom / 8;
            var bitOffset = bottom % 8;

            if (bitOffset == 0)
            {
                stretch.AsSpan(byteOffset, blockSize).CopyTo(offset);
            }
            else
            {
                for (var i = 0; i < blockSize; i++)
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
            CryptoHelpers.Clear(stretch);
            CryptoHelpers.Clear(ktop);
            CryptoHelpers.Clear(ktopInput);
            CryptoHelpers.Clear(nonceWord);
        }
    }

    /// <summary>
    /// Computes HASH(K, A), the OCB3 authentication of associated data per RFC 7253.
    /// </summary>
    /// <param name="aad">The associated authenticated data.</param>
    /// <returns>The HASH value of <paramref name="aad" /> per RFC 7253.</returns>
    private byte[] ComputeHash(ReadOnlySpan<byte> aad)
    {
        var blockSize = _cipher.BlockSize / 8;

        var sum = new byte[blockSize];

        if (aad.Length == 0)
            return sum;

        var offsetHash = new byte[blockSize];
        var block = new byte[blockSize];

        try
        {
            var m = (aad.Length + blockSize - 1) / blockSize;

            for (var blockIdx = 1; blockIdx <= m; blockIdx++)
            {
                var src = (blockIdx - 1) * blockSize;
                var blockLen = Math.Min(blockSize, aad.Length - src);
                var full = blockLen == blockSize;

                if (full)
                {
                    Xor(offsetHash, _lArray[Ntz(blockIdx)], offsetHash);

                    aad.Slice(src, blockSize).CopyTo(block);
                    Xor(block, offsetHash, block);
                    _cipher.Encrypt(block, block);
                    Xor(sum, block, sum);
                }
                else
                {
                    var padBlock = new byte[blockSize];

                    try
                    {
                        Xor(offsetHash, _lStar, offsetHash);

                        aad.Slice(src, blockLen).CopyTo(padBlock);
                        padBlock[blockLen] = 0x80;

                        Xor(padBlock, offsetHash, padBlock);

                        _cipher.Encrypt(padBlock, padBlock);
                        Xor(sum, padBlock, sum);
                    }
                    finally
                    {
                        CryptoHelpers.Clear(padBlock);
                    }
                }
            }

            return sum;
        }
        catch
        {
            CryptoHelpers.Clear(sum);
            throw;
        }
        finally
        {
            CryptoHelpers.Clear(block);
            CryptoHelpers.Clear(offsetHash);
        }
    }

    /// <summary>
    /// Multiplies <paramref name="x" /> by α in GF(2^128) with big-endian bit order and polynomial x^128 + x^7 + x^2 +
    /// x + 1.
    /// </summary>
    /// <param name="x">The 16-byte input block.</param>
    /// <returns>The GF(2<sup>128</sup>) doubling of <paramref name="x" />.</returns>
    private static byte[] GfDouble(byte[] x)
    {
        var result = new byte[x.Length];
        var msb = (x[0] & 0x80) != 0;

        for (var i = 0; i < x.Length - 1; i++)
            result[i] = (byte)((x[i] << 1) | (x[i + 1] >> 7));

        result[x.Length - 1] = (byte)(x[^1] << 1);

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
        for (var i = 0; i < result.Length; i++)
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

        var count = 0;

        while ((n & 1) == 0)
        {
            n >>= 1;
            count++;
        }

        return count;
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
#endif

}
