// -----------------------------------------------------------------------
// <copyright file="CubeHashTests.HashSize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public partial class CubeHashTests
    {
        /// <summary>
        /// Validates that a new instance of <see cref="CubeHash" /> has a default hash size hashValue of 512.
        /// </summary>
        [TestMethod]
        public void HashSize_WhenDefaultConstructed_ShouldBe16()
        {
            var algorithm = new CubeHash();
            Assert.AreEqual(512, algorithm.HashSize);
        }

        /// <summary>
        /// Validates that the hash size hashValue can be set and retrieved before any algorithming operation starts.
        /// </summary>
        [TestMethod]
        public void HashSize_WhenSetBeforeUse_ShouldBeRetained()
        {
            var algorithm = new CubeHash { HashSize = 32 };
            Assert.AreEqual(32, algorithm.HashSize);
        }

        /// <summary>
        /// Ensures that setting <see cref="CubeHash.HashSize" /> after a algorithm computation has started does not throw an exception.
        /// </summary>
        [TestMethod]
        public void HashSize_WhenSetAfterHashing_ShouldNotThrow()
        {
            var algorithm = new CubeHash();
            byte[] input = new byte[] { 1, 2, 3 };

            algorithm.ComputeHash(input);

            // Change the hash size hashValue after the first computation, and perform the second algorithm computation with the new hash
            // size hashValue.
            algorithm.HashSize = 32;
            algorithm.ComputeHash(input);
        }

        /// <summary>
        /// Verifies that setting different values for <see cref="CubeHash.HashSize" /> produces different hash outputs for the same input data.
        /// </summary>
        [TestMethod]
        public void HashSize_WhenDifferentValuesUsed_ShouldProduceDifferentHashes()
        {
            byte[] input = new byte[] { 0x10, 0x20, 0x30 };

            var algorithmA = new CubeHash { HashSize = 32 };
            var algorithmB = new CubeHash { HashSize = 64 };

            byte[] resultA = algorithmA.ComputeHash(input);
            byte[] resultB = algorithmB.ComputeHash(input);

            CollectionAssert.AreNotEqual(resultA, resultB);
        }

        /// <summary>
        /// Verifies that setting an invalid hashValue for <see cref="CubeHash.HashSize" /> throws <see cref="ArgumentOutOfRangeException" />.
        /// </summary>
        [DataTestMethod]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(7)]
        [DataRow(513)]
        [DataRow(9)]
        [DataRow(21)]
        [DataRow(45)]
        [DataRow(127)]
        [DataRow(int.MaxValue)]
        public void HashSize_WhenSetToInvalidValue_ShouldThrowExactly(int value)
        {
            using var algorithm = this.CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => algorithm.HashSize = value);
        }

        /// <summary>
        /// Verifies that setting a valid hashValue for <see cref="CubeHash.HashSize" />.
        /// </summary>
        [DataTestMethod]
        [DataRow(8)]
        [DataRow(16)]
        [DataRow(32)]
        [DataRow(64)]
        [DataRow(128)]
        [DataRow(256)]
        [DataRow(512)]
        public void HashSize_WhenSetToValidValue_ShouldBeAssigned(int size)
        {
            using var algorithm = this.CreateAlgorithm();
            int original = algorithm.HashSize;
            algorithm.HashSize = size;

            Assert.AreEqual(size, algorithm.HashSize);
        }

        /// <summary>
        /// Verifies that setting a valid hashValue for <see cref="CubeHash.HashSize" /> updates the internal state.
        /// </summary>
        [TestMethod]
        public void HashSize_WhenSetToValidValue_ShouldUpdateCorrectly()
        {
            using var algorithm = this.CreateAlgorithm();
            int size = 256;
            int original = algorithm.HashSize;
            algorithm.HashSize = size;

            Assert.AreEqual(size, algorithm.HashSize);
            Assert.AreNotEqual(original, algorithm.HashSize);
        }

        /// <summary>
        /// Verifies that modifying <see cref="CubeHash.HashSize" /> does not affect other configuration properties.
        /// </summary>
        [TestMethod]
        public void HashSize_WhenChanged_ShouldNotAffectOtherProperties()
        {
            var algorithm = new CubeHash
            {
                InitializationRounds = 10,
                Rounds = 16,
                FinalizationRounds = 32,
                TransformBlockSize = 32,
                HashSize = 256
            };

            algorithm.HashSize = 128;

            Assert.AreEqual(10, algorithm.InitializationRounds, $"{nameof(CubeHash.InitializationRounds)} should remain unchanged.");
            Assert.AreEqual(16, algorithm.Rounds, $"{nameof(CubeHash.Rounds)} should remain unchanged.");
            Assert.AreEqual(32, algorithm.FinalizationRounds, $"{nameof(CubeHash.FinalizationRounds)} should remain unchanged.");
            Assert.AreEqual(32, algorithm.TransformBlockSize, $"{nameof(CubeHash.TransformBlockSize)} should remain unchanged.");
            Assert.AreEqual(128, algorithm.HashSize, $"{nameof(CubeHash.HashSize)} should update.");
        }
    }
}