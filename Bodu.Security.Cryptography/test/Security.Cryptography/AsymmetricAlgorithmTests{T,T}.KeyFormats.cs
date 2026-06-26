// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsymmetricAlgorithmTests{T,T}.KeyFormats.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contract tests asserting that the inherited PKCS#8 / SubjectPublicKeyInfo / PEM / XML key-format members reject
/// consistently with <see cref="NotSupportedException" /> for every raw-key-only asymmetric algorithm.
/// </summary>
public abstract partial class AsymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Gets a value indicating whether the algorithm implements the RFC 8410 PKCS#8 / SubjectPublicKeyInfo key
    /// containers. Overridden to <see langword="true" /> by X25519 and Ed25519; the parameter-set algorithms leave it
    /// <see langword="false" />.
    /// </summary>
    protected virtual bool SupportsStandardKeyContainers =>
        false;

    /// <summary>
    /// Verifies that the inherited XML key-format members throw <see cref="NotSupportedException" /> for every
    /// algorithm, as RFC 8410 defines no XML representation.
    /// </summary>
    [TestMethod]
    public void XmlKeyFormatMembers_WhenInvoked_ShouldThrowNotSupportedException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ToXmlString(false); });
        Assert.ThrowsExactly<NotSupportedException>(() => { algorithm.FromXmlString("<X/>"); });
    }

    /// <summary>
    /// Verifies that the inherited encrypted-PKCS#8 members throw <see cref="NotSupportedException" /> for every
    /// algorithm; password-based encryption of the private key is out of scope.
    /// </summary>
    [TestMethod]
    public void EncryptedPkcs8Members_WhenInvoked_ShouldThrowNotSupportedException()
    {
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();
        var pbe = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1);
        byte[] buffer = new byte[8192];
        byte[] source = new byte[64];

        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ExportEncryptedPkcs8PrivateKey("pw", pbe); });
        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.TryExportEncryptedPkcs8PrivateKey("pw", pbe, buffer, out _); });
        Assert.ThrowsExactly<NotSupportedException>(() => { algorithm.ImportEncryptedPkcs8PrivateKey("pw", source, out _); });
    }

    /// <summary>
    /// Verifies that, for algorithms that do not support the standardized ASN.1 key containers, the inherited PKCS#8
    /// and SubjectPublicKeyInfo export and import members throw <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Pkcs8AndSpkiMembers_WhenContainersUnsupported_ShouldThrowNotSupportedException()
    {
        if (SupportsStandardKeyContainers)
            return;

        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();
        byte[] buffer = new byte[8192];
        byte[] source = new byte[64];

        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ExportPkcs8PrivateKey(); });
        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ExportPkcs8PrivateKeyPem(); });
        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ExportSubjectPublicKeyInfo(); });
        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ExportSubjectPublicKeyInfoPem(); });
        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.TryExportPkcs8PrivateKey(buffer, out _); });
        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.TryExportSubjectPublicKeyInfo(buffer, out _); });
        Assert.ThrowsExactly<NotSupportedException>(() => { algorithm.ImportPkcs8PrivateKey(source, out _); });
        Assert.ThrowsExactly<NotSupportedException>(() => { algorithm.ImportSubjectPublicKeyInfo(source, out _); });
    }

    /// <summary>
    /// Verifies that, for algorithms that support the RFC 8410 key containers, the PKCS#8, SubjectPublicKeyInfo, and
    /// PEM export/import members round-trip the raw key material.
    /// </summary>
    [TestMethod]
    public void StandardKeyContainers_WhenSupported_ShouldRoundTripRawKeys()
    {
        if (!SupportsStandardKeyContainers)
            return;

        using TAlgorithm original = CreateAlgorithmWithGeneratedKey();
        byte[] rawPublic = ExportPublicKey(original);
        byte[] rawPrivate = ExportPrivateKey(original);

        byte[] subjectPublicKeyInfo = original.ExportSubjectPublicKeyInfo();
        using TAlgorithm fromSpki = CreateAlgorithm();
        fromSpki.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out int spkiRead);
        Assert.AreEqual(subjectPublicKeyInfo.Length, spkiRead);
        CollectionAssert.AreEqual(rawPublic, ExportPublicKey(fromSpki));

        byte[] pkcs8 = original.ExportPkcs8PrivateKey();
        using TAlgorithm fromPkcs8 = CreateAlgorithm();
        fromPkcs8.ImportPkcs8PrivateKey(pkcs8, out int pkcs8Read);
        Assert.AreEqual(pkcs8.Length, pkcs8Read);
        CollectionAssert.AreEqual(rawPrivate, ExportPrivateKey(fromPkcs8));
        CollectionAssert.AreEqual(rawPublic, ExportPublicKey(fromPkcs8));

        using TAlgorithm fromPem = CreateAlgorithm();
        fromPem.ImportFromPem(original.ExportSubjectPublicKeyInfoPem());
        CollectionAssert.AreEqual(rawPublic, ExportPublicKey(fromPem));
    }

    /// <summary>
    /// Verifies that the algorithm-specific <c>AlgorithmName</c> and <c>SecurityStrengthBits</c> properties report a
    /// non-empty name and one of the NIST-category security strengths.
    /// </summary>
    [TestMethod]
    public void AlgorithmDescriptors_WhenRead_ShouldReportSensibleValues()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.IsFalse(string.IsNullOrEmpty(GetAlgorithmName(algorithm)));
        CollectionAssert.Contains(new[] { 128, 192, 256 }, GetSecurityStrengthBits(algorithm));
    }
}
