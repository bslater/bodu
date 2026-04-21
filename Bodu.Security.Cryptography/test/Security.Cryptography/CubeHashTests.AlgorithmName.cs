// -----------------------------------------------------------------------
// <copyright file="Bernstein.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    public partial class CubeHashTests
    {
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants), DynamicDataSourceType.Method)]
        public void AlgorithmName_WhenUsingVariant_ShouldReturnCorrectlyFormattedString(CubeHashVariants variant)
        {
            using var algorithm = CreateAlgorithm(variant);
            string expected = GetAlgorithmName(algorithm);

            Assert.AreEqual(expected, algorithm.AlgorithmName);
        }

        [TestMethod]
        public void AlgorithmName_WhenUsingCustomRounds_ShouldReturnCorrectlyFormattedString()
        {
            using var algorithm = new CubeHash
            {
                InitializationRounds = 3,
                Rounds = 5
            };
            string expected = GetAlgorithmName(algorithm);

            Assert.AreEqual(expected, algorithm.AlgorithmName);
        }

        private static string GetAlgorithmName(CubeHash algorithm) =>
            $"CubeHash{algorithm.InitializationRounds}+{algorithm.Rounds}/{algorithm.TransformBlockSize}+{algorithm.FinalizationRounds}-{algorithm.HashSize}";
    }
}