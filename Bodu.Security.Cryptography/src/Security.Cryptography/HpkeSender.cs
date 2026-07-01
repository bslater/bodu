// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HpkeSender.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents the sender side of an HPKE exchange (RFC 9180 §5.2): a session that encapsulates a shared secret to a
/// recipient once and then seals any number of messages and exports any number of secrets under that secret. This class
/// cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Obtain an instance from one of the <c>Setup*</c> factory methods, which return the sender and yield the encapsulated
/// key that the recipient needs to reconstruct the matching context. Each <see cref="Seal" /> call advances the message
/// sequence number, so the recipient's <see cref="HpkeReceiver.Open" /> calls must occur in the same order. Dispose the
/// instance to zero the derived key material.
/// </para>
/// <para>
/// For one-off encryption of a single message, prefer the single-shot <see cref="Hpke" /> façade.
/// </para>
/// <para>
/// Like the rest of the library, this implementation offers best-effort side-channel resistance and has not been
/// independently audited.
/// </para>
/// </remarks>
public sealed class HpkeSender : IDisposable
{
    /// <summary>The derived encryption context backing this sender.</summary>
    private readonly HpkeContext _context;

    /// <summary>Indicates whether the instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HpkeSender" /> class around the supplied context.
    /// </summary>
    /// <param name="context">The derived encryption context.</param>
    private HpkeSender(HpkeContext context) =>
        _context = context;

    /// <summary>
    /// Sets up a base-mode sender for the given recipient (RFC 9180 §5.1.1 <c>SetupBaseS</c>).
    /// </summary>
    /// <param name="suite">The cipher suite.</param>
    /// <param name="recipientPublicKey">The recipient's 32-byte X25519 public key.</param>
    /// <param name="info">The application-supplied context binding the exchange.</param>
    /// <param name="encapsulation">Receives the encapsulated key to transmit to the recipient.</param>
    /// <returns>A sender context ready to seal messages.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="suite" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="recipientPublicKey" /> is not exactly 32 bytes.</exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="recipientPublicKey" /> is a low-order point.
    /// </exception>
    public static HpkeSender SetupBase(HpkeSuite suite, ReadOnlySpan<byte> recipientPublicKey, ReadOnlySpan<byte> info, out byte[] encapsulation)
    {
        ThrowHelper.ThrowIfNull(suite);
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(recipientPublicKey, suite.KemAlgorithm.PublicKeySizeInBytes, "X25519 public", nameof(recipientPublicKey));

        (byte[]? sharedSecret, byte[]? enc) = suite.KemAlgorithm.Encapsulate(recipientPublicKey);

        try
        {
            HpkeContext context = HpkeKeySchedule.Create(suite, HpkeMode.Base, sharedSecret, info, default, default);
            encapsulation = enc;
            return new HpkeSender(context);
        }
        finally
        {
            CryptographyHelper.Clear(sharedSecret);
        }
    }

    /// <summary>
    /// Sets up a PSK-mode sender for the given recipient (RFC 9180 §5.1.2 <c>SetupPSKS</c>).
    /// </summary>
    /// <param name="suite">The cipher suite.</param>
    /// <param name="recipientPublicKey">The recipient's 32-byte X25519 public key.</param>
    /// <param name="info">The application-supplied context binding the exchange.</param>
    /// <param name="psk">The pre-shared key, shared out of band with the recipient.</param>
    /// <param name="pskId">The identifier of the pre-shared key.</param>
    /// <param name="encapsulation">Receives the encapsulated key to transmit to the recipient.</param>
    /// <returns>A sender context ready to seal messages.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="suite" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="recipientPublicKey" /> is not exactly 32 bytes.</exception>
    /// <exception cref="CryptographicException">
    /// The PSK inputs are inconsistent, or <paramref name="recipientPublicKey" /> is a low-order point.
    /// </exception>
    public static HpkeSender SetupPsk(HpkeSuite suite, ReadOnlySpan<byte> recipientPublicKey, ReadOnlySpan<byte> info, ReadOnlySpan<byte> psk, ReadOnlySpan<byte> pskId, out byte[] encapsulation)
    {
        ThrowHelper.ThrowIfNull(suite);
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(recipientPublicKey, suite.KemAlgorithm.PublicKeySizeInBytes, "X25519 public", nameof(recipientPublicKey));

        (byte[]? sharedSecret, byte[]? enc) = suite.KemAlgorithm.Encapsulate(recipientPublicKey);

        try
        {
            HpkeContext context = HpkeKeySchedule.Create(suite, HpkeMode.Psk, sharedSecret, info, psk, pskId);
            encapsulation = enc;
            return new HpkeSender(context);
        }
        finally
        {
            CryptographyHelper.Clear(sharedSecret);
        }
    }

