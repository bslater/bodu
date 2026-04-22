// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests.AlgorithmName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    public abstract partial class SipHashTests<TTest, TAlgorithm>
    {
        /// <summary>
        /// Verifies that Algorithm Name, when Using Variant, returns Correctly Formatted String.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants), DynamicDataSourceType.Method)]
        public void AlgorithmName_WhenUsingVariant_ShouldReturnCorrectlyFormattedString(SipHashVariant variant)
        {
            using var algorithm = CreateAlgorithm(variant);
            string expected = GetAlgorithmName(algorithm);

            Assert.AreEqual(expected, algorithm.AlgorithmName);
        }

        /// <summary>
        /// Verifies that Algorithm Name, when Using Custom Rounds, returns Correctly Formatted String.
        /// </summary>
        [TestMethod]
        public void AlgorithmName_WhenUsingCustomRounds_ShouldReturnCorrectlyFormattedString()
        {
            using var algorithm = new TAlgorithm
            {
                CompressionRounds = 3,
                FinalizationRounds = 5
            };
            string expected = GetAlgorithmName(algorithm);

            Assert.AreEqual(expected, algorithm.AlgorithmName);
        }

        private static string GetAlgorithmName(SipHash<TAlgorithm> algorithm) =>
            $"SipHash-{algorithm.CompressionRounds}-{algorithm.FinalizationRounds}-{algorithm.HashSize}";
    }
}