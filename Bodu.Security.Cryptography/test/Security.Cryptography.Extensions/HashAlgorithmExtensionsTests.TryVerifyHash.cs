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
        /// Verifies that TryVerifyHash returns true when span input matches the expected hash.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenSpanMatches_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            ReadOnlySpan<byte> spanInput = SampleData;
            ReadOnlySpan<byte> expected = SampleHash;
            Assert.IsTrue(algorithm.TryVerifyHash(spanInput, expected));
        }

        /// <summary>
        /// Verifies that TryVerifyHash returns true when memory input matches the expected hash.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenMemoryMatches_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            ReadOnlyMemory<byte> memory = SampleData;
            Assert.IsTrue(algorithm.TryVerifyHash(memory, SampleHash));
        }

        /// <summary>
        /// Verifies that TryVerifyHash returns true when the stream content matches the expected hex string.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenStreamMatchesHex_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            Assert.IsTrue(algorithm.TryVerifyHash(stream, SampleHex));
        }

        /// <summary>
        /// Verifies that TryVerifyHash returns true when the stream matches the expected hash.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenStreamMatchesMemory_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            ReadOnlyMemory<byte> expected = SampleHash;
            Assert.IsTrue(algorithm.TryVerifyHash(stream, expected.ToArray()));
        }

        /// <summary>
        /// Verifies that TryVerifyHash returns true when the byte array input matches the expected hash.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenByteArrayMatches_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            Assert.IsTrue(algorithm.TryVerifyHash(SampleData, SampleHash));
        }

        /// <summary>
        /// Verifies that TryVerifyHash returns true when the byte array input matches the expected hex string.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenByteArrayMatchesHex_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            Assert.IsTrue(algorithm.TryVerifyHash(SampleData, SampleHex));
        }

        /// <summary>
        /// Verifies that TryVerifyHash returns true when string and encoding match the expected hash.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenStringEncodedMatches_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            Assert.IsTrue(algorithm.TryVerifyHash(SampleString, SampleEncoding, SampleStringHash));
        }

        /// <summary>
        /// Verifies that TryVerifyHash returns false for empty input.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenInputIsEmpty_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            Assert.IsFalse(algorithm.TryVerifyHash(Array.Empty<byte>(), SampleHash));
        }

        /// <summary>
        /// Verifies that TryVerifyHash returns false for mismatched hash values.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenHashDoesNotMatch_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            byte[] badHash = BitConverter.GetBytes((uint)999);
            Assert.IsFalse(algorithm.TryVerifyHash(SampleData, badHash));
        }

        /// <summary>
        /// Verifies that TryVerifyHash returns false when the expected hash is null.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenExpectedHashIsNull__ShouldThrowExactly()
        {
            HashAlgorithm? algorithm = null;
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.TryVerifyHash(SampleData, (byte[])null!);
            });
        }

        /// <summary>
        /// Verifies that TryVerifyHash returns false when the input is null.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenInputIsNull_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            Assert.IsFalse(algorithm.TryVerifyHash((byte[])null!, SampleHash));
        }

        /// <summary>
        /// Verifies that TryVerifyHash throws ArgumentNullException when the algorithm is null.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenAlgorithmIsNull_ShouldThrowExactly()
        {
            HashAlgorithm? algorithm = null;
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm!.TryVerifyHash(SampleData, SampleHash);
            });
        }

        /// <summary>
        /// Verifies that TryVerifyHash throws ArgumentNullException when the algorithm is null.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenAStringInputIsNull_ShouldThrowExactly()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.TryVerifyHash(null!, SampleEncoding, SampleStringHash);
            });
        }

        /// <summary>
        /// Verifies that TryVerifyHash throws ArgumentNullException when the encoding is null.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenEncodingIsNull_ShouldThrowExactly()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.TryVerifyHash(SampleString, null!, SampleStringHash);
            });
        }

        /// <summary>
        /// Verifies that TryVerifyHash throws ArgumentNullException when the encoding is null.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenExpectedHashIsNul_ShouldThrowExactly()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.TryVerifyHash(SampleString, SampleEncoding, null!);
            });
        }
    }
}