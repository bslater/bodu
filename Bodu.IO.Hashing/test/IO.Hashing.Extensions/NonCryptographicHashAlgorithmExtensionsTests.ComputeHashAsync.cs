// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensionsTests.ComputeHashAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.IO.Hashing;
using System.Threading;
using Bodu.Test.IO;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// Tests for <see cref="NonCryptographicHashAlgorithmExtensions.ComputeHashAsync(NonCryptographicHashAlgorithm, Stream, int, CancellationToken)" />.
/// </summary>
public partial class NonCryptographicHashAlgorithmExtensionsTests
{
    // ─── Argument validation ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithmExtensions.ComputeHashAsync" /> throws
    /// <see cref="ArgumentNullException" /> when the algorithm is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
    {
        NonCryptographicHashAlgorithm? algorithm = null;
        using MemoryStream stream = new(SampleData);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            _ = await algorithm!.ComputeHashAsync(stream));
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithmExtensions.ComputeHashAsync" /> throws
    /// <see cref="ArgumentNullException" /> when the source stream is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        var algorithm = CreateAlgorithm();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            _ = await algorithm.ComputeHashAsync(null!));
    }

    /// <summary>
    /// Verifies that a zero <paramref name="bufferSize" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenBufferSizeIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        var algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            _ = await algorithm.ComputeHashAsync(stream, bufferSize: 0));
    }

    /// <summary>
    /// Verifies that a negative <paramref name="bufferSize" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenBufferSizeIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            _ = await algorithm.ComputeHashAsync(stream, bufferSize: -1));
    }

    // ─── Correctness ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an empty stream produces the digest corresponding to a zero accumulator.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenStreamIsEmpty_ShouldReturnEmptyHash()
    {
        var algorithm = CreateAlgorithm();
        byte[] expected = BitConverter.GetBytes((uint)0);

        byte[] result = await algorithm.ComputeHashAsync(new MemoryStream(Array.Empty<byte>()));

        CollectionAssert.AreEqual(expected, result);
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithmExtensions.ComputeHashAsync" /> returns the same
    /// digest as the synchronous stream overload for identical input.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenCalled_ShouldMatchSynchronousComputeHash()
    {
        MonitoringNonCryptographicHashAlgorithm syncAlgorithm = CreateAlgorithm();
        byte[] expected = syncAlgorithm.ComputeHash(new MemoryStream(SampleData));

        var algorithm = CreateAlgorithm();
        byte[] result = await algorithm.ComputeHashAsync(new MemoryStream(SampleData));

        CollectionAssert.AreEqual(expected, result);
    }

    /// <summary>
    /// Verifies that a small <paramref name="bufferSize" /> — forcing multiple read iterations — produces the
    /// expected digest.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenBufferSizeSmallerThanInput_ShouldReturnExpectedHash()
    {
        var algorithm = CreateAlgorithm();

        byte[] result = await algorithm.ComputeHashAsync(new MemoryStream(SampleData), bufferSize: 1);

        CollectionAssert.AreEqual(SampleHash, result);
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithmExtensions.ComputeHashAsync" /> incorporates any
    /// state previously accumulated on the algorithm before reading from the stream.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenAlgorithmHasPriorState_ShouldIncludePriorBytesInHash()
    {
        var algorithm = CreateAlgorithm();
        algorithm.AppendData(new ReadOnlySpan<byte>(new byte[] { 25 }));
        byte[] expected = BitConverter.GetBytes((uint)(25 + 1 + 2 + 3 + 4));

        byte[] result = await algorithm.ComputeHashAsync(new MemoryStream(SampleData));

        CollectionAssert.AreEqual(expected, result);
    }

    /// <summary>
    /// Verifies that successive calls reset the algorithm so each computation begins from a clean state.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenCalledTwice_ShouldResetStateBetweenCalls()
    {
        var algorithm = CreateAlgorithm();

        byte[] first = await algorithm.ComputeHashAsync(new MemoryStream(SampleData));
        byte[] second = await algorithm.ComputeHashAsync(new MemoryStream(new byte[] { 11, 12 }));
        byte[] expectedSecond = BitConverter.GetBytes((uint)(11 + 12));

        CollectionAssert.AreEqual(SampleHash, first);
        CollectionAssert.AreEqual(expectedSecond, second);
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithmExtensions.ComputeHashAsync" /> increments
    /// <see cref="MonitoringNonCryptographicHashAlgorithm.ResetCallCount" />, confirming the underlying
    /// <c>GetHashAndReset</c> path is exercised.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenCalled_ShouldResetAlgorithm()
    {
        var algorithm = CreateAlgorithm();
        int before = algorithm.ResetCallCount;

        _ = await algorithm.ComputeHashAsync(new MemoryStream(SampleData));

        Assert.AreEqual(before + 1, algorithm.ResetCallCount);
    }

    // ─── Stream variants ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> delivering data 1 byte at a time produces the expected
    /// digest.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenSourceIsFixedChunkStream_ShouldReturnExpectedHash()
    {
        var algorithm = CreateAlgorithm();

        byte[] result = await algorithm.ComputeHashAsync(new FixedChunkStream(SampleData, chunkSize: 1));

        CollectionAssert.AreEqual(SampleHash, result);
    }

    /// <summary>
    /// Verifies that a non-seekable stream is correctly hashed without invoking seek operations.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenSourceIsNonSeekable_ShouldReturnExpectedHash()
    {
        var algorithm = CreateAlgorithm();

        byte[] result = await algorithm.ComputeHashAsync(new NonSeekableStream(SampleData));

        CollectionAssert.AreEqual(SampleHash, result);
    }

    /// <summary>
    /// Verifies that an <see cref="IOException" /> thrown by a <see cref="FaultingStream" /> mid-read propagates
    /// cleanly out of <see cref="NonCryptographicHashAlgorithmExtensions.ComputeHashAsync" />.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenSourceFaultsMidRead_ShouldPropagateIOException()
    {
        var algorithm = CreateAlgorithm();

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            _ = await algorithm.ComputeHashAsync(new FaultingStream(SampleData, throwAfterBytes: 2)));
    }

    // ─── Cancellation ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithmExtensions.ComputeHashAsync" /> throws
    /// <see cref="OperationCanceledException" /> when the token is already cancelled before the call.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenTokenAlreadyCancelled_ShouldThrowOperationCanceledException()
    {
        var algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            _ = await algorithm.ComputeHashAsync(stream, cancellationToken: cts.Token));
    }

    /// <summary>
    /// Verifies that a <see cref="CancellationTriggerStream" /> cancelling the token mid-read causes
    /// <see cref="NonCryptographicHashAlgorithmExtensions.ComputeHashAsync" /> to throw
    /// <see cref="OperationCanceledException" />.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenCancellationTriggeredMidStream_ShouldThrowOperationCanceledException()
    {
        var algorithm = CreateAlgorithm();
        using CancellationTokenSource cts = new();

        // cancelAfterRead: 1 cancels the token after the first successful read; the next ReadAsync sees a
        // cancelled token and throws OperationCanceledException before reading any further bytes.
        using CancellationTriggerStream stream = new(
            new MemoryStream(SampleData),
            cts,
            cancelAfterRead: 1);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            _ = await algorithm.ComputeHashAsync(stream, bufferSize: 1, cancellationToken: cts.Token));
    }
}
