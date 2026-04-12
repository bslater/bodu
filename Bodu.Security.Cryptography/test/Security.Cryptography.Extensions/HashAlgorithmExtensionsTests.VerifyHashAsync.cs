using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Security.Cryptography.Extensions;
using Bodu.Infrastructure;
using Bodu.Security.Cryptography;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions
{
    public partial class HashAlgorithmExtensionsTests
    {
        /// <summary>
        /// Verifies that VerifyHashAsync returns true when stream content matches expected hex string.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenStreamMatchesHex_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            Assert.IsTrue(await algorithm.VerifyHashAsync(stream, SampleHex));
        }

        /// <summary>
        /// Verifies that VerifyHashAsync returns true when stream content matches expected ReadOnlyMemory hash.
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
        /// Verifies that VerifyHashAsync throws ArgumentNullException when expected hash is null.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenExpectedHashIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm.VerifyHashAsync(stream, (byte[])null!);
            });
        }

        /// <summary>
        /// Verifies that VerifyHashAsync returns true when stream matches the expected byte array hash.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenStreamMatchesHash_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            Assert.IsTrue(await algorithm.VerifyHashAsync(stream, SampleHash));
        }

        /// <summary>
        /// Verifies that VerifyHashAsync returns true when stream matches the expected hex string.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenStreamMatchesHexDuplicate_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            Assert.IsTrue(await algorithm.VerifyHashAsync(stream, SampleHex));
        }

        /// <summary>
        /// Verifies that VerifyHashAsync returns true when stream matches the expected ReadOnlyMemory hash.
        /// </summary>
        [TestMethod]
        public async Task VerifyHashAsync_WhenStreamMatchesReadOnlyMemory_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            Assert.IsTrue(await algorithm.VerifyHashAsync(stream, new ReadOnlyMemory<byte>(SampleHash)));
        }

        /// <summary>
        /// Verifies that VerifyHashAsync returns true and stream reads are tracked when using MonitoringStream.
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