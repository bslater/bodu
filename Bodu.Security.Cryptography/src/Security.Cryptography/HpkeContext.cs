// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HpkeContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the per-exchange HPKE encryption context produced by the key schedule (RFC 9180 §5.1) and implements the
/// sender and recipient operations of §5.2 (<c>Seal</c>/<c>Open</c>) and §5.3 (<c>Export</c>). A context owns the AEAD
/// key, the base nonce, the running sequence number, and the exporter secret.
/// </summary>
/// <remarks>
/// A context is single-threaded and stateful: each <see cref="Seal" /> or <see cref="Open" /> advances the sequence
/// number that derives the per-message nonce, so the sender's and recipient's calls must remain in lock-step. Key
/// material is zeroed by <see cref="Dispose" />.
/// </remarks>
internal sealed class HpkeContext
    : IDisposable
{
    /// <summary>The cipher suite that governs the AEAD and the exporter KDF.</summary>
    private readonly HpkeSuite _suite;

    /// <summary>The AEAD key (<c>Nk</c> bytes), or empty for an export-only suite.</summary>
    private byte[] _key;

    /// <summary>The base nonce (<c>Nn</c> bytes), or empty for an export-only suite.</summary>
    private byte[] _baseNonce;

    /// <summary>The exporter secret (<c>Nh</c> bytes).</summary>
    private byte[] _exporterSecret;

    /// <summary>The running message sequence number.</summary>
    private ulong _seq;

    /// <summary>The AES block cipher, created once on first use so the key schedule is reused across every message. Only populated for the AES-GCM suites.</summary>
    private AesBlockCipher? _aesCipher;

    /// <summary>The ChaCha20-Poly1305 AEAD, created once on first use so the key schedule is reused across every message. Only populated for the ChaCha20-Poly1305 suite.</summary>
    private ChaCha20Poly1305? _chachaAead;

    /// <summary>Indicates whether the instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HpkeContext" /> class, taking ownership of the supplied key
    /// material.
    /// </summary>
    /// <param name="suite">The cipher suite governing the AEAD and exporter KDF.</param>
    /// <param name="key">The AEAD key; ownership transfers to this instance.</param>
    /// <param name="baseNonce">The base nonce; ownership transfers to this instance.</param>
    /// <param name="exporterSecret">The exporter secret; ownership transfers to this instance.</param>
    internal HpkeContext(HpkeSuite suite, byte[] key, byte[] baseNonce, byte[] exporterSecret)
    {
        _suite = suite;
        _key = key;
        _baseNonce = baseNonce;
        _exporterSecret = exporterSecret;
    }

    /// <summary>
    /// Encrypts and authenticates <paramref name="plaintext" /> with the current sequence nonce, advancing the sequence
    /// number on success (RFC 9180 §5.2).
    /// </summary>
    /// <param name="associatedData">The associated data authenticated but not encrypted.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <returns>The ciphertext followed by the authentication tag.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="NotSupportedException">The suite is export-only.</exception>
    /// <exception cref="InvalidOperationException">The sequence number has reached its maximum.</exception>
    public byte[] Seal(ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> plaintext)
    {
        ThrowIfDisposed();
        ThrowIfExportOnly();
        ThrowIfMessageLimitReached();

        byte[] result = new byte[plaintext.Length + _suite.AeadTagSizeInBytes];
        Span<byte> nonce = stackalloc byte[_suite.AeadNonceSizeInBytes];
        ComputeNonce(nonce);

        switch (_suite.Aead)
        {
            case HpkeAead.Aes128Gcm:
            case HpkeAead.Aes256Gcm:
                // The per-message GcmModeTransform carries the nonce/counter state and is created per call, but it does
                // not own the cipher, so a single cached AesBlockCipher amortizes the AES key schedule across messages.
                using (var gcm = new GcmModeTransform(EnsureAesCipher(), nonce))
                {
                    gcm.ProcessAssociatedData(associatedData);
                    gcm.Encrypt(plaintext, result);
                }

                break;

            default: // ChaCha20Poly1305
                EnsureChaChaAead().Encrypt(nonce, plaintext, result.AsSpan(0, plaintext.Length), result.AsSpan(plaintext.Length), associatedData);
                break;
        }

        IncrementSequence();
        return result;
    }

    /// <summary>
    /// Verifies and decrypts <paramref name="ciphertext" /> with the current sequence nonce, advancing the sequence
    /// number on success (RFC 9180 §5.2).
    /// </summary>
    /// <param name="associatedData">The associated data that must match what was sealed.</param>
    /// <param name="ciphertext">The ciphertext followed by the authentication tag.</param>
    /// <returns>The recovered plaintext.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="NotSupportedException">The suite is export-only.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ciphertext" /> is shorter than the authentication tag.
    /// </exception>
    /// <exception cref="CryptographicException">Authentication failed.</exception>
    /// <exception cref="InvalidOperationException">The sequence number has reached its maximum.</exception>
    public byte[] Open(ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> ciphertext)
    {
        ThrowIfDisposed();
        ThrowIfExportOnly();
        ThrowIfMessageLimitReached();
        CryptographyThrowHelper.ThrowIfCiphertextTooShort(ciphertext, _suite.AeadTagSizeInBytes);

        int plaintextLength = ciphertext.Length - _suite.AeadTagSizeInBytes;
        byte[] result = new byte[plaintextLength];
        Span<byte> nonce = stackalloc byte[_suite.AeadNonceSizeInBytes];
        ComputeNonce(nonce);

        switch (_suite.Aead)
        {
            case HpkeAead.Aes128Gcm:
            case HpkeAead.Aes256Gcm:
                // The cached cipher (see Seal) is reused; the GcmModeTransform holding per-message state is not.
                using (var gcm = new GcmModeTransform(EnsureAesCipher(), nonce))
                {
                    gcm.ProcessAssociatedData(associatedData);
                    gcm.Decrypt(ciphertext, result);
                }

                break;

            default: // ChaCha20Poly1305
                try
                {
                    EnsureChaChaAead().Decrypt(nonce, ciphertext[..plaintextLength], ciphertext[plaintextLength..], result, associatedData);
                }
                catch (AuthenticationTagMismatchException ex)
                {
                    // Normalize the BCL's specific subtype so authentication failure surfaces as the same
                    // CryptographicException for every AEAD in the suite, matching the GCM transform's contract.
                    throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AuthenticationTagMismatch, ex);
                }

                break;
        }

        IncrementSequence();
        return result;
    }

    /// <summary>
    /// Derives <paramref name="length" /> bytes of secret keying material bound to <paramref name="exporterContext" />,
    /// per RFC 9180 §5.3 <c>Export</c>.
    /// </summary>
    /// <param name="exporterContext">The application-supplied context that scopes the exported secret.</param>
    /// <param name="length">The number of bytes to export.</param>
    /// <returns>The exported secret.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="length" /> is negative or exceeds 255 times the KDF hash length.
    /// </exception>
    public byte[] Export(ReadOnlySpan<byte> exporterContext, int length)
    {
        ThrowIfDisposed();
        ThrowHelper.ThrowIfLessThan(length, 0);
        ThrowHelper.ThrowIfGreaterThan(length, 255 * _suite.KdfHashLengthInBytes);

        byte[] output = new byte[length];
        HpkeLabeledKdf.LabeledExpand(_suite.KdfHashAlgorithm, _suite.SuiteId, _exporterSecret, "sec"u8, exporterContext, output);
        return output;
    }

    /// <summary>
    /// Releases the resources used by this instance, zeroing the AEAD key, base nonce, and exporter secret.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _aesCipher?.Dispose();
        _chachaAead?.Dispose();

        CryptographyHelper.ClearAndNullify(ref _key!);
        CryptographyHelper.ClearAndNullify(ref _baseNonce!);
        CryptographyHelper.ClearAndNullify(ref _exporterSecret!);

        _disposed = true;
    }

    /// <summary>
    /// Returns the cached AES block cipher, creating it from the AEAD key on first use so the key schedule is derived
    /// once per context rather than once per message.
    /// </summary>
    /// <returns>The shared <see cref="AesBlockCipher" /> for the AES-GCM suites.</returns>
    private AesBlockCipher EnsureAesCipher() =>
        _aesCipher ??= new AesBlockCipher(_key);

    /// <summary>
    /// Returns the cached ChaCha20-Poly1305 AEAD, creating it from the AEAD key on first use so the key schedule is
    /// derived once per context rather than once per message.
    /// </summary>
    /// <returns>The shared <see cref="ChaCha20Poly1305" /> for the ChaCha20-Poly1305 suite.</returns>
    private ChaCha20Poly1305 EnsureChaChaAead() =>
        _chachaAead ??= new ChaCha20Poly1305(_key);

    /// <summary>
    /// Computes the per-message nonce <c>base_nonce XOR I2OSP(seq, Nn)</c> into <paramref name="nonce" />.
    /// </summary>
    /// <param name="nonce">The span that receives the nonce; must be <c>Nn</c> bytes long.</param>
    private void ComputeNonce(Span<byte> nonce)
    {
        _baseNonce.CopyTo(nonce);

        Span<byte> sequence = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(sequence, _seq);

        // The sequence number occupies the trailing 8 bytes of the (>= 12 byte) nonce; leading bytes are unchanged.
        int start = nonce.Length - sizeof(ulong);
        for (int i = 0; i < sizeof(ulong); i++)
            nonce[start + i] ^= sequence[i];
    }

    /// <summary>
    /// Throws when the sequence number has reached its maximum, before any AEAD work is performed (RFC 9180 §5.2
    /// <c>MessageLimitReached</c>).
    /// </summary>
    /// <exception cref="InvalidOperationException">The sequence number has reached its maximum.</exception>
    /// <remarks>
    /// With Nn = 12 the spec ceiling (2^96 − 1) is far above <see cref="ulong.MaxValue" />, so guarding the 64-bit
    /// counter against overflow is both sufficient and conservative. The check runs before nonce computation and AEAD
    /// processing so a context at the limit fails without producing or consuming a ciphertext.
    /// </remarks>
    private void ThrowIfMessageLimitReached()
    {
        if (_seq == ulong.MaxValue)
            throw new InvalidOperationException(CryptoResourceStrings.Op_Invalid_HpkeMessageLimitReached);
    }

    /// <summary>
    /// Advances the message sequence number after a successful seal or open. The pre-operation
    /// <see cref="ThrowIfMessageLimitReached" /> guarantees the counter is below its maximum, so the increment cannot
    /// wrap.
    /// </summary>
    private void IncrementSequence() =>
        _seq++;

    /// <summary>
    /// Throws a <see cref="NotSupportedException" /> when the suite is export-only and cannot seal or open.
    /// </summary>
    /// <exception cref="NotSupportedException">The suite is export-only.</exception>
    private void ThrowIfExportOnly()
    {
        if (_suite.IsExportOnly)
            throw new NotSupportedException(CryptoResourceStrings.Op_NotSupported_HpkeExportOnly);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> when the instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}
