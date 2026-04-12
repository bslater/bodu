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
        public async Task TryVerifyHashAsync_WhenStreamMatchesHash_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            bool result = await algorithm.TryVerifyHashAsync(stream, SampleHash);
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync returns false for mismatched hash.
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
        /// Verifies that TryVerifyHashAsync returns false for empty input.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenInputIsEmpty_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            bool result = await algorithm.TryVerifyHashAsync(Array.Empty<byte>(), SampleHash);
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync throws ArgumentNullException if the expected hash is null.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenExpectedHashIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm.TryVerifyHashAsync(SampleData, (byte[])null!);
            });
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync throws ArgumentNullException if the input is null.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenInputIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm.TryVerifyHashAsync((byte[])null!, SampleHash);
            });
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync throws ArgumentNullException if the string input is null.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenStringInputIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm.TryVerifyHashAsync(null!, SampleEncoding, SampleStringHash);
            });
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync throws ArgumentNullException if the encoding is null.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync_WhenEncodingIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm.TryVerifyHashAsync(SampleString, null!, SampleStringHash);
            });
        }

        /// <summary>
        /// Verifies that TryVerifyHashAsync throws ArgumentNullException if the expected hash is null for string+encoding.
        /// </summary>
        [TestMethod]
        public async Task TryVerifyHashAsync__WhenExpectedHashIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await algorithm.TryVerifyHashAsync(SampleString, SampleEncoding, null!);
            });
        }
    }
}