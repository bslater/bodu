// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hpke.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the single-shot Hybrid Public Key Encryption (HPKE) operations of RFC 9180 §6, encrypting or decrypting one
/// message to or from a public key in a single call. This class cannot be instantiated.
/// </summary>
/// <remarks>
/// <para>
/// Each <c>Seal*</c> method performs a complete HPKE exchange — encapsulate a fresh shared secret to the recipient, run
/// the key schedule, and seal one message — returning both the encapsulated key and the ciphertext. The matching
/// <c>Open*</c> method reverses it. There is one method pair per establishment mode (<see cref="HpkeMode" />): base,
/// PSK, auth, and auth-PSK. For sending several messages under one encapsulation, use <see cref="HpkeSender" /> and
/// <see cref="HpkeReceiver" /> instead.
/// </para>
/// <para>
/// Recipient and sender private keys are supplied as <see cref="X25519" /> instances so the caller controls their
/// lifetime; public keys are passed as raw 32-byte spans, which is their on-the-wire form.
/// </para>
/// <para>
/// Like the rest of the library, this implementation offers best-effort side-channel resistance and has not been
/// independently audited.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var recipient = X25519.Create();
/// recipient.GenerateKey();
/// HpkeSuite suite = HpkeSuite.X25519_HkdfSha256_Aes128Gcm;
///
/// var (enc, ciphertext) = Hpke.Seal(suite, recipient.ExportPublicKey(), info, aad, plaintext);
/// byte[] recovered = Hpke.Open(suite, recipient, enc, info, aad, ciphertext);
///]]>
/// </code>
/// </example>
public static partial class Hpke
{
    /// <summary>
    /// Encrypts <paramref name="plaintext" /> to a recipient public key in base mode (RFC 9180 §6.1 <c>SealBase</c>).
    /// </summary>
    /// <param name="suite">The cipher suite.</param>
    /// <param name="recipientPublicKey">The recipient's 32-byte X25519 public key.</param>
    /// <param name="info">The application-supplied context binding the exchange.</param>
    /// <param name="associatedData">The associated data authenticated but not encrypted.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <returns>The encapsulated key and the ciphertext (ciphertext followed by the authentication tag).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="suite" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="recipientPublicKey" /> is not exactly 32 bytes.</exception>
    /// <exception cref="NotSupportedException">The suite is export-only.</exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="recipientPublicKey" /> is a low-order point.
    /// </exception>
    public static (byte[] Encapsulation, byte[] Ciphertext) Seal(
        HpkeSuite suite, ReadOnlySpan<byte> recipientPublicKey, ReadOnlySpan<byte> info, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> plaintext)
    {
        using var sender = HpkeSender.SetupBase(suite, recipientPublicKey, info, out byte[] encapsulation);
        byte[] ciphertext = sender.Seal(associatedData, plaintext);
        return (encapsulation, ciphertext);
    }

    /// <summary>
    /// Decrypts a base-mode message produced by <see cref="Seal" /> (RFC 9180 §6.1 <c>OpenBase</c>).
    /// </summary>
    /// <param name="suite">The cipher suite.</param>
    /// <param name="recipientKey">The recipient's X25519 key holding the private key.</param>
    /// <param name="encapsulation">The encapsulated key received from the sender.</param>
    /// <param name="info">The application-supplied context; must match the value used to seal.</param>
    /// <param name="associatedData">The associated data; must match the value used to seal.</param>
    /// <param name="ciphertext">The ciphertext followed by the authentication tag.</param>
    /// <returns>The recovered plaintext.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="suite" /> or <paramref name="recipientKey" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="encapsulation" /> is not exactly 32 bytes, or <paramref name="ciphertext" /> is shorter than the
    /// authentication tag.
    /// </exception>
    /// <exception cref="NotSupportedException">The suite is export-only.</exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="recipientKey" /> has no private key, <paramref name="encapsulation" /> is a low-order point, or
    /// authentication failed.
    /// </exception>
    public static byte[] Open(
        HpkeSuite suite, X25519 recipientKey, ReadOnlySpan<byte> encapsulation, ReadOnlySpan<byte> info, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> ciphertext)
    {
        using var receiver = HpkeReceiver.SetupBase(suite, recipientKey, encapsulation, info);
        return receiver.Open(associatedData, ciphertext);
    }
}
