// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsymmetricAlgorithmTests{T,T}.GenerateKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class AsymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that generating a key populates both the private and public material with the sizes documented by
    /// the specification.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenCalled_ShouldPopulateBothKeysWithDocumentedSizes()
    {
        AsymmetricAlgorithmSpecification spec = GetSpecification();
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();

        Assert.IsTrue(HasPrivateKey(algorithm));
        Assert.IsTrue(HasPublicKey(algorithm));
        Assert.AreEqual(spec.PrivateKeySizeBytes, ExportPrivateKey(algorithm).Length);
        Assert.AreEqual(spec.PublicKeySizeBytes, ExportPublicKey(algorithm).Length);
    }

    /// <summary>
    /// Verifies that consecutive key generations produce different private key material.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenCalledTwice_ShouldProduceDifferentPrivateKeys()
    {
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();
        byte[] first = ExportPrivateKey(algorithm);

        GenerateKey(algorithm);

        CollectionAssert.AreNotEqual(first, ExportPrivateKey(algorithm));
    }
}
