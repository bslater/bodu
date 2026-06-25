// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HpkeSuite.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Describes a complete HPKE cipher suite — the combination of a Key Encapsulation Mechanism (KEM), a Key Derivation
/// Function (KDF), and an Authenticated Encryption with Associated Data (AEAD) function — and exposes the derived
/// element lengths defined by RFC 9180. Instances are immutable.
/// </summary>
/// <remarks>
/// <para>
/// A suite fixes every algorithm an HPKE exchange uses. The <see cref="HpkeKem.X25519HkdfSha256" /> KEM is the only KEM
/// this implementation supports; it is paired with any of the HKDF-SHA2 KDFs and any of the registered AEADs. Use one
/// of the pre-configured static properties for the common RFC 9180 combinations, or construct a custom suite directly.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// HpkeSuite suite = HpkeSuite.X25519_HkdfSha256_Aes128Gcm;
/// // or, explicitly:
/// var custom = new HpkeSuite(HpkeKem.X25519HkdfSha256, HpkeKdf.HkdfSha256, HpkeAead.ChaCha20Poly1305);
///]]>
/// </code>
/// </example>
public sealed class HpkeSuite
{
    /// <summary>The 10-byte HPKE suite identifier, <c>"HPKE" ‖ I2OSP(kem,2) ‖ I2OSP(kdf,2) ‖ I2OSP(aead,2)</c>.</summary>
    private readonly byte[] _suiteId;

