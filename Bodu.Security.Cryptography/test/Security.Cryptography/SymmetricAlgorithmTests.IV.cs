// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.IV.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class SymmetricAlgorithmTests<TAlgorithm>
    {
        /// <summary>
        /// Verifies that the IV property is not null upon algorithm creation.
        /// </summary>
        [TestMethod]
        public void IV_WhenAccessed_ShouldNotBeNull()
        {
            using TAlgorithm algorithm = CreateAlgorithm();
            Assert.IsNotNull(algorithm.IV);
        }

        /// <summary>
        /// Verifies that accessing <see cref="SymmetricAlgorithm.IV" /> after the algorithm has been disposed throws
        /// an <see cref="ObjectDisposedException" />.
        /// </summary>
        [TestMethod]
        public void IV_WhenAccessedAfterDispose_ShouldThrowObjectDisposedException()
        {
            TAlgorithm algorithm = CreateAlgorithm();
            algorithm.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                _ = algorithm.IV;
            });
        }

        /// <summary>
        /// Verifies that setting the IV to null throws an ArgumentNullException.
        /// </summary>
        [TestMethod]
        public void IV_WhenSetToNull_ShouldThrowExactly()
        {
            using TAlgorithm algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() => algorithm.IV = null);
        }

        /// <summary>
        /// Verifies that setting an invalid IV size throws a CryptographicException.
        /// </summary>
        [TestMethod]
        public void IV_WhenSetToInvalidSize_ShouldThrowExactly()
        {
            using TAlgorithm algorithm = CreateAlgorithm();
            byte[] invalidIV = new byte[algorithm.BlockSize - 1];
            Assert.ThrowsExactly<CryptographicException>(() => algorithm.IV = invalidIV);
        }

        /// <summary>
        /// Verifies that setting IV returns the same hashValue on subsequent get.
        /// </summary>
        [TestMethod]
        public void IV_WhenSet_ShouldReturnSameValueOnGet()
        {
            using TAlgorithm algorithm = CreateAlgorithm();
            byte[] iv = new byte[algorithm.BlockSize / 8];
            CryptoHelpers.FillWithRandomNonZeroBytes(iv);

            algorithm.IV = iv;
            CollectionAssert.AreEqual(iv, algorithm.IV);
        }

        /// <summary>
        /// Verifies that the IV property returns a defensive copy (not the same reference).
        /// </summary>
        [TestMethod]
        public void IV_WhenSet_ShouldReturnDefensiveCopy()
        {
            using TAlgorithm algorithm = CreateAlgorithm();
            byte[] iv = new byte[algorithm.BlockSize / 8];
            CryptoHelpers.FillWithRandomNonZeroBytes(iv);

            algorithm.IV = iv;
            Assert.AreNotSame(iv, algorithm.IV);
        }

        /// <summary>
        /// Verifies that modifying the array returned by <see cref="SymmetricAlgorithm.IV" />
        /// does not mutate the algorithm's internal IV state.
        /// </summary>
        [TestMethod]
        public void IV_WhenReturnedArrayIsModified_ShouldNotChangeInternalValue()
        {
            using var algorithm = CreateAlgorithm();
            int size = algorithm.BlockSize;
            byte[] expected = Enumerable.Range(1, size / 8).Select(i => (byte)i).ToArray();

            algorithm.IV = expected;

            byte[] returned = algorithm.IV;
            returned[0] ^= 0xFF;

            byte[] actual = algorithm.IV;

            CollectionAssert.AreEqual(expected, actual);
            CollectionAssert.AreNotEqual(returned, actual);
        }

        /// <summary>
        /// Verifies that GenerateIV produces a different IV from the previous one.
        /// </summary>
        [TestMethod]
        public void GenerateIV_WhenCalled_ShouldChangeIV()
        {
            using TAlgorithm algorithm = CreateAlgorithm();
            byte[] initialIV = algorithm.IV;

            algorithm.GenerateIV();
            CollectionAssert.AreNotEqual(initialIV, algorithm.IV);
        }

        /// <summary>
        /// Verifies that creating an encryptor with a wrong-length IV throws
        /// <see cref="CryptographicException" /> whose message reports the offending IV bit-length
        /// rather than an unrelated value (e.g. the key length). Regression guard for a copy-paste
        /// in <see cref="Threefish" />'s validation diagnostics.
        /// </summary>
        [TestMethod]
        public void CreateEncryptor_WithInvalidIvLength_ShouldThrowArgumentException()
        {
            using var algorithm = CreateAlgorithm();
            algorithm.GenerateKey();

            int blockSizeBytes = algorithm.BlockSize / 8;
            byte[] badIv = new byte[blockSizeBytes - 1];
            int expectedBitLength = badIv.Length * 8;

            var ex = Assert.ThrowsExactly<CryptographicException>(() =>
            {
                using var _ = algorithm.CreateEncryptor(algorithm.Key, badIv);
            });

            Assert.IsTrue(
                ex.Message.Contains(expectedBitLength.ToString()),
                $"Expected IV bit-length {expectedBitLength} in message but got: {ex.Message}");
        }

    }
}