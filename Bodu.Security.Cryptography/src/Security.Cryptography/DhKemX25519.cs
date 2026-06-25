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
internal static class DhKemX25519
{
    /// <summary>The length, in bytes, of the KEM shared secret (<c>Nsecret</c>).</summary>
    public const int SharedSecretSizeInBytes = 32;

    /// <summary>The length, in bytes, of the encapsulated key (<c>Nenc</c>).</summary>
    public const int EncapsulationSizeInBytes = 32;

    /// <summary>The length, in bytes, of a serialized public key (<c>Npk</c>).</summary>
    public const int PublicKeySizeInBytes = 32;

    /// <summary>DHKEM(X25519, …) fixes HKDF-SHA256 as the KEM's KDF, independent of the HPKE suite KDF.</summary>
    private static HashAlgorithmName KemHash => HashAlgorithmName.SHA256;

    /// <summary>The KEM suite identifier <c>"KEM" ‖ I2OSP(0x0020, 2)</c>.</summary>
    private static ReadOnlySpan<byte> KemSuiteId => [(byte)'K', (byte)'E', (byte)'M', 0x00, 0x20];

    /// <summary>
    /// Encapsulates a fresh shared secret to the recipient public key (RFC 9180 base <c>Encap</c>).
    /// </summary>
    /// <param name="recipientPublicKey">The recipient's 32-byte X25519 public key.</param>
    /// <returns>The 32-byte shared secret and the 32-byte encapsulated key (the ephemeral public key).</returns>
    /// <exception cref="ArgumentException"><paramref name="recipientPublicKey" /> is not exactly 32 bytes.</exception>
    /// <exception cref="CryptographicException"><paramref name="recipientPublicKey" /> is a low-order point.</exception>
    public static (byte[] SharedSecret, byte[] Encapsulation) Encap(ReadOnlySpan<byte> recipientPublicKey)
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

    /// <summary>
    /// Decapsulates the shared secret from an encapsulated key using the recipient private key (RFC 9180 base
    /// <c>Decap</c>).
    /// </summary>
    /// <param name="encapsulation">The 32-byte encapsulated key produced by <see cref="Encap" />.</param>
    /// <param name="recipientKey">The recipient's X25519 key holding the private key.</param>
    /// <returns>The 32-byte shared secret.</returns>
    /// <exception cref="ArgumentException"><paramref name="encapsulation" /> is not exactly 32 bytes.</exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="recipientKey" /> has no private key, or <paramref name="encapsulation" /> is a low-order point.
    /// </exception>
    public static byte[] Decap(ReadOnlySpan<byte> encapsulation, X25519 recipientKey)
    {
        byte[] dh = recipientKey.DeriveSharedSecret(encapsulation);

        try
        {
            byte[] pkRm = recipientKey.ExportPublicKey();
            byte[] kemContext = Concat(encapsulation, pkRm);
            return ExtractAndExpand(dh, kemContext);
        }
        finally
        {
            CryptographyHelper.Clear(dh);
        }
    }

    /// <summary>
    /// Encapsulates a fresh shared secret to the recipient while authenticating the sender with its static private key
    /// (RFC 9180 <c>AuthEncap</c>).
    /// </summary>
    /// <param name="recipientPublicKey">The recipient's 32-byte X25519 public key.</param>
    /// <param name="senderKey">The sender's X25519 key holding the static private key.</param>
    /// <returns>The 32-byte shared secret and the 32-byte encapsulated key.</returns>
    /// <exception cref="ArgumentException"><paramref name="recipientPublicKey" /> is not exactly 32 bytes.</exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="senderKey" /> has no private key, or <paramref name="recipientPublicKey" /> is a low-order point.
    /// </exception>
    public static (byte[] SharedSecret, byte[] Encapsulation) AuthEncap(ReadOnlySpan<byte> recipientPublicKey, X25519 senderKey)
    {
        using var ephemeral = new X25519();
        ephemeral.GenerateKey();

        byte[] enc = ephemeral.ExportPublicKey();
        byte[] dhEphemeral = ephemeral.DeriveSharedSecret(recipientPublicKey);
        byte[] dhStatic = senderKey.DeriveSharedSecret(recipientPublicKey);
        byte[] dh = Concat(dhEphemeral, dhStatic);

        try
        {
            byte[] pkSm = senderKey.ExportPublicKey();
            byte[] kemContext = Concat(enc, recipientPublicKey, pkSm);
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

    /// <summary>
    /// Decapsulates the shared secret and verifies the sender's authentication using the sender's public key (RFC 9180
    /// <c>AuthDecap</c>).
    /// </summary>
    /// <param name="encapsulation">The 32-byte encapsulated key produced by <see cref="AuthEncap" />.</param>
    /// <param name="recipientKey">The recipient's X25519 key holding the private key.</param>
    /// <param name="senderPublicKey">The sender's 32-byte X25519 public key.</param>
    /// <returns>The 32-byte shared secret.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="encapsulation" /> or <paramref name="senderPublicKey" /> is not exactly 32 bytes.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="recipientKey" /> has no private key, or an input is a low-order point.
    /// </exception>
    public static byte[] AuthDecap(ReadOnlySpan<byte> encapsulation, X25519 recipientKey, ReadOnlySpan<byte> senderPublicKey)
    {
        byte[] dhEphemeral = recipientKey.DeriveSharedSecret(encapsulation);
        byte[] dhStatic = recipientKey.DeriveSharedSecret(senderPublicKey);
        byte[] dh = Concat(dhEphemeral, dhStatic);

        try
        {
            byte[] pkRm = recipientKey.ExportPublicKey();
            byte[] kemContext = Concat(encapsulation, pkRm, senderPublicKey);
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
            byte[] sharedSecret = new byte[SharedSecretSizeInBytes];
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
    /// <returns>A new array containing <paramref name="a" />, <paramref name="b" />, then <paramref name="c" />.</returns>
    private static byte[] Concat(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, ReadOnlySpan<byte> c)
    {
        byte[] result = new byte[a.Length + b.Length + c.Length];
        a.CopyTo(result);
        b.CopyTo(result.AsSpan(a.Length));
        c.CopyTo(result.AsSpan(a.Length + b.Length));
        return result;
    }
}
