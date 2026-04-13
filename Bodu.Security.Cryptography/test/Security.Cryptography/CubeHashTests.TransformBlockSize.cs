// -----------------------------------------------------------------------
// <copyright file="CubeHashTests.TransformBlockSize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    public partial class CubeHashTests
    {
        /// <summary>
        /// Validates that a new instance of <see cref="CubeHash" /> has a default transform block size hashValue of 32.
        /// </summary>
        [TestMethod]
        public void TransformBlockSize_WhenDefaultConstructed_ShouldBe16()
        {
            var algorithm = new CubeHash();
            Assert.AreEqual(32, algorithm.TransformBlockSize);
        }

        /// <summary>
        /// Validates that the transform block size hashValue can be set and retrieved before any algorithming operation starts.
        /// </summary>
        [TestMethod]
        public void TransformBlockSize_WhenSetBeforeUse_ShouldBeRetained()
        {
            var algorithm = new CubeHash { TransformBlockSize = 32 };
            Assert.AreEqual(32, algorithm.TransformBlockSize);
        }

        /// <summary>
        /// Ensures that setting <see cref="CubeHash.TransformBlockSize" /> after a algorithm computation has started does not throw an exception.
        /// </summary>
        [TestMethod]
        public void TransformBlockSize_WhenSetAfterHashing_ShouldNotThrow()
        {
            var algorithm = new CubeHash();
            byte[] input = new byte[] { 1, 2, 3 };

            algorithm.ComputeHash(input);

            // Change the transform block size hashValue after the first computation, and perform the second algorithm computation with the
            // new transform block size hashValue.
            algorithm.TransformBlockSize = 32;
            algorithm.ComputeHash(input);
        }

        /// <summary>
        /// Verifies that setting different values for <see cref="CubeHash.TransformBlockSize" /> produces different hash outputs for the
        /// same input data.
        /// </summary>
        [TestMethod]
        public void TransformBlockSize_WhenDifferentValuesUsed_ShouldProduceDifferentHashes()
        {
            byte[] input = new byte[] { 0x10, 0x20, 0x30 };

            var algorithmA = new CubeHash { TransformBlockSize = 32 };
            var algorithmB = new CubeHash { TransformBlockSize = 64 };

            byte[] resultA = algorithmA.ComputeHash(input);
            byte[] resultB = algorithmB.ComputeHash(input);

            CollectionAssert.AreNotEqual(resultA, resultB);
        }

        /// <summary>
        /// Verifies that setting an invalid hashValue for <see cref="CubeHash.TransformBlockSize" /> throws <see cref="ArgumentOutOfRangeException" />.
        /// </summary>
        [TestMethod]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(129)]
        [DataRow(256)]
        [DataRow(int.MaxValue)]
        public void TransformBlockSize_WhenSetToInvalidValue_ShouldThrowExactly(int value)
        {
            using var algorithm = this.CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => algorithm.TransformBlockSize = value);
        }

        /// <summary>
        /// Verifies that setting a valid hashValue for <see cref="CubeHash.TransformBlockSize" /> updates the internal state.
        /// </summary>
        [TestMethod]
        [DataRow(1)]
        [DataRow(8)]
        [DataRow(16)]
        [DataRow(32)]
        [DataRow(64)]
        [DataRow(128)]
        public void TransformBlockSize_WhenSetToValidValue_ShouldBeAssigned(int size)
        {
            using var algorithm = this.CreateAlgorithm();
            algorithm.TransformBlockSize = size;

            Assert.AreEqual(size, algorithm.TransformBlockSize);
        }

        /// <summary>
        /// Verifies that setting a valid hashValue for <see cref="CubeHash.TransformBlockSize" /> updates the internal state.
        /// </summary>
        [TestMethod]
        public void TransformBlockSize_WhenSetToValidValue_ShouldUpdateCorrectly()
        {
            using var algorithm = this.CreateAlgorithm();
            int size = 64;
            int original = algorithm.TransformBlockSize;
            algorithm.TransformBlockSize = size;

            Assert.AreEqual(size, algorithm.TransformBlockSize);
            Assert.AreNotEqual(original, algorithm.TransformBlockSize);
        }

        /// <summary>
        /// Verifies that modifying <see cref="CubeHash.TransformBlockSize" /> does not affect other configuration properties.
        /// </summary>
        [TestMethod]
        public void TransformBlockSize_WhenChanged_ShouldNotAffectOtherProperties()
        {
            var algorithm = new CubeHash
            {
                InitializationRounds = 10,
                Rounds = 16,
                FinalizationRounds = 32,
                TransformBlockSize = 32,
                HashSize = 256
            };

            algorithm.TransformBlockSize = 20;

            Assert.AreEqual(10, algorithm.InitializationRounds, $"{nameof(CubeHash.InitializationRounds)} should remain unchanged.");
            Assert.AreEqual(16, algorithm.Rounds, $"{nameof(CubeHash.Rounds)} should remain unchanged.");
            Assert.AreEqual(32, algorithm.FinalizationRounds, $"{nameof(CubeHash.FinalizationRounds)} should remain unchanged.");
            Assert.AreEqual(20, algorithm.TransformBlockSize, $"{nameof(CubeHash.TransformBlockSize)} should update.");
            Assert.AreEqual(256, algorithm.HashSize, $"{nameof(CubeHash.HashSize)} should remain unchanged.");
        }
    }
}