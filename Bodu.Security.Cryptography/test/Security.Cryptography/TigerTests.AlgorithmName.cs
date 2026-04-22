// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerTests.AlgorithmName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    public partial class TigerTests
    {
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants), DynamicDataSourceType.Method)]
        public void AlgorithmName_WhenUsingVariant_ShouldReturnCorrectlyFormattedString(TigerVariant variant)
        {
            using var algorithm = CreateAlgorithm(variant);

            Assert.AreEqual($"Tiger/{algorithm.HashSize}", algorithm.AlgorithmName);
        }
    }
}