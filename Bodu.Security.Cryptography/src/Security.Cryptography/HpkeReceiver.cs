// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HpkeReceiver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents the recipient side of an HPKE exchange (RFC 9180 §5.2): a session that reconstructs the shared secret
/// from an encapsulated key and then opens any number of sealed messages and exports any number of secrets under that
/// secret. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Obtain an instance from one of the <c>Setup*</c> factory methods, supplying the recipient's private key and the
/// encapsulated key produced by the matching <see cref="HpkeSender" /> setup. Each <see cref="Open" /> call advances
/// the message sequence number, so messages must be opened in the order they were sealed. Dispose the instance to zero
/// the derived key material.
/// </para>
/// <para>For one-off decryption of a single message, prefer the single-shot <see cref="Hpke" /> façade.</para>
/// </remarks>
public sealed class HpkeReceiver : IDisposable
{
    /// <summary>The derived encryption context backing this receiver.</summary>
    private readonly HpkeContext _context;

    /// <summary>Indicates whether the instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HpkeReceiver" /> class around the supplied context.
    /// </summary>
    /// <param name="context">The derived encryption context.</param>
    private HpkeReceiver(HpkeContext context) =>
        _context = context;

    /// <summary>
    /// Sets up a base-mode receiver (RFC 9180 §5.1.1 <c>SetupBaseR</c>).
    /// </summary>
    /// <param name="suite">The cipher suite.</param>
    /// <param name="recipientKey">The recipient's X25519 key holding the private key.</param>
    /// <param name="encapsulation">The encapsulated key received from the sender.</param>
    /// <param name="info">The application-supplied context binding the exchange; must match the sender's.</param>
    /// <returns>A receiver context ready to open messages.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="suite" /> or <paramref name="recipientKey" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="encapsulation" /> is not exactly 32 bytes.</exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="recipientKey" /> has no private key, or <paramref name="encapsulation" /> is a low-order point.
    /// </exception>
    public static HpkeReceiver SetupBase(HpkeSuite suite, X25519 recipientKey, ReadOnlySpan<byte> encapsulation, ReadOnlySpan<byte> info)
    {
        ThrowHelper.ThrowIfNull(suite);
        ThrowHelper.ThrowIfNull(recipientKey);
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(encapsulation, DhKemX25519.EncapsulationSizeInBytes, "X25519 public", nameof(encapsulation));

        byte[] sharedSecret = DhKemX25519.Decap(encapsulation, recipientKey);

        try
        {
            return new HpkeReceiver(HpkeKeySchedule.Create(suite, HpkeMode.Base, sharedSecret, info, default, default));
        }
        finally
        {
            CryptographyHelper.Clear(sharedSecret);
        }
    }

    /// <summary>
    /// Sets up a PSK-mode receiver (RFC 9180 §5.1.2 <c>SetupPSKR</c>).
    /// </summary>
    /// <param name="suite">The cipher suite.</param>
    /// <param name="recipientKey">The recipient's X25519 key holding the private key.</param>
    /// <param name="encapsulation">The encapsulated key received from the sender.</param>
    /// <param name="info">The application-supplied context binding the exchange; must match the sender's.</param>
    /// <param name="psk">The pre-shared key, shared out of band with the sender.</param>
    /// <param name="pskId">The identifier of the pre-shared key.</param>
    /// <returns>A receiver context ready to open messages.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="suite" /> or <paramref name="recipientKey" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="encapsulation" /> is not exactly 32 bytes.</exception>
    /// <exception cref="CryptographicException">
    /// The PSK inputs are inconsistent, <paramref name="recipientKey" /> has no private key, or
    /// <paramref name="encapsulation" /> is a low-order point.
    /// </exception>
    public static HpkeReceiver SetupPsk(HpkeSuite suite, X25519 recipientKey, ReadOnlySpan<byte> encapsulation, ReadOnlySpan<byte> info, ReadOnlySpan<byte> psk, ReadOnlySpan<byte> pskId)
    {
        ThrowHelper.ThrowIfNull(suite);
        ThrowHelper.ThrowIfNull(recipientKey);
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(encapsulation, DhKemX25519.EncapsulationSizeInBytes, "X25519 public", nameof(encapsulation));

        byte[] sharedSecret = DhKemX25519.Decap(encapsulation, recipientKey);

        try
        {
            return new HpkeReceiver(HpkeKeySchedule.Create(suite, HpkeMode.Psk, sharedSecret, info, psk, pskId));
        }
        finally
        {
            CryptographyHelper.Clear(sharedSecret);
        }
    }

