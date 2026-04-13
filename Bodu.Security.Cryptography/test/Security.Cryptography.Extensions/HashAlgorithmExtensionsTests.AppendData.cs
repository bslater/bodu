// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsTests_AppendData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;
using Bodu.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography.Extensions
{
    public partial class HashAlgorithmExtensionsTests
    {
        // ---------------------------------------------------------------------------------------------------------------
        // AppendData(ReadOnlySpan<byte>)
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="HashAlgorithmExtensions.AppendData" /> throws <see cref="ArgumentNullException" />
        /// when the algorithm argument is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void AppendData_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
        {
            HashAlgorithm? algorithm = null;

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                algorithm!.AppendData(new ReadOnlySpan<byte>(new byte[] { 1, 2, 3 })));
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithmExtensions.AppendData" /> does not alter the accumulated state
        /// when called with an empty span.
        /// </summary>
        [TestMethod]
        public void AppendData_WhenSpanIsEmpty_ShouldNotContributeToHash()
        {
            using var algorithm = CreateAlgorithm();

            algorithm.AppendData(ReadOnlySpan<byte>.Empty);
            algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            byte[] expected = BitConverter.GetBytes((uint)0);
            CollectionAssert.AreEqual(expected, algorithm.Hash);
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithmExtensions.AppendData" /> contributes the supplied bytes to the
        /// final computed hash when the transform is subsequently finalised.
        /// </summary>
        [TestMethod]
        public void AppendData_WhenSpanContainsData_ShouldContributeToFinalHash()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = { 1, 2, 3, 4 };

            algorithm.AppendData(new ReadOnlySpan<byte>(data));
            algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            // MonitoringHashAlgorithm accumulates bytes as a uint sum.
            byte[] expected = BitConverter.GetBytes((uint)(1 + 2 + 3 + 4));
            CollectionAssert.AreEqual(expected, algorithm.Hash);
        }

        /// <summary>
        /// Verifies that calling <see cref="HashAlgorithmExtensions.AppendData" /> multiple times accumulates all
        /// supplied bytes correctly in the final hash, matching a single <c>ComputeHash</c> call over the concatenated input.
        /// </summary>
        [TestMethod]
        public void AppendData_WhenCalledMultipleTimes_ShouldAccumulateAllBytes()
        {
            using var algorithm = CreateAlgorithm();

            algorithm.AppendData(new ReadOnlySpan<byte>(new byte[] { 10, 20 }));
            algorithm.AppendData(new ReadOnlySpan<byte>(new byte[] { 30, 40 }));
            algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            byte[] expected = BitConverter.GetBytes((uint)(10 + 20 + 30 + 40));
            CollectionAssert.AreEqual(expected, algorithm.Hash);
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithmExtensions.AppendData" /> produces a hash identical to
        /// <see cref="HashAlgorithm.ComputeHash(byte[])" /> when given the same input.
        /// </summary>
        [TestMethod]
        public void AppendData_WhenHashFinalised_ShouldMatchComputeHash()
        {
            byte[] data = { 5, 10, 15, 20 };

            byte[] expected;
            using (var reference = CreateAlgorithm())
                expected = reference.ComputeHash(data);

            using var algorithm = CreateAlgorithm();
            algorithm.AppendData(new ReadOnlySpan<byte>(data));
            algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            CollectionAssert.AreEqual(expected, algorithm.Hash);
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithmExtensions.AppendData" /> increments the algorithm's
        /// <see cref="MonitoringHashAlgorithm.HashCoreCallCount" />, confirming the underlying
        /// <c>TransformBlock</c> path is exercised.
        /// </summary>
        [TestMethod]
        public void AppendData_WhenCalled_ShouldInvokeHashCoreOnce()
        {
            using var algorithm = CreateAlgorithm();
            int before = algorithm.HashCoreCallCount;

            algorithm.AppendData(new ReadOnlySpan<byte>(new byte[] { 1, 2, 3 }));

            Assert.AreEqual(before + 1, algorithm.HashCoreCallCount);
        }

        /// <summary>
        /// Verifies that a single-byte span contributes exactly that byte value to the computed hash.
        /// </summary>
        [TestMethod]
        public void AppendData_WhenSpanHasSingleByte_ShouldHashCorrectly()
        {
            using var algorithm = CreateAlgorithm();

            algorithm.AppendData(new ReadOnlySpan<byte>(new byte[] { 0xFF }));
            algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            byte[] expected = BitConverter.GetBytes((uint)0xFF);
            CollectionAssert.AreEqual(expected, algorithm.Hash);
        }
    }
}
