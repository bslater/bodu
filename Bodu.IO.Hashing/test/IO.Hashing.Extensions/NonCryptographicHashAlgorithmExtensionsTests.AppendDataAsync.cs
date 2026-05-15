// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensionsTests.AppendDataAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using Bodu.Test.IO;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// Tests for <see cref="NonCryptographicHashAlgorithmExtensions.AppendDataAsync" />.
/// </summary>
public partial class NonCryptographicHashAlgorithmExtensionsTests
{
    // ─── Argument validation ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithmExtensions.AppendDataAsync" /> throws
    /// <see cref="ArgumentNullException" /> when the algorithm is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
    {
        NonCryptographicHashAlgorithm? algorithm = null;
        using MemoryStream stream = new(s_sampleData);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await algorithm!.AppendDataAsync(stream));
    }

    /// <summary>
    /// Verifies that a negative <paramref name="bufferSize" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenBufferSizeIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            await algorithm.AppendDataAsync(stream, bufferSize: -1));
    }

    /// <summary>
    /// Verifies that a zero <paramref name="bufferSize" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenBufferSizeIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            await algorithm.AppendDataAsync(stream, bufferSize: 0));
    }

    /// <summary>
    /// Verifies that a small <paramref name="bufferSize" /> — forcing multiple read iterations — produces the correct
    /// hash.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenBufferSizeSmallerThanInput_ShouldProduceCorrectHash()
    {
        MonitoringNonCryptographicHashAlgorithm reference = CreateAlgorithm();
        reference.AppendData(s_sampleData.AsSpan());
        var expected = reference.GetCurrentHash();

        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        await algorithm.AppendDataAsync(new MemoryStream(s_sampleData), bufferSize: 1);

        CollectionAssert.AreEqual(expected, algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that stream bytes feed into the accumulator and produce the same digest as the synchronous span
    /// overload on the same data.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenCalled_ShouldMatchSynchronousAppendData()
    {
        MonitoringNonCryptographicHashAlgorithm reference = CreateAlgorithm();
        reference.AppendData(s_sampleData.AsSpan());
        var expected = reference.GetCurrentHash();

        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        await algorithm.AppendDataAsync(new MemoryStream(s_sampleData));

        CollectionAssert.AreEqual(expected, algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that multiple <see cref="NonCryptographicHashAlgorithmExtensions.AppendDataAsync" /> calls accumulate
    /// all bytes in order, producing the same result as hashing the concatenated data in a single operation.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenCalledMultipleTimes_ShouldAccumulateAllBytes()
    {
        byte[] part1 = [1, 2, 3, 4];
        byte[] part2 = [5, 6, 7, 8];
        byte[] combined = [1, 2, 3, 4, 5, 6, 7, 8];

        MonitoringNonCryptographicHashAlgorithm reference = CreateAlgorithm();
        reference.AppendData(combined.AsSpan());
        var expected = reference.GetCurrentHash();

        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        await algorithm.AppendDataAsync(new MemoryStream(part1));
        await algorithm.AppendDataAsync(new MemoryStream(part2));

        CollectionAssert.AreEqual(expected, algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that a <see cref="CancellationTriggerStream" /> cancelling the token mid-read causes
    /// <see cref="NonCryptographicHashAlgorithmExtensions.AppendDataAsync" /> to throw
    /// <see cref="OperationCanceledException" />.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenCancellationTriggeredMidStream_ShouldThrowOperationCanceledException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using CancellationTokenSource cts = new();

        // cancelAfterRead: 1 cancels the token after the first successful read; the next ReadAsync sees a
        // cancelled token and throws OperationCanceledException before reading any further bytes.
        using CancellationTriggerStream stream = new(new MemoryStream(s_sampleData), cts, cancelAfterRead: 1);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await algorithm.AppendDataAsync(stream, cancellationToken: cts.Token));
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithmExtensions.AppendDataAsync" /> interleaved with
    /// the synchronous <see cref="NonCryptographicHashAlgorithmExtensions.AppendData(NonCryptographicHashAlgorithm, ReadOnlySpan{byte})" />
    /// accumulates all bytes correctly.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenMixedWithSyncAppendData_ShouldAccumulateAllBytes()
    {
        byte[] part1 = [10, 20];
        byte[] part2 = [30, 40];
        byte[] combined = [10, 20, 30, 40];

        MonitoringNonCryptographicHashAlgorithm reference = CreateAlgorithm();
        reference.AppendData(combined.AsSpan());
        var expected = reference.GetCurrentHash();

        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.AppendData(part1.AsSpan());
        await algorithm.AppendDataAsync(new MemoryStream(part2));

        CollectionAssert.AreEqual(expected, algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that an <see cref="IOException" /> thrown by a <see cref="FaultingStream" /> mid-read propagates
    /// cleanly out of <see cref="NonCryptographicHashAlgorithmExtensions.AppendDataAsync" />.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenSourceFaultsMidRead_ShouldPropagateIOException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            await algorithm.AppendDataAsync(new FaultingStream(s_sampleData, throwAfterBytes: 2)));
    }

    // ─── Stream variants ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> delivering data 1 byte at a time is correctly accumulated.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenSourceIsFixedChunkStream_ShouldProduceCorrectHash()
    {
        MonitoringNonCryptographicHashAlgorithm reference = CreateAlgorithm();
        reference.AppendData(s_sampleData.AsSpan());
        var expected = reference.GetCurrentHash();

        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        await algorithm.AppendDataAsync(new FixedChunkStream(s_sampleData, chunkSize: 1));

        CollectionAssert.AreEqual(expected, algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that a non-seekable stream is correctly accumulated without invoking seek operations.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenSourceIsNonSeekable_ShouldProduceCorrectHash()
    {
        MonitoringNonCryptographicHashAlgorithm reference = CreateAlgorithm();
        reference.AppendData(s_sampleData.AsSpan());
        var expected = reference.GetCurrentHash();

        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        await algorithm.AppendDataAsync(new NonSeekableStream(s_sampleData));

        CollectionAssert.AreEqual(expected, algorithm.GetCurrentHash());
    }

    // ─── Correctness ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an empty stream does not alter the current hash state.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenStreamIsEmpty_ShouldNotContributeToHash()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        MonitoringNonCryptographicHashAlgorithm baseline = CreateAlgorithm();

        await algorithm.AppendDataAsync(new MemoryStream([]));

        CollectionAssert.AreEqual(baseline.GetCurrentHash(), algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithmExtensions.AppendDataAsync" /> throws
    /// <see cref="ArgumentNullException" /> when the source stream is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await algorithm.AppendDataAsync(null!));
    }

    // ─── Cancellation ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithmExtensions.AppendDataAsync" /> throws
    /// <see cref="OperationCanceledException" /> when the token is already cancelled before the call.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenTokenAlreadyCancelled_ShouldThrowOperationCanceledException()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await algorithm.AppendDataAsync(stream, cancellationToken: cts.Token));
    }

}
