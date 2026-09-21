// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.WorkerFaults.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// A faulting leaf or node hash surfaces as itself, and every algorithm the factory produced is disposed however the
/// computation ends.
/// </summary>
public partial class MerkleTreeTests
{
    /// <summary>
    /// Verifies that a leaf hash faulting on either instance propagates the original exception, not an aggregate.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The degree of parallelism.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(-1)]
    public void ComputeRootOfBlocks_WhenLeafHashingFaults_ShouldPropagateTheOriginalException(int maxDegreeOfParallelism)
    {
        var tree = new MerkleTree(() => new FaultingHashAlgorithm(faultOnPayloadLength: 1 + 4, static () => { }), maxDegreeOfParallelism: maxDegreeOfParallelism);

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => { _ = tree.ComputeRootOfBlocks(MakeData(8), 4); });

        Assert.AreEqual("Hashing failed.", ex.Message);
    }

    /// <summary>
    /// Verifies that an internal-node hash faulting propagates the original exception.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The degree of parallelism.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(-1)]
    public void ComputeRootOfBlocks_WhenInternalNodeHashingFaults_ShouldPropagateTheOriginalException(int maxDegreeOfParallelism)
    {
        var tree = new MerkleTree(() => new FaultingHashAlgorithm(faultOnPayloadLength: 1 + (2 * 4), static () => { }), maxDegreeOfParallelism: maxDegreeOfParallelism);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => { _ = tree.ComputeRootOfBlocks(MakeData(8), 4); });
    }

    /// <summary>
    /// Verifies that every algorithm created during a faulting computation is disposed, whether the leaf or the node
    /// hash faulted, on the sequential and the parallel instance.
    /// </summary>
    /// <param name="faultOnPayloadLength">The prefixed payload length that faults: a leaf or a two-child node.</param>
    /// <param name="maxDegreeOfParallelism">The degree of parallelism.</param>
    [TestMethod]
    [DataRow(1 + 4, 1)]
    [DataRow(1 + 4, -1)]
    [DataRow(1 + (2 * 4), 1)]
    [DataRow(1 + (2 * 4), -1)]
    public void ComputeRootOfBlocks_WhenHashingFaults_ShouldDisposeEveryCreatedAlgorithm(int faultOnPayloadLength, int maxDegreeOfParallelism)
    {
        int created = 0, disposed = 0;
        var tree = new MerkleTree(
            () =>
            {
                Interlocked.Increment(ref created);
                return new FaultingHashAlgorithm(faultOnPayloadLength, () => Interlocked.Increment(ref disposed));
            },
            maxDegreeOfParallelism: maxDegreeOfParallelism);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => { _ = tree.ComputeRootOfBlocks(new MemoryStream(MakeData(12)), 4); });

        Assert.IsGreaterThan(1, created, "the probe and at least one computation algorithm must have been created");
        Assert.AreEqual(created, disposed, "every created algorithm must be disposed when hashing faults");
    }

    /// <summary>
    /// Verifies that a computation cancelled mid-stream disposes every algorithm it created, on both instances.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The degree of parallelism.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    [DataRow(1)]
    [DataRow(-1)]
    public async Task ComputeRootOfBlocksAsync_WhenCancelledMidStream_ShouldDisposeEveryCreatedAlgorithm(int maxDegreeOfParallelism)
    {
        int created = 0, disposed = 0;
        using var cts = new CancellationTokenSource();
        var tree = new MerkleTree(
            () =>
            {
                Interlocked.Increment(ref created);
                return new FaultingHashAlgorithm(faultOnPayloadLength: -1, () => Interlocked.Increment(ref disposed));
            },
            maxDegreeOfParallelism: maxDegreeOfParallelism);

        // The stream cancels the token itself after fifty reads, so the cancellation lands mid-stream deterministically.
        using var stream = new CancelAfterReadsStream(MakeData(100_000), readsBeforeCancel: 50, cts);

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            _ = await tree.ComputeRootOfBlocksAsync(stream, 4, cancellationToken: cts.Token);
        });

        Assert.IsGreaterThan(0, created);
        Assert.AreEqual(created, disposed, "every created algorithm must be disposed when the computation is cancelled");
    }

    /// <summary>
    /// A 4-byte additive hash that faults on one prefixed payload length and reports its disposal.
    /// </summary>
    private sealed class FaultingHashAlgorithm
        : HashAlgorithm
    {
        private readonly int _faultOnPayloadLength;
        private readonly Action _onDispose;
        private uint _sum;

        /// <summary>Initializes a new instance of the <see cref="FaultingHashAlgorithm" /> class.</summary>
        public FaultingHashAlgorithm(int faultOnPayloadLength, Action onDispose)
        {
            _faultOnPayloadLength = faultOnPayloadLength;
            _onDispose = onDispose;
            HashSizeValue = 32;
        }

        /// <inheritdoc />
        public override void Initialize() =>
            _sum = 0;

        /// <inheritdoc />
        protected override void HashCore(byte[] array, int ibStart, int cbSize) =>
            HashCore(array.AsSpan(ibStart, cbSize));

        /// <inheritdoc />
        protected override void HashCore(ReadOnlySpan<byte> source)
        {
            if (source.Length == _faultOnPayloadLength)
                throw new InvalidOperationException("Hashing failed.");

            foreach (byte b in source)
                _sum += b;
        }

        /// <inheritdoc />
        protected override byte[] HashFinal()
        {
            byte[] result = new byte[4];
            BitConverter.TryWriteBytes(result, _sum);
            return result;
        }

        /// <inheritdoc />
        protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < 4)
            {
                bytesWritten = 0;
                return false;
            }

            BitConverter.TryWriteBytes(destination, _sum);
            bytesWritten = 4;
            return true;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _onDispose();

            base.Dispose(disposing);
        }
    }
}
