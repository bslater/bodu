// -----------------------------------------------------------------------
// <copyright file="Bernstein.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Bodu.Infrastructure;

using System.Text;

namespace Bodu.Security.Cryptography
{
    public partial class TigerTests
    {
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants), DynamicDataSourceType.Method)]
        public void AlgorithmName_WhenUsingVariant_ShouldReturnCorrectlyFormattedString(TigerVariant variant)
        {
            using var algorithm = this.CreateAlgorithm(variant);

            Assert.AreEqual($"Tiger/{algorithm.HashSize}", algorithm.AlgorithmName);
        }
    }
}