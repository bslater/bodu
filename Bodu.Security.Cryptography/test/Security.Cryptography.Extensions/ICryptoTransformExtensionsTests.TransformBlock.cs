// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensionsTests_TransformBlock.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;
using Bodu.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography.Extensions
{
    public partial class ICryptoTransformExtensionsTests
    {
        // ---------------------------------------------------------------------------------------------------------------
        // TransformBlock(byte[]) — in-place overload
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformBlock(ICryptoTransform,byte[])" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="cryptoTransform" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void TransformBlock_WhenTransformIsNull_ShouldThrowArgumentNullException()
        {
            ICryptoTransform? transform = null;

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                transform!.TransformBlock(new byte[] { 1, 2, 3, 4 }));
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformBlock(ICryptoTransform,byte[])" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="array" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void TransformBlock_WhenArrayIsNull_ShouldThrowArgumentNullException()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                transform.TransformBlock(null!));
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformBlock(ICryptoTransform,byte[])" /> writes
        /// the transformed output in-place and returns the number of bytes written.
        /// </summary>
        [TestMethod]
        public void TransformBlock_WhenArrayIsValid_ShouldTransformInPlaceAndReturnByteCount()
        {
            // BlockSize = 32 bits = 4 bytes; reversal of {1,2,3,4} → {4,3,2,1}
            byte[] block = { 1, 2, 3, 4 };
            byte[] expected = { 4, 3, 2, 1 };

            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

            int written = transform.TransformBlock(block);

            Assert.AreEqual(block.Length, written);
            CollectionAssert.AreEqual(expected, block);
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformBlock(ICryptoTransform,byte[])" /> returns
        /// zero when given an empty array, with no side effects.
        /// </summary>
        [TestMethod]
        public void TransformBlock_WhenArrayIsEmpty_ShouldReturnZero()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            byte[] empty = Array.Empty<byte>();

            int written = transform.TransformBlock(empty);

            Assert.AreEqual(0, written);
        }

        /// <summary>
        /// Verifies that consecutive calls to
        /// <see cref="ICryptoTransformExtensions.TransformBlock(ICryptoTransform,byte[])" /> each independently
        /// transform the supplied block, confirming the transform processes each call in isolation.
        /// </summary>
        [TestMethod]
        public void TransformBlock_WhenCalledRepeatedly_ShouldTransformEachBlockIndependently()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

            byte[] blockA = { 1, 2, 3, 4 };
            byte[] blockB = { 5, 6, 7, 8 };

            transform.TransformBlock(blockA);
            transform.TransformBlock(blockB);

            CollectionAssert.AreEqual(new byte[] { 4, 3, 2, 1 }, blockA);
            CollectionAssert.AreEqual(new byte[] { 8, 7, 6, 5 }, blockB);
        }
    }
}
