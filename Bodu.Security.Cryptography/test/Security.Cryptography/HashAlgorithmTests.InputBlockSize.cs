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
        /// Verifies that <see cref="HashAlgorithm.InputBlockSize" /> returns the expected default block size.
        /// </summary>
        [TestMethod]
        public void InputBlockSize_ShouldBeExpectedInputBlockSize()
        {
            using var algorithm = this.CreateAlgorithm();
            Assert.AreEqual(this.ExpectedInputBlockSize, algorithm.InputBlockSize, $"{typeof(TAlgorithm).Name} InputBlockSize should be {this.ExpectedInputBlockSize}.");
        }
    }
}