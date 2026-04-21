// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsTests_VerifyHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography.Extensions
{
    /// <summary>
    /// Tests for the synchronous <see cref="HashAlgorithmExtensions.VerifyHash" /> overloads covering
    /// byte-array, span, memory, string-with-encoding, and stream inputs.
    /// </summary>
    /// <remarks>
    /// <c>VerifyHash</c> throws on any null argument (algorithm, input, expected hash, encoding) and
    /// returns a simple <see langword="true" />/<see langword="false" /> result for valid inputs;
    /// exception-swallowing behaviour belongs to the <c>TryVerifyHash</c> family instead.
    /// </remarks>
    public partial class HashAlgorithmExtensionsTests
    {
        // ─── Byte-array, span, and memory overloads — matching input ──────────────────────────────

        /// <summary>
        /// Verifies that the byte-array overload returns <see langword="true" /> when the input
        /// matches the expected hash.
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
        /// Verifies that the byte-array overload accepts an expected hash supplied as a hex string.
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
        /// Verifies that both the <see cref="ReadOnlySpan{T}" /> and <see cref="ReadOnlyMemory{T}" />
        /// overloads accept the same input and produce the same verification result.
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

        // ─── String-with-encoding overload ────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that the string overload encodes the input with the supplied encoding before
        /// hashing and returns <see langword="true" /> when the result matches.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenEncodedStringMatches_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            string input = "ABC"; // ASCII bytes sum to 65 + 66 + 67 = 198
            byte[] expected = BitConverter.GetBytes((uint)198);
            Assert.IsTrue(algorithm.VerifyHash(input, Encoding.ASCII, expected));
        }

        // ─── Stream overload ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that the stream overload reads the entire stream and returns
        /// <see langword="true" /> when the accumulated hash matches.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenStreamMatchesHash_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(new byte[] { 5, 5, 5 });
            byte[] expected = BitConverter.GetBytes((uint)15);
            Assert.IsTrue(algorithm.VerifyHash(stream, expected));
        }

        // ─── Mismatch and edge cases ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that the method returns <see langword="false" /> when the expected hash does not
        /// match the computed hash of the input.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenHashDoesNotMatch_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            byte[] expected = BitConverter.GetBytes((uint)999);
            Assert.IsFalse(algorithm.VerifyHash(SampleData, expected));
        }

        /// <summary>
        /// Verifies that an empty input produces a hash of 0, which does not match
        /// <see cref="SampleHash" />.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenInputIsEmpty_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            Assert.IsFalse(algorithm.VerifyHash(Array.Empty<byte>(), SampleHash));
        }

        // ─── Argument validation ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that a <see langword="null" /> algorithm receiver raises
        /// <see cref="ArgumentNullException" />.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                HashAlgorithm? algorithm = null;
                algorithm!.VerifyHash(SampleData, SampleHash);
            });
        }

        /// <summary>
        /// Verifies that a null expected hash byte array raises <see cref="ArgumentNullException" />.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenExpectedHashIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.VerifyHash(SampleData, (byte[])null!);
            });
        }

        /// <summary>
        /// Verifies that a null expected hex string raises <see cref="ArgumentNullException" />.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenExpectedHexIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.VerifyHash(SampleData, (string)null!);
            });
        }

        /// <summary>
        /// Verifies that a null expected byte array raises <see cref="ArgumentNullException" /> on
        /// the stream overload.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenStreamExpectedHashIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.VerifyHash(stream, (byte[])null!);
            });
        }

        /// <summary>
        /// Verifies that a null expected hex string raises <see cref="ArgumentNullException" /> on
        /// the stream overload.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenStreamExpectedHexIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.VerifyHash(stream, (string)null!);
            });
        }

        /// <summary>
        /// Verifies that a null string input raises <see cref="ArgumentNullException" /> on the
        /// string+encoding overload.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenStringInputIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.VerifyHash(null!, Encoding.ASCII, SampleHash);
            });
        }

        /// <summary>
        /// Verifies that a null encoding raises <see cref="ArgumentNullException" /> on the
        /// string+encoding overload.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenEncodingIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.VerifyHash("hello", null!, SampleHash);
            });
        }

        /// <summary>
        /// Verifies that a null expected hash raises <see cref="ArgumentNullException" /> on the
        /// string+encoding overload.
        /// </summary>
        [TestMethod]
        public void VerifyHash_WhenExpectedHashIsNullForString_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.VerifyHash("hello", Encoding.ASCII, null!);
            });
        }
    }
}