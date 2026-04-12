// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
    {
        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.OutputBlockSize" /> returns the expected default block size.
        /// </summary>
        [TestMethod]
        public void OutputBlockSize_ShouldBeExpectedOutputBlockSize()
        {
            using var algorithm = this.CreateAlgorithm();
            Assert.AreEqual(this.ExpectedOutputBlockSize, algorithm.OutputBlockSize, $"{typeof(TAlgorithm).Name} OutputBlockSize should be {this.ExpectedOutputBlockSize}.");
        }
    }
}