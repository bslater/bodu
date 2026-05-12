// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensionsTests.TryVerifyHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.IO.Hashing;
using System.Text;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// Tests for the synchronous try-pattern <see cref="NonCryptographicHashAlgorithmExtensions.TryVerifyHash" />
/// overloads.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <c>VerifyHash</c>, the try-pattern treats a <see langword="null" /> byte-array input and a malformed hex
/// expected value as graceful non-matches (<see langword="false" />) rather than throwing.
/// </para>
/// <para>
/// Nulls that still raise <see cref="ArgumentNullException" /> are those that represent programmer error: a null
/// algorithm receiver (the extension method cannot dispatch without one) and the string-overload arguments
/// (<c>input</c>, <c>encoding</c>, and expected hash).
/// </para>
/// </remarks>
public partial class NonCryptographicHashAlgorithmExtensionsTests
{
    // ─── Matching input → true ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a matching byte-array input returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenByteArrayMatches_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        Assert.IsTrue(algorithm.TryVerifyHash(s_sampleData, s_sampleHash));
    }

    /// <summary>
    /// Verifies that a matching byte-array input returns <see langword="true" /> when the expected hash is supplied
    /// as a hex string.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenByteArrayMatchesHex_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        Assert.IsTrue(algorithm.TryVerifyHash(s_sampleData, s_sampleHex));
    }

    /// <summary>
    /// Verifies that a matching <see cref="ReadOnlySpan{T}" /> input returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenSpanMatches_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        ReadOnlySpan<byte> input = s_sampleData;
        ReadOnlySpan<byte> expected = s_sampleHash;

        Assert.IsTrue(algorithm.TryVerifyHash(input, expected));
    }

    /// <summary>
    /// Verifies that a matching <see cref="ReadOnlyMemory{T}" /> input returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenMemoryMatches_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        ReadOnlyMemory<byte> memory = s_sampleData;

        Assert.IsTrue(algorithm.TryVerifyHash(memory, s_sampleHash));
    }

    /// <summary>
    /// Verifies that a matching string + encoding input returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStringEncodedMatches_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        Assert.IsTrue(algorithm.TryVerifyHash(s_sampleString, s_sampleEncoding, s_sampleStringHash));
    }

    /// <summary>
    /// Verifies that a matching stream input returns <see langword="true" /> when the expected hash is a byte array.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStreamMatchesByteArray_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        Assert.IsTrue(algorithm.TryVerifyHash(stream, s_sampleHash));
    }

    /// <summary>
    /// Verifies that a matching stream input returns <see langword="true" /> when the expected hash is a hex string.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStreamMatchesHex_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        Assert.IsTrue(algorithm.TryVerifyHash(stream, s_sampleHex));
    }

    // ─── Non-matching input → false ───────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a non-matching byte-array input returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenByteArrayDoesNotMatch_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        var wrong = BitConverter.GetBytes((uint)999);

        Assert.IsFalse(algorithm.TryVerifyHash(s_sampleData, wrong));
    }

    /// <summary>
    /// Verifies that a malformed hex string is treated as a non-match and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenHexStringIsMalformed_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.IsFalse(algorithm.TryVerifyHash(s_sampleData, "ZZZZZZZZ"));
    }

    // ─── Null inputs handled gracefully ───────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a null byte-array input returns <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenByteArrayInputIsNull_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.IsFalse(algorithm.TryVerifyHash((byte[])null!, s_sampleHash));
    }

    /// <summary>
    /// Verifies that a null stream returns <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStreamIsNull_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.IsFalse(algorithm.TryVerifyHash((Stream)null!, s_sampleHash));
    }

    /// <summary>
    /// Verifies that a null stream in the hex overload returns <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStreamIsNull_ForHexOverload_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.IsFalse(algorithm.TryVerifyHash((Stream)null!, s_sampleHex));
    }

    // ─── Out-bool overload ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the out-bool overload returns <see langword="true" /> and sets <paramref name="result" /> to
    /// <see langword="true" /> when the hash matches.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenByteArrayMatchesWithOutBool_ShouldReturnTrueAndSetResult()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var succeeded = algorithm.TryVerifyHash(s_sampleData, s_sampleHash, out var result);

        Assert.IsTrue(succeeded);
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that the out-bool overload returns <see langword="true" /> and sets <paramref name="result" /> to
    /// <see langword="false" /> when the hash does not match.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenByteArrayDoesNotMatchWithOutBool_ShouldReturnTrueAndSetResultFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        var wrong = BitConverter.GetBytes((uint)999);

        var succeeded = algorithm.TryVerifyHash(s_sampleData, wrong, out var result);

        Assert.IsTrue(succeeded);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the out-bool overload returns <see langword="false" /> and sets <paramref name="result" /> to
    /// <see langword="false" /> when the input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenInputIsNullWithOutBool_ShouldReturnFalseAndSetResultFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var succeeded = algorithm.TryVerifyHash((byte[])null!, s_sampleHash, out var result);

        Assert.IsFalse(succeeded);
        Assert.IsFalse(result);
    }

    // ─── Argument validation — exceptions still thrown ────────────────────────────────────────

    /// <summary>
    /// Verifies that a <see langword="null" /> algorithm receiver still raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            NonCryptographicHashAlgorithm? algorithm = null;
            algorithm!.TryVerifyHash(s_sampleData, s_sampleHash);
        });
    }

    /// <summary>
    /// Verifies that a null expected hash in the byte-array overload still raises
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenExpectedHashIsNull_ShouldThrowArgumentNullException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.TryVerifyHash(s_sampleData, (byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that a null expected hex string still raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenExpectedHexIsNull_ShouldThrowArgumentNullException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.TryVerifyHash(s_sampleData, (string)null!);
        });
    }

    /// <summary>
    /// Verifies that a null expected hash in the stream overload still raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStreamExpectedHexIsNull_ShouldThrowArgumentNullException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.TryVerifyHash(new MemoryStream(), (string)null!);
        });
    }
}
