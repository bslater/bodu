// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsTests_VerifyHashAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Bodu.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography.Extensions
{
    public partial class HashAlgorithmExtensionsTests
    {
        /// <summary>
        /// Verifies that VerifyHashAsync returns true when stream content matches the expected hex string.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenStreamMatchesHex_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            Assert.IsTrue(await algorithm.VerifyHashAsync(stream, SampleHex));
        }

        /// <summary>
        /// Verifies that VerifyHashAsync returns true when stream content matches the expected byte array.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenStreamMatchesByteArray_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            Assert.IsTrue(await algorithm.VerifyHashAsync(stream, SampleHash));
        }

        /// <summary>
        /// Verifies that VerifyHashAsync returns true when stream content matches the expected ReadOnlyMemory hash.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenStreamMatchesMemory_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            ReadOnlyMemory<byte> expected = SampleHash;
            Assert.IsTrue(await algorithm.VerifyHashAsync(stream, expected));
        }

        /// <summary>
        /// Verifies that VerifyHashAsync returns false for mismatched hash values.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenHashDoesNotMatch_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            byte[] badHash = BitConverter.GetBytes((uint)9999);
            Assert.IsFalse(await algorithm.VerifyHashAsync(stream, badHash));
        }

        /// <summary>
        /// Verifies that VerifyHashAsync returns false when the expected hex string is malformed, without consuming the stream.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenHexIsMalformed_ShouldReturnFalseWithoutReadingStream()
        {
            using var algorithm = CreateAlgorithm();
            using var baseStream = new MemoryStream(SampleData);
            using var monitored = new MonitoringStream(baseStream);

            bool result = await algorithm.VerifyHashAsync(monitored, "ZZZZ");

            Assert.IsFalse(result);
            Assert.AreEqual(0, monitored.Reads.Count);
        }

        /// <summary>
        /// Verifies that VerifyHashAsync throws ArgumentNullException when the algorithm is null.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
        {
            HashAlgorithm? algorithm = null;
            using var stream = new MemoryStream(SampleData);
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm!.VerifyHashAsync(stream, SampleHash);
            });
        }

        /// <summary>
        /// Verifies that VerifyHashAsync throws ArgumentNullException when the stream is null.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenStreamIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm.VerifyHashAsync((Stream)null!, SampleHash);
            });
        }

        /// <summary>
        /// Verifies that VerifyHashAsync throws ArgumentNullException when the expected byte array is null.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenExpectedHashIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm.VerifyHashAsync(stream, (byte[])null!);
            });
        }

        /// <summary>
        /// Verifies that VerifyHashAsync throws ArgumentNullException when the expected hex string is null.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenExpectedHexIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm.VerifyHashAsync(stream, (string)null!);
            });
        }

        /// <summary>
        /// Verifies that VerifyHashAsync throws OperationCanceledException when the token is already cancelled.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenTokenAlreadyCancelled_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            {
                await algorithm.VerifyHashAsync(stream, SampleHash, cts.Token);
            });
        }

        /// <summary>
        /// Verifies that VerifyHashAsync tracks stream reads via MonitoringStream when computing the hash.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenUsingMonitoringStream_ShouldTrackReadsAndReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var baseStream = new MemoryStream(new byte[] { 2, 3 });
            using var monitored = new MonitoringStream(baseStream);
            byte[] expected = BitConverter.GetBytes((uint)5);

            bool result = await algorithm.VerifyHashAsync(monitored, expected);

            Assert.IsTrue(result);
            Assert.IsTrue(monitored.Reads.Count > 0);
        }
    }
}
