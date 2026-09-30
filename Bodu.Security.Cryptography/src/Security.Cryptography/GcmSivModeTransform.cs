// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmSivModeTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies GCM-SIV mode to an underlying <see cref="IBlockCipher" />, providing nonce-misuse resistant authenticated
/// encryption per RFC 8452.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/aead-mode.svg" alt="Generic AEAD data flow - GCM-SIV runs a POLYVAL-based MAC over nonce, associated data, and plaintext to derive a synthetic tag, then uses that tag as the CTR initial counter."/>
/// </para>
/// <para>
/// GCM-SIV shares SIV's misuse-resistant ordering - the MAC pipeline runs before the keystream pipeline so that the tag
/// doubles as the CTR counter base - but swaps GHASH for <b>POLYVAL</b>, which is GHASH composed with a byte/bit
/// reflection that makes little-endian processing efficient on modern processors.
/// </para>
/// <para>
/// GCM-SIV derives per-message authentication and encryption keys from the master key and a 12-byte nonce by encrypting
/// blocks that carry little-endian counters (RFC 8452 Section 4). The encryption key is as long as the master key, so a
/// 128-bit master key takes four blocks and a 256-bit one six, encrypted in one multi-block call:
/// <code>
///<![CDATA[
/// K_auth = E_K(LE32(0) || nonce)[0..7] || E_K(LE32(1) || nonce)[0..7]                      (16 bytes)
/// K_enc  = E_K(LE32(2) || nonce)[0..7] || E_K(LE32(3) || nonce)[0..7]                      (16 bytes, 128-bit K)
/// K_enc  = E_K(LE32(2) || nonce)[0..7] || ... || E_K(LE32(5) || nonce)[0..7]               (32 bytes, 256-bit K)
///]]>
/// </code>
/// </para>
/// <para>
/// POLYVAL runs on the library's GHASH kernels through RFC 8452 Appendix A's isomorphism with GHASH, folding four
/// blocks into each reduction on processors with a carry-less multiply (<c>PCLMULQDQ</c> on x64, <c>PMULL</c> on ARM64)
/// and using a constant-time scalar multiply elsewhere. The associated data is hashed when it is supplied, so only the
/// 16-byte POLYVAL state is kept until the message is processed, and the keystream is produced a 4 KiB run of counter
/// blocks at a time.
/// </para>
/// <para>
/// Because GCM-SIV must create a fresh cipher instance keyed with the derived <c>K_enc</c>, a
/// <see cref="Func{T,TResult}" /> cipher factory is required in the constructor alongside the master cipher. The
/// factory is called once per transform instance.
/// </para>
/// <para>
/// Ciphertext is output as <c>C || Tag</c> (16-byte tag appended), consistent with the
/// <see cref="IAeadBlockCipherModeTransform" /> convention.
/// </para>
/// <para>
/// <strong>When to use GCM-SIV.</strong> The right modern AEAD pick when nonce uniqueness cannot be guaranteed -
/// distributed systems where a coordinator might re-issue the same nonce after a crash, key wrapping, deduplication, or
/// any context where a fresh nonce per message is impractical. Under nonce reuse, GCM-SIV's only leak is that two
/// identical <c>(plaintext, AAD)</c> pairs encrypt to identical ciphertexts - confidentiality and authenticity for
/// distinct messages remain intact. Throughput is lower than <see cref="GcmModeTransform" /> because of the two-pass
/// MAC-then-encrypt structure; for nonce-disciplined high-throughput contexts prefer GCM.
/// <see cref="SivModeTransform" /> is the AES-SIV (RFC 5297) sibling - also misuse-resistant but with a different MAC
/// (S2V) and key schedule.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using IBlockCipher master = new AesBlockCipher(masterKey);
/// byte[] iv = BuildSivIv(nonce); // 12-byte nonce padded to the cipher block size
/// using IAeadBlockCipherModeTransform sivlike = new GcmSivModeTransform(
///     masterCipher: master,
///     cipherFactory: derivedKey => new AesBlockCipher(derivedKey),
///     iv: iv);
/// byte[] sealed_ = sivlike.Encrypt(plaintext, associatedData: header);
///]]>
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/aead-modes.html#gcm-siv---the-modern-replacement-for-gcm">GCM-SIV walk-through
/// in the AEAD-modes guide</seealso> <seealso cref="AesBlockCipher"/>
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions"/>
public sealed class GcmSivModeTransform
    : IAeadBlockCipherModeTransform, IDisposable
{
    /// <summary>Length of the AES-GCM-SIV nonce is 96 bits (12 bytes). Byte length derived inline via <see cref="NonceSizeBits" /> / 8.</summary>
    private const int NonceSizeBits = 96;

    /// <summary>Length of the AES-GCM-SIV authentication tag is 128 bits (16 bytes). Byte length derived inline via <see cref="TagSizeBits" /> / 8.</summary>
    private const int TagSizeBits = 128;

    /// <summary>The block size, in bytes, of the cipher, of POLYVAL, and of a counter block.</summary>
    private const int BlockBytes = 16;

    /// <summary>The block cipher keyed with the derived encryption key <c>K_enc</c>.</summary>
    private readonly IBlockCipher _encCipher;

    /// <summary>The derived authentication key <c>K_auth</c>, prepared as the POLYVAL key for the process's GHASH kernel; reset to <see langword="default" /> on disposal.</summary>
    private Ghash.Key _polyvalKey;

    /// <summary>The 12-byte nonce in the first twelve bytes of a block whose last four bytes are zero; reset to <see langword="default" /> on disposal.</summary>
    private Vector128<byte> _nonceBlock;

    /// <summary>The POLYVAL state after the associated data, which POLYVAL takes first; reset to <see langword="default" /> on disposal.</summary>
    private Vector128<byte> _aadHash;

    /// <summary>The length, in bytes, of the associated data folded into <see cref="_aadHash" />.</summary>
    private int _aadLength;

    /// <summary>Indicates whether associated data has been processed for this instance.</summary>
    private bool _aadProcessed;

    /// <summary>Indicates whether encryption or decryption has completed for this single-use instance.</summary>
    private bool _completed;

    /// <summary>Indicates whether this instance has been disposed and its key material and nonce cleared.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GcmSivModeTransform" /> class, reading the key-generating key's
    /// size from <paramref name="masterCipher" />.
    /// </summary>
    /// <param name="masterCipher">
    /// The block cipher keyed with the master key. Used for per-message key derivation. Must have a 16-byte block size.
    /// </param>
    /// <param name="cipherFactory">
    /// A factory that creates a fresh <see cref="IBlockCipher" /> instance keyed with the supplied byte array. Called
    /// once to produce the per-message encryption cipher.
    /// </param>
    /// <param name="iv">
    /// The initialization vector. The first 12 bytes are used as the GCM-SIV nonce. Must equal the master cipher block
    /// size. A defensive copy is taken.
    /// </param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="iv" /> length does not equal the cipher block size.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="cipherFactory" /> returned <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// An <see cref="AesBlockCipher" /> created with a 256-bit key derives a 256-bit message-encryption key, as RFC
    /// 8452 specifies for AEAD_AES_256_GCM_SIV. Every other master cipher derives a 128-bit one, including AES-192,
    /// which RFC 8452 does not define. To use a 256-bit key-generating key through another <see cref="IBlockCipher" />,
    /// call the overload that takes the key size.
    /// </remarks>
    public GcmSivModeTransform(IBlockCipher masterCipher, Func<byte[], IBlockCipher> cipherFactory, byte[] iv)
        : this(masterCipher, cipherFactory, iv, masterCipher is AesBlockCipher { KeySize: 256 } ? 256 : 128)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GcmSivModeTransform" /> class for a key-generating key of the
    /// specified size.
    /// </summary>
    /// <param name="masterCipher">
    /// The block cipher keyed with the master key. Used for per-message key derivation. Must have a 16-byte block size.
    /// </param>
    /// <param name="cipherFactory">
    /// A factory that creates a fresh <see cref="IBlockCipher" /> instance keyed with the supplied byte array. Called
    /// once to produce the per-message encryption cipher.
    /// </param>
    /// <param name="iv">
    /// The initialization vector. The first 12 bytes are used as the GCM-SIV nonce. Must equal the master cipher block
    /// size. A defensive copy is taken.
    /// </param>
    /// <param name="keySize">
    /// The size, in bits, of the key-generating key <paramref name="masterCipher" /> is keyed with.
    /// </param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="iv" /> length does not equal the cipher block size.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="keySize" /> is neither 128 nor 256.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="cipherFactory" /> returned <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// RFC 8452 defines GCM-SIV for 128- and 256-bit key-generating keys, and derives a message-encryption key of the
    /// same size: four cipher blocks for 128 bits, six for 256. That key is what <paramref name="cipherFactory" />
    /// receives.
    /// </remarks>
    public GcmSivModeTransform(IBlockCipher masterCipher, Func<byte[], IBlockCipher> cipherFactory, byte[] iv, int keySize)
    {
        ThrowHelper.ThrowIfNull(masterCipher);
        ThrowHelper.ThrowIfNull(cipherFactory);
        CryptographyThrowHelper.ThrowIfIvLengthInvalid(iv, masterCipher.BlockSize);
        if (keySize != 128 && keySize != 256) throw new ArgumentOutOfRangeException(nameof(keySize), keySize, CryptoResourceStrings.Arg_OutOfRange_GcmSivKeySize);

        ReadOnlySpan<byte> nonce = iv.AsSpan(0, NonceSizeBits / 8);
        Span<byte> nonceBlock = stackalloc byte[BlockBytes];
        nonceBlock.Clear();
        nonce.CopyTo(nonceBlock);
        _nonceBlock = Vector128.Create((ReadOnlySpan<byte>)nonceBlock);

        // Derive K_auth and K_enc per RFC 8452 Section 4. K_enc is handed to the factory, which takes an array.
        Span<byte> authKey = stackalloc byte[BlockBytes];
        byte[] encKey = new byte[keySize / 8];

        try
        {
            DeriveKeys(masterCipher, nonce, authKey, encKey);
            _polyvalKey = Ghash.Key.ForPolyval(authKey, Ghash.SelectKernel());
            _encCipher = cipherFactory(encKey)
                ?? throw new InvalidOperationException(
                    CryptoResourceStrings.Op_Invalid_CipherFactoryReturnedNull);
        }
        catch
        {
            _polyvalKey = default;
            _nonceBlock = default;
            throw;
        }
        finally
        {
            CryptographyHelper.Clear(authKey);
            CryptographyHelper.Clear(encKey);
        }
    }

    /// <inheritdoc />
    /// <value>Length of the AES-GCM-SIV authentication tag is 128 bits (16 bytes).</value>
    public int TagSize => TagSizeBits;

    /// <inheritdoc />
    /// <remarks>
    /// <strong>Authentication pattern: write-then-clear.</strong> The candidate plaintext is decrypted into
    /// <paramref name="output" /> first because the synthetic IV used by AES-GCM-SIV is derived over the plaintext. The
    /// tag is then compared in constant time; on any failure - an authentication mismatch or an exception from the
    /// underlying cipher mid-transform - the plaintext region of <paramref name="output" /> is zeroed via
    /// <see cref="CryptographyHelper.Clear" /> before the exception propagates - no plaintext is observable to the
    /// caller. See <see cref="IAeadBlockCipherModeTransform.Decrypt" /> for the library-wide failure contract.
    /// </remarks>
    public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();

        CryptographyThrowHelper.ThrowIfCiphertextTooShort(ciphertextWithTag, TagSizeBits / 8);
        int plaintextLength = ciphertextWithTag.Length - (TagSizeBits / 8);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, plaintextLength);
        EnsureAadProcessed();

        Span<byte> receivedTag = stackalloc byte[TagSizeBits / 8];
        Span<byte> expectedTag = stackalloc byte[TagSizeBits / 8];

        try
        {
            // Copy the tag first: it seeds the counter, and the plaintext may be written over the buffer holding it.
            ciphertextWithTag[plaintextLength..].CopyTo(receivedTag);
            ApplyCtr(ciphertextWithTag[..plaintextLength], output[..plaintextLength], receivedTag);

            // Recompute and verify the tag.
            ComputeTag(output[..plaintextLength], expectedTag);
            if (!CryptographicOperations.FixedTimeEquals(expectedTag, receivedTag))
            {
                CryptographyHelper.Clear(output[..plaintextLength]);
                throw new CryptographicException(
                    CryptoResourceStrings.Crypt_Invalid_AuthenticationTagMismatch);
            }

            return plaintextLength;
        }
        catch
        {
            // Zero the plaintext region on any failure - a tag mismatch or a fault from the underlying
            // cipher mid-transform - so the unverified plaintext this write-then-clear mode has already
            // written never leaks.
            CryptographyHelper.Clear(output[..plaintextLength]);
            throw;
        }
        finally
        {
            CryptographyHelper.Clear(expectedTag);
            CryptographyHelper.Clear(receivedTag);
            _completed = true;
        }
    }

    /// <summary>
    /// Releases the resources used by this instance and clears the retained authentication key, nonce, and
    /// associated-data state, and disposes the derived encryption cipher.
    /// </summary>
    /// <remarks>
    /// The supplied master cipher is not disposed by this type. Ownership of the master cipher remains with the caller.
    /// The derived encryption cipher created by the supplied factory is owned by this transform and is disposed here.
    /// </remarks>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();

        int required = plaintext.Length + (TagSizeBits / 8);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, required);
        EnsureAadProcessed();

        Span<byte> tag = stackalloc byte[TagSizeBits / 8];

        try
        {
            // Tag = E(K_enc, POLYVAL(K_auth, AAD, PT) XOR nonce) with bit 127 cleared.
            ComputeTag(plaintext, tag);

            // Encrypt plaintext with CTR(K_enc) seeded from tag.
            ApplyCtr(plaintext, output[..plaintext.Length], tag);
            tag.CopyTo(output[plaintext.Length..]);
            return required;
        }
        finally
        {
            CryptographyHelper.Clear(tag);
            _completed = true;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// The associated data is folded into the POLYVAL state here, since POLYVAL takes it before the plaintext, so the
    /// transform keeps neither the data nor a copy of it.
    /// </remarks>
    [SkipLocalsInit]
    public void ProcessAssociatedData(ReadOnlySpan<byte> associatedData)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfAssociatedDataAlreadyProcessed(_aadProcessed);

        Span<byte> state = stackalloc byte[BlockBytes];
        state.Clear();

        try
        {
            Ghash.Update(in _polyvalKey, state, associatedData);
            _aadHash = Vector128.Create((ReadOnlySpan<byte>)state);
            _aadLength = associatedData.Length;
        }
        finally
        {
            CryptographyHelper.Clear(state);
        }

        _aadProcessed = true;
    }

    /// <summary>
    /// Derives K_auth (16 bytes) and K_enc (16 or 32 bytes, as long as the key-generating key) from the master cipher
    /// and nonce per RFC 8452 Section 4. Block <c>i</c> is <c>LE32(i) || nonce</c>, and each key is the first 8 bytes
    /// of the encryptions of consecutive blocks: blocks 0 and 1 for K_auth, then blocks 2 and 3 - and 4 and 5 for a
    /// 256-bit key-generating key - for K_enc. All the blocks are encrypted in one multi-block call.
    /// </summary>
    /// <param name="cipher">The master block cipher keyed with the key-generating key.</param>
    /// <param name="nonce">The 12-byte nonce.</param>
    /// <param name="authKey">Receives the 16-byte message-authentication key.</param>
    /// <param name="encKey">
    /// Receives the message-encryption key; its length, 16 or 32 bytes, selects the derivation.
    /// </param>
    [SkipLocalsInit]
    private static void DeriveKeys(IBlockCipher cipher, ReadOnlySpan<byte> nonce, Span<byte> authKey, Span<byte> encKey)
    {
        int blocks = 2 + (encKey.Length / 8);
        int length = blocks * BlockBytes;
        Span<byte> input = stackalloc byte[6 * BlockBytes];
        Span<byte> derived = stackalloc byte[6 * BlockBytes];
        input = input[..length];
        derived = derived[..length];

        try
        {
            for (int i = 0; i < blocks; i++)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(input.Slice(i * BlockBytes), (uint)i);
                nonce.CopyTo(input.Slice((i * BlockBytes) + 4));
            }

            cipher.EncryptBlocks(input, derived);

            for (int i = 0; i < blocks; i++)
            {
                Span<byte> half = i < 2 ? authKey.Slice(8 * i, 8) : encKey.Slice(8 * (i - 2), 8);
                derived.Slice(i * BlockBytes, 8).CopyTo(half);
            }
        }
        finally
        {
            CryptographyHelper.Clear(derived);
        }
    }

    /// <summary>
    /// Computes the GCM-SIV tag per RFC 8452 Section 5: POLYVAL over the associated data, the plaintext, and the length
    /// block <c>LE64(|A| · 8) || LE64(|P| · 8)</c>, XORed with the nonce, with bit 127 cleared, then encrypted with
    /// K_enc.
    /// </summary>
    /// <param name="plaintext">The plaintext bytes authenticated by the tag.</param>
    /// <param name="tag">Receives the 16-byte tag; cleared if the cipher throws.</param>
    [SkipLocalsInit]
    private void ComputeTag(ReadOnlySpan<byte> plaintext, Span<byte> tag)
    {
        Span<byte> state = stackalloc byte[BlockBytes];
        Span<byte> lengths = stackalloc byte[BlockBytes];

        try
        {
            _aadHash.CopyTo(state);
            Ghash.Update(in _polyvalKey, state, plaintext);

            BinaryPrimitives.WriteUInt64LittleEndian(lengths, (ulong)_aadLength * 8);
            BinaryPrimitives.WriteUInt64LittleEndian(lengths[8..], (ulong)plaintext.Length * 8);
            Ghash.Update(in _polyvalKey, state, lengths);

            // XOR the nonce into the first twelve bytes and clear the top bit of the last.
            (Vector128.Create((ReadOnlySpan<byte>)state) ^ _nonceBlock).CopyTo(state);
            state[15] &= 0x7F;

            _encCipher.Encrypt(state, tag);
        }
        catch
        {
            // Zero the partially written tag when the derived cipher faults mid-encrypt.
            CryptographyHelper.Clear(tag);
            throw;
        }
        finally
        {
            CryptographyHelper.Clear(state);
        }
    }

    /// <summary>
    /// Applies the CTR keystream seeded from <paramref name="tag" />: the counter block is the tag with its most
    /// significant bit set, and successive blocks increment its first 32 bits as a little-endian integer, modulo
    /// <c>2³²</c>, leaving the other 96 bits unchanged (RFC 8452 Section 4). The counter blocks are encrypted a run at
    /// a time.
    /// </summary>
    /// <param name="input">The plaintext (or ciphertext) bytes.</param>
    /// <param name="output">The destination span; must be at least <paramref name="input" />.Length bytes.</param>
    /// <param name="tag">The 16-byte tag that seeds the counter.</param>
    [SkipLocalsInit]
    private void ApplyCtr(ReadOnlySpan<byte> input, Span<byte> output, ReadOnlySpan<byte> tag)
    {
        Span<byte> counters = stackalloc byte[CounterKeystream.BatchBytes];
        Span<byte> keystream = stackalloc byte[CounterKeystream.BatchBytes];
        int used = 0;

        uint count = BinaryPrimitives.ReadUInt32LittleEndian(tag);
        uint fixedLow = BinaryPrimitives.ReadUInt32LittleEndian(tag[4..]);
        ulong fixedHigh = BinaryPrimitives.ReadUInt64LittleEndian(tag[8..]) | 0x8000_0000_0000_0000UL;

        try
        {
            int offset = 0;
            while (offset < input.Length)
            {
                int length = Math.Min(counters.Length, input.Length - offset);
                int filled = (length + BlockBytes - 1) & ~(BlockBytes - 1);
                for (int position = 0; position < filled; position += BlockBytes)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(counters[position..], count);
                    BinaryPrimitives.WriteUInt32LittleEndian(counters.Slice(position + 4), fixedLow);
                    BinaryPrimitives.WriteUInt64LittleEndian(counters.Slice(position + 8), fixedHigh);
                    count = unchecked(count + 1);
                }

                // Widen the extent to clear before the cipher writes keystream, so a throwing cipher leaves none behind.
                used = Math.Max(used, filled);
                CounterKeystream.Apply(_encCipher, counters[..filled], keystream, input.Slice(offset, length), output.Slice(offset, length));
                offset += length;
            }
        }
        finally
        {
            CryptographyHelper.Clear(keystream[..used]);
        }
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
            if (_encCipher is IDisposable disposableCipher)
                disposableCipher.Dispose();

            _polyvalKey = default;
            _nonceBlock = default;
            _aadHash = default;
            _aadLength = 0;
            _aadProcessed = false;
        }

        _disposed = true;
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks the associated data as processed before the payload is, so a transform given no associated data
    /// authenticates an empty string and rejects a later <see cref="ProcessAssociatedData" /> call.
    /// </summary>
    private void EnsureAadProcessed() =>
        _aadProcessed = true;

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if this transform has already encrypted or decrypted a message.
    /// GCM-SIV transforms are single-use; create a fresh instance per message.
    /// </summary>
    private void ThrowIfCompleted() =>
        CryptographyThrowHelper.ThrowIfAlreadyCompleted(_completed);

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