    /// <summary>
    /// Initializes a new instance of the <see cref="HpkeSuite" /> class from the specified KEM, KDF, and AEAD
    /// identifiers.
    /// </summary>
    /// <param name="kem">The key encapsulation mechanism.</param>
    /// <param name="kdf">The key derivation function.</param>
    /// <param name="aead">The authenticated-encryption function.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="kem" />, <paramref name="kdf" />, or <paramref name="aead" /> is not a supported value.
    /// </exception>
    public HpkeSuite(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
    {
        if (kem != HpkeKem.X25519HkdfSha256)
            throw new ArgumentOutOfRangeException(nameof(kem), string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_OutOfRange_HpkeKem, (ushort)kem));
        if (kdf is not (HpkeKdf.HkdfSha256 or HpkeKdf.HkdfSha384 or HpkeKdf.HkdfSha512))
            throw new ArgumentOutOfRangeException(nameof(kdf), string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_OutOfRange_HpkeKdf, (ushort)kdf));
        if (aead is not (HpkeAead.Aes128Gcm or HpkeAead.Aes256Gcm or HpkeAead.ChaCha20Poly1305 or HpkeAead.ExportOnly))
            throw new ArgumentOutOfRangeException(nameof(aead), string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_OutOfRange_HpkeAead, (ushort)aead));

        Kem = kem;
        Kdf = kdf;
        Aead = aead;

        (KdfHashAlgorithm, KdfHashLengthInBytes) = kdf switch
        {
            HpkeKdf.HkdfSha256 => (HashAlgorithmName.SHA256, 32),
            HpkeKdf.HkdfSha384 => (HashAlgorithmName.SHA384, 48),
            _ => (HashAlgorithmName.SHA512, 64),
        };

        (AeadKeySizeInBytes, AeadNonceSizeInBytes, AeadTagSizeInBytes) = aead switch
        {
            HpkeAead.Aes128Gcm => (16, 12, 16),
            HpkeAead.Aes256Gcm => (32, 12, 16),
            HpkeAead.ChaCha20Poly1305 => (32, 12, 16),
            _ => (0, 0, 0),
        };

        // suite_id = "HPKE" ‖ I2OSP(kem_id, 2) ‖ I2OSP(kdf_id, 2) ‖ I2OSP(aead_id, 2)
        _suiteId = new byte[10];
        "HPKE"u8.CopyTo(_suiteId);
        BinaryPrimitives.WriteUInt16BigEndian(_suiteId.AsSpan(4), (ushort)kem);
        BinaryPrimitives.WriteUInt16BigEndian(_suiteId.AsSpan(6), (ushort)kdf);
        BinaryPrimitives.WriteUInt16BigEndian(_suiteId.AsSpan(8), (ushort)aead);
    }

    /// <summary>Gets the DHKEM(X25519, HKDF-SHA256) / HKDF-SHA256 / AES-128-GCM suite.</summary>
    /// <value>The pre-configured suite <c>0x0020, 0x0001, 0x0001</c>.</value>
    public static HpkeSuite X25519_HkdfSha256_Aes128Gcm { get; } =
        new(HpkeKem.X25519HkdfSha256, HpkeKdf.HkdfSha256, HpkeAead.Aes128Gcm);

    /// <summary>Gets the DHKEM(X25519, HKDF-SHA256) / HKDF-SHA256 / AES-256-GCM suite.</summary>
    /// <value>The pre-configured suite <c>0x0020, 0x0001, 0x0002</c>.</value>
    public static HpkeSuite X25519_HkdfSha256_Aes256Gcm { get; } =
        new(HpkeKem.X25519HkdfSha256, HpkeKdf.HkdfSha256, HpkeAead.Aes256Gcm);

    /// <summary>Gets the DHKEM(X25519, HKDF-SHA256) / HKDF-SHA256 / ChaCha20Poly1305 suite.</summary>
    /// <value>The pre-configured suite <c>0x0020, 0x0001, 0x0003</c>.</value>
    public static HpkeSuite X25519_HkdfSha256_ChaCha20Poly1305 { get; } =
        new(HpkeKem.X25519HkdfSha256, HpkeKdf.HkdfSha256, HpkeAead.ChaCha20Poly1305);

    /// <summary>Gets the key encapsulation mechanism of this suite.</summary>
    /// <value>The KEM identifier.</value>
    public HpkeKem Kem { get; }

    /// <summary>Gets the key derivation function of this suite.</summary>
    /// <value>The KDF identifier.</value>
    public HpkeKdf Kdf { get; }

    /// <summary>Gets the authenticated-encryption function of this suite.</summary>
    /// <value>The AEAD identifier.</value>
    public HpkeAead Aead { get; }

    /// <summary>Gets the length, in bytes, of the KEM shared secret (<c>Nsecret</c>).</summary>
    /// <value>32 for the X25519 KEM.</value>
    public int SharedSecretSizeInBytes => DhKemX25519.SharedSecretSizeInBytes;

    /// <summary>Gets the length, in bytes, of the KEM encapsulated key (<c>Nenc</c>).</summary>
    /// <value>32 for the X25519 KEM.</value>
    public int EncapsulationSizeInBytes => DhKemX25519.EncapsulationSizeInBytes;

    /// <summary>Gets the length, in bytes, of the AEAD key (<c>Nk</c>).</summary>
    /// <value>The AEAD key length, or 0 for an export-only suite.</value>
    public int AeadKeySizeInBytes { get; }

    /// <summary>Gets the length, in bytes, of the AEAD nonce (<c>Nn</c>).</summary>
    /// <value>The AEAD nonce length, or 0 for an export-only suite.</value>
    public int AeadNonceSizeInBytes { get; }

    /// <summary>Gets the length, in bytes, of the AEAD authentication tag (<c>Nt</c>).</summary>
    /// <value>The AEAD tag length, or 0 for an export-only suite.</value>
    public int AeadTagSizeInBytes { get; }

    /// <summary>Gets a value indicating whether this suite is export-only and rejects encryption and decryption.</summary>
    /// <value><see langword="true" /> when <see cref="Aead" /> is <see cref="HpkeAead.ExportOnly" />; otherwise <see langword="false" />.</value>
    public bool IsExportOnly => Aead == HpkeAead.ExportOnly;

    /// <summary>Gets the HKDF hash algorithm corresponding to <see cref="Kdf" />.</summary>
    /// <value>The hash algorithm used by the suite's KDF.</value>
    internal HashAlgorithmName KdfHashAlgorithm { get; }

    /// <summary>Gets the length, in bytes, of the suite's KDF hash output (<c>Nh</c>).</summary>
    /// <value>32, 48, or 64 for HKDF-SHA256, HKDF-SHA384, or HKDF-SHA512 respectively.</value>
    internal int KdfHashLengthInBytes { get; }

    /// <summary>Gets the 10-byte HPKE suite identifier used to bind the labeled KDF to this suite.</summary>
    /// <value>The suite identifier <c>"HPKE" ‖ I2OSP(kem,2) ‖ I2OSP(kdf,2) ‖ I2OSP(aead,2)</c>.</value>
    internal ReadOnlySpan<byte> SuiteId => _suiteId;
}
