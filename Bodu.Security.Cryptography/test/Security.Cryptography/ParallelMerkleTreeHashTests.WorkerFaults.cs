// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelMerkleTreeHashTests.WorkerFaults.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.IO;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests that a fault raised by a hash algorithm during a computation — on a parallel leaf worker or in the
/// reduction — surfaces to the caller as itself, and that every algorithm the computation created is disposed on
/// fault and on cancellation alike.
/// </summary>
/// <remarks>
/// Leaves are hashed by the parallel workers and internal nodes by the reduction, so the faulting algorithm below
/// picks its path by payload length: a leaf payload is the prefix plus a block, an internal node's the prefix plus
/// <c>fanOut</c> four-byte child hashes.
/// </remarks>
public partial class ParallelMerkleTreeHashTests
{
    /// <summary>
    /// Verifies that a fault while hashing a leaf on a parallel worker propagates as the original exception rather than
    /// an <see cref="AggregateException" /> or a wrong root.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenLeafHashingFaults_ShouldPropagateTheOriginalException()
    {
        Func<HashAlgorithm> factory = () => new FaultingHashAlgorithm(faultOnPayloadLength: 1 + 4, static () => { });
        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = hasher.ComputeHash(MakeData(8));
        });
    }

    /// <summary>
    /// Verifies that a fault while hashing an internal node in the reduction propagates as the original exception
    /// rather than a wrong root.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInternalNodeHashingFaults_ShouldPropagateTheOriginalException()
    {
        Func<HashAlgorithm> factory = () => new FaultingHashAlgorithm(faultOnPayloadLength: 1 + (2 * 4), static () => { });
        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = hasher.ComputeHash(MakeData(8));
        });
    }

    /// <summary>
    /// Verifies that when a leaf worker faults, every <see cref="HashAlgorithm" /> the computation created — the
    /// workers' and the reduction's — is disposed before the exception reaches the caller.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenLeafHashingFaults_ShouldDisposeEveryCreatedHasher()
    {
        int created = 0, disposed = 0;
        Func<HashAlgorithm> factory = () =>
        {
            Interlocked.Increment(ref created);
            return new FaultingHashAlgorithm(faultOnPayloadLength: 1 + 4, () => Interlocked.Increment(ref disposed));
        };
        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = hasher.ComputeHash(MakeData(12));
        });

        Assert.IsGreaterThan(0, created, "Expected at least one hasher to be created.");
        Assert.AreEqual(created, disposed, "Every created hasher must be disposed when a leaf worker faults.");
    }

    /// <summary>
    /// Verifies that when the reduction faults, every <see cref="HashAlgorithm" /> the computation created is
    /// disposed before the exception reaches the caller.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInternalNodeHashingFaults_ShouldDisposeEveryCreatedHasher()
    {
        int created = 0, disposed = 0;
        Func<HashAlgorithm> factory = () =>
        {
            Interlocked.Increment(ref created);
            return new FaultingHashAlgorithm(faultOnPayloadLength: 1 + (2 * 4), () => Interlocked.Increment(ref disposed));
        };
        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = hasher.ComputeHash(MakeData(12));
        });

        Assert.IsGreaterThan(0, created, "Expected at least one hasher to be created.");
        Assert.AreEqual(created, disposed, "Every created hasher must be disposed when the reduction faults.");
    }

    /// <summary>
    /// Verifies that cancelling mid-stream disposes every <see cref="HashAlgorithm" /> the computation had created.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenCancelledMidStream_ShouldDisposeEveryCreatedHasher()
    {
        int created = 0, disposed = 0;
        using var cts = new CancellationTokenSource();
        Func<HashAlgorithm> factory = () =>
        {
            Interlocked.Increment(ref created);
            return new FaultingHashAlgorithm(faultOnPayloadLength: -1, () => Interlocked.Increment(ref disposed));
        };
        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);

        // One byte per read keeps the stream open long enough for the cancellation to land mid-stream.
        var stream = new FixedChunkStream(MakeData(100_000), chunkSize: 1);
        cts.CancelAfter(TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            _ = await hasher.ComputeHashAsync(stream, cancellationToken: cts.Token);
        });

        Assert.IsGreaterThan(0, created, "Expected at least one hasher to be created before cancellation.");
        Assert.AreEqual(created, disposed, "Every created hasher must be disposed when the computation is cancelled.");
    }

    /// <summary>
    /// A four-byte additive <see cref="HashAlgorithm" /> that throws when asked to hash a payload of one specific
    /// length, so a fault can be aimed at leaves or at internal nodes deterministically, and signals its disposal
    /// through a callback.
    /// </summary>
    private sealed class FaultingHashAlgorithm
        : HashAlgorithm
    {
        private readonly int _faultOnPayloadLength;
        private readonly Action _onDispose;
        private uint _sum;

        /// <summary>
        /// Initializes a new instance of the <see cref="FaultingHashAlgorithm" /> class.
        /// </summary>
        /// <param name="faultOnPayloadLength">The payload length that triggers the fault, or a negative value never to fault.</param>
        /// <param name="onDispose">A callback invoked once when the instance is disposed.</param>
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
