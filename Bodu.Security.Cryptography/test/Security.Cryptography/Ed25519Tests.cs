// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Ed25519" /> signature algorithm covering construction defaults and the
/// <see cref="AsymmetricAlgorithm" /> surface.
/// </summary>
[TestClass]
public partial class Ed25519Tests
{
    /// <summary>
    /// Verifies that a newly constructed <see cref="Ed25519" /> instance reports the fixed 256-bit key size and
    /// holds no key material.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldReportFixedKeySizeAndNoKeys()
    {
        using var algorithm = new Ed25519();

        Assert.AreEqual(256, algorithm.KeySize);
        Assert.IsFalse(algorithm.HasPrivateKey);
        Assert.IsFalse(algorithm.HasPublicKey);
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519.Create" /> returns a fresh instance with default state.
    /// </summary>
    [TestMethod]
    public void Create_WhenCalled_ShouldReturnNewInstanceWithDefaults()
    {
        using Ed25519 algorithm = Ed25519.Create();

        Assert.IsNotNull(algorithm);
        Assert.AreEqual(256, algorithm.KeySize);
        Assert.IsFalse(algorithm.HasPrivateKey);
    }

    /// <summary>
    /// Verifies that <see cref="AsymmetricAlgorithm.LegalKeySizes" /> exposes exactly one fixed 256-bit entry.
    /// </summary>
    [TestMethod]
    public void LegalKeySizes_WhenRead_ShouldContainSingleFixed256BitEntry()
    {
        using var algorithm = new Ed25519();

        KeySizes[] sizes = algorithm.LegalKeySizes;

        Assert.AreEqual(1, sizes.Length);
        Assert.AreEqual(256, sizes[0].MinSize);
        Assert.AreEqual(256, sizes[0].MaxSize);
        Assert.AreEqual(0, sizes[0].SkipSize);
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519.SignatureAlgorithm" /> identifies the algorithm and
    /// <see cref="Ed25519.KeyExchangeAlgorithm" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void SignatureAlgorithm_WhenRead_ShouldReturnEd25519AndNullKeyExchangeAlgorithm()
    {
        using var algorithm = new Ed25519();

        Assert.AreEqual("Ed25519", algorithm.SignatureAlgorithm);
        Assert.IsNull(algorithm.KeyExchangeAlgorithm);
    }

    /// <summary>
    /// Verifies that the PKCS#8 / SubjectPublicKeyInfo export members inherited from
    /// <see cref="AsymmetricAlgorithm" /> retain their unimplemented base behavior, because only raw RFC 8032 key
    /// encodings are supported.
    /// </summary>
    [TestMethod]
    public void ExportSubjectPublicKeyInfo_WhenCalled_ShouldThrowNotImplementedException()
    {
        using var algorithm = new Ed25519();
        algorithm.GenerateKey();

        var ex = Assert.ThrowsExactly<NotImplementedException>(() =>
        {
            _ = algorithm.ExportSubjectPublicKeyInfo();
        });

        Assert.IsNotNull(ex);
    }
}
