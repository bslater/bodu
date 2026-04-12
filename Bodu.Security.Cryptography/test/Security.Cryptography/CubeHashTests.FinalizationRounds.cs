// -----------------------------------------------------------------------
// <copyright file="CubeHashTests.FinalizationRounds.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public partial class CubeHashTests
    {
        /// <summary>
        /// Validates that a new instance of <see cref="CubeHash" /> has a default finalization rounds hashValue of 32.
        /// </summary>
        [TestMethod]
        public void FinalizationRounds_WhenDefaultConstructed_ShouldBe32()
        {
            var algorithm = new CubeHash();
            Assert.AreEqual(32, algorithm.FinalizationRounds);
        }

        /// <summary>
        /// Validates that the finalization rounds hashValue can be set and retrieved before any algorithming operation starts.
        /// </summary>
        [TestMethod]
        public void FinalizationRounds_WhenSetBeforeUse_ShouldBeRetained()
        {
            var algorithm = new CubeHash { FinalizationRounds = 64 };
            Assert.AreEqual(64, algorithm.FinalizationRounds);
        }

        /// <summary>
        /// Ensures that setting <see cref="CubeHash.FinalizationRounds" /> after a algorithm computation has started does not throw an exception.
        /// </summary>
        [TestMethod]
        public void FinalizationRounds_WhenSetAfterHashing_ShouldNotThrow()
        {
            var algorithm = new CubeHash();
            byte[] input = new byte[] { 1, 2, 3 };

            algorithm.ComputeHash(input);

            // Change the finalization rounds hashValue after the first computation, and perform the second algorithm computation with the
            // new finalization rounds hashValue.
            algorithm.FinalizationRounds = 64;
            algorithm.ComputeHash(input);
        }

        /// <summary>
        /// Verifies that setting different values for <see cref="CubeHash.FinalizationRounds" /> produces different hash outputs for the
        /// same input data.
        /// </summary>
        [TestMethod]
        public void FinalizationRounds_WhenDifferentValuesUsed_ShouldProduceDifferentHashes()
        {
            byte[] input = new byte[] { 0x10, 0x20, 0x30 };

            var algorithmA = new CubeHash { FinalizationRounds = 32 };
            var algorithmB = new CubeHash { FinalizationRounds = 64 };

            byte[] resultA = algorithmA.ComputeHash(input);
            byte[] resultB = algorithmB.ComputeHash(input);

            CollectionAssert.AreNotEqual(resultA, resultB);
        }

        /// <summary>
        /// Verifies that setting an invalid hashValue for <see cref="CubeHash.FinalizationRounds" /> throws <see cref="ArgumentOutOfRangeException" />.
        /// </summary>
        [DataTestMethod]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(4097)]
        [DataRow(int.MaxValue)]
        public void FinalizationRounds_WhenSetToInvalidValue_ShouldThrowExactly(int value)
        {
            using var algorithm = this.CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => algorithm.FinalizationRounds = value);
        }

        /// <summary>
        /// Verifies that setting a valid hashValue for <see cref="CubeHash.FinalizationRounds" />.
        /// </summary>
        [DataTestMethod]
        [DataRow(1)]
        [DataRow(4)]
        [DataRow(8)]
        [DataRow(64)]
        [DataRow(128)]
        [DataRow(256)]
        [DataRow(4096)]
        public void FinalizationRounds_WhenSetToValidValue_ShouldBeAssigned(int size)
        {
            using var algorithm = this.CreateAlgorithm();
            int original = algorithm.FinalizationRounds;
            algorithm.FinalizationRounds = size;

            Assert.AreEqual(size, algorithm.FinalizationRounds);
        }

        /// <summary>
        /// Verifies that setting a valid hashValue for <see cref="CubeHash.FinalizationRounds" /> updates the internal state.
        /// </summary>
        [TestMethod]
        public void FinalizationRounds_WhenSetToValidValue_ShouldUpdateCorrectly()
        {
            using var algorithm = this.CreateAlgorithm();
            int round = 100;
            int original = algorithm.FinalizationRounds;
            algorithm.FinalizationRounds = round;

            Assert.AreEqual(round, algorithm.FinalizationRounds);
            Assert.AreNotEqual(original, algorithm.FinalizationRounds);
        }

        /// <summary>
        /// Verifies that modifying <see cref="CubeHash.FinalizationRounds" /> does not affect other configuration properties.
        /// </summary>
        [TestMethod]
        public void FinalizationRounds_WhenChanged_ShouldNotAffectOtherProperties()
        {
            var algorithm = new CubeHash
            {
                InitializationRounds = 10,
                Rounds = 16,
                FinalizationRounds = 32,
                TransformBlockSize = 32,
                HashSize = 256
            };

            algorithm.FinalizationRounds = 20;

            Assert.AreEqual(10, algorithm.InitializationRounds, $"{nameof(CubeHash.InitializationRounds)} should remain unchanged.");
            Assert.AreEqual(16, algorithm.Rounds, $"{nameof(CubeHash.Rounds)} should remain unchanged.");
            Assert.AreEqual(20, algorithm.FinalizationRounds, $"{nameof(CubeHash.FinalizationRounds)} should update.");
            Assert.AreEqual(32, algorithm.TransformBlockSize, $"{nameof(CubeHash.TransformBlockSize)} should remain unchanged.");
            Assert.AreEqual(256, algorithm.HashSize, $"{nameof(CubeHash.HashSize)} should remain unchanged.");
        }
    }
}