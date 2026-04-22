// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsTests.TryVerifyHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions
{
    /// <summary>
    /// Tests for the synchronous try-pattern <see cref="HashAlgorithmExtensions.TryVerifyHash" />
    /// overloads.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike <c>VerifyHash</c>, the try-pattern treats empty input, a null byte-array input, and a
    /// malformed hex expected value as graceful non-matches (<see langword="false" />) rather than
    /// throwing.
    /// </para>
    /// <para>
    /// Nulls that still raise <see cref="ArgumentNullException" /> are those that represent
    /// programmer error rather than a failed verification — a null algorithm receiver (the extension
    /// method cannot dispatch without one) and the string-overload arguments (<c>input</c>,
    /// <c>encoding</c>, and expected hash).
    /// </para>
    /// </remarks>
    public partial class HashAlgorithmExtensionsTests
    {
        // ─── Matching input → true ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that a matching byte-array input returns <see langword="true" />.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenByteArrayMatches_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            Assert.IsTrue(algorithm.TryVerifyHash(SampleData, SampleHash));
        }

        /// <summary>
        /// Verifies that a matching byte-array input returns <see langword="true" /> when the
        /// expected hash is supplied as a hex string.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenByteArrayMatchesHex_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            Assert.IsTrue(algorithm.TryVerifyHash(SampleData, SampleHex));
        }

        /// <summary>
        /// Verifies that a matching <see cref="ReadOnlySpan{T}" /> input returns
        /// <see langword="true" />.
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
        /// Verifies that a matching <see cref="ReadOnlyMemory{T}" /> input returns
        /// <see langword="true" />.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenMemoryMatches_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            ReadOnlyMemory<byte> memory = SampleData;
            Assert.IsTrue(algorithm.TryVerifyHash(memory, SampleHash));
        }

        /// <summary>
        /// Verifies that a matching string + encoding input returns <see langword="true" />.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenStringEncodedMatches_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            Assert.IsTrue(algorithm.TryVerifyHash(SampleString, SampleEncoding, SampleStringHash));
        }

        /// <summary>
        /// Verifies that a matching stream input returns <see langword="true" /> when the expected
        /// hash is supplied as a byte array.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenStreamMatchesByteArray_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            Assert.IsTrue(algorithm.TryVerifyHash(stream, SampleHash));
        }

        /// <summary>
        /// Verifies that a matching stream input returns <see langword="true" /> when the expected
        /// hash is supplied as a hex string.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenStreamMatchesHex_ShouldReturnTrue()
        {
            using var algorithm = CreateAlgorithm();
            using var stream = new MemoryStream(SampleData);
            Assert.IsTrue(algorithm.TryVerifyHash(stream, SampleHex));
        }

        // ─── Out-parameter variant ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that the out-parameter overload returns <see langword="true" /> from the method
        /// and sets the <c>result</c> out parameter to <see langword="true" /> when the inputs match.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_OutVariant_WhenByteArrayMatches_ShouldReturnTrueAndSetResult()
        {
            using var algorithm = CreateAlgorithm();

            bool ok = algorithm.TryVerifyHash(SampleData, SampleHash, out bool result);

            Assert.IsTrue(ok);
            Assert.IsTrue(result);
        }

        // ─── Graceful false returns ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that a mismatched hash returns <see langword="false" />.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenHashDoesNotMatch_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            byte[] badHash = BitConverter.GetBytes((uint)999);
            Assert.IsFalse(algorithm.TryVerifyHash(SampleData, badHash));
        }

        /// <summary>
        /// Verifies that an empty input returns <see langword="false" /> rather than throwing.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenInputIsEmpty_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            Assert.IsFalse(algorithm.TryVerifyHash(Array.Empty<byte>(), SampleHash));
        }

        /// <summary>
        /// Verifies that a null byte-array input returns <see langword="false" /> rather than
        /// throwing — the try-pattern treats this as a verification failure, not a programmer error.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenInputIsNull_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            Assert.IsFalse(algorithm.TryVerifyHash((byte[])null!, SampleHash));
        }

        /// <summary>
        /// Verifies that a malformed hex expected value returns <see langword="false" /> rather
        /// than surfacing a <see cref="FormatException" />.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenHexIsMalformed_ShouldReturnFalse()
        {
            using var algorithm = CreateAlgorithm();
            Assert.IsFalse(algorithm.TryVerifyHash(SampleData, "ZZZZ"));
        }

        // ─── Argument validation ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that a null algorithm receiver raises <see cref="ArgumentNullException" /> —
        /// the extension method cannot dispatch without a receiver.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
        {
            HashAlgorithm? algorithm = null;
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm!.TryVerifyHash(SampleData, SampleHash);
            });
        }

        /// <summary>
        /// Verifies that a null algorithm still raises <see cref="ArgumentNullException" /> even
        /// when the expected hash is also null — receiver validation takes precedence.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenAlgorithmAndExpectedHashAreNull_ShouldThrowArgumentNullException()
        {
            HashAlgorithm? algorithm = null;
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm!.TryVerifyHash(SampleData, (byte[])null!);
            });
        }

        /// <summary>
        /// Verifies that a null string input raises <see cref="ArgumentNullException" /> on the
        /// string+encoding overload.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenStringInputIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.TryVerifyHash(null!, SampleEncoding, SampleStringHash);
            });
        }

        /// <summary>
        /// Verifies that a null encoding raises <see cref="ArgumentNullException" /> on the
        /// string+encoding overload.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenEncodingIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.TryVerifyHash(SampleString, null!, SampleStringHash);
            });
        }

        /// <summary>
        /// Verifies that a null expected hash raises <see cref="ArgumentNullException" /> on the
        /// string+encoding overload.
        /// </summary>
        [TestMethod]
        public void TryVerifyHash_WhenStringExpectedHashIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.TryVerifyHash(SampleString, SampleEncoding, null!);
            });
        }
    }
}