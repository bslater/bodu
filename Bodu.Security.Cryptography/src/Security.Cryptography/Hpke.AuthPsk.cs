// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hpke.AuthPsk.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public static partial class Hpke
{
    /// <summary>
    /// Encrypts <paramref name="plaintext" /> to a recipient public key with both sender authentication and a
    /// pre-shared key (RFC 9180 §6.1 <c>SealAuthPSK</c>).
    /// </summary>
    /// <param name="suite">The cipher suite.</param>
    /// <param name="recipientPublicKey">The recipient's 32-byte X25519 public key.</param>
    /// <param name="info">The application-supplied context binding the exchange.</param>
    /// <param name="senderKey">The sender's X25519 key holding the static private key.</param>
    /// <param name="psk">The pre-shared key, shared out of band with the recipient.</param>
    /// <param name="pskId">The identifier of the pre-shared key.</param>
    /// <param name="associatedData">The associated data authenticated but not encrypted.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <returns>The encapsulated key and the ciphertext (ciphertext followed by the authentication tag).</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="suite" /> or <paramref name="senderKey" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="recipientPublicKey" /> is not exactly 32 bytes.</exception>
    /// <exception cref="NotSupportedException">The suite is export-only.</exception>
    /// <exception cref="CryptographicException">
    /// The PSK inputs are inconsistent, <paramref name="senderKey" /> has no private key, or
    /// <paramref name="recipientPublicKey" /> is a low-order point.
    /// </exception>
    public static (byte[] Encapsulation, byte[] Ciphertext) SealAuthPsk(
        HpkeSuite suite, ReadOnlySpan<byte> recipientPublicKey, ReadOnlySpan<byte> info, X25519 senderKey, ReadOnlySpan<byte> psk, ReadOnlySpan<byte> pskId, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> plaintext)
    {
        using var sender = HpkeSender.SetupAuthPsk(suite, recipientPublicKey, info, senderKey, psk, pskId, out byte[] encapsulation);
        byte[] ciphertext = sender.Seal(associatedData, plaintext);
        return (encapsulation, ciphertext);
    }

    /// <summary>
    /// Decrypts an auth-PSK-mode message produced by <see cref="SealAuthPsk" /> and verifies sender authentication (RFC
    /// 9180 §6.1 <c>OpenAuthPSK</c>).
    /// </summary>
    /// <param name="suite">The cipher suite.</param>
    /// <param name="recipientKey">The recipient's X25519 key holding the private key.</param>
    /// <param name="encapsulation">The encapsulated key received from the sender.</param>
    /// <param name="info">The application-supplied context; must match the value used to seal.</param>
    /// <param name="senderPublicKey">The sender's 32-byte X25519 public key, used to verify authentication.</param>
    /// <param name="psk">The pre-shared key, shared out of band with the sender.</param>
    /// <param name="pskId">The identifier of the pre-shared key.</param>
    /// <param name="associatedData">The associated data; must match the value used to seal.</param>
    /// <param name="ciphertext">The ciphertext followed by the authentication tag.</param>
    /// <returns>The recovered plaintext.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="suite" /> or <paramref name="recipientKey" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="encapsulation" /> or <paramref name="senderPublicKey" /> is not exactly 32 bytes, or
    /// <paramref name="ciphertext" /> is shorter than the authentication tag.
    /// </exception>
    /// <exception cref="NotSupportedException">The suite is export-only.</exception>
    /// <exception cref="CryptographicException">
    /// The PSK inputs are inconsistent, <paramref name="recipientKey" /> has no private key, an input is a low-order
    /// point, or authentication failed.
    /// </exception>
    public static byte[] OpenAuthPsk(
        HpkeSuite suite, X25519 recipientKey, ReadOnlySpan<byte> encapsulation, ReadOnlySpan<byte> info, ReadOnlySpan<byte> senderPublicKey, ReadOnlySpan<byte> psk, ReadOnlySpan<byte> pskId, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> ciphertext)
    {
        using var receiver = HpkeReceiver.SetupAuthPsk(suite, recipientKey, encapsulation, info, senderPublicKey, psk, pskId);
        return receiver.Open(associatedData, ciphertext);
    }
}
