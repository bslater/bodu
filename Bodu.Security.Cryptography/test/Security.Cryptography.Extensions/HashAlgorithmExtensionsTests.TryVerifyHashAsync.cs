// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsTests_TryVerifyHashAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography.Extensions
{
    public partial class HashAlgorithmExtensionsTests
    {
        /// <summary>
        /// Verifies that TryVerifyHashAsync returns true for matching byte array and expected hash.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenByteArrayMatches_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            bool result = await algorithm.TryVerifyHashAsync(SampleData, SampleHash);
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync returns true for matching byte array and expected hex string.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenByteArrayMatchesHex_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            bool result = await algorithm.TryVerifyHashAsync(SampleData, SampleHex);
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync returns true for string input encoded with encoding and matching hash.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenEncodedStringMatches_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            bool result = await algorithm.TryVerifyHashAsync(SampleString, SampleEncoding, SampleStringHash);
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync returns true for stream content matching expected hex string.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenStreamMatchesHex_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            bool result = await algorithm.TryVerifyHashAsync(stream, SampleHex);
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync returns true for stream content matching expected byte array.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenStreamMatchesByteArray_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            bool result = await algorithm.TryVerifyHashAsync(stream, SampleHash);
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync returns true for stream content matching expected ReadOnlyMemory hash.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenStreamMatchesMemory_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            ReadOnlyMemory<byte> expected = SampleHash;
            bool result = await algorithm.TryVerifyHashAsync(stream, expected);
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync returns false for a mismatched hash.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenHashDoesNotMatch_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            byte[] badHash = BitConverter.GetBytes((uint)1234);
            bool result = await algorithm.TryVerifyHashAsync(SampleData, badHash);
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync returns false when the stream argument is null.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenStreamIsNull_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            bool result = await algorithm.TryVerifyHashAsync((Stream)null!, SampleHash);
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync returns false when the stream expected hash is null.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenStreamExpectedHashIsNull_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            bool result = await algorithm.TryVerifyHashAsync(stream, (byte[])null!);
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync throws ArgumentNullException when the byte array input is null.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenByteArrayInputIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm.TryVerifyHashAsync((byte[])null!, SampleHash);
            });
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync throws ArgumentNullException when the byte array expected hash is null.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenByteArrayExpectedHashIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm.TryVerifyHashAsync(SampleData, (byte[])null!);
            });
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync throws ArgumentNullException when the string input is null.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenStringInputIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm.TryVerifyHashAsync(null!, SampleEncoding, SampleStringHash);
            });
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync throws ArgumentNullException when the encoding is null.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenEncodingIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm.TryVerifyHashAsync(SampleString, null!, SampleStringHash);
            });
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync throws ArgumentNullException when the string+encoding expected hash is null.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenStringExpectedHashIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm.TryVerifyHashAsync(SampleString, SampleEncoding, null!);
            });
        }
    }
}
