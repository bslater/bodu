// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensionsTests_TransformFinalBlock.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Bodu.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography.Extensions
{
    public partial class ICryptoTransformExtensionsTests
    {
        // ---------------------------------------------------------------------------------------------------------------
        // TransformFinalBlock() — no-input overload
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform)" /> throws
        /// <see cref="ArgumentNullException" /> when <paramref name="cryptoTransform" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_NoInput_WhenTransformIsNull_ShouldThrowArgumentNullException()
        {
            ICryptoTransform? transform = null;

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                transform!.TransformFinalBlock());
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform)" /> finalises
        /// the transform without processing any data and returns a non-null byte array.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_NoInput_WhenTransformIsValid_ShouldReturnByteArray()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

            byte[] result = transform.TransformFinalBlock();

            Assert.IsNotNull(result);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // TransformFinalBlock(byte[]) — full-array overload
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[])" />
        /// throws <see cref="ArgumentNullException" /> when <paramref name="cryptoTransform" /> is
        /// <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_ByteArray_WhenTransformIsNull_ShouldThrowArgumentNullException()
        {
            ICryptoTransform? transform = null;

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                transform!.TransformFinalBlock(new byte[] { 1, 2, 3, 4 }));
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[])" />
        /// throws <see cref="ArgumentNullException" /> when <paramref name="array" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_ByteArray_WhenArrayIsNull_ShouldThrowArgumentNullException()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                transform.TransformFinalBlock(null!));
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[])" />
        /// transforms the entire array and returns the correct output.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_ByteArray_WhenArrayIsValid_ShouldReturnTransformedOutput()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            byte[] input = { 1, 2, 3, 4 };
            byte[] expected = { 4, 3, 2, 1 };

            byte[] result = transform.TransformFinalBlock(input);

            CollectionAssert.AreEqual(expected, result);
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[])" />
        /// returns a non-null, empty-or-padded result when given an empty array.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_ByteArray_WhenArrayIsEmpty_ShouldReturnNonNullResult()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

            byte[] result = transform.TransformFinalBlock(Array.Empty<byte>());

            Assert.IsNotNull(result);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // TransformFinalBlock(byte[], int) — from-offset overload
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[],int)" />
        /// throws <see cref="ArgumentNullException" /> when <paramref name="cryptoTransform" /> is
        /// <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_ByteArrayOffset_WhenTransformIsNull_ShouldThrowArgumentNullException()
        {
            ICryptoTransform? transform = null;

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                transform!.TransformFinalBlock(new byte[] { 1, 2, 3, 4 }, 0));
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[],int)" />
        /// throws <see cref="ArgumentNullException" /> when <paramref name="array" /> is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_ByteArrayOffset_WhenArrayIsNull_ShouldThrowArgumentNullException()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                transform.TransformFinalBlock(null!, 0));
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[],int)" />
        /// throws <see cref="ArgumentOutOfRangeException" /> when <paramref name="offset" /> is negative.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_ByteArrayOffset_WhenOffsetIsNegative_ShouldThrowArgumentOutOfRangeException()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                transform.TransformFinalBlock(new byte[] { 1, 2, 3, 4 }, -1));
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[],int)" />
        /// throws <see cref="ArgumentOutOfRangeException" /> when <paramref name="offset" /> exceeds the array bounds.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_ByteArrayOffset_WhenOffsetExceedsBounds_ShouldThrowArgumentOutOfRangeException()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                transform.TransformFinalBlock(new byte[] { 1, 2, 3, 4 }, 5));
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[],int)" />
        /// transforms from the given offset to the end of the array and returns the correct output.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_ByteArrayOffset_WhenOffsetIsZero_ShouldTransformEntireArray()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            byte[] input = { 1, 2, 3, 4 };
            byte[] expected = { 4, 3, 2, 1 };

            byte[] result = transform.TransformFinalBlock(input, 0);

            CollectionAssert.AreEqual(expected, result);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // TransformFinalBlock(ReadOnlySpan<byte>)
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,ReadOnlySpan{byte})" />
        /// throws <see cref="ArgumentNullException" /> when <paramref name="cryptoTransform" /> is
        /// <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_Span_WhenTransformIsNull_ShouldThrowArgumentNullException()
        {
            ICryptoTransform? transform = null;

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                transform!.TransformFinalBlock((ReadOnlySpan<byte>)new byte[] { 1, 2, 3, 4 }));
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,ReadOnlySpan{byte})" />
        /// correctly transforms a valid input span and returns the expected output.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_Span_WhenSpanIsValid_ShouldReturnTransformedOutput()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            ReadOnlySpan<byte> input = new byte[] { 1, 2, 3, 4 };
            byte[] expected = { 4, 3, 2, 1 };

            byte[] result = transform.TransformFinalBlock(input);

            CollectionAssert.AreEqual(expected, result);
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,ReadOnlySpan{byte})" />
        /// returns a non-null result when given an empty span.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_Span_WhenSpanIsEmpty_ShouldReturnNonNullResult()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

            byte[] result = transform.TransformFinalBlock(ReadOnlySpan<byte>.Empty);

            Assert.IsNotNull(result);
        }

        /// <summary>
        /// Verifies that the span overload of <c>TransformFinalBlock</c> produces output identical to the
        /// equivalent byte-array overload for the same input data.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_Span_WhenComparedToByteArrayOverload_ShouldProduceIdenticalOutput()
        {
            byte[] input = { 1, 2, 3, 4 };

            byte[] fromArray;
            using (var transformA = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest))
                fromArray = transformA.TransformFinalBlock(input);

            byte[] fromSpan;
            using (var transformB = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest))
                fromSpan = transformB.TransformFinalBlock((ReadOnlySpan<byte>)input);

            CollectionAssert.AreEqual(fromArray, fromSpan);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // TransformFinalBlock(ReadOnlyMemory<byte>)
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,ReadOnlyMemory{byte})" />
        /// throws <see cref="ArgumentNullException" /> when <paramref name="cryptoTransform" /> is
        /// <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_Memory_WhenTransformIsNull_ShouldThrowArgumentNullException()
        {
            ICryptoTransform? transform = null;

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                transform!.TransformFinalBlock(new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4 })));
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,ReadOnlyMemory{byte})" />
        /// transforms the memory region and returns the correct output.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_Memory_WhenMemoryIsValid_ShouldReturnTransformedOutput()
        {
            using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
            var input = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4 });
            byte[] expected = { 4, 3, 2, 1 };

            byte[] result = transform.TransformFinalBlock(input);

            CollectionAssert.AreEqual(expected, result);
        }

        /// <summary>
        /// Verifies that the memory overload of <c>TransformFinalBlock</c> produces output identical to the span
        /// overload for the same input, confirming the two overloads delegate consistently.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_Memory_WhenComparedToSpanOverload_ShouldProduceIdenticalOutput()
        {
            byte[] input = { 1, 2, 3, 4 };

            byte[] fromMemory;
            using (var transformA = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest))
                fromMemory = transformA.TransformFinalBlock(new ReadOnlyMemory<byte>(input));

            byte[] fromSpan;
            using (var transformB = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest))
                fromSpan = transformB.TransformFinalBlock((ReadOnlySpan<byte>)input);

            CollectionAssert.AreEqual(fromMemory, fromSpan);
        }
    }
}
