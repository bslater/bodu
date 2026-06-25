// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsymmetricAlgorithmTests{T,T}.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class AsymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that a freshly constructed instance reports the specification's key-size designator, algorithm
    /// names, and no key material.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldReportSpecificationDefaultsAndNoKeys()
    {
        AsymmetricAlgorithmSpecification spec = GetSpecification();
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.AreEqual(spec.KeySizeDesignator, algorithm.KeySize);
        Assert.AreEqual(spec.KeyExchangeAlgorithmName, algorithm.KeyExchangeAlgorithm);
        Assert.AreEqual(spec.SignatureAlgorithmName, algorithm.SignatureAlgorithm);
        Assert.IsFalse(HasPrivateKey(algorithm));
        Assert.IsFalse(HasPublicKey(algorithm));
    }

    /// <summary>
    /// Verifies that <see cref="AsymmetricAlgorithm.LegalKeySizes" /> exposes exactly one fixed entry equal to the
    /// specification's key-size designator.
    /// </summary>
    [TestMethod]
    public void LegalKeySizes_WhenRead_ShouldContainSingleFixedEntry()
    {
        AsymmetricAlgorithmSpecification spec = GetSpecification();
        using TAlgorithm algorithm = CreateAlgorithm();

        KeySizes[] sizes = algorithm.LegalKeySizes;

        Assert.HasCount(1, sizes);
        Assert.AreEqual(spec.KeySizeDesignator, sizes[0].MinSize);
        Assert.AreEqual(spec.KeySizeDesignator, sizes[0].MaxSize);
        Assert.AreEqual(0, sizes[0].SkipSize);
    }

    /// <summary>
    /// Verifies that re-assigning the one legal value through <see cref="AsymmetricAlgorithm.KeySize" /> succeeds
    /// while any other value throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void KeySize_WhenSetToLegalOrUnsupportedValue_ShouldAcceptOrThrowCryptographicException()
    {
        AsymmetricAlgorithmSpecification spec = GetSpecification();
        using TAlgorithm algorithm = CreateAlgorithm();

        algorithm.KeySize = spec.KeySizeDesignator;
        Assert.AreEqual(spec.KeySizeDesignator, algorithm.KeySize);

        CryptographicException ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.KeySize = spec.KeySizeDesignator - 1;
        });

        Assert.IsNotNull(ex);
    }

    /// <summary>
    /// Verifies that the PKCS#8 / SubjectPublicKeyInfo export members inherited from
    /// <see cref="AsymmetricAlgorithm" /> retain their unimplemented base behaviour, because the asymmetric types in
    /// this library support only their raw specification key encodings.
    /// </summary>
    [TestMethod]
    public void ExportDerEncodings_WhenCalled_ShouldThrowNotImplementedException()
    {
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();

        Assert.ThrowsExactly<NotImplementedException>(() => { _ = algorithm.ExportSubjectPublicKeyInfo(); });
        Assert.ThrowsExactly<NotImplementedException>(() => { _ = algorithm.ExportPkcs8PrivateKey(); });
    }
}
