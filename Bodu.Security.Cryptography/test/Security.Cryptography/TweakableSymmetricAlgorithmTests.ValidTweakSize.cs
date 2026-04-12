using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public abstract partial class TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
    {
        public static IEnumerable<object[]> ValidTweakSizesTestData()
        {
            using var instance = new TTest().CreateAlgorithm();

            foreach (var size in instance.LegalTweakSizes)
            {
                for (int i = size.MinSize; i < size.MaxSize; i += size.SkipSize == 0 ? int.MaxValue : size.SkipSize)
                {
                    yield return new object[] { i };
                }
            }
        }

        /// <summary>
        /// Verifies that <see cref="TweakableSymmetricAlgorithm.ValidTweakSize(int)" /> returns true when the specified length matches
        /// <c>MinSize</c> and <c>SkipSize</c> is 0.
        /// </summary>
        [TestMethod]
        public void ValidTweakSize_WhenSkipSizeIsZeroAndLengthMatchesMinSize_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();

            var keySize = algorithm.LegalTweakSizes.FirstOrDefault(k => k.SkipSize == 0);
            if (keySize is null)
            {
                Assert.Inconclusive("This algorithm does not define any KeySizes with SkipSize == 0.");
            }

            bool isValid = algorithm.ValidTweakSize(keySize.MinSize);

            Assert.IsTrue(isValid, $"Expected ValidTweakSize({keySize.MinSize}) to be true when SkipSize is 0.");
        }

        /// <summary>
        /// Verifies that ValidTweakSize returns true for supported sizes.
        /// </summary>
        [DataTestMethod]
        [DynamicData(nameof(ValidTweakSizesTestData), DynamicDataSourceType.Method)]
        public void ValidTweakSize_WhenSupportedSize_ShouldReturnTrue(int size)
        {
            using var algorithm = CreateAlgorithm();

            Assert.IsTrue(algorithm.ValidTweakSize(size), $"Expected valid tweak size: {size}");
        }

        /// <summary>
        /// Verifies that ValidTweakSize returns false for unsupported sizes.
        /// </summary>
        [TestMethod]
        public void ValidTweakSize_WhenUnsupportedSize_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            Assert.IsFalse(algorithm.ValidTweakSize(999)); // intentionally unsupported
        }
    }
}