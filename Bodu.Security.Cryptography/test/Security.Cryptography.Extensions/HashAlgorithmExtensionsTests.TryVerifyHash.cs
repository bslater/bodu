// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsTests.TryVerifyHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Tests for the synchronous try-pattern <see cref="HashAlgorithmExtensions.TryVerifyHash" />
/// overloads.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <c>VerifyHash</c>, the try-pattern treats every <see langword="null" /> data parameter
/// (<c>input</c>, <c>expectedHash</c>, <c>expectedHex</c>, <c>encoding</c>, <c>stream</c>),
/// empty input, and malformed hex as a graceful non-match (<see langword="false" />) rather than
/// throwing.
/// </para>
/// <para>
/// The only exception is the <c>algorithm</c> receiver: a <see langword="null" /> receiver still
/// raises <see cref="ArgumentNullException" />, since the extension method cannot dispatch
/// without one.
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
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        Assert.IsTrue(algorithm.TryVerifyHash(SampleData, SampleHash));
    }

    /// <summary>
    /// Verifies that a matching byte-array input returns <see langword="true" /> when the
    /// expected hash is supplied as a hex string.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenByteArrayMatchesHex_ShouldReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        Assert.IsTrue(algorithm.TryVerifyHash(SampleData, SampleHex));
    }

    /// <summary>
    /// Verifies that a matching <see cref="ReadOnlySpan{T}" /> input returns
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenSpanMatches_ShouldReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
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
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        ReadOnlyMemory<byte> memory = SampleData;
        Assert.IsTrue(algorithm.TryVerifyHash(memory, SampleHash));
    }

    /// <summary>
    /// Verifies that a matching string + encoding input returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStringEncodedMatches_ShouldReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        Assert.IsTrue(algorithm.TryVerifyHash(SampleString, SampleEncoding, SampleStringHash));
    }

    /// <summary>
    /// Verifies that a matching stream input returns <see langword="true" /> when the expected
    /// hash is supplied as a byte array.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStreamMatchesByteArray_ShouldReturnTrue()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
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
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        Assert.IsTrue(algorithm.TryVerifyHash(stream, SampleHex));
    }

    // ─── Graceful false returns ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a mismatched hash returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenHashDoesNotMatch_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        var badHash = BitConverter.GetBytes((uint)999);
        Assert.IsFalse(algorithm.TryVerifyHash(SampleData, badHash));
    }

    /// <summary>
    /// Verifies that an empty input returns <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenInputIsEmpty_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        Assert.IsFalse(algorithm.TryVerifyHash(Array.Empty<byte>(), SampleHash));
    }

    /// <summary>
    /// Verifies that a null byte-array input returns <see langword="false" /> rather than
    /// throwing — the try-pattern treats this as a verification failure, not a programmer error.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenInputIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        Assert.IsFalse(algorithm.TryVerifyHash((byte[])null!, SampleHash));
    }

    /// <summary>
    /// Verifies that a null expected hash on the byte-array overload returns
    /// <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenExpectedHashIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        Assert.IsFalse(algorithm.TryVerifyHash(SampleData, (byte[])null!));
    }

    /// <summary>
    /// Verifies that a null expected hex on the byte-array overload returns
    /// <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenByteArrayExpectedHexIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        Assert.IsFalse(algorithm.TryVerifyHash(SampleData, (string)null!));
    }

    /// <summary>
    /// Verifies that a malformed hex expected value returns <see langword="false" /> rather
    /// than surfacing a <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenHexIsMalformed_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        Assert.IsFalse(algorithm.TryVerifyHash(SampleData, "ZZZZ"));
    }

    /// <summary>
    /// Verifies that a null expected hash on the memory overload returns <see langword="false" />
    /// rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenMemoryExpectedHashIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        ReadOnlyMemory<byte> memory = SampleData;
        Assert.IsFalse(algorithm.TryVerifyHash(memory, (byte[])null!));
    }

    /// <summary>
    /// Verifies that a null expected hex on the stream overload returns <see langword="false" />
    /// rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStreamExpectedHexIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        Assert.IsFalse(algorithm.TryVerifyHash(stream, (string)null!));
    }

    /// <summary>
    /// Verifies that a null string input on the string+encoding overload returns
    /// <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStringInputIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        Assert.IsFalse(algorithm.TryVerifyHash(null!, SampleEncoding, SampleStringHash));
    }

    /// <summary>
    /// Verifies that a null encoding on the string+encoding overload returns
    /// <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenEncodingIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        Assert.IsFalse(algorithm.TryVerifyHash(SampleString, null!, SampleStringHash));
    }

    /// <summary>
    /// Verifies that a null expected hash on the string+encoding overload returns
    /// <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStringExpectedHashIsNull_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        Assert.IsFalse(algorithm.TryVerifyHash(SampleString, SampleEncoding, null!));
    }

    // ─── Argument validation ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a null algorithm receiver raises <see cref="ArgumentNullException" /> —
    /// the extension method cannot dispatch without a receiver.
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
    /// Verifies that a null algorithm still raises <see cref="ArgumentNullException" /> even
    /// when the expected hash is also null — receiver validation takes precedence over the
    /// graceful null-data handling.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenAlgorithmAndExpectedHashAreNull_ShouldThrowExactly()
    {
        HashAlgorithm? algorithm = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm!.TryVerifyHash(SampleData, (byte[])null!);
        });
    }
}