    /// <summary>
    /// Sets up an auth-mode receiver that verifies the sender's static-key authentication (RFC 9180 §5.1.3
    /// <c>SetupAuthR</c>).
    /// </summary>
    /// <param name="suite">The cipher suite.</param>
    /// <param name="recipientKey">The recipient's X25519 key holding the private key.</param>
    /// <param name="encapsulation">The encapsulated key received from the sender.</param>
    /// <param name="info">The application-supplied context binding the exchange; must match the sender's.</param>
    /// <param name="senderPublicKey">The sender's 32-byte X25519 public key, used to verify authentication.</param>
    /// <returns>A receiver context ready to open messages.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="suite" /> or <paramref name="recipientKey" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="encapsulation" /> or <paramref name="senderPublicKey" /> is not exactly 32 bytes.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="recipientKey" /> has no private key, or an input is a low-order point.
    /// </exception>
    public static HpkeReceiver SetupAuth(HpkeSuite suite, X25519 recipientKey, ReadOnlySpan<byte> encapsulation, ReadOnlySpan<byte> info, ReadOnlySpan<byte> senderPublicKey)
    {
        ThrowHelper.ThrowIfNull(suite);
        ThrowHelper.ThrowIfNull(recipientKey);
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(encapsulation, DhKemX25519.EncapsulationSizeInBytes, "X25519 public", nameof(encapsulation));
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(senderPublicKey, DhKemX25519.PublicKeySizeInBytes, "X25519 public", nameof(senderPublicKey));

        byte[] sharedSecret = DhKemX25519.AuthDecap(encapsulation, recipientKey, senderPublicKey);

        try
        {
            return new HpkeReceiver(HpkeKeySchedule.Create(suite, HpkeMode.Auth, sharedSecret, info, default, default));
        }
        finally
        {
            CryptographyHelper.Clear(sharedSecret);
        }
    }

    /// <summary>
    /// Sets up an auth-PSK-mode receiver combining sender authentication with a pre-shared key (RFC 9180 §5.1.4
    /// <c>SetupAuthPSKR</c>).
    /// </summary>
    /// <param name="suite">The cipher suite.</param>
    /// <param name="recipientKey">The recipient's X25519 key holding the private key.</param>
    /// <param name="encapsulation">The encapsulated key received from the sender.</param>
    /// <param name="info">The application-supplied context binding the exchange; must match the sender's.</param>
    /// <param name="senderPublicKey">The sender's 32-byte X25519 public key, used to verify authentication.</param>
    /// <param name="psk">The pre-shared key, shared out of band with the sender.</param>
    /// <param name="pskId">The identifier of the pre-shared key.</param>
    /// <returns>A receiver context ready to open messages.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="suite" /> or <paramref name="recipientKey" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="encapsulation" /> or <paramref name="senderPublicKey" /> is not exactly 32 bytes.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// The PSK inputs are inconsistent, <paramref name="recipientKey" /> has no private key, or an input is a low-order
    /// point.
    /// </exception>
    public static HpkeReceiver SetupAuthPsk(HpkeSuite suite, X25519 recipientKey, ReadOnlySpan<byte> encapsulation, ReadOnlySpan<byte> info, ReadOnlySpan<byte> senderPublicKey, ReadOnlySpan<byte> psk, ReadOnlySpan<byte> pskId)
    {
        ThrowHelper.ThrowIfNull(suite);
        ThrowHelper.ThrowIfNull(recipientKey);
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(encapsulation, DhKemX25519.EncapsulationSizeInBytes, "X25519 public", nameof(encapsulation));
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(senderPublicKey, DhKemX25519.PublicKeySizeInBytes, "X25519 public", nameof(senderPublicKey));

        byte[] sharedSecret = DhKemX25519.AuthDecap(encapsulation, recipientKey, senderPublicKey);

        try
        {
            return new HpkeReceiver(HpkeKeySchedule.Create(suite, HpkeMode.AuthPsk, sharedSecret, info, psk, pskId));
        }
        finally
        {
            CryptographyHelper.Clear(sharedSecret);
        }
    }

    /// <summary>
    /// Verifies and decrypts the next sealed message.
    /// </summary>
    /// <param name="associatedData">The associated data that must match what the sender supplied.</param>
    /// <param name="ciphertext">The ciphertext followed by the authentication tag.</param>
    /// <returns>The recovered plaintext.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="NotSupportedException">The suite is export-only.</exception>
    /// <exception cref="ArgumentException"><paramref name="ciphertext" /> is shorter than the authentication tag.</exception>
    /// <exception cref="CryptographicException">Authentication failed.</exception>
    /// <exception cref="InvalidOperationException">The message sequence number has reached its maximum.</exception>
    public byte[] Open(ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> ciphertext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _context.Open(associatedData, ciphertext);
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
}
