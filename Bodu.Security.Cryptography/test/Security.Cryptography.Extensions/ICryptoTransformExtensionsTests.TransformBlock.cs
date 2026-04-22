// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensionsTests.TransformBlock.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

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
        [DynamicData(nameof(GetValidTransformTestData), DynamicDataSourceType.Method)]
        public void TransformBlock_WhenArrayIsValid_ShouldTransformInPlaceAndReturnByteCount(KnownAnswerTest kat)
        {
            // Take a fresh copy since TransformBlock modifies the array in-place.
            byte[] block = (byte[])kat.Input.Clone();

            using var transform = CreateTransform(kat);
            int written = transform.TransformBlock(block);

            Assert.AreEqual(kat.Input.Length, written,
                $"[{kat.Name}] Written byte count must equal input length.");
            CollectionAssert.AreEqual(kat.ExpectedOutput, block,
                $"[{kat.Name}] In-place transform did not produce the expected output.");
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformBlock(ICryptoTransform,byte[])" />
        /// throws <see cref="ArgumentException" /> when given an empty array, since a zero-length block
        /// is not a meaningful input for any block cipher operation.
        /// </summary>
        [TestMethod]
        public void TransformBlock_WhenArrayIsEmpty_ShouldThrowArgumentException()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

            Assert.ThrowsExactly<ArgumentException>(() =>
                transform.TransformBlock(Array.Empty<byte>()));
        }

        /// <summary>
        /// Verifies that consecutive calls to
        /// <see cref="ICryptoTransformExtensions.TransformBlock(ICryptoTransform,byte[])" /> each independently
        /// transform the supplied block, confirming that ECB mode produces no cross-call state leakage —
        /// the same input always yields the same output regardless of previous calls.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetValidTransformTestData), DynamicDataSourceType.Method)]
        public void TransformBlock_WhenCalledRepeatedly_ShouldTransformEachBlockIndependently(KnownAnswerTest kat)
        {
            // Two independent copies of the same input — the transform must produce identical
            // output for each, confirming there is no state leakage between consecutive calls.
            byte[] blockA = (byte[])kat.Input.Clone();
            byte[] blockB = (byte[])kat.Input.Clone();

            using var transform = CreateTransform(kat);
            transform.TransformBlock(blockA);
            transform.TransformBlock(blockB);

            CollectionAssert.AreEqual(kat.ExpectedOutput, blockA,
                $"[{kat.Name}] First TransformBlock call produced wrong output.");
            CollectionAssert.AreEqual(kat.ExpectedOutput, blockB,
                $"[{kat.Name}] Second TransformBlock call produced wrong output — possible state leakage from first call.");
        }
    }
}