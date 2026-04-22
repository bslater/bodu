// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerTests.Variant.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography
{
    public partial class TigerTests
    {
        /// <summary>
        /// Verifies that setting <see cref="Pearson.Variant" /> to a valid predefined type allows computing a algorithm without error, and
        /// that the algorithm output is the correct length for the current <see cref="HashAlgorithm.HashSize" />.
        /// </summary>
        /// <param name="variant">The table type to test.</param>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants), DynamicDataSourceType.Method)]
        public void Variant_Set_WhenValid_ShouldProduceExpectedHash(TigerVariant variant)
        {
            using var algorithm = CreateAlgorithm(variant);

            byte[] input = Encoding.ASCII.GetBytes("Test input");

            byte[] result = algorithm.ComputeHash(input);

            Assert.IsNotNull(result, $"Result should not be null for table variant {variant}.");
            Assert.AreEqual(algorithm.HashSize / 8, result.Length, $"Hash length should match expected size for variant {variant}.");
            Assert.IsTrue(result.Any(b => b != 0), $"Hash result should not be all zeros for variant {variant}.");
        }

        /// <summary>
        /// Verifies that Variant, Get, when Default, returns Pearson.
        /// </summary>
        [TestMethod]
        public void Variant_Get_WhenDefault_ShouldReturnPearson()
        {
            using var algorithm = CreateAlgorithm();

            var type = algorithm.Variant;

            Assert.AreEqual(Bodu.Security.Cryptography.TigerHashingVariant.Tiger, type, "Default Variant should be Tiger.");
        }

        /// <summary>
        /// Verifies that <see cref="Tiger.Variant" />, Set, when HashingStarted, throws <see cref="CryptographicUnexpectedOperationException" />.
        /// </summary>
        [TestMethod]
        public void Variant_Set_WhenHashingStarted_ShouldThrowExactly()
        {
            using var algorithm = CreateAlgorithm();
            algorithm.TransformBlock(new byte[] { 1, 2, 3 }, 0, 3, null, 0);

            Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
            {
                algorithm.Variant = Bodu.Security.Cryptography.TigerHashingVariant.Tiger2;
            });
        }

        /// <summary>
        /// Verifies that Variant, Set, when Disposed, throws Exactly.
        /// </summary>
        [TestMethod]
        public void Variant_Set_WhenDisposed_ShouldThrowExactly()
        {
            using var algorithm = CreateAlgorithm();
            algorithm.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                algorithm.Variant = Bodu.Security.Cryptography.TigerHashingVariant.Tiger2;
            });
        }

        /// <summary>
        /// Verifies that Variant, Get, when Disposed, throws Exactly.
        /// </summary>
        [TestMethod]
        public void Variant_Get_WhenDisposed_ShouldThrowExactly()
        {
            using var algorithm = CreateAlgorithm();
            algorithm.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                var _ = algorithm.Variant;
            });
        }
    }
}