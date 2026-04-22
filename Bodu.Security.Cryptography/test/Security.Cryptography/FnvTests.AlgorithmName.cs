// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FnvTests.AlgorithmName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    public abstract partial class FnvTests<TTest, TAlgorithm>
    {
        /// <summary>
        /// Verifies that <see cref="Fnv.AlgorithmName" />, when UsingVariant, returns the expected value.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants), DynamicDataSourceType.Method)]
        public void AlgorithmName_WhenUsingVariant_ShouldReturnCorrectlyFormattedString(SingleTestVariant variant)
        {
            using var algorithm = CreateAlgorithm(variant);
            string typeName = algorithm.GetType().Name;

            // Infer whether this is FNV-1a based on the type name
            string expectedVariant = typeName.Contains("1a", StringComparison.OrdinalIgnoreCase) ? "1a" : "1";
            string expected = $"FNV-{expectedVariant}-{algorithm.HashSize}";

            Assert.AreEqual(expected, algorithm.AlgorithmName);
        }
    }
}