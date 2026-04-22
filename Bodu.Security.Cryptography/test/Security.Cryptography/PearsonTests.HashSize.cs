// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PearsonTests.HashSize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography
{
    public partial class PearsonTests
    {
        /// <summary>
        /// Verifies that <see cref="Pearson.HashSize" />, Get, when Default, returns the expected value.
        /// </summary>
        [TestMethod]
        public void HashSize_Get_WhenDefault_ShouldReturn8()
        {
            using var algorithm = new Pearson();
            Assert.AreEqual(8, algorithm.HashSize, "Default algorithm size should be 8 bits.");
        }

        /// <summary>
        /// Verifies that <see cref="Pearson.HashSize" />, Set, when Valid, returns the expected value.
        /// </summary>
        [TestMethod]
        [DataRow(8)]
        [DataRow(64)]
        [DataRow(128)]
        [DataRow(512)]
        [DataRow(2048)]
        public void HashSize_Set_WhenValid_ShouldUpdateSize(int bits)
        {
            using var algorithm = new Pearson
            {
                HashSize = bits
            };

            Assert.AreEqual(bits, algorithm.HashSize, $"HashSize should be set to {bits} bits.");
        }

        /// <summary>
        /// Verifies that <see cref="Pearson.ComputeHash" />, when HashSizeSet, returns the expected value.
        /// </summary>
        [TestMethod]
        [DataRow(8)]
        [DataRow(64)]
        [DataRow(128)]
        [DataRow(512)]
        [DataRow(2048)]
        public void ComputeHash_WhenHashSizeSet_ShouldReturnExpectedByteLength(int bits)
        {
            using var algorithm = new Pearson
            {
                HashSize = bits
            };

            byte[] input = Encoding.ASCII.GetBytes("abc");

            byte[] result = algorithm.ComputeHash(input);

            int expectedLength = bits / 8;
            Assert.AreEqual(expectedLength, result.Length, $"Expected algorithm length for {bits} bits is {expectedLength} bytes.");
        }

        /// <summary>
        /// Verifies that <see cref="Pearson.HashSize" />, Set, when OutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        [DataRow(7)]
        [DataRow(9)]
        [DataRow(2056)]
        [DataRow(-8)]
        public void HashSize_Set_WhenOutOfRange_ShouldThrow(int bits)
        {
            using var algorithm = new Pearson();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                algorithm.HashSize = bits;
            });
        }

        /// <summary>
        /// Verifies that <see cref="Pearson.HashSize" />, Set, when HashingStarted, throws <see cref="CryptographicUnexpectedOperationException" />.
        /// </summary>
        [TestMethod]
        public void HashSize_Set_WhenHashingStarted_ShouldThrowExactly()
        {
            using var algorithm = new Pearson();
            _ = algorithm.TransformBlock(new byte[] { 1, 2, 3 }, 0, 3, null, 0);

            Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
            {
                algorithm.HashSize = 64;
            });
        }

        /// <summary>
        /// Verifies that <see cref="Pearson.HashSize" />, Get, when Disposed, throws <see cref="ObjectDisposedException" />.
        /// </summary>
        [TestMethod]
        public void HashSize_Get_WhenDisposed_ShouldThrowExactly()
        {
            var algorithm = new Pearson();
            algorithm.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                _ = algorithm.HashSize;
            });
        }

        /// <summary>
        /// Verifies that <see cref="Pearson.HashSize" />, Set, when Disposed, throws <see cref="ObjectDisposedException" />.
        /// </summary>
        [TestMethod]
        public void HashSize_Set_WhenDisposed_ShouldThrowExactly()
        {
            var algorithm = new Pearson();
            algorithm.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                algorithm.HashSize = 64;
            });
        }
    }
}