    /// <summary>
    /// Sets up an auth-mode sender that authenticates itself with a static key (RFC 9180 §5.1.3 <c>SetupAuthS</c>).
    /// </summary>
    /// <param name="suite">The cipher suite.</param>
    /// <param name="recipientPublicKey">The recipient's 32-byte X25519 public key.</param>
    /// <param name="info">The application-supplied context binding the exchange.</param>
    /// <param name="senderKey">The sender's X25519 key holding the static private key.</param>
    /// <param name="encapsulation">Receives the encapsulated key to transmit to the recipient.</param>
    /// <returns>A sender context ready to seal messages.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="suite" /> or <paramref name="senderKey" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="recipientPublicKey" /> is not exactly 32 bytes.</exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="senderKey" /> has no private key, or <paramref name="recipientPublicKey" /> is a low-order
    /// point.
    /// </exception>
    public static HpkeSender SetupAuth(HpkeSuite suite, ReadOnlySpan<byte> recipientPublicKey, ReadOnlySpan<byte> info, X25519 senderKey, out byte[] encapsulation)
    {
        ThrowHelper.ThrowIfNull(suite);
        ThrowHelper.ThrowIfNull(senderKey);
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(recipientPublicKey, suite.KemAlgorithm.PublicKeySizeInBytes, "X25519 public", nameof(recipientPublicKey));

        (byte[]? sharedSecret, byte[]? enc) = AuthEncapsulate(suite, recipientPublicKey, senderKey);

        try
        {
            HpkeContext context = HpkeKeySchedule.Create(suite, HpkeMode.Auth, sharedSecret, info, default, default);
            encapsulation = enc;
            return new HpkeSender(context);
        }
        finally
        {
            CryptographyHelper.Clear(sharedSecret);
        }
    }

    /// <summary>
    /// Sets up an auth-PSK-mode sender combining static-key authentication with a pre-shared key (RFC 9180 §5.1.4
    /// <c>SetupAuthPSKS</c>).
    /// </summary>
    /// <param name="suite">The cipher suite.</param>
    /// <param name="recipientPublicKey">The recipient's 32-byte X25519 public key.</param>
    /// <param name="info">The application-supplied context binding the exchange.</param>
    /// <param name="senderKey">The sender's X25519 key holding the static private key.</param>
    /// <param name="psk">The pre-shared key, shared out of band with the recipient.</param>
    /// <param name="pskId">The identifier of the pre-shared key.</param>
    /// <param name="encapsulation">Receives the encapsulated key to transmit to the recipient.</param>
    /// <returns>A sender context ready to seal messages.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="suite" /> or <paramref name="senderKey" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="recipientPublicKey" /> is not exactly 32 bytes.</exception>
    /// <exception cref="CryptographicException">
    /// The PSK inputs are inconsistent, <paramref name="senderKey" /> has no private key, or
    /// <paramref name="recipientPublicKey" /> is a low-order point.
    /// </exception>
    public static HpkeSender SetupAuthPsk(HpkeSuite suite, ReadOnlySpan<byte> recipientPublicKey, ReadOnlySpan<byte> info, X25519 senderKey, ReadOnlySpan<byte> psk, ReadOnlySpan<byte> pskId, out byte[] encapsulation)
    {
        ThrowHelper.ThrowIfNull(suite);
        ThrowHelper.ThrowIfNull(senderKey);
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(recipientPublicKey, suite.KemAlgorithm.PublicKeySizeInBytes, "X25519 public", nameof(recipientPublicKey));

        (byte[]? sharedSecret, byte[]? enc) = AuthEncapsulate(suite, recipientPublicKey, senderKey);

        try
        {
            HpkeContext context = HpkeKeySchedule.Create(suite, HpkeMode.AuthPsk, sharedSecret, info, psk, pskId);
            encapsulation = enc;
            return new HpkeSender(context);
        }
        finally
        {
            CryptographyHelper.Clear(sharedSecret);
        }
    }

    /// <summary>
    /// Encrypts and authenticates <paramref name="plaintext" /> with the next message nonce.
    /// </summary>
    /// <param name="associatedData">The associated data authenticated but not encrypted.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <returns>The ciphertext followed by the authentication tag.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="NotSupportedException">The suite is export-only.</exception>
    /// <exception cref="InvalidOperationException">The message sequence number has reached its maximum.</exception>
    public byte[] Seal(ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> plaintext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _context.Seal(associatedData, plaintext);
    }

    /// <summary>
    /// Derives <paramref name="length" /> bytes of secret keying material bound to <paramref name="exporterContext" />.
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _context.Export(exporterContext, length);
    }

    /// <summary>
    /// Releases the resources used by this instance, zeroing the derived key material.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _context.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Performs the suite KEM's authenticated encapsulation, exporting the sender's static key material as raw bytes
    /// for the duration of the call and zeroing the exported private key afterward.
    /// </summary>
    /// <param name="suite">The cipher suite supplying the KEM.</param>
    /// <param name="recipientPublicKey">The recipient's serialized public key.</param>
    /// <param name="senderKey">The sender's static key holding the private key.</param>
    /// <returns>The shared secret and the encapsulated key.</returns>
    private static (byte[] SharedSecret, byte[] Encapsulation) AuthEncapsulate(HpkeSuite suite, ReadOnlySpan<byte> recipientPublicKey, X25519 senderKey)
    {
        byte[] senderPrivateKey = senderKey.ExportPrivateKey();

        try
        {
            return suite.KemAlgorithm.AuthEncapsulate(recipientPublicKey, senderPrivateKey, senderKey.ExportPublicKey());
        }
        finally
        {
            CryptographyHelper.Clear(senderPrivateKey);
        }
    }
}
