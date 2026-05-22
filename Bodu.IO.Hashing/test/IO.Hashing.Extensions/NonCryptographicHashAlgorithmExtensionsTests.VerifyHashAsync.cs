// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensionsTests.VerifyHashAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using Bodu.Test.IO;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// Tests for the asynchronous <see cref="NonCryptographicHashAlgorithmExtensions.VerifyHashAsync" /> overloads
/// covering stream inputs, memory buffers, and hex strings.
/// </summary>
public partial class NonCryptographicHashAlgorithmExtensionsTests
{

    // ─── Argument validation ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a <see langword="null" /> algorithm raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenAlgorithmIsNull_ShouldThrowExactly()
    {
        NonCryptographicHashAlgorithm? algorithm = null;
        using MemoryStream stream = new(s_sampleData);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await algorithm!.VerifyHashAsync(stream, s_sampleHash));
    }

    // ─── State reset behaviour ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <c>VerifyHashAsync</c> resets prior accumulated state so it hashes only the stream content.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenCalledAfterAppend_ShouldIgnorePriorState()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append(new byte[] { 100, 200 }); // prior state — must be discarded

        using MemoryStream stream = new(s_sampleData);

        var result = await algorithm.VerifyHashAsync(stream, s_sampleHash);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> expected hash raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenExpectedHashIsNull_ShouldThrowExactly()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await algorithm.VerifyHashAsync(stream, (byte[])null!));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> expected hex string raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenExpectedHexIsNull_ShouldThrowExactly()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await algorithm.VerifyHashAsync(stream, (string)null!));
    }

    /// <summary>
    /// Verifies that a malformed hex string is treated as a non-match and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenHexStringIsMalformed_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        var result = await algorithm.VerifyHashAsync(stream, "ZZZZZZZZ");

        Assert.IsFalse(result);
    }

    // ─── Stream variants ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> delivering data 1 byte at a time is correctly hashed.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenSourceIsFixedChunkStream_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = await algorithm.VerifyHashAsync(new FixedChunkStream(s_sampleData, chunkSize: 1), s_sampleHash);

        Assert.IsTrue(result);
    }

    /// <summary>
    /// Verifies that the stream-with-byte-array overload returns <see langword="false" /> when the hashes differ.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenStreamDoesNotMatchByteArray_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);
        var wrong = BitConverter.GetBytes((uint)999);

        var result = await algorithm.VerifyHashAsync(stream, wrong);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the stream-with-memory overload returns <see langword="false" /> when the hashes differ.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenStreamDoesNotMatchMemory_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);
        ReadOnlyMemory<byte> wrong = BitConverter.GetBytes((uint)999);

        var result = await algorithm.VerifyHashAsync(stream, wrong);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> stream raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenStreamIsNull_ShouldThrowExactly()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await algorithm.VerifyHashAsync((Stream)null!, s_sampleHash));
    }
    // ─── Byte-array expected hash ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the stream-with-byte-array overload returns <see langword="true" /> when the hashes match.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenStreamMatchesByteArray_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        var result = await algorithm.VerifyHashAsync(stream, s_sampleHash);

        Assert.IsTrue(result);
    }

    // ─── Hex string expected hash ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the stream-with-hex overload returns <see langword="true" /> when the hashes match.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenStreamMatchesHex_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        var result = await algorithm.VerifyHashAsync(stream, s_sampleHex);

        Assert.IsTrue(result);
    }

    // ─── ReadOnlyMemory expected hash ─────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the stream-with-memory overload returns <see langword="true" /> when the hashes match.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenStreamMatchesMemory_ShouldReturnTrue()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);
        ReadOnlyMemory<byte> expected = s_sampleHash;

        var result = await algorithm.VerifyHashAsync(stream, expected);

        Assert.IsTrue(result);
    }

    // ─── Cancellation ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an already-cancelled token causes <see cref="OperationCanceledException" /> before any I/O.
    /// </summary>
    [TestMethod]
    public async Task VerifyHashAsync_WhenTokenAlreadyCancelled_ShouldThrowExactly()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await algorithm.VerifyHashAsync(stream, s_sampleHash, cts.Token));
    }

}
