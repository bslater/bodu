// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensionsTests.TryVerifyHashAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// Tests for the asynchronous try-pattern <see cref="NonCryptographicHashAlgorithmExtensions.TryVerifyHashAsync" />
/// overloads.
/// </summary>
/// <remarks>
/// The try-pattern async overloads catch all exceptions (including
/// <see cref="OperationCanceledException" />) and return <see langword="false" /> rather than propagating them, with
/// the sole exception of a <see langword="null" /> algorithm receiver which still throws
/// <see cref="ArgumentNullException" />.
/// </remarks>
public partial class NonCryptographicHashAlgorithmExtensionsTests
{

    // ─── Argument validation ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a <see langword="null" /> algorithm receiver still raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenAlgorithmIsNull_ShouldThrowExactly()
    {
        NonCryptographicHashAlgorithm? algorithm = null;
        using MemoryStream stream = new(s_sampleData);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await algorithm!.TryVerifyHashAsync(stream, s_sampleHash));
    }

    /// <summary>
    /// Verifies that a non-matching byte-array input returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayDoesNotMatch_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        var wrong = BitConverter.GetBytes((uint)999);

        var result = await algorithm.TryVerifyHashAsync(s_sampleData, wrong);

        Assert.IsFalse(result);
    }

    // ─── Byte-array input ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a matching byte-array input returns <see langword="true" /> when expected hash is a byte array.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayMatchesByteArray_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = await algorithm.TryVerifyHashAsync(s_sampleData, s_sampleHash);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a matching byte-array input returns <see langword="true" /> when expected hash is a hex string.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayMatchesHex_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = await algorithm.TryVerifyHashAsync(s_sampleData, s_sampleHex);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a null expected hash returns <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenExpectedHashIsNull_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        var result = await algorithm.TryVerifyHashAsync(stream, (byte[])null!);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a null expected hex string still raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenExpectedHexIsNull_ShouldThrowExactly()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await algorithm.TryVerifyHashAsync(stream, (string)null!));
    }

    /// <summary>
    /// Verifies that a malformed hex string returns <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenHexStringIsMalformed_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        var result = await algorithm.TryVerifyHashAsync(stream, "ZZZZZZZZ");

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a non-matching stream input returns <see langword="false" /> for the byte-array overload.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamDoesNotMatchByteArray_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);
        var wrong = BitConverter.GetBytes((uint)999);

        var result = await algorithm.TryVerifyHashAsync(stream, wrong);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a null stream returns <see langword="false" /> rather than throwing for the byte-array overload.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamIsNull_ForByteArrayOverload_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = await algorithm.TryVerifyHashAsync((Stream)null!, s_sampleHash);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a null stream returns <see langword="false" /> for the hex overload.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamIsNull_ForHexOverload_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = await algorithm.TryVerifyHashAsync((Stream)null!, s_sampleHex);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a null stream returns <see langword="false" /> for the memory overload.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamIsNull_ForMemoryOverload_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        ReadOnlyMemory<byte> expected = s_sampleHash;

        var result = await algorithm.TryVerifyHashAsync((Stream)null!, expected);

        Assert.IsFalse(result);
    }
    // ─── Stream + byte-array ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a matching stream input returns <see langword="true" /> for the byte-array overload.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamMatchesByteArray_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        var result = await algorithm.TryVerifyHashAsync(stream, s_sampleHash);

        Assert.IsTrue(result);
    }

    // ─── Stream + hex string ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a matching stream input returns <see langword="true" /> for the hex overload.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamMatchesHex_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        var result = await algorithm.TryVerifyHashAsync(stream, s_sampleHex);

        Assert.IsTrue(result);
    }

    // ─── Stream + ReadOnlyMemory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a matching stream input returns <see langword="true" /> for the memory overload.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamMatchesMemory_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);
        ReadOnlyMemory<byte> expected = s_sampleHash;

        var result = await algorithm.TryVerifyHashAsync(stream, expected);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a non-matching encoded string input returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStringEncodedDoesNotMatch_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        var wrong = BitConverter.GetBytes((uint)999);

        var result = await algorithm.TryVerifyHashAsync(s_sampleString, s_sampleEncoding, wrong);

        Assert.IsFalse(result);
    }

    // ─── String + encoding ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a matching encoded string input returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStringEncodedMatches_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = await algorithm.TryVerifyHashAsync(s_sampleString, s_sampleEncoding, s_sampleStringHash);

        Assert.IsTrue(result);
    }

}
