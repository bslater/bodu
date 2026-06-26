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
    /// Verifies that the inherited XML key-format members throw <see cref="NotSupportedException" /> rather than the
    /// default <see cref="NotImplementedException" /> base behaviour.
    /// </summary>
    [TestMethod]
    public void XmlKeyFormatMembers_WhenInvoked_ShouldThrowNotSupportedException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ToXmlString(false); });
        Assert.ThrowsExactly<NotSupportedException>(() => { algorithm.FromXmlString("<X/>"); });
    }

    /// <summary>
    /// Verifies that the inherited export members for the standardized ASN.1 key containers throw
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Pkcs8AndSpkiExportMembers_WhenInvoked_ShouldThrowNotSupportedException()
    {
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();
        byte[] buffer = new byte[8192];

        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ExportPkcs8PrivateKey(); });
        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ExportPkcs8PrivateKeyPem(); });
        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ExportSubjectPublicKeyInfo(); });
        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.ExportSubjectPublicKeyInfoPem(); });
        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.TryExportPkcs8PrivateKey(buffer, out _); });
        Assert.ThrowsExactly<NotSupportedException>(() => { _ = algorithm.TryExportSubjectPublicKeyInfo(buffer, out _); });
    }

    /// <summary>
    /// Verifies that the inherited import members for the standardized ASN.1 key containers throw
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Pkcs8AndSpkiImportMembers_WhenInvoked_ShouldThrowNotSupportedException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        byte[] source = new byte[64];

        Assert.ThrowsExactly<NotSupportedException>(() => { algorithm.ImportPkcs8PrivateKey(source, out _); });
        Assert.ThrowsExactly<NotSupportedException>(() => { algorithm.ImportSubjectPublicKeyInfo(source, out _); });
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
