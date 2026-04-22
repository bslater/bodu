// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests.CompressionRounds.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Security.Cryptography
{
    public abstract partial class SipHashTests<TTest, TAlgorithm>
    {
        /// <summary> <summary> Verifies that setting an invalid hashValue for <see cref="SipHash.CompressionRounds"/> throws <see
        /// cref="ArgumentOutOfRangeException"/>. </summary>
        [TestMethod]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1)]
        public void CompressionRounds_WhenSetToInvalidValue_ShouldThrowExactly(int value)
        {
            using var algorithm = CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                algorithm.CompressionRounds = value;
            });
        }

        /// <summary>
        /// Verifies that setting a valid hashValue for <see cref="SipHash.CompressionRounds" /> updates the internal state.
        /// </summary>
        [TestMethod]
        [DataRow(16)]
        [DataRow(32)]
        public void CompressionRounds_WhenSetToValidValue_ShouldUpdateCorrectly(int size)
        {
            using var algorithm = CreateAlgorithm();
            int original = algorithm.CompressionRounds;
            algorithm.CompressionRounds = size;

            Assert.AreEqual(size, algorithm.CompressionRounds);
            Assert.AreNotEqual(original, algorithm.CompressionRounds);
        }

        /// <summary>
        /// Verifies that using different <see cref="SipHash.CompressionRounds" /> values results in different hash outputs for the same input.
        /// </summary>
        [TestMethod]
        public void CompressionRounds_WhenDifferent_ShouldProduceDifferentHash()
        {
            byte[] input = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
            byte[] hashWithRounds4;
            byte[] hashWithRounds8;

            using (var algorithm = CreateAlgorithm())
            {
                algorithm.CompressionRounds = 4;
                hashWithRounds4 = algorithm.ComputeHash(input);
            }

            using (var algorithm = CreateAlgorithm())
            {
                algorithm.CompressionRounds = 8;
                hashWithRounds8 = algorithm.ComputeHash(input);
            }

            CollectionAssert.AreNotEqual(hashWithRounds4, hashWithRounds8, "Hashes should differ when compression rounds are different.");
        }
    }
}