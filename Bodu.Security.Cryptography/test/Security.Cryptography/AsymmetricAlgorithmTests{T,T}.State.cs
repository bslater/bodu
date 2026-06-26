// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsymmetricAlgorithmTests{T,T}.State.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class AsymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that re-assigning the single legal key size on a generated instance preserves the public key.
    /// </summary>
    [TestMethod]
    public void KeySize_WhenReassignedOnGeneratedInstance_ShouldPreservePublicKey()
    {
        AsymmetricAlgorithmSpecification spec = GetSpecification();
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();
        byte[] publicKey = ExportPublicKey(algorithm);

        algorithm.KeySize = spec.KeySizeDesignator;

        CollectionAssert.AreEqual(publicKey, ExportPublicKey(algorithm));
    }

    /// <summary>
    /// Verifies that re-assigning the single legal key size on a generated instance preserves the private key.
    /// </summary>
    [TestMethod]
    public void KeySize_WhenReassignedOnGeneratedInstance_ShouldPreservePrivateKey()
    {
        AsymmetricAlgorithmSpecification spec = GetSpecification();
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();
        byte[] privateKey = ExportPrivateKey(algorithm);

        algorithm.KeySize = spec.KeySizeDesignator;

        CollectionAssert.AreEqual(privateKey, ExportPrivateKey(algorithm));
    }

    /// <summary>
    /// Verifies that re-assigning the single legal key size on a public-only instance preserves the public key.
    /// </summary>
    [TestMethod]
    public void KeySize_WhenReassignedOnPublicOnlyInstance_ShouldPreservePublicKey()
    {
        AsymmetricAlgorithmSpecification spec = GetSpecification();
        using TAlgorithm donor = CreateAlgorithmWithGeneratedKey();
        using TAlgorithm publicOnly = CreatePublicOnlyAlgorithm(donor);
        byte[] publicKey = ExportPublicKey(publicOnly);

        publicOnly.KeySize = spec.KeySizeDesignator;

        CollectionAssert.AreEqual(publicKey, ExportPublicKey(publicOnly));
    }

    /// <summary>
    /// Verifies that re-assigning the single legal key size on a public-only instance leaves it without a private key.
    /// </summary>
    [TestMethod]
    public void KeySize_WhenReassignedOnPublicOnlyInstance_ShouldRemainWithoutPrivateKey()
    {
        AsymmetricAlgorithmSpecification spec = GetSpecification();
        using TAlgorithm donor = CreateAlgorithmWithGeneratedKey();
        using TAlgorithm publicOnly = CreatePublicOnlyAlgorithm(donor);

        publicOnly.KeySize = spec.KeySizeDesignator;

        Assert.IsFalse(HasPrivateKey(publicOnly));
    }

    /// <summary>
    /// Verifies that re-assigning the single legal key size on a keyless instance leaves it without a public key.
    /// </summary>
    [TestMethod]
    public void KeySize_WhenReassignedOnKeylessInstance_ShouldRemainWithoutPublicKey()
    {
        AsymmetricAlgorithmSpecification spec = GetSpecification();
        using TAlgorithm keyless = CreateAlgorithm();

        keyless.KeySize = spec.KeySizeDesignator;

        Assert.IsFalse(HasPublicKey(keyless));
    }

    /// <summary>
    /// Verifies that mutating the array returned by the public-key export adapter does not affect the stored key.
    /// </summary>
    [TestMethod]
    public void ExportPublicKey_WhenReturnedArrayMutated_ShouldNotAffectStoredKey()
    {
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();

        byte[] firstExport = ExportPublicKey(algorithm);
        byte[] reference = (byte[])firstExport.Clone();
        firstExport[0] ^= 0xFF;

        CollectionAssert.AreEqual(reference, ExportPublicKey(algorithm));
    }

    /// <summary>
    /// Verifies that mutating the array returned by the private-key export adapter does not affect the stored key.
    /// </summary>
    [TestMethod]
    public void ExportPrivateKey_WhenReturnedArrayMutated_ShouldNotAffectStoredKey()
    {
        using TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();

        byte[] firstExport = ExportPrivateKey(algorithm);
        byte[] reference = (byte[])firstExport.Clone();
        firstExport[0] ^= 0xFF;

        CollectionAssert.AreEqual(reference, ExportPrivateKey(algorithm));
    }

    /// <summary>
    /// Verifies the import trust-boundary gate for the public-key adapter: empty and clearly-too-short inputs are
    /// rejected with <see cref="ArgumentException" />.
    /// </summary>
    /// <param name="length">The malformed input length; every asymmetric key is at least 32 bytes.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    public void ImportPublicKey_WhenGivenTooShortInput_ShouldThrowArgumentException(int length)
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentException>(() => ImportPublicKey(algorithm, new byte[length]));
    }

    /// <summary>
    /// Verifies the import trust-boundary gate for the private-key adapter: empty and clearly-too-short inputs are
    /// rejected with <see cref="ArgumentException" />.
    /// </summary>
    /// <param name="length">The malformed input length; every asymmetric key is at least 32 bytes.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    public void ImportPrivateKey_WhenGivenTooShortInput_ShouldThrowArgumentException(int length)
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentException>(() => ImportPrivateKey(algorithm, new byte[length]));
    }
}
