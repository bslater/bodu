// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.CreateDecryptor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class SymmetricAlgorithmTests<TAlgorithm>
    {
        /// <summary>
        /// Verifies that setting <see cref="SymmetricAlgorithm.CreateDecryptor" /> after the algorithm has been disposed throws
        /// an <see cref="ObjectDisposedException" />.
        /// </summary>
        [TestMethod]
        public void CreateDecryptor_WhenSetAfterDispose_ShouldThrowObjectDisposedException()
        {
            TAlgorithm algorithm = CreateAlgorithm();
            algorithm.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                _ = algorithm.CreateDecryptor();
            });
        }

        /// <summary>
        /// Verifies that attempting to create a cryptographic transform on a disposed
        /// <typeparamref name="TAlgorithm" /> instance throws <see cref="ObjectDisposedException" /> whose
        /// <see cref="ObjectDisposedException.ObjectName" /> carries the concrete algorithm type
        /// name. Regression guard for defects where <c>nameof(T)</c> on a non-generic base class
        /// produced the literal string <c>"T"</c> instead of the derived type name.
        /// </summary>
        [TestMethod]
        public void CreateDecryptor_WhenDisposes_ShouldReportConcreteTypeName()
        {
            var algorithm = CreateAlgorithm();
            algorithm.Dispose();

            try
            {
                using var _ = algorithm.CreateDecryptor();
                Assert.Fail("Expected ObjectDisposedException after disposal.");
            }
            catch (ObjectDisposedException ex)
            {
                Assert.AreEqual(typeof(TAlgorithm).FullName, ex.ObjectName,
                    $"ObjectDisposedException.ObjectName must match the concrete type name '{typeof(TAlgorithm).FullName}'.");
            }
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> throws
        /// <see cref="ArgumentNullException" /> when the key is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void CreateDecryptor_WhenKeyIsNull_ShouldThrowArgumentNullException_fix()
        {
            using var algorithm = CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = algorithm.CreateDecryptor(null!, new byte[algorithm.BlockSize / 8]);
            });
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> throws
        /// <see cref="ArgumentNullException" /> when the IV is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void CreateDecryptor_WhenIvIsNull_ShouldThrowArgumentNullException_fix()
        {
            using var algorithm = CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = algorithm.CreateDecryptor(new byte[algorithm.KeySize / 8], null!);
            });
        }
    }
}