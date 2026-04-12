// -----------------------------------------------------------------------
// <copyright file="Bernstein.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Bodu.Infrastructure;

using System.Text;

namespace Bodu.Security.Cryptography
{
    public abstract partial class FletcherTests<TTest, TAlgorithm>
    {
        [TestMethod]
        public void AlgorithmName_WhenUsingVariant_ShouldReturnCorrectlyFormattedString()
        {
            using var algorithm = this.CreateAlgorithm();

            Assert.AreEqual($"Fletcher-{algorithm.HashSize}", algorithm.AlgorithmName);
        }
    }
}