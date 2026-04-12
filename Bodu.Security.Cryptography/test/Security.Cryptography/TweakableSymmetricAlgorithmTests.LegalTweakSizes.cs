using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public abstract partial class TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
    {
        /// <summary>
        /// Verifies that LegalTweakSizes returns a new instance each call.
        /// </summary>
        [TestMethod]
        public void LegalTweakSizes_WhenCalledMultipleTimes_ShouldReturnNewArrayInstances()
        {
            using TAlgorithm algorithm = CreateAlgorithm();

            Assert.AreNotSame(algorithm.LegalTweakSizes, algorithm.LegalTweakSizes);
        }

        /// <summary>
        /// Verifies that LegalTweakSizes do not overlap and are unique.
        /// </summary>
        [TestMethod]
        public void LegalTweakSizes_WhenDefined_ShouldHaveNonOverlappingValues()
        {
            var blockSizes = this.CreateAlgorithm().LegalTweakSizes;
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
        /// Verifies that LegalTweakSizes define valid MinSize, MaxSize, and SkipSize values.
        /// </summary>
        [TestMethod]
        public void LegalTweakSizes_WhenDefined_ShouldHaveValidRanges()
        {
            var blockSizes = this.CreateAlgorithm().LegalTweakSizes;

            foreach (var blockSize in blockSizes)
            {
                Assert.IsTrue(blockSize.MinSize <= blockSize.MaxSize, "MinSize must be less than or equal to MaxSize.");
                Assert.IsTrue(blockSize.SkipSize >= 0, "SkipSize must be greater than or equal to zero.");
            }
        }
    }
}