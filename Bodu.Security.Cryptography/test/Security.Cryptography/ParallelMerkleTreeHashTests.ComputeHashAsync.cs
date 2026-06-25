// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelMerkleTreeHashTests.ComputeHashAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="ParallelMerkleTreeHash.ComputeHashAsync" /> — the stream-based,
/// cancellable asynchronous entry point that is exposed only by the parallel implementation.
/// </summary>
/// <remarks>
/// <para>
/// Scenarios covered here fall into five groups: argument validation, result shape and
/// equivalence with the synchronous overload, cancellation precision, unreliable-stream
/// tolerance (partial reads, byte-at-a-time, zero-byte stalls, non-seekable, faulting), and
/// post-fault recovery.
/// </para>
/// <para>
/// Post-dispose error behaviour is covered in <c>ParallelMerkleTreeHashTests.EmptyAndDisposed.cs</c>
/// alongside the synchronous post-dispose test, so it is not duplicated here.
/// </para>
/// </remarks>
public partial class ParallelMerkleTreeHashTests
{
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Argument validation
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that a <see langword="null" /> stream raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenStreamIsNull_ShouldThrowExactly()
    {
        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await hasher.ComputeHashAsync(null!);
        });
    }

    /// <summary>
    /// Verifies that hashing an empty stream raises <see cref="InvalidOperationException" />
    /// because no leaf nodes can be produced from zero bytes.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenStreamIsEmpty_ShouldThrowExactly()
    {
        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await hasher.ComputeHashAsync(new MemoryStream());
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Result shape
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that <c>ComputeHashAsync</c> returns a non-null hash of the expected length.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenValidStreamProvided_ShouldReturnHashOfExpectedLength()
    {
        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
        byte[] result = await hasher.ComputeHashAsync(new MemoryStream(MakeData(8)));
        Assert.IsNotNull(result);
        Assert.HasCount(4, result); // MonitoringHashAlgorithm: sizeof(uint)
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Equivalence with the synchronous overload
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that <c>ComputeHashAsync</c> and the synchronous <c>ComputeHash</c> produce
    /// identical results for the same input data.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenSameInputAsSync_ShouldReturnIdenticalHash()
    {
        byte[] data = MakeData(21);

        using var async = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
        using var sync = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

        byte[] asyncResult = await async.ComputeHashAsync(new MemoryStream(data));
        byte[] syncResult = sync.ComputeHash(data);

        CollectionAssert.AreEqual(syncResult, asyncResult);
    }

    /// <summary>
    /// Verifies that a stream delivering data in partial chunks produces the same result as
    /// the synchronous span overload on the same bytes.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenStreamDeliversPartialReads_ShouldProduceSameResultAsSync()
    {
        const int length = 37;

        using var asyncHasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
        using var syncHasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

        // IncrementingByteStream delivers at most half its remaining bytes per Read call.
        using var stream = new IncrementingByteStream(length);
        byte[] asyncResult = await asyncHasher.ComputeHashAsync(stream);
        byte[] syncResult = syncHasher.ComputeHash(new IncrementingByteStream(length).ToArray());

        CollectionAssert.AreEqual(syncResult, asyncResult);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Cancellation
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that an already-cancelled token causes <c>ComputeHashAsync</c> to throw
    /// <see cref="TaskCanceledException" /> before completing.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenTokenAlreadyCancelled_ShouldThrowExactly()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await hasher.ComputeHashAsync(new IncrementingByteStream(1024), cancellationToken: cts.Token);
        });
    }

    /// <summary>
    /// Verifies that cancelling the token mid-stream causes <c>ComputeHashAsync</c> to throw
    /// <see cref="TaskCanceledException" />.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenTokenCancelledMidStream_ShouldThrowExactly()
    {
        using var cts = new CancellationTokenSource();

        const int length = 1_000_000;
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 256, fanOut: 2);

        // CancellationTriggerStream cancels the CTS after exactly 3 successful reads,
        // making the test deterministic — the next ReadAsync detects the cancelled token.
        using var stream = new CancellationTriggerStream(
            new IncrementingByteStream(length), cts, cancelAfterRead: 3);

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await hasher.ComputeHashAsync(stream, cancellationToken: cts.Token);
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Various configurations
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that <c>ComputeHashAsync</c> produces the expected additive root across a range
    /// of block sizes and fan-out values.
    /// </summary>
    [TestMethod]
    [DataRow(4, 2, 8)]
    [DataRow(4, 2, 9)]
    [DataRow(4, 3, 12)]
    [DataRow(16, 2, 50)]
    public async Task ComputeHashAsync_WhenVariousConfigurationsUsed_ShouldMatchHandComputedRoot(
        int blockSize, int fanOut, int dataLength)
    {
        byte[] data = MakeData(dataLength);
        byte[] expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
        byte[] actual = await hasher.ComputeHashAsync(new MemoryStream(data));

        CollectionAssert.AreEqual(expected, actual,
            $"Mismatch: blockSize={blockSize}, fanOut={fanOut}, length={dataLength}");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Unreliable streams — read pattern must not affect correctness
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> with <c>chunkSize=1</c> (one byte per
    /// read) produces the same root as the synchronous overload on the same data.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenStreamDeliversOneByteAtATime_ShouldMatchSyncResult()
    {
        byte[] data = MakeData(13);

        using var syncHasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
        using var asyncHasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

        byte[] expected = syncHasher.ComputeHash(data);
        byte[] actual = await asyncHasher.ComputeHashAsync(new FixedChunkStream(data, chunkSize: 1));

        CollectionAssert.AreEqual(expected, actual,
            "FixedChunkStream(chunkSize=1) produced a different root than the sync overload.");
    }

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> with a chunk size smaller than the
    /// block size produces the same root as the synchronous overload.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public async Task ComputeHashAsync_WhenChunkSmallerThanBlockSize_ShouldMatchSyncResult(int chunkSize)
    {
        byte[] data = MakeData(17);

        using var syncHasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
        using var asyncHasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

        byte[] expected = syncHasher.ComputeHash(data);
        byte[] actual = await asyncHasher.ComputeHashAsync(new FixedChunkStream(data, chunkSize));

        CollectionAssert.AreEqual(expected, actual,
            $"FixedChunkStream(chunkSize={chunkSize}) produced a different root than the sync overload.");
    }

    /// <summary>
    /// Verifies that a <see cref="FixedChunkStream" /> with a chunk size larger than the block
    /// size but not a multiple of it produces the same root as the synchronous overload.
    /// </summary>
    [TestMethod]
    [DataRow(6)]
    [DataRow(7)]
    [DataRow(13)]
    public async Task ComputeHashAsync_WhenChunkLargerThanBlockSize_ShouldMatchSyncResult(int chunkSize)
    {
        byte[] data = MakeData(25);

        using var syncHasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
        using var asyncHasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

        byte[] expected = syncHasher.ComputeHash(data);
        byte[] actual = await asyncHasher.ComputeHashAsync(new FixedChunkStream(data, chunkSize));

        CollectionAssert.AreEqual(expected, actual,
            $"FixedChunkStream(chunkSize={chunkSize}) produced a different root than the sync overload.");
    }

    /// <summary>
    /// Verifies that an <see cref="IntermittentZeroReadStream" /> still produces the correct
    /// root without hanging or returning a truncated result.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenStreamIntermittentlyReturnsZeroBytes_ShouldProduceCorrectRoot()
    {
        byte[] data = MakeData(17);
        byte[] expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
        byte[] actual = await hasher.ComputeHashAsync(
            new IntermittentZeroReadStream(data, stallEveryN: 2));

        CollectionAssert.AreEqual(expected, actual,
            "IntermittentZeroReadStream produced a different root than expected.");
    }

    /// <summary>
    /// Verifies that a <see cref="NonSeekableStream" /> is accepted without error, confirming
    /// the implementation does not require seekability.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenStreamIsNonSeekable_ShouldSucceedAndProduceCorrectRoot()
    {
        byte[] data = MakeData(13);
        byte[] expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
        byte[] actual = await hasher.ComputeHashAsync(new NonSeekableStream(data));

        CollectionAssert.AreEqual(expected, actual,
            "NonSeekableStream produced a different root than expected.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Faulting streams — IOException must propagate cleanly
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that an <see cref="IOException" /> from a <see cref="FaultingStream" />
    /// propagates out of <see cref="ParallelMerkleTreeHash.ComputeHashAsync" /> without
    /// deadlocking the level workers.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenStreamThrowsIOExceptionMidRead_ShouldPropagateException()
    {
        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
        {
            await hasher.ComputeHashAsync(
                new FaultingStream(MakeData(32), throwAfterBytes: 6));
        });
    }

    /// <summary>
    /// Verifies that after an <see cref="IOException" /> the instance resets cleanly and
    /// succeeds on the next call.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_AfterStreamFault_ShouldSucceedOnNextCall()
    {
        byte[] data = MakeData(8);
        byte[] expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

        try
        {
            await hasher.ComputeHashAsync(new FaultingStream(MakeData(32), throwAfterBytes: 6));
        }
        catch (IOException) { /* expected */ }

        byte[] actual = await hasher.ComputeHashAsync(new MemoryStream(data));
        CollectionAssert.AreEqual(expected, actual,
            "Instance did not recover correctly after a faulting stream.");
    }

    /// <summary>
    /// Verifies that the instance resets cleanly when a fault occurs before any bytes have been
    /// delivered to <c>ProcessBytes</c> — that is, the level-0 channel and worker created by
    /// <c>Reset</c> are idle when the exception fires.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_AfterStreamFault_WhenFaultBeforeAnyData_ShouldSucceedOnNextCall()
    {
        byte[] data = MakeData(8);
        byte[] expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

        try
        {
            await hasher.ComputeHashAsync(new FaultingStream(MakeData(32), throwAfterBytes: 0));
        }
        catch (IOException) { /* expected */ }

        byte[] actual = await hasher.ComputeHashAsync(new MemoryStream(data));
        CollectionAssert.AreEqual(expected, actual,
            "Instance did not recover correctly after a fault before any data was delivered.");
    }

    /// <summary>
    /// Verifies that the instance resets cleanly when a fault occurs after exactly one complete
    /// block has been submitted as a leaf — exercising the path where the level-0 worker has
    /// one pending item when the exception fires.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_AfterStreamFault_WhenFaultAfterWholeBlock_ShouldSucceedOnNextCall()
    {
        byte[] data = MakeData(8);
        byte[] expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

        try
        {
            await hasher.ComputeHashAsync(new FaultingStream(MakeData(32), throwAfterBytes: DefaultBlockSize));
        }
        catch (IOException) { /* expected */ }

        byte[] actual = await hasher.ComputeHashAsync(new MemoryStream(data));
        CollectionAssert.AreEqual(expected, actual,
            "Instance did not recover correctly after a fault at an exact block boundary.");
    }

    /// <summary>
    /// Verifies that fault recovery is repeatable — two consecutive faulting calls followed by a
    /// clean call must still produce the correct root, confirming the fix is not a one-shot path.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_AfterConsecutiveStreamFaults_ShouldSucceedOnNextCall()
    {
        byte[] data = MakeData(8);
        byte[] expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

        try
        {
            await hasher.ComputeHashAsync(new FaultingStream(MakeData(32), throwAfterBytes: 6));
        }
        catch (IOException) { /* expected */ }

        try
        {
            await hasher.ComputeHashAsync(new FaultingStream(MakeData(32), throwAfterBytes: 3));
        }
        catch (IOException) { /* expected */ }

        byte[] actual = await hasher.ComputeHashAsync(new MemoryStream(data));
        CollectionAssert.AreEqual(expected, actual,
            "Instance did not recover correctly after two consecutive faulting streams.");
    }

    /// <summary>
    /// Verifies that fault and success calls can be freely interleaved — a successful call between
    /// two faults must not leave the instance unable to recover from the subsequent fault.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenFaultAndSuccessInterleaved_ShouldAlwaysProduceCorrectResult()
    {
        byte[] data = MakeData(8);
        byte[] expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

        try
        {
            await hasher.ComputeHashAsync(new FaultingStream(MakeData(32), throwAfterBytes: 6));
        }
        catch (IOException) { /* expected */ }

        byte[] first = await hasher.ComputeHashAsync(new MemoryStream(data));
        CollectionAssert.AreEqual(expected, first,
            "First recovery call produced the wrong root.");

        try
        {
            await hasher.ComputeHashAsync(new FaultingStream(MakeData(32), throwAfterBytes: 2));
        }
        catch (IOException) { /* expected */ }

        byte[] second = await hasher.ComputeHashAsync(new MemoryStream(data));
        CollectionAssert.AreEqual(expected, second,
            "Second recovery call (after fault following a successful call) produced the wrong root.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Cancellation precision
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that a token cancelled before the call causes immediate cancellation.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenTokenCancelledBeforeCall_ShouldThrowExactly()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await hasher.ComputeHashAsync(
                new MemoryStream(MakeData(8)),
                cancellationToken: cts.Token);
        });
    }

    /// <summary>
    /// Verifies that a linked <see cref="CancellationToken" /> propagates cancellation
    /// correctly when the parent source is cancelled mid-stream.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenLinkedTokenCancelled_ShouldThrowExactly()
    {
        using var parent = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(parent.Token);

        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);

        _ = Task.Run(async () =>
        {
            await Task.Delay(10);
            await parent.CancelAsync();
        });

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            // FixedChunkStream(chunkSize:1) ensures the delay fires mid-read.
            await hasher.ComputeHashAsync(
                new FixedChunkStream(MakeData(100_000), chunkSize: 1),
                cancellationToken: linked.Token);
        });
    }

    /// <summary>
    /// Verifies that cancellation does not leave level workers deadlocked — a subsequent call
    /// after a cancelled operation must complete successfully within a reasonable time.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenCancelledThenRetried_ShouldCompleteSuccessfully()
    {
        byte[] data = MakeData(8);
        byte[] expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        try
        {
            await hasher.ComputeHashAsync(
                new MemoryStream(MakeData(50_000)),
                cancellationToken: cts.Token);
        }
        catch (TaskCanceledException) { /* expected */ }

        byte[] actual = await hasher
            .ComputeHashAsync(new MemoryStream(data))
            .WaitAsync(TimeSpan.FromSeconds(10)); // deadlock guard

        CollectionAssert.AreEqual(expected, actual,
            "Retry after cancellation produced the wrong root.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Empty-stream re-use behaviour
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that an empty stream supplied after a successful call throws
    /// <see cref="InvalidOperationException" /> and does not return stale data from the prior call.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenEmptyStreamFollowsSuccessfulCall_ShouldThrowExactly()
    {
        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
        hasher.ComputeHash(MakeData(8));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await hasher.ComputeHashAsync(new MemoryStream());
        });
    }
}
