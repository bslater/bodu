// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.Specification.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that a freshly constructed <typeparamref name="TAlgorithm" /> instance reports the block size declared in
    /// <see cref="SymmetricAlgorithmSpecification.BlockSizeBits" />.
    /// </summary>
    [TestMethod]
    public void BlockSize_WhenCreated_ShouldMatchSpecification()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SymmetricAlgorithmSpecification spec = GetSpecification();
        Assert.AreEqual(spec.BlockSizeBits, algorithm.BlockSize);
    }

    /// <summary>
    /// Verifies that a freshly constructed <typeparamref name="TAlgorithm" /> instance reports the key size declared in
    /// <see cref="SymmetricAlgorithmSpecification.DefaultKeySizeBits" />.
    /// </summary>
    [TestMethod]
    public void KeySize_WhenCreated_ShouldMatchSpecification()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SymmetricAlgorithmSpecification spec = GetSpecification();
        Assert.AreEqual(spec.DefaultKeySizeBits, algorithm.KeySize);
    }

    /// <summary>
    /// Verifies that a freshly constructed <typeparamref name="TAlgorithm" /> instance reports the cipher mode declared in
    /// <see cref="SymmetricAlgorithmSpecification.DefaultMode" />.
    /// </summary>
    [TestMethod]
    public void Mode_WhenCreated_ShouldMatchSpecification()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SymmetricAlgorithmSpecification spec = GetSpecification();
        Assert.AreEqual(spec.DefaultMode, algorithm.Mode);
    }

    /// <summary>
    /// Verifies that a freshly constructed <typeparamref name="TAlgorithm" /> instance reports the padding mode declared in
    /// <see cref="SymmetricAlgorithmSpecification.DefaultPadding" />.
    /// </summary>
    [TestMethod]
    public void Padding_WhenCreated_ShouldMatchSpecification()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SymmetricAlgorithmSpecification spec = GetSpecification();
        Assert.AreEqual(spec.DefaultPadding, algorithm.Padding);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateEncryptor(byte[], byte[])" /> succeeds and returns a non-null
    /// transform for each key size listed in <see cref="SymmetricAlgorithmSpecification.LegalKeySizesBits" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LegalKeySizesBitsData))]
    public void CreateEncryptor_ForEachLegalKeySize_ShouldSucceed(int keySizeBits)
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.KeySize = keySizeBits;
        algorithm.GenerateKey();
        algorithm.GenerateIV();

        using ICryptoTransform transform = algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> succeeds and returns a non-null
    /// transform for each key size listed in <see cref="SymmetricAlgorithmSpecification.LegalKeySizesBits" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LegalKeySizesBitsData))]
    public void CreateDecryptor_ForEachLegalKeySize_ShouldSucceed(int keySizeBits)
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.KeySize = keySizeBits;
        algorithm.GenerateKey();
        algorithm.GenerateIV();

        using ICryptoTransform transform = algorithm.CreateDecryptor(algorithm.Key, algorithm.IV);
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateEncryptor(byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> when the supplied IV is one byte shorter than the block size, exercised for each
    /// key size listed in <see cref="SymmetricAlgorithmSpecification.LegalKeySizesBits" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LegalKeySizesBitsData))]
    public void CreateEncryptor_WhenIvIsTooShort_ForEachLegalKeySize_ShouldThrowCryptographicException(int keySizeBits)
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.KeySize = keySizeBits;
        algorithm.GenerateKey();

        var badIv = new byte[(algorithm.BlockSize / 8) - 1];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateEncryptor(algorithm.Key, badIv);
        });
    }
}
