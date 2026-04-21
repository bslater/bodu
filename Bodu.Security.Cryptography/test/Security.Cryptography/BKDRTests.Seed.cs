// -----------------------------------------------------------------------
// <copyright file="BKDRTests.Seed.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="BKDR.Seed" /> property and its interaction with the algorithming lifecycle.
    /// </summary>
    public partial class BKDRTests
    {
        /// <summary>
        /// Validates that a new instance of <see cref="BKDR" /> has a default seed hashValue of zero.
        /// </summary>
        [TestMethod]
        public void Seed_WhenDefaultConstructed_ShouldBeZero()
        {
            var algorithm = CreateAlgorithm();
            Assert.AreEqual<uint>(131, algorithm.Seed);
        }

        /// <summary>
        /// Validates that the seed hashValue can be set and retrieved before any algorithming operation starts.
        /// </summary>
        [TestMethod]
        public void Seed_WhenSetBeforeUse_ShouldBeRetained()
        {
            var algorithm = new BKDR { Seed = 1313 };
            Assert.AreEqual<uint>(1313, algorithm.Seed);
        }

        /// <summary>
        /// Ensures setting <see cref="BKDR.Seed" /> after a algorithm computation has begun throws a <see cref="CryptographicException" />.
        /// </summary>
        [TestMethod]
        public void Seed_WhenSetAfterHashingStarted_ShouldThrow()
        {
            var algorithm = CreateAlgorithm();
            byte[] input = new byte[] { 1, 2, 3 };
            algorithm.TransformBlock(input, 0, input.Length, input, 0);

            Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() => algorithm.Seed = 1234);
        }

        /// <summary>
        /// Ensures that setting <see cref="BKDR.Seed" /> after a algorithm computation has started does not throw a <see cref="CryptographicException" />.
        /// </summary>
        [TestMethod]
        public void Seed_WhenSetAfterHashing_ShouldNotThrow()
        {
            var algorithm = CreateAlgorithm();
            byte[] input = new byte[] { 1, 2, 3 };

            algorithm.ComputeHash(input);

            // Change the seed hashValue after the first computation, and perform the second algorithm computation with the new seed hashValue.
            algorithm.Seed = 131;
            algorithm.ComputeHash(input);
        }

        /// <summary>
        /// Confirms that using different seed values results in different algorithm outputs for the same input.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WithDifferentSeeds_ShouldReturnDifferentResults()
        {
            byte[] input = new byte[] { 0x10, 0x20, 0x30 };

            var algorithmA = new BKDR { Seed = 131313U };
            var algorithmB = new BKDR { Seed = 13131U };

            byte[] resultA = algorithmA.ComputeHash(input);
            byte[] resultB = algorithmB.ComputeHash(input);

            CollectionAssert.AreNotEqual(resultA, resultB);
        }

        /// <summary>
        /// Ensures that calling <see cref="BKDR.Initialize" /> resets the internal algorithm state to the seed hashValue.
        /// </summary>
        [TestMethod]
        public void Initialize_ShouldResetHashStateToSeed()
        {
            var algorithm = new BKDR { Seed = 13131U };

            _ = algorithm.ComputeHash(new byte[] { 0x01, 0x02 });
            algorithm.Initialize();

            byte[] fresh = algorithm.ComputeHash(Array.Empty<byte>());

            // Should match seed state as algorithm result
            var expected = BitConverter.GetBytes(13131U);
            if (BitConverter.IsLittleEndian) Array.Reverse(expected);

            CollectionAssert.AreEqual(expected, fresh);
        }
    }
}