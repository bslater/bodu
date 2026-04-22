// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BernsteinTests.InitialValue.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    public partial class BernsteinTests
    {
        /// <summary>
        /// Verifies that the default InitialValue is set to Bernstein.DefaultInitialValue.
        /// </summary>
        [TestMethod]
        public void InitialValue_WhenNotModified_ShouldEqualDefaultInitialValue()
        {
            using var algorithm = new Bernstein();
            Assert.AreEqual(Bernstein.DefaultInitialValue, algorithm.InitialValue);
        }

        /// <summary>
        /// Verifies that changing InitialValue before hashing begins affects the resulting hash.
        /// </summary>
        [TestMethod]
        public void InitialValue_WhenSetBeforeHashing_ShouldAffectResult()
        {
            byte[] input = CryptoTestUtilities.SimpleTextAsciiBytes;

            using var algorithm1 = new Bernstein { InitialValue = 1 };
            byte[] hash1 = algorithm1.ComputeHash(input);

            using var algorithm2 = new Bernstein { InitialValue = 2 };
            byte[] hash2 = algorithm2.ComputeHash(input);

            CollectionAssert.AreNotEqual(hash1, hash2);
        }

        /// <summary>
        /// Verifies that setting a new InitialValue after Initialize allows reuse with different results.
        /// </summary>
        [TestMethod]
        public void InitialValue_WhenResetWithInitialize_ShouldAllowReuseWithDifferentInitialValues()
        {
            byte[] input = CryptoTestUtilities.SimpleTextAsciiBytes;

            using var algorithm = new Bernstein();
            algorithm.InitialValue = 42;
            byte[] hash1 = algorithm.ComputeHash(input);

            algorithm.Initialize();
            algorithm.InitialValue = 123;
            byte[] hash2 = algorithm.ComputeHash(input);

            CollectionAssert.AreNotEqual(hash1, hash2);
        }

        /// <summary>
        /// Verifies that using different InitialValues on separate instances leads to different results.
        /// </summary>
        [TestMethod]
        public void InitialValue_WhenMutatedAcrossInstances_ShouldProduceDifferentHashes()
        {
            byte[] input = CryptoTestUtilities.SimpleTextAsciiBytes;

            using var algorithm1 = new Bernstein { InitialValue = 0xDEADBEEF };
            byte[] hash1 = algorithm1.ComputeHash(input);

            using var algorithm2 = new Bernstein { InitialValue = 0xCAFEBABE };
            byte[] hash2 = algorithm2.ComputeHash(input);

            CollectionAssert.AreNotEqual(hash1, hash2);
        }

        /// <summary>
        /// Verifies that resetting the algorithm with the same InitialValue produces identical results.
        /// </summary>
        [TestMethod]
        public void InitialValue_WhenResetToSameValue_ShouldProduceSameHash()
        {
            byte[] input = CryptoTestUtilities.SimpleTextAsciiBytes;

            using var algorithm = new Bernstein { InitialValue = 0xABCDEF };
            byte[] hash1 = algorithm.ComputeHash(input);

            algorithm.InitialValue = 0xABCDEF;
            byte[] hash2 = algorithm.ComputeHash(input);

            CollectionAssert.AreEqual(hash1, hash2);
        }

        /// <summary>
        /// Verifies that setting InitialValue after a full hash computation is accepted.
        /// </summary>
        [TestMethod]
        public void InitialValue_WhenSetAfterHashFinal_ShouldBeAccepted()
        {
            byte[] input = CryptoTestUtilities.SimpleTextAsciiBytes;

            using var algorithm = new Bernstein();
            _ = algorithm.ComputeHash(input);
            algorithm.InitialValue = 0xABCDEFU;

            Assert.AreEqual(0xABCDEFU, algorithm.InitialValue);
        }

        /// <summary>
        /// Verifies that setting InitialValue to zero is accepted and produces a result.
        /// </summary>
        [TestMethod]
        public void InitialValue_WhenSetToZero_ShouldBeAccepted()
        {
            byte[] input = CryptoTestUtilities.SimpleTextAsciiBytes;

            using var algorithm = new Bernstein { InitialValue = 0 };
            byte[] hash = algorithm.ComputeHash(input);

            Assert.IsNotNull(hash);
            Assert.AreEqual(4, hash.Length); // 32-bit result
        }

        /// <summary>
        /// Verifies that setting InitialValue to <see cref="UInt32.MaxValue" /> is accepted and produces a result.
        /// </summary>
        [TestMethod]
        public void InitialValue_WhenSetToMaxValue_ShouldBeAccepted()
        {
            byte[] input = CryptoTestUtilities.SimpleTextAsciiBytes;

            using var algorithm = new Bernstein { InitialValue = UInt32.MaxValue };
            byte[] hash = algorithm.ComputeHash(input);

            Assert.IsNotNull(hash);
        }
    }
}