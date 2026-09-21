// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.ComputeBlockedAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="MerkleTree.ComputeBlockedAsync(Stream, int, MerkleTreeDiagnostics, CancellationToken)" />.
/// </summary>
public partial class MerkleTreeTests
{
    private static readonly int[] AllDegrees = [1, 2, -1];

    /// <summary>
    /// Verifies that the asynchronous block computation reproduces the published block-mode roots, input lengths and
    /// leaf counts at every degree.
    /// </summary>
    /// <param name="kat">The input length and the published root.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    [DynamicData(nameof(BlockModeRoots), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public async Task ComputeBlockedAsync_WhenReadingAStream_ShouldReproduceRfc6962MerkleTreeHash(ValidKat<int, string> kat)
    {
        foreach (int degree in AllDegrees)
        {
            using var stream = new MemoryStream(BlockModeInput(kat.Input));
            MerkleBlockComputation computation = await CreateParallelTree(degree).ComputeBlockedAsync(stream, VectorBlockSize);

            Assert.AreEqual(kat.Expected, Hex(computation.Root), $"degree {degree}");
            Assert.AreEqual(kat.Input, computation.InputLength, $"degree {degree}");
            Assert.AreEqual(MerkleBlocks.BlockCount(kat.Input, VectorBlockSize), computation.LeafHashes.Count, $"degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that the asynchronous computation agrees with the synchronous one on root and every leaf hash for
    /// inputs that cross a batch boundary, at every degree.
    /// </summary>
    /// <param name="length">The input length in bytes.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(33)]
    [DataRow(1025)]
    public async Task ComputeBlockedAsync_WhenComparedWithTheSynchronousPath_ShouldAgreeOnRootAndEveryLeafHash(int length)
    {
        byte[] input = BlockModeInput(length);
        MerkleBlockComputation expected = CreateTree().ComputeBlocked(input.AsSpan(), 1);

        foreach (int degree in AllDegrees)
        {
            using var stream = new MemoryStream(input);
            MerkleBlockComputation actual = await CreateParallelTree(degree).ComputeBlockedAsync(stream, 1);

            Assert.AreEqual(Hex(expected.Root), Hex(actual.Root), $"degree {degree}");
            CollectionAssert.AreEqual(expected.LeafHashes.Select(l => Hex(l)).ToArray(), actual.LeafHashes.Select(l => Hex(l)).ToArray(), $"degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that short asynchronous reads are topped up rather than taken as the end of the stream.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ComputeBlockedAsync_WhenStreamReturnsShortReads_ShouldProduceTheSameRoot()
    {
        foreach (int degree in AllDegrees)
        {
            using var throttled = new ThrottledStream(BlockModeInput(33), maxBytesPerRead: 3);
            MerkleBlockComputation computation = await CreateParallelTree(degree).ComputeBlockedAsync(throttled, VectorBlockSize);

            Assert.AreEqual("31dce6ff9c5ac1336d203f99ea214a31eaba3429a3316fdc7eb7dfa1e787e9ec", Hex(computation.Root), $"degree {degree}");
            Assert.AreEqual(9, computation.LeafHashes.Count, $"degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that a pre-cancelled token surfaces as a plain <see cref="OperationCanceledException" /> at every
    /// degree, whatever the stream would have thrown.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ComputeBlockedAsync_WhenCancellationIsRequested_ShouldThrowOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        foreach (int degree in AllDegrees)
        {
            MerkleTree tree = CreateParallelTree(degree);
            using var stream = new MemoryStream(BlockModeInput(256));

            OperationCanceledException ex = await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
            {
                _ = await tree.ComputeBlockedAsync(stream, VectorBlockSize, cancellationToken: cts.Token);
            });

            Assert.AreEqual(cts.Token, ex.CancellationToken, $"degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that a token cancelled part-way through the stream surfaces as a plain
    /// <see cref="OperationCanceledException" /> carrying that token, rather than the stream's own subtype.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ComputeBlockedAsync_WhenCancelledMidStream_ShouldThrowOperationCanceledExceptionCarryingTheToken()
    {
        using var cts = new CancellationTokenSource();
        using var stream = new CancelAfterReadsStream(BlockModeInput(64 * VectorBlockSize), readsBeforeCancel: 3, cts);

        OperationCanceledException ex = await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            _ = await CreateTree().ComputeBlockedAsync(stream, VectorBlockSize, cancellationToken: cts.Token);
        });

        Assert.AreEqual(cts.Token, ex.CancellationToken);
    }

    /// <summary>
    /// Verifies that an I/O fault from the stream propagates unchanged through the asynchronous computation.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ComputeBlockedAsync_WhenStreamFaults_ShouldPropagateTheIOException()
    {
        foreach (int degree in AllDegrees)
        {
            using var faulting = new FaultingStream(BlockModeInput(64), throwAfterBytes: 20);
            MerkleTree tree = CreateParallelTree(degree);

            _ = await Assert.ThrowsExactlyAsync<IOException>(async () =>
            {
                _ = await tree.ComputeBlockedAsync(faulting, VectorBlockSize);
            });
        }
    }

    /// <summary>
    /// Verifies that a null stream and a non-positive block size are rejected before any read.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ComputeBlockedAsync_WhenArgumentsAreInvalid_ShouldThrowWithTheParameterNamed()
    {
        MerkleTree tree = CreateTree();

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => { _ = await tree.ComputeBlockedAsync(null!, 4); });
        var ex = await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () => { _ = await tree.ComputeBlockedAsync(new MemoryStream(), 0); });

        Assert.AreEqual("blockSize", ex.ParamName);
    }

    /// <summary>
    /// A stream that cancels the supplied source after a number of reads, so cancellation arrives mid-stream.
    /// </summary>
    private sealed class CancelAfterReadsStream : MemoryStream
    {
        private readonly CancellationTokenSource _source;
        private int _remainingReads;

        /// <summary>Initializes a new instance of the <see cref="CancelAfterReadsStream" /> class.</summary>
        public CancelAfterReadsStream(byte[] data, int readsBeforeCancel, CancellationTokenSource source)
            : base(data)
        {
            _remainingReads = readsBeforeCancel;
            _source = source;
        }

        /// <inheritdoc />
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (--_remainingReads < 0)
                _source.Cancel();

            return base.ReadAsync(buffer, cancellationToken);
        }
    }
}
