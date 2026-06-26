// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IHpkeKem.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Abstracts an HPKE key encapsulation mechanism (RFC 9180 §4) so the HPKE key schedule and contexts are independent of
/// the underlying KEM. The single shipped implementation is DHKEM(X25519, HKDF-SHA256); the interface exists as the
/// seam through which additional registered KEMs are introduced without re-wiring the surrounding code.
/// </summary>
/// <remarks>
/// The recipient and sender private keys cross this boundary as raw bytes. Callers own those buffers and are
/// responsible for zeroing them after the call returns; implementations do not retain references to them.
/// </remarks>
internal interface IHpkeKem
{
    /// <summary>
    /// Gets the two-byte KEM identifier from the RFC 9180 IANA registry.
    /// </summary>
    ushort KemId { get; }

    /// <summary>
    /// Gets the length, in bytes, of the KEM shared secret (<c>Nsecret</c>).
    /// </summary>
    int SharedSecretSizeInBytes { get; }

    /// <summary>
    /// Gets the length, in bytes, of an encapsulated key (<c>Nenc</c>).
    /// </summary>
    int EncapsulationSizeInBytes { get; }

    /// <summary>
    /// Gets the length, in bytes, of a serialized public key (<c>Npk</c>).
    /// </summary>
    int PublicKeySizeInBytes { get; }

    /// <summary>
    /// Generates a fresh shared secret and encapsulates it to <paramref name="recipientPublicKey" /> (RFC 9180
    /// <c>Encap</c>).
    /// </summary>
    /// <param name="recipientPublicKey">The recipient's serialized public key.</param>
    /// <returns>The shared secret and the encapsulated key to transmit.</returns>
    (byte[] SharedSecret, byte[] Encapsulation) Encapsulate(ReadOnlySpan<byte> recipientPublicKey);

    /// <summary>
    /// Recovers the shared secret from <paramref name="encapsulation" /> using the recipient's key pair (RFC 9180
    /// <c>Decap</c>).
    /// </summary>
    /// <param name="encapsulation">The encapsulated key received from the sender.</param>
    /// <param name="recipientPrivateKey">The recipient's serialized private key.</param>
    /// <param name="recipientPublicKey">The recipient's serialized public key.</param>
    /// <returns>The shared secret.</returns>
    byte[] Decapsulate(ReadOnlySpan<byte> encapsulation, ReadOnlySpan<byte> recipientPrivateKey, ReadOnlySpan<byte> recipientPublicKey);

    /// <summary>
    /// Generates a shared secret encapsulated to the recipient and authenticated with the sender's static key (RFC 9180
    /// <c>AuthEncap</c>).
    /// </summary>
    /// <param name="recipientPublicKey">The recipient's serialized public key.</param>
    /// <param name="senderPrivateKey">The sender's serialized static private key.</param>
    /// <param name="senderPublicKey">The sender's serialized static public key.</param>
    /// <returns>The shared secret and the encapsulated key to transmit.</returns>
    (byte[] SharedSecret, byte[] Encapsulation) AuthEncapsulate(ReadOnlySpan<byte> recipientPublicKey, ReadOnlySpan<byte> senderPrivateKey, ReadOnlySpan<byte> senderPublicKey);

    /// <summary>
    /// Recovers the shared secret and verifies the sender's authentication (RFC 9180 <c>AuthDecap</c>).
    /// </summary>
    /// <param name="encapsulation">The encapsulated key received from the sender.</param>
    /// <param name="recipientPrivateKey">The recipient's serialized private key.</param>
    /// <param name="recipientPublicKey">The recipient's serialized public key.</param>
    /// <param name="senderPublicKey">The sender's serialized static public key.</param>
    /// <returns>The shared secret.</returns>
    byte[] AuthDecapsulate(ReadOnlySpan<byte> encapsulation, ReadOnlySpan<byte> recipientPrivateKey, ReadOnlySpan<byte> recipientPublicKey, ReadOnlySpan<byte> senderPublicKey);
}
