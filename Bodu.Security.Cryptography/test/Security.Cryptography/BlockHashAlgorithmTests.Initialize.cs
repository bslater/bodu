// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockHashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    public abstract partial class BlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
        : HashAlgorithmTests<TTest, TAlgorithm, TVariant>
        where TTest : HashAlgorithmTests<TTest, TAlgorithm, TVariant>, new()
        where TAlgorithm : BlockHashAlgorithm<TAlgorithm>, new()
        where TVariant : Enum
    {
        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.Initialize" /> clears the residual buffer and
        /// total-length tracking so the instance can be reused cleanly after a completed hash.
        /// </summary>
        /// <remarks>
        /// Regression coverage for the family of defects in which residual bytes from a prior
        /// computation leak into the accumulator for the next hash. Hashing identical input twice,
        /// separated by <see cref="HashAlgorithm.Initialize" />, must produce identical digests.
        /// </remarks>
        [TestMethod]
        public virtual void Initialize_AfterHashing_ShouldClearResidualBlockState()
        {
            using var algorithm = this.CreateAlgorithm();

            byte[] input = Enumerable.Range(0, this.ExpectedBlockSizeBytes + (this.ExpectedBlockSizeBytes / 2))
                                     .Select(i => (byte)i)
                                     .ToArray();

            byte[] first = algorithm.ComputeHash(input);
            algorithm.Initialize();
            byte[] second = algorithm.ComputeHash(input);

            CollectionAssert.AreEqual(
                first,
                second,
                "Residual block state was not reset by Initialize — identical inputs produced different digests.");
        }
    }
}
