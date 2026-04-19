// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Direct regression tests for <see cref="ThreefishTransform" /> argument validation. These supplement the
    /// base-class <c>CreateEncryptor</c>/<c>CreateDecryptor</c> coverage by invoking the transform methods
    /// themselves with <see langword="null" /> buffers.
    /// </summary>
    [TestClass]
    public sealed class ThreefishTransformTests
    {
        /// <summary>
        /// Verifies that <see cref="ThreefishTransform.TransformBlock" /> throws
        /// <see cref="ArgumentNullException" /> when <c>inputBuffer</c> is <see langword="null" />.
        /// Regression guard for the prior <see cref="NullReferenceException" /> via <c>.AsSpan</c>.
        /// </summary>
        [TestMethod]
        public void TransformBlock_WhenInputBufferIsNull_ShouldThrowArgumentNullException_fix()
        {
            using var transform = CreateEncryptor();
            byte[] output = new byte[transform.OutputBlockSize];

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = transform.TransformBlock(null!, 0, 0, output, 0);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ThreefishTransform.TransformBlock" /> throws
        /// <see cref="ArgumentNullException" /> when <c>outputBuffer</c> is <see langword="null" />.
        /// Regression guard for the prior <see cref="NullReferenceException" /> via <c>.AsSpan</c>.
        /// </summary>
        [TestMethod]
        public void TransformBlock_WhenOutputBufferIsNull_ShouldThrowArgumentNullException_fix()
        {
            using var transform = CreateEncryptor();
            byte[] input = new byte[transform.InputBlockSize];

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = transform.TransformBlock(input, 0, input.Length, null!, 0);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ThreefishTransform.TransformFinalBlock" /> throws
        /// <see cref="ArgumentNullException" /> when <c>inputBuffer</c> is <see langword="null" />.
        /// Regression guard for the prior <see cref="NullReferenceException" /> via <c>.AsSpan</c>.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_WhenInputBufferIsNull_ShouldThrowArgumentNullException_fix()
        {
            using var transform = CreateEncryptor();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = transform.TransformFinalBlock(null!, 0, 0);
            });
        }

        private static System.Security.Cryptography.ICryptoTransform CreateEncryptor()
        {
            using var algorithm = new Threefish256();
            algorithm.GenerateKey();
            algorithm.GenerateIV();
            algorithm.GenerateTweak();
            return algorithm.CreateEncryptor(algorithm.Key, algorithm.IV, algorithm.Tweak);
        }
    }
}
