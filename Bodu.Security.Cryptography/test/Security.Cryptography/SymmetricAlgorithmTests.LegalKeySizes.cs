using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public abstract partial class SymmetricAlgorithmTests<T>
    {
        /// <summary>
        /// Verifies that LegalKeySizes returns a new instance each call.
        /// </summary>
        [TestMethod]
        public void LegalKeySizes_WhenCalledMultipleTimes_ShouldReturnNewArrayInstances()
        {
            using T algorithm = CreateAlgorithm();
            Assert.AreNotSame(algorithm.LegalKeySizes, algorithm.LegalKeySizes);
        }

        /// <summary>
        /// Verifies that LegalKeySizes define valid MinSize, MaxSize, and SkipSize values.
        /// </summary>
        [TestMethod]
        public void LegalKeySizes_WhenDefined_ShouldHaveValidRanges()
        {
            var keySizes = this.CreateAlgorithm().LegalKeySizes;

            foreach (var keySize in keySizes)
            {
                Assert.IsTrue(keySize.MinSize <= keySize.MaxSize, "MinSize must be less than or equal to MaxSize.");
                Assert.IsTrue(keySize.SkipSize >= 0, "SkipSize must be greater than or equal to zero.");
            }
        }

        /// <summary>
        /// Verifies that LegalKeySizes do not overlap and are unique.
        /// </summary>
        [TestMethod]
        public void LegalKeySizes_WhenDefined_ShouldHaveNonOverlappingValues()
        {
            var keySizes = this.CreateAlgorithm().LegalKeySizes;
            HashSet<int> uniqueSizes = new();

            foreach (var keySize in keySizes)
            {
                for (int size = keySize.MinSize; size <= keySize.MaxSize; size += keySize.SkipSize == 0 ? int.MaxValue : keySize.SkipSize)
                {
                    Assert.IsTrue(uniqueSizes.Add(size), $"Duplicate or overlapping key size detected: {size}.");
                }
            }
        }
    }
}