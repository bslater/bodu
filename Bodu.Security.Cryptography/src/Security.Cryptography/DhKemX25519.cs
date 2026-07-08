// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DhKemX25519.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements DHKEM(X25519, HKDF-SHA256) — the Diffie-Hellman key encapsulation mechanism of RFC 9180 §4.1 built on
/// X25519 and HKDF-SHA256, providing the base <c>Encap</c>/<c>Decap</c> operations and the authenticated
/// <c>AuthEncap</c>/<c>AuthDecap</c> operations used by HPKE's Auth and AuthPSK modes.
/// </summary>
internal sealed class DhKemX25519
    : IHpkeKem
{
    /// <summary>The shared instance; the KEM holds no mutable state.</summary>
    public static readonly DhKemX25519 Instance = new();

    /// <summary>The length, in bytes, of the KEM shared secret (<c>Nsecret</c>).</summary>
    public const int SharedSecretSize = 32;

    /// <summary>The length, in bytes, of the encapsulated key (<c>Nenc</c>).</summary>
    public const int EncapsulationSize = 32;

    /// <summary>The length, in bytes, of a serialized public key (<c>Npk</c>).</summary>
    public const int PublicKeySize = 32;

    /// <summary>The IANA KEM identifier for DHKEM(X25519, HKDF-SHA256).</summary>
    private const ushort DhKemX25519HkdfSha256Id = 0x0020;

    /// <summary>
    /// Initializes a new instance of the <see cref="DhKemX25519" /> class. The type is a stateless singleton; consumers
    /// use <see cref="Instance" />.
    /// </summary>
    private DhKemX25519()
    {
    }

    /// <inheritdoc />
    public ushort KemId =>
        DhKemX25519HkdfSha256Id;

    /// <inheritdoc />
    public int SharedSecretSizeInBytes =>
        SharedSecretSize;

    /// <inheritdoc />
    public int EncapsulationSizeInBytes =>
        EncapsulationSize;

    /// <inheritdoc />
    public int PublicKeySizeInBytes =>
        PublicKeySize;

    /// <summary>
    /// Gets the hash algorithm of the KEM's KDF; DHKEM(X25519, …) fixes HKDF-SHA256, independent of the HPKE suite KDF.
    /// </summary>
    private static HashAlgorithmName KemHash => HashAlgorithmName.SHA256;

    /// <summary>
    /// Gets the KEM suite identifier <c>"KEM" ‖ I2OSP(0x0020, 2)</c>.
    /// </summary>
    private static ReadOnlySpan<byte> KemSuiteId => [(byte)'K', (byte)'E', (byte)'M', 0x00, 0x20];

    /// <inheritdoc />
    public (byte[] SharedSecret, byte[] Encapsulation) Encapsulate(ReadOnlySpan<byte> recipientPublicKey)
    {
        using var ephemeral = new X25519();
        ephemeral.GenerateKey();

        byte[] enc = ephemeral.ExportPublicKey();
        byte[] dh = ephemeral.DeriveSharedSecret(recipientPublicKey);

        try
        {
            byte[] kemContext = Concat(enc, recipientPublicKey);
            byte[] sharedSecret = ExtractAndExpand(dh, kemContext);
            return (sharedSecret, enc);
        }
        finally
        {
            CryptographyHelper.Clear(dh);
        }
    }

    /// <inheritdoc />
    public byte[] Decapsulate(ReadOnlySpan<byte> encapsulation, ReadOnlySpan<byte> recipientPrivateKey, ReadOnlySpan<byte> recipientPublicKey)
    {
        using var recipient = new X25519();
        recipient.ImportPrivateKey(recipientPrivateKey);

        byte[] dh = recipient.DeriveSharedSecret(encapsulation);

        try
        {
            byte[] kemContext = Concat(encapsulation, recipientPublicKey);
            return ExtractAndExpand(dh, kemContext);
        }
        finally
        {
            CryptographyHelper.Clear(dh);
        }
    }

    /// <inheritdoc />
    public (byte[] SharedSecret, byte[] Encapsulation) AuthEncapsulate(ReadOnlySpan<byte> recipientPublicKey, ReadOnlySpan<byte> senderPrivateKey, ReadOnlySpan<byte> senderPublicKey)
    {
        using var ephemeral = new X25519();
        ephemeral.GenerateKey();

        using var sender = new X25519();
        sender.ImportPrivateKey(senderPrivateKey);

        byte[] enc = ephemeral.ExportPublicKey();
        byte[] dhEphemeral = ephemeral.DeriveSharedSecret(recipientPublicKey);
        byte[] dhStatic = sender.DeriveSharedSecret(recipientPublicKey);
        byte[] dh = Concat(dhEphemeral, dhStatic);

        try
        {
            byte[] kemContext = Concat(enc, recipientPublicKey, senderPublicKey);
            byte[] sharedSecret = ExtractAndExpand(dh, kemContext);
            return (sharedSecret, enc);
        }
        finally
        {
            CryptographyHelper.Clear(dhEphemeral);
            CryptographyHelper.Clear(dhStatic);
            CryptographyHelper.Clear(dh);
        }
    }

    /// <inheritdoc />
    public byte[] AuthDecapsulate(ReadOnlySpan<byte> encapsulation, ReadOnlySpan<byte> recipientPrivateKey, ReadOnlySpan<byte> recipientPublicKey, ReadOnlySpan<byte> senderPublicKey)
    {
        using var recipient = new X25519();
        recipient.ImportPrivateKey(recipientPrivateKey);

        byte[] dhEphemeral = recipient.DeriveSharedSecret(encapsulation);
        byte[] dhStatic = recipient.DeriveSharedSecret(senderPublicKey);
        byte[] dh = Concat(dhEphemeral, dhStatic);

        try
        {
            byte[] kemContext = Concat(encapsulation, recipientPublicKey, senderPublicKey);
            return ExtractAndExpand(dh, kemContext);
        }
        finally
        {
            CryptographyHelper.Clear(dhEphemeral);
            CryptographyHelper.Clear(dhStatic);
            CryptographyHelper.Clear(dh);
        }
    }

    /// <summary>
    /// Derives the KEM shared secret from a Diffie-Hellman result and KEM context, per RFC 9180 §4.1
    /// <c>ExtractAndExpand</c>.
    /// </summary>
    /// <param name="dh">The Diffie-Hellman output (one or two concatenated 32-byte secrets).</param>
    /// <param name="kemContext">The KEM context binding the encapsulated key and the participating public keys.</param>
    /// <returns>The 32-byte shared secret.</returns>
    private static byte[] ExtractAndExpand(ReadOnlySpan<byte> dh, ReadOnlySpan<byte> kemContext)
    {
        byte[] eaePrk = HpkeLabeledKdf.LabeledExtract(KemHash, KemSuiteId, default, "eae_prk"u8, dh);

        try
        {
            byte[] sharedSecret = new byte[SharedSecretSize];
            HpkeLabeledKdf.LabeledExpand(KemHash, KemSuiteId, eaePrk, "shared_secret"u8, kemContext, sharedSecret);
            return sharedSecret;
        }
        finally
        {
            CryptographyHelper.Clear(eaePrk);
        }
    }

    /// <summary>
    /// Concatenates two byte sequences into a new array.
    /// </summary>
    /// <param name="a">The first sequence.</param>
    /// <param name="b">The second sequence.</param>
    /// <returns>A new array containing <paramref name="a" /> followed by <paramref name="b" />.</returns>
    private static byte[] Concat(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        byte[] result = new byte[a.Length + b.Length];
        a.CopyTo(result);
        b.CopyTo(result.AsSpan(a.Length));
        return result;
    }

    /// <summary>
    /// Concatenates three byte sequences into a new array.
    /// </summary>
    /// <param name="a">The first sequence.</param>
    /// <param name="b">The second sequence.</param>
    /// <param name="c">The third sequence.</param>
    /// <returns>
    /// A new array containing <paramref name="a" />, <paramref name="b" />, then <paramref name="c" />.
    /// </returns>
    private static byte[] Concat(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, ReadOnlySpan<byte> c)
    {
        byte[] result = new byte[a.Length + b.Length + c.Length];
        a.CopyTo(result);
        b.CopyTo(result.AsSpan(a.Length));
        c.CopyTo(result.AsSpan(a.Length + b.Length));
        return result;
    }
}
