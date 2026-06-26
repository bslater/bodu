// ---------------------------------------------------------------------------------------------------------------
// <copyright file="X25519Tests.KeyFormats.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains X25519-specific tests for the RFC 8410 PKCS#8 / SubjectPublicKeyInfo key-format support, pinned against a
/// known X25519 public key; the cross-algorithm round-trip and unsupported-format contracts are inherited from the
/// asymmetric base.
/// </summary>
public sealed partial class X25519Tests
{
    /// <summary>
    /// Verifies that importing an RFC 8410 X25519 SubjectPublicKeyInfo (OID 1.3.101.110) over the RFC 7748 §6.1 Alice
    /// public key extracts the raw key and re-exports the identical DER, pinning the exact encoding.
    /// </summary>
    [TestMethod]
    public void ImportSubjectPublicKeyInfo_WhenGivenKnownVector_ShouldExtractKeyAndReExportIdentically()
    {
        byte[] subjectPublicKeyInfo = Convert.FromHexString("302a300506032b656e0321008520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a");
        byte[] expectedKey = Convert.FromHexString("8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a");

        using var x25519 = new X25519();
        x25519.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out int bytesRead);

        Assert.AreEqual(subjectPublicKeyInfo.Length, bytesRead);
        CollectionAssert.AreEqual(expectedKey, x25519.ExportPublicKey());
        CollectionAssert.AreEqual(subjectPublicKeyInfo, x25519.ExportSubjectPublicKeyInfo());
    }

    /// <summary>
    /// Verifies that a SubjectPublicKeyInfo declaring the Ed25519 OID is rejected with
    /// <see cref="CryptographicException" /> when imported as an X25519 key.
    /// </summary>
    [TestMethod]
    public void ImportSubjectPublicKeyInfo_WhenAlgorithmOidIsWrong_ShouldThrowCryptographicException()
    {
        // The same structure with the Ed25519 OID (1.3.101.112) substituted for X25519.
        byte[] wrongOid = Convert.FromHexString("302a300506032b65700321008520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a");

        using var x25519 = new X25519();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            x25519.ImportSubjectPublicKeyInfo(wrongOid, out _);
        });
    }
}
