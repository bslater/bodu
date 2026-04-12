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
        /// Verifies that the declared <see cref="HashAlgorithm.HashSize" /> matches the bit length of the computed hash.
        /// </summary>
        [TestMethod]
        public void HashSize_Get_DeclaredHashSize_ShouldMatchComputedHashLength()
        {
            using var algorithm = this.CreateAlgorithm();
            byte[] result = algorithm.ComputeHash(Array.Empty<byte>());
            int computedBitLength = result.ToBitLength();
            Assert.AreEqual(computedBitLength, algorithm.HashSize);
        }
    }
}