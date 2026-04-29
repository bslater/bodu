// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensionsTests.VerifyHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.IO.Hashing;
using System.Text;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// Tests for the synchronous <see cref="NonCryptographicHashAlgorithmExtensions.VerifyHash" /> overloads covering
/// byte-array, span, memory, string-with-encoding, and stream inputs.
/// </summary>
/// <remarks>
/// <c>VerifyHash</c> throws on any null argument (algorithm, input, expected hash, encoding) and returns a simple
/// <see langword="true" />/<see langword="false" /> result for valid inputs. Exception-swallowing behaviour belongs
/// to the <c>TryVerifyHash</c> family instead.
/// </remarks>
public partial class NonCryptographicHashAlgorithmExtensionsTests
{
    // ─── Byte-array overload — matching input ─────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the byte-array overload returns <see langword="true" /> when the input matches the expected hash.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenByteArrayMatches_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        byte[] input = { 1, 2, 3, 4 };
        byte[] expected = BitConverter.GetBytes((uint)(1 + 2 + 3 + 4));

        Assert.IsTrue(algorithm.VerifyHash(input, expected));
    }

    /// <summary>
    /// Verifies that the byte-array overload accepts an expected hash supplied as a hex string and returns
    /// <see langword="true" /> when the hashes match.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenByteArrayMatchesHex_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        byte[] input = { 10, 10 };
        string expectedHex = Convert.ToHexString(BitConverter.GetBytes((uint)20));

        Assert.IsTrue(algorithm.VerifyHash(input, expectedHex));
    }

    /// <summary>
    /// Verifies that the byte-array overload returns <see langword="false" /> when the input produces a different hash.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenByteArrayDoesNotMatch_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        byte[] wrong = BitConverter.GetBytes((uint)999);

        Assert.IsFalse(algorithm.VerifyHash(SampleData, wrong));
    }

    /// <summary>
    /// Verifies that a malformed hex string is treated as a non-match and returns <see langword="false" /> rather than
    /// throwing.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenHexStringIsMalformed_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.IsFalse(algorithm.VerifyHash(SampleData, "ZZZZZZZZ"));
    }

    /// <summary>
    /// Verifies that an empty input produces a hash of zero, which does not match <see cref="SampleHash" />.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenInputIsEmpty_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.IsFalse(algorithm.VerifyHash(Array.Empty<byte>(), SampleHash));
    }

    /// <summary>
    /// Verifies that <c>VerifyHash</c> resets any prior accumulated state before computing, so successive calls
    /// with different inputs produce independent results.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenCalledSuccessively_ShouldResetStateBeforeEachComputation()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.IsTrue(algorithm.VerifyHash(SampleData, SampleHash));
        Assert.IsTrue(algorithm.VerifyHash(SampleData, SampleHash));
    }

    // ─── Span and memory overloads ────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that both the <see cref="ReadOnlySpan{T}" /> and <see cref="ReadOnlyMemory{T}" /> overloads accept
    /// the same input and produce the same verification result.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenSpanAndMemoryMatch_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        byte[] input = { 6, 6 };
        ReadOnlyMemory<byte> memory = input;
        byte[] expected = BitConverter.GetBytes((uint)12);

        Assert.IsTrue(algorithm.VerifyHash(new ReadOnlySpan<byte>(input), new ReadOnlySpan<byte>(expected)));
        Assert.IsTrue(algorithm.VerifyHash(memory, expected));
    }

    /// <summary>
    /// Verifies that the span overload returns <see langword="false" /> when the hashes differ.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenSpanDoesNotMatch_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        ReadOnlySpan<byte> wrong = BitConverter.GetBytes((uint)999);

        Assert.IsFalse(algorithm.VerifyHash(new ReadOnlySpan<byte>(SampleData), wrong));
    }

    // ─── String-with-encoding overload ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the string overload encodes the input with the supplied encoding before hashing and returns
    /// <see langword="true" /> when the result matches.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenEncodedStringMatches_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        string input = "ABC"; // ASCII: 65 + 66 + 67 = 198
        byte[] expected = BitConverter.GetBytes((uint)198);

        Assert.IsTrue(algorithm.VerifyHash(input, Encoding.ASCII, expected));
    }

    /// <summary>
    /// Verifies that the string overload returns <see langword="false" /> when the encoded hash does not match.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenEncodedStringDoesNotMatch_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        byte[] wrong = BitConverter.GetBytes((uint)999);

        Assert.IsFalse(algorithm.VerifyHash(SampleString, SampleEncoding, wrong));
    }

    // ─── Stream overload ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the stream overload reads the entire stream and returns <see langword="true" /> when the
    /// accumulated hash matches.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenStreamMatchesHash_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(new byte[] { 5, 5, 5 });
        byte[] expected = BitConverter.GetBytes((uint)15);

        Assert.IsTrue(algorithm.VerifyHash(stream, expected));
    }

    /// <summary>
    /// Verifies that the stream-with-hex overload returns <see langword="true" /> when the stream hash matches the
    /// hex string.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenStreamMatchesHex_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);

        Assert.IsTrue(algorithm.VerifyHash(stream, SampleHex));
    }

    /// <summary>
    /// Verifies that the stream overload resets prior state before reading, so it hashes only the stream content.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenStreamCalledAfterAppend_ShouldIgnorePriorState()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append(new byte[] { 100, 200 }); // prior state — must be discarded

        using MemoryStream stream = new(SampleData);

        Assert.IsTrue(algorithm.VerifyHash(stream, SampleHash));
    }

    // ─── Argument validation ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a <see langword="null" /> algorithm receiver raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            NonCryptographicHashAlgorithm? algorithm = null;
            algorithm!.VerifyHash(SampleData, SampleHash);
        });
    }

    /// <summary>
    /// Verifies that a null input byte array raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenInputIsNull_ShouldThrowArgumentNullException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.VerifyHash((byte[])null!, SampleHash);
        });
    }

    /// <summary>
    /// Verifies that a null expected hash byte array raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenExpectedHashIsNull_ShouldThrowArgumentNullException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

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
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.VerifyHash(SampleData, (string)null!);
        });
    }

    /// <summary>
    /// Verifies that a null stream raises <see cref="ArgumentNullException" /> on the stream-with-byte-array overload.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.VerifyHash((Stream)null!, SampleHash);
        });
    }

    /// <summary>
    /// Verifies that a null expected hash raises <see cref="ArgumentNullException" /> on the stream overload.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenStreamExpectedHashIsNull_ShouldThrowArgumentNullException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.VerifyHash(stream, (byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that a null expected hex string raises <see cref="ArgumentNullException" /> on the stream overload.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenStreamExpectedHexIsNull_ShouldThrowArgumentNullException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.VerifyHash(stream, (string)null!);
        });
    }

    /// <summary>
    /// Verifies that a null string input raises <see cref="ArgumentNullException" /> on the string+encoding overload.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenStringInputIsNull_ShouldThrowArgumentNullException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.VerifyHash(null!, Encoding.ASCII, SampleHash);
        });
    }

    /// <summary>
    /// Verifies that a null encoding raises <see cref="ArgumentNullException" /> on the string+encoding overload.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenEncodingIsNull_ShouldThrowArgumentNullException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.VerifyHash("hello", null!, SampleHash);
        });
    }

    /// <summary>
    /// Verifies that a null expected hash raises <see cref="ArgumentNullException" /> on the string+encoding overload.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenExpectedHashIsNullForString_ShouldThrowArgumentNullException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.VerifyHash("hello", Encoding.ASCII, null!);
        });
    }

    /// <summary>
    /// Verifies that a null algorithm raises <see cref="ArgumentNullException" /> on the span overload.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenAlgorithmIsNull_ForSpanOverload_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            NonCryptographicHashAlgorithm? algorithm = null;
            algorithm!.VerifyHash(new ReadOnlySpan<byte>(SampleData), new ReadOnlySpan<byte>(SampleHash));
        });
    }

    /// <summary>
    /// Verifies that a null algorithm raises <see cref="ArgumentNullException" /> on the memory overload.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenAlgorithmIsNull_ForMemoryOverload_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            NonCryptographicHashAlgorithm? algorithm = null;
            algorithm!.VerifyHash(new ReadOnlyMemory<byte>(SampleData), SampleHash);
        });
    }

    /// <summary>
    /// Verifies that a null expected hash raises <see cref="ArgumentNullException" /> on the memory overload.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenExpectedHashIsNull_ForMemoryOverload_ShouldThrowArgumentNullException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.VerifyHash(new ReadOnlyMemory<byte>(SampleData), null!);
        });
    }
}
