// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmTests.LegalTweakSizes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Security.Cryptography
{
    public abstract partial class TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
    {
        /// <summary>
        /// Verifies that <see cref="TweakableSymmetricAlgorithm.LegalTweakSizes" /> returns a new instance each call.
        /// </summary>
        [TestMethod]
        public void LegalTweakSizes_WhenCalledMultipleTimes_ShouldReturnNewArrayInstances()
        {
            using TAlgorithm algorithm = CreateAlgorithm();

            Assert.AreNotSame(algorithm.LegalTweakSizes, algorithm.LegalTweakSizes);
        }

        /// <summary>
        /// Verifies that <see cref="TweakableSymmetricAlgorithm.LegalTweakSizes" /> do not overlap and are unique.
        /// </summary>
        [TestMethod]
        public void LegalTweakSizes_WhenDefined_ShouldHaveNonOverlappingValues()
        {
            var blockSizes = CreateAlgorithm().LegalTweakSizes;
            HashSet<int> uniqueSizes = new();

            foreach (var blockSize in blockSizes)
            {
                for (int size = blockSize.MinSize; size <= blockSize.MaxSize; size += blockSize.SkipSize == 0 ? int.MaxValue : blockSize.SkipSize)
                {
                    Assert.IsTrue(uniqueSizes.Add(size), $"Duplicate or overlapping block size detected: {size}.");
                }
            }
        }

        /// <summary>
        /// Verifies that <see cref="TweakableSymmetricAlgorithm.LegalTweakSizes" /> define valid MinSize, MaxSize, and SkipSize values.
        /// </summary>
        [TestMethod]
        public void LegalTweakSizes_WhenDefined_ShouldHaveValidRanges()
        {
            var blockSizes = CreateAlgorithm().LegalTweakSizes;

            foreach (var blockSize in blockSizes)
            {
                Assert.IsTrue(blockSize.MinSize <= blockSize.MaxSize, "MinSize must be less than or equal to MaxSize.");
                Assert.IsTrue(blockSize.SkipSize >= 0, "SkipSize must be greater than or equal to zero.");
            }
        }
    }
}