// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.LegalKeySizes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.LegalKeySizes" /> returns a new instance each call.
    /// </summary>
    [TestMethod]
    public void LegalKeySizes_WhenCalledMultipleTimes_ShouldReturnNewArrayInstances()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        Assert.AreNotSame(algorithm.LegalKeySizes, algorithm.LegalKeySizes);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.LegalKeySizes" /> define valid MinSize, MaxSize, and SkipSize values.
    /// </summary>
    [TestMethod]
    public void LegalKeySizes_WhenDefined_ShouldHaveValidRanges()
    {
        KeySizes[] keySizes = CreateAlgorithm().LegalKeySizes;

        foreach (KeySizes keySize in keySizes)
        {
            Assert.IsTrue(keySize.MinSize <= keySize.MaxSize, "MinSize must be less than or equal to MaxSize.");
            Assert.IsTrue(keySize.SkipSize >= 0, "SkipSize must be greater than or equal to zero.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.LegalKeySizes" /> do not overlap and are unique.
    /// </summary>
    [TestMethod]
    public void LegalKeySizes_WhenDefined_ShouldHaveNonOverlappingValues()
    {
        KeySizes[] keySizes = CreateAlgorithm().LegalKeySizes;
        HashSet<int> uniqueSizes = new();

        foreach (KeySizes keySize in keySizes)
        {
            for (int size = keySize.MinSize; size <= keySize.MaxSize; size += keySize.SkipSize == 0 ? int.MaxValue : keySize.SkipSize)
            {
                Assert.IsTrue(uniqueSizes.Add(size), $"Duplicate or overlapping key size detected: {size}.");
            }
        }
    }
}
