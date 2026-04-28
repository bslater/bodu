// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.LegalBlockSizes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.LegalBlockSizes" /> returns a new instance each call.
    /// </summary>
    [TestMethod]
    public void LegalBlockSizes_WhenCalledMultipleTimes_ShouldReturnNewArrayInstances()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        Assert.AreNotSame(algorithm.LegalBlockSizes, algorithm.LegalBlockSizes);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.LegalBlockSizes" /> define valid MinSize, MaxSize, and SkipSize values.
    /// </summary>
    [TestMethod]
    public void LegalBlockSizes_WhenDefined_ShouldHaveValidRanges()
    {
        var blockSizes = CreateAlgorithm().LegalBlockSizes;

        foreach (var blockSize in blockSizes)
        {
            Assert.IsTrue(blockSize.MinSize <= blockSize.MaxSize, "MinSize must be less than or equal to MaxSize.");
            Assert.IsTrue(blockSize.SkipSize >= 0, "SkipSize must be greater than or equal to zero.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.LegalBlockSizes" /> do not overlap and are unique.
    /// </summary>
    [TestMethod]
    public void LegalBlockSizes_WhenDefined_ShouldHaveNonOverlappingValues()
    {
        var blockSizes = CreateAlgorithm().LegalBlockSizes;
        HashSet<int> uniqueSizes = new();

        foreach (var blockSize in blockSizes)
        {
            for (int size = blockSize.MinSize; size <= blockSize.MaxSize; size += blockSize.SkipSize == 0 ? int.MaxValue : blockSize.SkipSize)
            {
                Assert.IsTrue(uniqueSizes.Add(size), $"Duplicate or overlapping block size detected: {size}.");
            }
        }
    }
}
