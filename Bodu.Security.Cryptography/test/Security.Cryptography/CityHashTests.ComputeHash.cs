// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests_ComputeHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    public abstract partial class CityHashTests<TTest, TAlgorithm>
    {
        // ---------------------------------------------------------------------------------------------------------------
        // Output contract
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[])" /> returns a byte array whose length
        /// equals <see cref="HashAlgorithm.HashSize" /> divided by 8.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenCalled_ShouldReturnCorrectByteLength()
        {
            using var algorithm = this.CreateAlgorithm();
            byte[] hash = algorithm.ComputeHash(Array.Empty<byte>());

            Assert.AreEqual(algorithm.HashSize / 8, hash.Length,
                $"Expected {algorithm.HashSize / 8} bytes for a {algorithm.HashSize}-bit hash.");
        }

        /// <summary>
        /// Verifies that the empty-input hash is not all-zero bytes, confirming that the seeding
        /// constants produce a non-trivial initial state.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenInputIsEmpty_ShouldReturnNonZeroHash()
        {
            using var algorithm = this.CreateAlgorithm();
            byte[] hash = algorithm.ComputeHash(Array.Empty<byte>());

            Assert.IsTrue(hash.Any(b => b != 0), "Hash of empty input must not be all-zero bytes.");
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Determinism
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that two calls with the same input on the same instance produce identical output.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenCalledTwiceWithSameInput_ShouldReturnIdenticalHashes()
        {
            byte[] input = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

            using var algorithm = this.CreateAlgorithm();
            byte[] first = algorithm.ComputeHash(input);
            byte[] second = algorithm.ComputeHash(input);

            CollectionAssert.AreEqual(first, second,
                "Repeated ComputeHash calls with identical input must produce identical results.");
        }

        /// <summary>
        /// Verifies that two separate instances produce identical output for the same input, confirming
        /// that the algorithm carries no cross-instance state.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenCalledOnSeparateInstances_ShouldProduceIdenticalResults()
        {
            byte[] input = Enumerable.Range(0, 64).Select(i => (byte)(i * 3)).ToArray();

            using var first = this.CreateAlgorithm();
            using var second = this.CreateAlgorithm();

            CollectionAssert.AreEqual(first.ComputeHash(input), second.ComputeHash(input),
                "Separate instances must produce the same hash for identical input.");
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Sensitivity
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that flipping a single bit in the input produces a different hash, confirming basic
        /// input sensitivity (avalanche behaviour).
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenSingleBitDiffers_ShouldProduceDifferentHash()
        {
            byte[] input = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
            byte[] modified = (byte[])input.Clone();
            modified[0] ^= 0x01;

            using var algorithm = this.CreateAlgorithm();

            CollectionAssert.AreNotEqual(algorithm.ComputeHash(input), algorithm.ComputeHash(modified),
                "A single-bit input difference must produce a different hash.");
        }

        /// <summary>
        /// Verifies that inputs that differ only in length produce different hash values, confirming that
        /// message length is encoded into the hash state.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenInputLengthDiffers_ShouldProduceDifferentHash()
        {
            using var algorithm = this.CreateAlgorithm();

            CollectionAssert.AreNotEqual(
                algorithm.ComputeHash(new byte[8]),
                algorithm.ComputeHash(new byte[9]),
                "Inputs of different lengths must hash to different values.");
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Internal path coverage
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Provides input lengths that exercise every internal length-dispatching path in both
        /// <see cref="CityHash32" /> and <see cref="CityHash64" />, as well as the boundary values
        /// at each path transition.
        /// </summary>
        public static IEnumerable<object[]> GetPathBoundaryLengths()
        {
            // CityHash32 paths: ≤4, ≤12, ≤24, 25+
            // CityHash64 paths: ≤16, ≤32, ≤64, 65+
            // Values chosen to hit both boundaries and interiors.
            int[] lengths =
            {
                0, 1, 2, 3, 4,       // CityHash32 path 1 boundary
                5, 8, 12,            // CityHash32 path 2 interior/boundary
                13, 16, 24,          // CityHash32 path 3 / CityHash64 path 1 boundary
                25, 32,              // CityHash32 path 4 entry / CityHash64 path 2 boundary
                33, 48, 63, 64,      // CityHash64 path 3 interior/boundary
                65, 96, 128, 200     // CityHash64 path 4 (65+)
            };

            foreach (int len in lengths)
                yield return new object[] { len };
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[])" /> returns the correct number of bytes
        /// for inputs spanning every distinct algorithm path.
        /// </summary>
        /// <param name="length">The number of bytes in the test input.</param>
        [TestMethod]
        [DynamicData(nameof(GetPathBoundaryLengths), DynamicDataSourceType.Method)]
        public void ComputeHash_AcrossAllPathLengths_ShouldReturnCorrectByteLength(int length)
        {
            using var algorithm = this.CreateAlgorithm();
            byte[] input = Enumerable.Range(0, length).Select(i => (byte)(i % 251)).ToArray();
            byte[] hash = algorithm.ComputeHash(input);

            Assert.AreEqual(algorithm.HashSize / 8, hash.Length,
                $"Input length {length}: expected {algorithm.HashSize / 8}-byte output.");
        }

        /// <summary>
        /// Verifies that hashing the same input twice returns the same result for every algorithm path,
        /// confirming path-level determinism.
        /// </summary>
        /// <param name="length">The number of bytes in the test input.</param>
        [TestMethod]
        [DynamicData(nameof(GetPathBoundaryLengths), DynamicDataSourceType.Method)]
        public void ComputeHash_AcrossAllPathLengths_ShouldBeDeterministic(int length)
        {
            using var algorithm = this.CreateAlgorithm();
            byte[] input = Enumerable.Range(0, length).Select(i => (byte)(i % 251)).ToArray();

            CollectionAssert.AreEqual(
                algorithm.ComputeHash(input),
                algorithm.ComputeHash(input),
                $"Input length {length}: repeated hashing must yield identical results.");
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Initialize / reset
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> between two hash operations resets
        /// the instance so that the second hash matches the first.
        /// </summary>
        [TestMethod]
        public void Initialize_WhenCalledBetweenHashes_ShouldResetStateCompletely()
        {
            byte[] input = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

            using var algorithm = this.CreateAlgorithm();
            byte[] first = algorithm.ComputeHash(input);

            algorithm.Initialize();
            byte[] second = algorithm.ComputeHash(input);

            CollectionAssert.AreEqual(first, second,
                "Initialize must fully reset the algorithm state for subsequent operations.");
        }
    }
}