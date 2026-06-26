// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsymmetricAlgorithmTests{T,T}.KeyFormats.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contract tests asserting, one member per test, that the inherited XML and encrypted-PKCS#8 key-format members
/// throw <see cref="NotSupportedException" /> for every algorithm; that the plain PKCS#8 / SubjectPublicKeyInfo members
/// throw for algorithms without RFC 8410 support and round-trip for those with it; and that the algorithm descriptors
/// report sensible values.
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
    /// Verifies that <c>ToXmlString</c> throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void ToXmlString_WhenInvoked_ShouldThrowNotSupportedException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ToXmlString(false); });
    }

    /// <summary>
    /// Verifies that <c>FromXmlString</c> throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void FromXmlString_WhenInvoked_ShouldThrowNotSupportedException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<NotSupportedException>(() => { algorithm.FromXmlString("<X/>"); });
    }

    /// <summary>
    /// Verifies that <c>ExportEncryptedPkcs8PrivateKey</c> throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void ExportEncryptedPkcs8PrivateKey_WhenInvoked_ShouldThrowNotSupportedException()
    {
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();
        var pbe = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1);

        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ExportEncryptedPkcs8PrivateKey("pw", pbe); });
    }

    /// <summary>
    /// Verifies that <c>TryExportEncryptedPkcs8PrivateKey</c> throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void TryExportEncryptedPkcs8PrivateKey_WhenInvoked_ShouldThrowNotSupportedException()
    {
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();
        var pbe = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1);

        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.TryExportEncryptedPkcs8PrivateKey("pw", pbe, new byte[8192], out _); });
    }

    /// <summary>
    /// Verifies that <c>ImportEncryptedPkcs8PrivateKey</c> throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void ImportEncryptedPkcs8PrivateKey_WhenInvoked_ShouldThrowNotSupportedException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<NotSupportedException>(() => { algorithm.ImportEncryptedPkcs8PrivateKey("pw", new byte[64], out _); });
    }

    /// <summary>
    /// Verifies that <c>ExportPkcs8PrivateKey</c> throws <see cref="NotSupportedException" /> when the algorithm does
    /// not support the standardized key containers.
    /// </summary>
    [TestMethod]
    public void ExportPkcs8PrivateKey_WhenContainersUnsupported_ShouldThrowNotSupportedException()
    {
        if (SupportsStandardKeyContainers)
            return;

        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();

        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ExportPkcs8PrivateKey(); });
    }

    /// <summary>
    /// Verifies that <c>ExportPkcs8PrivateKeyPem</c> throws <see cref="NotSupportedException" /> when the algorithm
    /// does not support the standardized key containers.
    /// </summary>
    [TestMethod]
    public void ExportPkcs8PrivateKeyPem_WhenContainersUnsupported_ShouldThrowNotSupportedException()
    {
        if (SupportsStandardKeyContainers)
            return;

        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();

        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ExportPkcs8PrivateKeyPem(); });
    }

    /// <summary>
    /// Verifies that <c>ExportSubjectPublicKeyInfo</c> throws <see cref="NotSupportedException" /> when the algorithm
    /// does not support the standardized key containers.
    /// </summary>
    [TestMethod]
    public void ExportSubjectPublicKeyInfo_WhenContainersUnsupported_ShouldThrowNotSupportedException()
    {
        if (SupportsStandardKeyContainers)
            return;

        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();

        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ExportSubjectPublicKeyInfo(); });
    }

    /// <summary>
    /// Verifies that <c>ExportSubjectPublicKeyInfoPem</c> throws <see cref="NotSupportedException" /> when the
    /// algorithm does not support the standardized key containers.
    /// </summary>
    [TestMethod]
    public void ExportSubjectPublicKeyInfoPem_WhenContainersUnsupported_ShouldThrowNotSupportedException()
    {
        if (SupportsStandardKeyContainers)
            return;

        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();

        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ExportSubjectPublicKeyInfoPem(); });
    }

    /// <summary>
    /// Verifies that <c>TryExportPkcs8PrivateKey</c> throws <see cref="NotSupportedException" /> when the algorithm
    /// does not support the standardized key containers.
    /// </summary>
    [TestMethod]
    public void TryExportPkcs8PrivateKey_WhenContainersUnsupported_ShouldThrowNotSupportedException()
    {
        if (SupportsStandardKeyContainers)
            return;

        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();

        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.TryExportPkcs8PrivateKey(new byte[8192], out _); });
    }

    /// <summary>
    /// Verifies that <c>TryExportSubjectPublicKeyInfo</c> throws <see cref="NotSupportedException" /> when the
    /// algorithm does not support the standardized key containers.
    /// </summary>
    [TestMethod]
    public void TryExportSubjectPublicKeyInfo_WhenContainersUnsupported_ShouldThrowNotSupportedException()
    {
        if (SupportsStandardKeyContainers)
            return;

        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();

        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.TryExportSubjectPublicKeyInfo(new byte[8192], out _); });
    }

    /// <summary>
    /// Verifies that <c>ImportPkcs8PrivateKey</c> throws <see cref="NotSupportedException" /> when the algorithm does
    /// not support the standardized key containers.
    /// </summary>
    [TestMethod]
    public void ImportPkcs8PrivateKey_WhenContainersUnsupported_ShouldThrowNotSupportedException()
    {
        if (SupportsStandardKeyContainers)
            return;

        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<NotSupportedException>(() => { algorithm.ImportPkcs8PrivateKey(new byte[64], out _); });
    }

    /// <summary>
    /// Verifies that <c>ImportSubjectPublicKeyInfo</c> throws <see cref="NotSupportedException" /> when the algorithm
    /// does not support the standardized key containers.
    /// </summary>
    [TestMethod]
    public void ImportSubjectPublicKeyInfo_WhenContainersUnsupported_ShouldThrowNotSupportedException()
    {
        if (SupportsStandardKeyContainers)
            return;

        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<NotSupportedException>(() => { algorithm.ImportSubjectPublicKeyInfo(new byte[64], out _); });
    }

    /// <summary>
    /// Verifies that, for algorithms that support the RFC 8410 containers, a SubjectPublicKeyInfo round-trip recovers
    /// the raw public key.
    /// </summary>
    [TestMethod]
    public void ImportSubjectPublicKeyInfo_WhenContainersSupported_ShouldRecoverPublicKey()
    {
        if (!SupportsStandardKeyContainers)
            return;

        using TAlgorithm original = CreateAlgorithmWithGeneratedKey();
        byte[] rawPublic = ExportPublicKey(original);

        using TAlgorithm imported = CreateAlgorithm();
        imported.ImportSubjectPublicKeyInfo(original.ExportSubjectPublicKeyInfo(), out _);

        CollectionAssert.AreEqual(rawPublic, ExportPublicKey(imported));
    }

    /// <summary>
    /// Verifies that, for algorithms that support the RFC 8410 containers, a PKCS#8 round-trip recovers the raw
    /// private key.
    /// </summary>
    [TestMethod]
    public void ImportPkcs8PrivateKey_WhenContainersSupported_ShouldRecoverPrivateKey()
    {
        if (!SupportsStandardKeyContainers)
            return;

        using TAlgorithm original = CreateAlgorithmWithGeneratedKey();
        byte[] rawPrivate = ExportPrivateKey(original);

        using TAlgorithm imported = CreateAlgorithm();
        imported.ImportPkcs8PrivateKey(original.ExportPkcs8PrivateKey(), out _);

        CollectionAssert.AreEqual(rawPrivate, ExportPrivateKey(imported));
    }

    /// <summary>
    /// Verifies that, for algorithms that support the RFC 8410 containers, importing PKCS#8 also recovers the matching
    /// public key.
    /// </summary>
    [TestMethod]
    public void ImportPkcs8PrivateKey_WhenContainersSupported_ShouldRecoverPublicKey()
    {
        if (!SupportsStandardKeyContainers)
            return;

        using TAlgorithm original = CreateAlgorithmWithGeneratedKey();
        byte[] rawPublic = ExportPublicKey(original);

        using TAlgorithm imported = CreateAlgorithm();
        imported.ImportPkcs8PrivateKey(original.ExportPkcs8PrivateKey(), out _);

        CollectionAssert.AreEqual(rawPublic, ExportPublicKey(imported));
    }

    /// <summary>
    /// Verifies that, for algorithms that support the RFC 8410 containers, a SubjectPublicKeyInfo PEM round-trip
    /// recovers the raw public key.
    /// </summary>
    [TestMethod]
    public void ImportFromPem_WhenContainersSupported_ShouldRecoverPublicKey()
    {
        if (!SupportsStandardKeyContainers)
            return;

        using TAlgorithm original = CreateAlgorithmWithGeneratedKey();
        byte[] rawPublic = ExportPublicKey(original);

        using TAlgorithm imported = CreateAlgorithm();
        imported.ImportFromPem(original.ExportSubjectPublicKeyInfoPem());

        CollectionAssert.AreEqual(rawPublic, ExportPublicKey(imported));
    }

    /// <summary>
    /// Verifies that <c>AlgorithmName</c> reports a non-empty value.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenRead_ShouldNotBeEmpty()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.IsFalse(string.IsNullOrEmpty(GetAlgorithmName(algorithm)));
    }

    /// <summary>
    /// Verifies that <c>SecurityStrengthBits</c> reports one of the NIST-category security strengths.
    /// </summary>
    [TestMethod]
    public void SecurityStrengthBits_WhenRead_ShouldBeNistCategoryStrength()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        CollectionAssert.Contains(new[] { 128, 192, 256 }, GetSecurityStrengthBits(algorithm));
    }
}
