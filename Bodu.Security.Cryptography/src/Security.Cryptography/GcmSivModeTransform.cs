// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmSivModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies GCM-SIV mode to an underlying <see cref="IBlockCipher"/>, providing nonce-misuse
/// resistant authenticated encryption per RFC 8452.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/aead-mode.svg" alt="Generic AEAD data flow — GCM-SIV runs a POLYVAL-based MAC over nonce, associated data, and plaintext to derive a synthetic tag, then uses that tag as the CTR initial counter."/>
/// </para>
/// <para>
/// GCM-SIV shares SIV's misuse-resistant ordering — the MAC pipeline runs before the keystream
/// pipeline so that the tag doubles as the CTR counter base — but swaps GHASH for <b>POLYVAL</b>,
/// which is GHASH composed with a byte/bit reflection that makes little-endian processing efficient
/// on modern processors.
/// </para>
/// <para>
/// GCM-SIV derives per-message authentication and encryption keys from the master key and a
/// 12-byte nonce using four cipher calls with little-endian counters (RFC 8452 Section 4):
/// <code>
/// K_auth = E_K(LE32(0) || nonce)[0..7] || E_K(LE32(1) || nonce)[0..7]   (16 bytes)
/// K_enc  = E_K(LE32(2) || nonce)[0..7] || E_K(LE32(3) || nonce)[0..7]   (16 bytes)
/// </code>
/// </para>
/// <para>
/// POLYVAL is computed via the GHASH isomorphism: each field element is reflected (byte-reversed
/// + bit-reversed within each byte) before GHASH multiplication, and the result is reflected back.
/// </para>
/// <para>
/// Because GCM-SIV must create a fresh cipher instance keyed with the derived <c>K_enc</c>,
/// a <see cref="Func{T,TResult}"/> cipher factory is required in the constructor alongside
/// the master cipher. The factory is called once per transform instance.
/// </para>
/// <para>
/// Ciphertext is output as <c>C || Tag</c> (16-byte tag appended), consistent with the
/// <see cref="IAeadBlockCipherModeTransform"/> convention.
/// </para>
/// <para>
/// <strong>When to use GCM-SIV.</strong> The right modern AEAD pick when nonce uniqueness cannot be
/// guaranteed — distributed systems where a coordinator might re-issue the same nonce after a crash, key
/// wrapping, deduplication, or any context where a fresh nonce per message is impractical. Under nonce
/// reuse, GCM-SIV's only leak is that two identical <c>(plaintext, AAD)</c> pairs encrypt to identical
/// ciphertexts — confidentiality and authenticity for distinct messages remain intact. Throughput is lower
/// than <see cref="GcmModeTransform"/> because of the two-pass MAC-then-encrypt structure; for
/// nonce-disciplined high-throughput contexts prefer GCM. <see cref="SivModeTransform"/> is the AES-SIV
/// (RFC 5297) sibling — also misuse-resistant but with a different MAC (S2V) and key schedule.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using IBlockCipher master = new AesBlockCipher(masterKey);
/// byte[] iv = BuildSivIv(nonce); // 12-byte nonce padded to the cipher block size
/// using IAeadBlockCipherModeTransform sivlike = new GcmSivModeTransform(
///     masterCipher: master,
///     cipherFactory: derivedKey =&gt; new AesBlockCipher(derivedKey),
///     iv: iv);
///
/// byte[] sealed_ = sivlike.Encrypt(plaintext, associatedData: header);
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/aead-modes.html#gcm-siv--the-modern-replacement-for-gcm">GCM-SIV walk-through in the AEAD-modes guide</seealso>
/// <seealso cref="AesBlockCipher"/>
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions"/>
public sealed class GcmSivModeTransform
    : IAeadBlockCipherModeTransform, IDisposable
{
    /// <summary>Length of the AES-GCM-SIV authentication tag is 128 bits (16 bytes). Byte length derived inline via <see cref="TagSizeBits"/> / 8.</summary>
    private const int TagSizeBits = 128;

    /// <summary>Length of the AES-GCM-SIV nonce is 96 bits (12 bytes). Byte length derived inline via <see cref="NonceSizeBits"/> / 8.</summary>
    private const int NonceSizeBits = 96;

    private readonly IBlockCipher _encCipher;  // cipher keyed with derived K_enc
    private readonly byte[] _authKey;          // derived K_auth (POLYVAL key)
    private readonly byte[] _nonce;            // 12-byte nonce
    private byte[]? _aad;
    private bool _aadProcessed;
    private bool _completed;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GcmSivModeTransform"/> class.
    /// </summary>
    /// <param name="masterCipher">
    /// The block cipher keyed with the master key. Used for per-message key derivation
    /// (four encrypt calls). Must have a 16-byte block size.
    /// </param>
    /// <param name="cipherFactory">
    /// A factory that creates a fresh <see cref="IBlockCipher"/> instance keyed with the
    /// supplied byte array. Called once to produce the per-message encryption cipher.
    /// </param>
    /// <param name="iv">
    /// The initialization vector. The first 12 bytes are used as the GCM-SIV nonce.
    /// Must equal the master cipher block size. A defensive copy is taken.
    /// </param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="iv"/> length does not equal the cipher block size.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="cipherFactory"/> returned <see langword="null"/>.
    /// </exception>
    public GcmSivModeTransform(IBlockCipher masterCipher, Func<byte[], IBlockCipher> cipherFactory, byte[] iv)
    {
        ThrowHelper.ThrowIfNull(masterCipher);
        ThrowHelper.ThrowIfNull(cipherFactory);
        CryptoHelpers.ThrowIfIvLengthInvalid(iv, masterCipher.BlockSize);

        this._nonce = new byte[NonceSizeBits / 8];
        iv.AsSpan(0, NonceSizeBits / 8).CopyTo(this._nonce);

        // Derive K_auth and K_enc per RFC 8452 Section 4.
        // Each call: E_K(LE32(i) || nonce), take first 8 bytes.
        (var authKey, var encKeyMaterial) = DeriveKeys(masterCipher, this._nonce);

        try
        {
            this._authKey = authKey;
            this._encCipher = cipherFactory(encKeyMaterial)
                ?? throw new InvalidOperationException(
                    CryptoResourceStrings.Op_Invalid_CipherFactoryReturnedNull);
        }
        catch
        {
            CryptoHelpers.Clear(authKey);
            throw;
        }
        finally
        {
            CryptoHelpers.Clear(encKeyMaterial);
        }
    }

    /// <inheritdoc />
    /// <value>Length of the AES-GCM-SIV authentication tag is 128 bits (16 bytes).</value>
    public int TagSize => TagSizeBits;

    /// <inheritdoc />
    public void ProcessAssociatedData(ReadOnlySpan<byte> associatedData)
    {
        this.ThrowIfDisposed();
        CryptoHelpers.ThrowIfAssociatedDataAlreadyProcessed(this._aadProcessed);

        this._aad = associatedData.ToArray();
        this._aadProcessed = true;
    }

    /// <inheritdoc />
    public int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        this.ThrowIfDisposed();
        this.ThrowIfCompleted();

        var required = plaintext.Length + (TagSizeBits / 8);
        CryptoHelpers.ThrowIfOutputBufferTooSmall(output, required);
        EnsureAadProcessed();

        try
        {
            // Tag = E(K_enc, POLYVAL(K_auth, AAD, PT) XOR nonce) with bits [31] and [63] cleared.
            var tag = ComputeTag(this._aad.AsSpan(), plaintext);

            // Encrypt plaintext with CTR(K_enc) seeded from tag.
            var ctrIv = BuildCtrIv(tag);
            CtrEncrypt(plaintext, output[..plaintext.Length], ctrIv);
            tag.CopyTo(output[plaintext.Length..]);
            return required;
        }
        finally
        {
            this._completed = true;
        }
    }

    /// <inheritdoc />
    public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        this.ThrowIfDisposed();
        this.ThrowIfCompleted();

        CryptoHelpers.ThrowIfCiphertextTooShort(ciphertextWithTag, TagSizeBits / 8);
        var plaintextLength = ciphertextWithTag.Length - (TagSizeBits / 8);
        CryptoHelpers.ThrowIfOutputBufferTooSmall(output, plaintextLength);
        EnsureAadProcessed();

        try
        {
            ReadOnlySpan<byte> ciphertext = ciphertextWithTag[..plaintextLength];
            ReadOnlySpan<byte> receivedTag = ciphertextWithTag[plaintextLength..];

            // Decrypt CTR.
            var ctrIv = BuildCtrIv(receivedTag.ToArray());
            CtrEncrypt(ciphertext, output[..plaintextLength], ctrIv);

            // Recompute and verify tag.
            var expectedTag = ComputeTag(this._aad.AsSpan(), output[..plaintextLength]);
            if (!CryptographicOperations.FixedTimeEquals(expectedTag, receivedTag))
            {
                CryptoHelpers.Clear(output[..plaintextLength]);
                throw new CryptographicException(
                    CryptoResourceStrings.Crypt_Invalid_AuthenticationTagMismatch);
            }
            return plaintextLength;
        }
        finally
        {
            this._completed = true;
        }
    }

    /// <summary>
    /// Releases the resources used by this instance and clears retained authentication key, nonce,
    /// associated-data state, and the derived encryption cipher.
    /// </summary>
    /// <remarks>
    /// The supplied master cipher is not disposed by this type. Ownership of the master cipher remains with the caller.
    /// The derived encryption cipher created by the supplied factory is owned by this transform and is disposed here.
    /// </remarks>
    public void Dispose()
    {
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> if this transform has already encrypted or
    /// decrypted a message. GCM-SIV transforms are single-use; create a fresh instance per message.
    /// </summary>
    private void ThrowIfCompleted() =>
        CryptoHelpers.ThrowIfAlreadyCompleted(this._completed);

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release managed resources; <see langword="false"/> to release unmanaged resources only.
    /// </param>
    private void Dispose(bool disposing)
    {
        if (this._disposed)
            return;

        if (disposing)
        {
            if (this._encCipher is IDisposable disposableCipher)
                disposableCipher.Dispose();

            CryptoHelpers.Clear(this._authKey);
            CryptoHelpers.Clear(this._nonce);
            CryptoHelpers.ClearAndNullify(ref this._aad);

            this._aadProcessed = false;
        }

        this._disposed = true;
    }
    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the associated-data (AAD) POLYVAL contribution has been finalized exactly once
    /// before payload bytes are processed; no-op on subsequent invocations.
    /// </summary>
    private void EnsureAadProcessed()
    {
        if (!this._aadProcessed)
        {
            this._aad = [];
            this._aadProcessed = true;
        }
    }

    /// <summary>
    /// Derives K_auth (16 bytes) and K_enc (16 bytes) from the master cipher and nonce
    /// using four cipher calls per RFC 8452 Section 4.
    /// Input block format: LE32(i) || nonce (4 + 12 = 16 bytes). Take first 8 bytes of each output.
    /// </summary>
    private static (byte[] authKey, byte[] encKey) DeriveKeys(IBlockCipher cipher, byte[] nonce)
    {
        var blockSize = cipher.BlockSize / 8;
        var authKey = new byte[blockSize];
        var encKey = new byte[blockSize];

        byte[]? b0 = null;
        byte[]? b1 = null;
        byte[]? b2 = null;
        byte[]? b3 = null;

        try
        {
            // K_auth = first 8 bytes of call(0) || first 8 bytes of call(1).
            b0 = Derive(0);
            b1 = Derive(1);
            b0.AsSpan(0, 8).CopyTo(authKey.AsSpan(0));
            b1.AsSpan(0, 8).CopyTo(authKey.AsSpan(8));

            // K_enc = first 8 bytes of call(2) || first 8 bytes of call(3).
            b2 = Derive(2);
            b3 = Derive(3);
            b2.AsSpan(0, 8).CopyTo(encKey.AsSpan(0));
            b3.AsSpan(0, 8).CopyTo(encKey.AsSpan(8));

            return (authKey, encKey);
        }
        catch
        {
            CryptoHelpers.Clear(authKey);
            CryptoHelpers.Clear(encKey);
            throw;
        }
        finally
        {
            CryptoHelpers.ClearAndNullify(ref b3);
            CryptoHelpers.ClearAndNullify(ref b2);
            CryptoHelpers.ClearAndNullify(ref b1);
            CryptoHelpers.ClearAndNullify(ref b0);
        }

        byte[] Derive(int counter)
        {
            var block = new byte[blockSize];
            var output = new byte[blockSize];

            try
            {
                // Little-endian 32-bit counter in first 4 bytes.
                block[0] = (byte)counter;
                block[1] = (byte)(counter >> 8);
                block[2] = (byte)(counter >> 16);
                block[3] = (byte)(counter >> 24);

                nonce.CopyTo(block, 4);

                cipher.Encrypt(block, output);

                return output;
            }
            finally
            {
                CryptoHelpers.Clear(block);
            }
        }
    }
    /// <summary>
    /// Computes the GCM-SIV tag per RFC 8452 Section 5.2.
    /// POLYVAL(K_auth, len(A)||len(C), A blocks, C blocks) XOR nonce, then clear bit 31 and 63,
    /// then encrypt with K_enc.
    /// </summary>
    /// <param name="aad">The associated authenticated data.</param>
    /// <param name="plaintext">The plaintext bytes authenticated by the tag.</param>
    /// <returns>The computed AES-GCM-SIV authentication tag.</returns>
    private byte[] ComputeTag(ReadOnlySpan<byte> aad, ReadOnlySpan<byte> plaintext)
    {
        var blockSize = this._encCipher.BlockSize / 8;

        // POLYVAL accumulation: process AAD blocks, then plaintext blocks, then length block.
        // polyvalResult holds intermediate MAC state XOR'd with the nonce — cleared in finally.
        var polyvalResult = new byte[blockSize];
        try
        {
            PolyvalUpdate(polyvalResult, aad);
            PolyvalUpdate(polyvalResult, plaintext);

            // Length block: LE64(|A| * 8) || LE64(|P| * 8). Stack-allocated; never reaches the heap.
            Span<byte> lenBlock = stackalloc byte[blockSize];
            var aadBits = (ulong)aad.Length * 8;
            var ptBits = (ulong)plaintext.Length * 8;
            for (var i = 0; i < 8; i++) lenBlock[i] = (byte)(aadBits >> (8 * i));
            for (var i = 0; i < 8; i++) lenBlock[8 + i] = (byte)(ptBits >> (8 * i));
            PolyvalUpdate(polyvalResult, lenBlock);

            // XOR with nonce, clear bit 31 (byte 3 MSB) and bit 63 (byte 7 MSB).
            for (var i = 0; i < (NonceSizeBits / 8); i++)
                polyvalResult[i] ^= this._nonce[i];
            polyvalResult[15] &= 0x7F; // clear bit 127 (RFC calls this bit 31 of the last 32-bit word)

            // Encrypt with K_enc to produce the tag.
            var tag = new byte[blockSize];
            this._encCipher.Encrypt(polyvalResult, tag);
            return tag;
        }
        finally
        {
            CryptoHelpers.Clear(polyvalResult);
        }
    }

    /// <summary>
    /// Accumulates <paramref name="data"/> into the POLYVAL state block-by-block.
    /// POLYVAL(H, X) = reflect(GHASH(reflect(H), reflect(X))).
    /// Internally uses GHASH multiplication on reflected inputs.
    /// </summary>
    /// <param name="state">The POLYVAL accumulator (16 bytes); updated in place.</param>
    /// <param name="data">The input bytes to fold into the POLYVAL state.</param>
    private void PolyvalUpdate(byte[] state, ReadOnlySpan<byte> data)
    {
        const int blockSize = 16;
        for (var offset = 0; offset < data.Length; offset += blockSize)
        {
            // Stack-allocate the per-block scratch buffer so plaintext/AAD fragments never reach the heap.
            Span<byte> block = stackalloc byte[blockSize];
            var len = Math.Min(blockSize, data.Length - offset);
            data.Slice(offset, len).CopyTo(block);
            // state ^= block, then multiply by H (authKey) via POLYVAL.
            Xor(state, block, state);
            PolyvalMultiply(state, this._authKey, state);
        }
    }

    /// <summary>
    /// Computes POLYVAL(H, X) by reflecting both operands, applying GHASH multiplication,
    /// and reflecting the result. reflect(X) = byte-reverse + bit-reverse within each byte.
    /// </summary>
    /// <param name="x">The left operand block (16 bytes).</param>
    /// <param name="h">The POLYVAL hash key (16 bytes).</param>
    /// <param name="result">The destination block (16 bytes); receives the POLYVAL product.</param>
    private static void PolyvalMultiply(Span<byte> x, ReadOnlySpan<byte> h, Span<byte> result)
    {
        // Stack-allocate all three scratch blocks so reflected key material never reaches the managed heap.
        Span<byte> xr = stackalloc byte[16];
        Span<byte> hr = stackalloc byte[16];
        ReflectBytesAndBits(x, xr);
        ReflectBytesAndBits(h, hr);

        Span<byte> product = stackalloc byte[16];
        GhashMultiply(xr, hr, product);

        ReflectBytesAndBits(product, result);
    }

    /// <summary>
    /// Reflects a 128-bit value: reverses byte order and bit-reverses each byte.
    /// </summary>
    /// <param name="input">The source 16-byte block.</param>
    /// <param name="output">The destination 16-byte block; receives <paramref name="input"/> with byte and bit order reversed.</param>
    private static void ReflectBytesAndBits(ReadOnlySpan<byte> input, Span<byte> output)
    {
        for (var i = 0; i < 16; i++)
        {
            var b = input[15 - i];
            // Reverse bits within byte.
            b = (byte)(((b & 0x01) << 7) | ((b & 0x02) << 5) | ((b & 0x04) << 3) | ((b & 0x08) << 1) |
                       ((b & 0x10) >> 1) | ((b & 0x20) >> 3) | ((b & 0x40) >> 5) | ((b & 0x80) >> 7));
            output[i] = b;
        }
    }

    /// <summary>
    /// Multiplies two 128-bit big-endian field elements in GF(2^128) with polynomial
    /// x^128 + x^7 + x^2 + x + 1 (GCM/GHASH field). Shift-and-XOR algorithm.
    /// </summary>
    /// <param name="x">The left operand block (16 bytes).</param>
    /// <param name="h">The hash subkey <c>H</c> (16 bytes).</param>
    /// <param name="result">The destination block (16 bytes); receives the GF(2<sup>128</sup>) product.</param>
    private static void GhashMultiply(ReadOnlySpan<byte> x, ReadOnlySpan<byte> h, Span<byte> result)
    {
        // Stack-allocate both working buffers. v is a mutable copy of the auth subkey H and must
        // not escape to the managed heap — stack allocation ensures it is reclaimed with the frame.
        Span<byte> z = stackalloc byte[16];
        Span<byte> v = stackalloc byte[16];
        h.CopyTo(v);

        for (var i = 0; i < 16; i++)
        {
            var xi = x[i];
            for (var bit = 7; bit >= 0; bit--)
            {
                if (((xi >> bit) & 1) == 1)
                    Xor(z, v, z);

                var lsb = (v[15] & 1) == 1;
                // Right-shift v by 1.
                for (var j = 15; j > 0; j--)
                    v[j] = (byte)((v[j] >> 1) | (v[j - 1] << 7));
                v[0] >>= 1;
                // Reduce: if LSB was set, XOR with 0xE1 in MSByte (x^128 + x^7 + x^2 + x + 1).
                if (lsb) v[0] ^= 0xE1;
            }
        }
        z.CopyTo(result);
    }

    /// <summary>
    /// Builds the GCM-SIV CTR IV from the tag: set MSB of last byte (bit 127) to 1 to
    /// distinguish CTR from POLYVAL blocks per RFC 8452 Section 5.
    /// </summary>
    /// <param name="tag">The POLYVAL-derived authentication tag.</param>
    /// <returns>The CTR initialization vector derived from <paramref name="tag"/> per RFC 8452.</returns>
    private static byte[] BuildCtrIv(byte[] tag)
    {
        var ctrIv = (byte[])tag.Clone();
        ctrIv[15] |= 0x80; // set bit 127
        return ctrIv;
    }

    /// <summary>
    /// Applies AES-CTR encryption using the supplied <paramref name="counter"/> block,
    /// producing <c>input XOR keystream</c> in <paramref name="output"/>.
    /// </summary>
    /// <param name="input">The plaintext (or ciphertext) bytes.</param>
    /// <param name="output">The destination span; must be at least <paramref name="input"/>.Length bytes.</param>
    /// <param name="counter">The initial counter block; the low 32 bits are incremented per
    /// block per RFC 8452.</param>
    private void CtrEncrypt(ReadOnlySpan<byte> input, Span<byte> output, byte[] counter)
    {
        var blockSize = this._encCipher.BlockSize / 8;
        // Stack-allocate the mutable counter copy so the ephemeral CTR state never reaches the heap.
        Span<byte> ctr = stackalloc byte[blockSize];
        counter.CopyTo(ctr);
        Span<byte> ks = stackalloc byte[blockSize];

        for (var offset = 0; offset < input.Length; offset += blockSize)
        {
            this._encCipher.Encrypt(ctr, ks);
            // GCM-SIV CTR increments only the last 32 bits (little-endian), per RFC 8452.
            var lo = (uint)(ctr[12] | (ctr[13] << 8) | (ctr[14] << 16) | (ctr[15] << 24));
            lo++;
            ctr[12] = (byte)lo;
            ctr[13] = (byte)(lo >> 8);
            ctr[14] = (byte)(lo >> 16);
            ctr[15] = (byte)(lo >> 24);
            var len = Math.Min(blockSize, input.Length - offset);
            for (var i = 0; i < len; i++)
                output[offset + i] = (byte)(input[offset + i] ^ ks[i]);
        }
    }

    /// <summary>
    /// Writes the byte-wise XOR of <paramref name="a"/> and <paramref name="b"/> into
    /// <paramref name="result"/>.
    /// </summary>
    /// <param name="a">The first operand span.</param>
    /// <param name="b">The second operand span; must be at least <paramref name="a"/>.Length bytes.</param>
    /// <param name="result">The destination span; must be at least <paramref name="a"/>.Length bytes.</param>
    private static void Xor(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> result)
    {
        for (var i = 0; i < result.Length; i++) result[i] = (byte)(a[i] ^ b[i]);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(this.GetType().Name);
#endif
    }
}
