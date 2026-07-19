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
/// Tests that a fault raised inside a level worker during normal finalization surfaces to the caller rather than
/// being swallowed and reported as a wrong or truncated root.
/// </summary>
/// <remarks>
/// The success drain is <c>FinalizeAsync</c>, which awaits every level worker without a catch; only the error-path
/// <c>DrainWorkersAsync</c> suppresses faults (so the original exception wins). These tests pin that a worker fault
/// on the success path propagates.
/// </remarks>
public partial class ParallelMerkleTreeHashTests
{
    /// <summary>
    /// Verifies that when internal-node hashing throws during finalization (leaves hash cleanly, the combining step
    /// faults), <see cref="ParallelMerkleTreeHash.ComputeHash(ReadOnlySpan{byte})" /> propagates the fault instead
    /// of returning a root.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInternalNodeHashingFaults_ShouldPropagateAndNotReturnRoot()
    {
        // Two full blocks → two leaves (span path, succeed) + one internal node (array/TransformBlock path, throws).
        Func<HashAlgorithm> factory = () => new ThrowOnInternalPathHashAlgorithm(static () => { });

        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);

        Exception? caught = null;
        try
        {
            hasher.ComputeHash(MakeData(8));
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        Assert.IsNotNull(caught,
            "A fault inside the internal-node hasher was silently swallowed on the success drain path.");

        bool propagated =
            caught is InvalidOperationException
            || (caught is AggregateException agg && agg.Flatten().InnerExceptions.Any(e => e is InvalidOperationException))
            || caught.InnerException is InvalidOperationException;

        Assert.IsTrue(propagated,
            $"Expected the internal-node fault to propagate; got {caught.GetType().Name}: {caught.Message}");
    }

    /// <summary>
    /// Verifies that when internal-node hashing faults, every <see cref="HashAlgorithm" /> the computation created
    /// is disposed by the time the call returns — confirming every level worker ran to completion and released its
    /// resources rather than being abandoned as a zombie.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInternalNodeFaults_ShouldDrainAndDisposeEveryCreatedHasher()
    {
        int created = 0;
        int disposed = 0;

        Func<HashAlgorithm> factory = () =>
        {
            Interlocked.Increment(ref created);
            return new ThrowOnInternalPathHashAlgorithm(() => Interlocked.Increment(ref disposed));
        };

        // Three full blocks, fanOut=2: leaves hash cleanly; the internal combinations fault.
        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);

        Assert.ThrowsExactly<InvalidOperationException>(() => hasher.ComputeHash(MakeData(12)));

        Assert.AreEqual(created, disposed,
            $"Not every created hasher was disposed after the fault ({disposed}/{created}) — a worker leaked.");
        Assert.IsGreaterThan(0, created, "Expected at least one hasher to be created.");
    }

    /// <summary>
    /// Verifies that when the token is cancelled mid-stream, the computation throws and every created
    /// <see cref="MonitoringHashAlgorithm" /> is disposed by the time the call returns — confirming the level
    /// workers are drained and torn down on cancellation, leaving no running thread behind.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenCancelledMidStream_ShouldDrainAndDisposeEveryCreatedHasher()
    {
        using var cts = new CancellationTokenSource();

        int created = 0;
        int disposed = 0;

        Func<HashAlgorithm> factory = () =>
        {
            Interlocked.Increment(ref created);
            var algo = new MonitoringHashAlgorithm();
            algo.DisposeCalled += (_, _) => Interlocked.Increment(ref disposed);
            return algo;
        };

        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 256, fanOut: 2);

        // CancellationTriggerStream cancels the CTS after exactly three successful reads, so cancellation lands
        // while leaves are still being produced and level workers are live.
        using var stream = new CancellationTriggerStream(
            new IncrementingByteStream(1_000_000), cts, cancelAfterRead: 3);

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await hasher.ComputeHashAsync(stream, cancellationToken: cts.Token);
        });

        Assert.AreEqual(created, disposed,
            $"Not every created hasher was disposed after cancellation ({disposed}/{created}) — a worker leaked.");
        Assert.IsGreaterThan(0, created, "Expected at least one hasher to be created before cancellation.");
    }

    /// <summary>
    /// A <see cref="HashAlgorithm" /> that hashes cleanly on the span/one-shot path used by leaves, but throws on
    /// the array-based <c>HashCore(byte[], int, int)</c> path used by internal-node combination — deterministically
    /// faulting internal nodes regardless of worker scheduling. Signals disposal via a supplied callback.
    /// </summary>
    private sealed class ThrowOnInternalPathHashAlgorithm
        : HashAlgorithm
    {
        private readonly Action _onDispose;
        private uint _sum;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrowOnInternalPathHashAlgorithm" /> class.
        /// </summary>
        /// <param name="onDispose">A callback invoked once when the instance is disposed.</param>
        public ThrowOnInternalPathHashAlgorithm(Action onDispose)
        {
            _onDispose = onDispose;
            HashSizeValue = 32;
        }

        /// <inheritdoc />
        public override void Initialize() => _sum = 0;

        /// <inheritdoc />
        protected override void HashCore(byte[] array, int ibStart, int cbSize) =>
            throw new InvalidOperationException("Internal-node hashing failed.");

        /// <inheritdoc />
        protected override void HashCore(ReadOnlySpan<byte> source)
        {
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
