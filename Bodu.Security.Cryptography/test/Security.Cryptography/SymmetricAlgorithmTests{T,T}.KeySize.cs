// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests{T,T}.KeySize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that the Key property is not null upon algorithm creation.
    /// </summary>
    [TestMethod]
    public void KeySize_WhenAccessed_ShouldNotBeDefault()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SymmetricAlgorithmSpecification specification = GetSpecification();
        Assert.AreEqual(specification.DefaultKeySizeBits, algorithm.KeySize);
    }

    /// <summary>
    /// Verifies that assigning a key whose length in bits is not among <see cref="SymmetricAlgorithm.LegalKeySizes" />
    /// throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(InvalidKeySizeBitsData))]
    public void KeySize_WhenSetToInvalidSize_ShouldThrowExactly(int keySize)
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.KeySize = keySize;
        },
        $"Setting a {keySize}-bit key should throw CryptographicException " +
        $"for {typeof(TAlgorithm).Name}.");
    }

    /// <summary>
    /// Verifies that setting <see cref="SymmetricAlgorithm.KeySize" /> returns the same value on subsequent get.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LegalKeySizeBitsData))]
    public void KeySize_WhenSet_ShouldReturnSameValueOnGet(int keySize)
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        algorithm.KeySize = keySize;

        Assert.AreEqual(
            keySize,
            algorithm.KeySize,
            $"Algorithm {typeof(TAlgorithm).Name} did not return the key size that was set.");
    }
}
