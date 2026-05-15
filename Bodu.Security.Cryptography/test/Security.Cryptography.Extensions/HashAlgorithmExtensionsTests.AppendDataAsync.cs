// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsTests.AppendDataAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.IO;

namespace Bodu.Security.Cryptography.Extensions;

public partial class HashAlgorithmExtensionsTests
{
    // ─── AppendDataAsync — argument validation ────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.AppendDataAsync" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="algorithm" /> is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
    {
        HashAlgorithm? algorithm = null;
        using var stream = new MemoryStream(SampleData);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await algorithm!.AppendDataAsync(stream));
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.AppendDataAsync" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="source" /> is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await algorithm.AppendDataAsync(null!));
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.AppendDataAsync" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="bufferSize" /> is zero.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenBufferSizeIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            await algorithm.AppendDataAsync(stream, bufferSize: 0));
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.AppendDataAsync" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="bufferSize" /> is
    /// negative.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenBufferSizeIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            await algorithm.AppendDataAsync(stream, bufferSize: -1));
    }

    // ─── AppendDataAsync — correctness ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.AppendDataAsync" /> with an empty stream
    /// does not contribute any bytes to the hash, so that finalising immediately after produces
    /// the same result as hashing an empty input.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenStreamIsEmpty_ShouldNotContributeToHash()
    {
        byte[] emptyHash;
        using (MonitoringHashAlgorithm reference = CreateAlgorithm())
            emptyHash = reference.ComputeHash(Array.Empty<byte>());

        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        await algorithm.AppendDataAsync(new MemoryStream(Array.Empty<byte>()));
        algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        CollectionAssert.AreEqual(emptyHash, algorithm.Hash,
            "AppendDataAsync with an empty stream must not alter the hash state.");
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.AppendDataAsync" /> feeds all stream
    /// bytes into the accumulator, so that finalising produces the same result as
    /// <see cref="HashAlgorithm.ComputeHash(byte[])" /> on the same data.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenCalled_ShouldContributeToFinalHash()
    {
        byte[] expected;
        using (MonitoringHashAlgorithm reference = CreateAlgorithm())
            expected = reference.ComputeHash(SampleData);

        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        await algorithm.AppendDataAsync(new MemoryStream(SampleData));
        algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        CollectionAssert.AreEqual(expected, algorithm.Hash,
            "AppendDataAsync must feed all stream bytes into the accumulator.");
    }

    /// <summary>
    /// Verifies that multiple <see cref="HashAlgorithmExtensions.AppendDataAsync" /> calls
    /// accumulate all bytes in order, producing the same result as hashing the concatenated
    /// data in a single operation.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenCalledMultipleTimes_ShouldAccumulateAllBytes()
    {
        var part1 = CryptoTestUtilities.ByteSequence64;
        var part2 = CryptoTestUtilities.ByteSequence128;

        var combined = new byte[part1.Length + part2.Length];
        part1.CopyTo(combined, 0);
        part2.CopyTo(combined, part1.Length);

        byte[] expected;
        using (MonitoringHashAlgorithm reference = CreateAlgorithm())
            expected = reference.ComputeHash(combined);

        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        await algorithm.AppendDataAsync(new MemoryStream(part1));
        await algorithm.AppendDataAsync(new MemoryStream(part2));
        algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        CollectionAssert.AreEqual(expected, algorithm.Hash,
            "Multiple AppendDataAsync calls must accumulate all bytes in order.");
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.AppendDataAsync" /> interleaved with
    /// the synchronous <see cref="HashAlgorithmExtensions.AppendData(HashAlgorithm,System.ReadOnlySpan{byte})" />
    /// accumulates all bytes correctly, confirming both methods delegate to
    /// <see cref="HashAlgorithm.TransformBlock" /> on the same state.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenMixedWithSyncAppendData_ShouldAccumulateAllBytes()
    {
        var part1 = new byte[] { 1, 2, 3, 4 };
        var part2 = new byte[] { 5, 6, 7, 8 };
        var combined = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        byte[] expected;
        using (MonitoringHashAlgorithm reference = CreateAlgorithm())
            expected = reference.ComputeHash(combined);

        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();

        // Synchronous append of first half, async append of second half.
        algorithm.AppendData(part1.AsSpan());
        await algorithm.AppendDataAsync(new MemoryStream(part2));
        algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        CollectionAssert.AreEqual(expected, algorithm.Hash,
            "Sync AppendData and async AppendDataAsync must accumulate bytes on the same hash state.");
    }

    /// <summary>
    /// Verifies that the hash produced after <see cref="HashAlgorithmExtensions.AppendDataAsync" />
    /// with a small <paramref name="bufferSize" /> — forcing multiple read iterations — matches
    /// the result from a single-read operation on the same data.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenBufferSizeSmallerThanInput_ShouldProduceCorrectHash()
    {
        var data = CryptoTestUtilities.ByteSequence128;

        byte[] expected;
        using (MonitoringHashAlgorithm reference = CreateAlgorithm())
            expected = reference.ComputeHash(data);

        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        await algorithm.AppendDataAsync(new MemoryStream(data), bufferSize: 7);
        algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        CollectionAssert.AreEqual(expected, algorithm.Hash,
            "AppendDataAsync must produce the correct hash regardless of buffer size.");
    }

    // ─── AppendDataAsync — stream variants ───────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.AppendDataAsync" /> with a
    /// <see cref="FixedChunkStream" /> delivering data 1 byte at a time accumulates all bytes
    /// correctly.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenSourceIsFixedChunkStream_ShouldProduceCorrectHash()
    {
        byte[] expected;
        using (MonitoringHashAlgorithm reference = CreateAlgorithm())
            expected = reference.ComputeHash(SampleData);

        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        await algorithm.AppendDataAsync(new FixedChunkStream(SampleData, chunkSize: 1));
        algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        CollectionAssert.AreEqual(expected, algorithm.Hash,
            "AppendDataAsync must accumulate correctly from a 1-byte-per-read source.");
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.AppendDataAsync" /> with a
    /// <see cref="NonSeekableStream" /> accumulates all bytes correctly, confirming the method
    /// never calls seek-related members on the source.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenSourceIsNonSeekable_ShouldProduceCorrectHash()
    {
        byte[] expected;
        using (MonitoringHashAlgorithm reference = CreateAlgorithm())
            expected = reference.ComputeHash(SampleData);

        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        await algorithm.AppendDataAsync(new NonSeekableStream(SampleData));
        algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        CollectionAssert.AreEqual(expected, algorithm.Hash,
            "AppendDataAsync must accumulate correctly from a NonSeekableStream.");
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.AppendDataAsync" /> with a
    /// <see cref="ThrottledIncrementingByteStream" /> — which delays each read — still
    /// accumulates all bytes correctly.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenSourceIsThrottledStream_ShouldProduceCorrectHash()
    {
        const int length = 64;

        byte[] expected;
        using (MonitoringHashAlgorithm reference = CreateAlgorithm())
            expected = reference.ComputeHash(new IncrementingByteStream(length).ToArray());

        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        await algorithm.AppendDataAsync(new ThrottledIncrementingByteStream(length, readDelay: 5));
        algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        CollectionAssert.AreEqual(expected, algorithm.Hash,
            "AppendDataAsync must accumulate correctly from a throttled stream.");
    }

    /// <summary>
    /// Verifies that an <see cref="IOException" /> thrown by a <see cref="FaultingStream" />
    /// mid-read propagates cleanly out of
    /// <see cref="HashAlgorithmExtensions.AppendDataAsync" />.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenSourceFaultsMidRead_ShouldPropagateIOException()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new FaultingStream(
            CryptoTestUtilities.CreateDeterministicBytes(64), throwAfterBytes: 16);

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            await algorithm.AppendDataAsync(stream),
            "AppendDataAsync must propagate IOException from a faulting source stream.");
    }

    // ─── AppendDataAsync — cancellation ──────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.AppendDataAsync" /> throws
    /// <see cref="OperationCanceledException" /> when the token is already cancelled before the
    /// call.
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenTokenAlreadyCancelled_ShouldThrow()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await algorithm.AppendDataAsync(stream, cancellationToken: cts.Token),
            "AppendDataAsync must throw immediately when the token is already cancelled.");
    }

    /// <summary>
    /// Verifies that a <see cref="CancellationTriggerStream" /> that cancels the token after
    /// two reads causes <see cref="HashAlgorithmExtensions.AppendDataAsync" /> to throw
    /// <see cref="OperationCanceledException" /> (or its subtype
    /// <see cref="TaskCanceledException" />).
    /// </summary>
    [TestMethod]
    public async Task AppendDataAsync_WhenCancellationTriggeredMidStream_ShouldThrow()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var cts = new CancellationTokenSource();
        using var stream = new CancellationTriggerStream(
            new IncrementingByteStream(1024), cts, cancelAfterRead: 2);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await algorithm.AppendDataAsync(stream, cancellationToken: cts.Token),
            "AppendDataAsync must throw OperationCanceledException when cancelled mid-stream.");
    }
}
