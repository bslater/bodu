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
        /// Verifies that VerifyHash returns true when the byte array input matches the expected hash.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenByteArrayMatches_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            byte[] input = { 1, 2, 3, 4 };
            byte[] expected = BitConverter.GetBytes((uint)(1 + 2 + 3 + 4));
            Assert.IsTrue(algorithm.VerifyHash(input, expected));
        }

        /// <summary>
        /// Verifies that VerifyHash returns true when the byte array hash matches the expected hex string.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenByteArrayMatchesHex_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            byte[] input = { 10, 10 };
            string expectedHex = Convert.ToHexString(BitConverter.GetBytes((uint)20));
            Assert.IsTrue(algorithm.VerifyHash(input, expectedHex));
        }

        /// <summary>
        /// Verifies that VerifyHash returns true when the stream content matches the expected hash.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenStreamMatchesHash_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(new byte[] { 5, 5, 5 });
            byte[] expected = BitConverter.GetBytes((uint)15);
            Assert.IsTrue(algorithm.VerifyHash(stream, expected));
        }

        /// <summary>
        /// Verifies that VerifyHash returns true when the span and memory inputs match the expected hash.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenSpanAndMemoryMatch_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            ReadOnlySpan<byte> input = new byte[] { 6, 6 };
            ReadOnlyMemory<byte> memory = input.ToArray();
            byte[] expected = BitConverter.GetBytes((uint)12);
            Assert.IsTrue(algorithm.VerifyHash(input, expected));
            Assert.IsTrue(algorithm.VerifyHash(memory, expected));
        }

        /// <summary>
        /// Verifies that VerifyHash returns true when the encoded string matches the expected hash.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenEncodedStringMatches_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            string input = "ABC";
            Encoding encoding = Encoding.ASCII;
            byte[] expected = BitConverter.GetBytes((uint)198);
            Assert.IsTrue(algorithm.VerifyHash(input, encoding, expected));
        }

        /// <summary>
        /// Verifies that VerifyHash returns false for mismatched hash values.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenHashDoesNotMatch_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            byte[] expected = BitConverter.GetBytes((uint)999);
            Assert.IsFalse(algorithm.VerifyHash(SampleData, expected));
        }

        /// <summary>
        /// Verifies that VerifyHash returns false for an empty input.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenInputIsEmpty_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            Assert.IsFalse(algorithm.VerifyHash(Array.Empty<byte>(), SampleHash));
        }

        /// <summary>
        /// Verifies that VerifyHash throws ArgumentNullException for null algorithm.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenAlgorithmIsNull_ShouldThrow()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                HashAlgorithm? algorithm = null;
                algorithm.VerifyHash(SampleData, SampleHash);
            });
        }

        /// <summary>
        /// Verifies that VerifyHash throws ArgumentNullException for null expected hash.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenExpectedHashIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.VerifyHash(SampleData, (byte[])null!);
            });
        }

        /// <summary>
        /// Verifies that VerifyHash throws ArgumentNullException for null expected hex string.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenExpectedHexIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.VerifyHash(SampleData, (string)null!);
            });
        }

        /// <summary>
        /// Verifies that VerifyHash throws ArgumentNullException for null stream hash.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenStreamExpectedHashIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.VerifyHash(stream, (byte[])null!);
            });
        }

        /// <summary>
        /// Verifies that VerifyHash throws ArgumentNullException for null stream hex.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenStreamExpectedHexIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.VerifyHash(stream, (string)null!);
            });
        }

        /// <summary>
        /// Verifies that VerifyHash throws ArgumentNullException when string input is null.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenStringInputIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.VerifyHash(null!, Encoding.ASCII, SampleHash);
            });
        }

        /// <summary>
        /// Verifies that VerifyHash throws ArgumentNullException when encoding is null.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenEncodingIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.VerifyHash("hello", null!, SampleHash);
            });
        }

        /// <summary>
        /// Verifies that VerifyHash throws ArgumentNullException when expected hash is null for string input.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenExpectedHashIsNullForString_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.VerifyHash("hello", Encoding.ASCII, null!);
            });
        }
    }
}