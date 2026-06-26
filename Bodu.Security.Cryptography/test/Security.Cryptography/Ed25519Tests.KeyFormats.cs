// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.KeyFormats.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains Ed25519-specific tests for the RFC 8410 PKCS#8 / SubjectPublicKeyInfo key-format support, pinned against
/// the published RFC 8410 §10.3 vector; the cross-algorithm round-trip and unsupported-format contracts are inherited
/// from the asymmetric base.
/// </summary>
public sealed partial class Ed25519Tests
{
    /// <summary>
    /// Verifies that importing the RFC 8410 §10.3 Ed25519 PKCS#8 example extracts the published seed and re-exports the
    /// identical DER, pinning the exact OneAsymmetricKey encoding (OID 1.3.101.112, version 0).
    /// </summary>
    [TestMethod]
    public void ImportPkcs8PrivateKey_WhenGivenRfc8410Vector_ShouldExtractSeedAndReExportIdentically()
    {
        byte[] pkcs8 = Convert.FromHexString("302e020100300506032b657004220420d4ee72dbf913584ad5b6d8f1f769f8ad3afe7c28cbf1d4fbe097a88f44755842");
        byte[] expectedSeed = Convert.FromHexString("d4ee72dbf913584ad5b6d8f1f769f8ad3afe7c28cbf1d4fbe097a88f44755842");

        using var ed25519 = new Ed25519();
        ed25519.ImportPkcs8PrivateKey(pkcs8, out int bytesRead);

        Assert.AreEqual(pkcs8.Length, bytesRead);
        CollectionAssert.AreEqual(expectedSeed, ed25519.ExportPrivateKey());
        CollectionAssert.AreEqual(pkcs8, ed25519.ExportPkcs8PrivateKey());
    }

    /// <summary>
    /// Verifies that a structurally valid RFC 8410 structure declaring a different algorithm OID (X25519) is rejected
    /// with <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void ImportPkcs8PrivateKey_WhenAlgorithmOidIsWrong_ShouldThrowCryptographicException()
    {
        // Same structure as the RFC 8410 vector but with the X25519 OID (1.3.101.110) substituted for Ed25519.
        byte[] wrongOid = Convert.FromHexString("302e020100300506032b656e04220420d4ee72dbf913584ad5b6d8f1f769f8ad3afe7c28cbf1d4fbe097a88f44755842");

        using var ed25519 = new Ed25519();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            ed25519.ImportPkcs8PrivateKey(wrongOid, out _);
        });
    }

    /// <summary>
    /// Verifies that malformed DER passed to <see cref="Ed25519.ImportSubjectPublicKeyInfo" /> throws
    /// <see cref="CryptographicException" /> rather than an ASN.1 parser exception.
    /// </summary>
    [TestMethod]
    public void ImportSubjectPublicKeyInfo_WhenDerIsMalformed_ShouldThrowCryptographicException()
    {
        using var ed25519 = new Ed25519();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            ed25519.ImportSubjectPublicKeyInfo(new byte[] { 0x30, 0x05, 0x01, 0x02, 0x03 }, out _);
        });
    }
}
