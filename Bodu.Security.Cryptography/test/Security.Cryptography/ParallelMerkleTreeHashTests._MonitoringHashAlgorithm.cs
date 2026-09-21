// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelMerkleTreeHashTests._MonitoringHashAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Security.Cryptography;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Pins how <see cref="ParallelMerkleTreeHash" /> drives the <see cref="HashAlgorithm" /> instances it creates, using
/// <see cref="MonitoringHashAlgorithm" /> to count calls and bytes: every instance is disposed exactly once, every
/// hash goes through the one-shot span path, and the bytes hashed are exactly one prefixed payload per leaf and per
/// hashed internal node — a promoted node costs nothing.
/// </summary>
/// <remarks>
/// <see cref="HashAlgorithm.TryComputeHash" /> calls <c>Initialize</c> after each hash, which resets the monitoring
/// algorithm's byte count, and <c>Dispose</c> zeroes every counter before <c>DisposeCalled</c> fires — so per-hash
/// figures are captured inside the <c>TryHashFinalCalled</c> handler, while the instance is live and before the
/// reset. Workers run concurrently, so the captures go through thread-safe collections.
/// </remarks>
public partial class ParallelMerkleTreeHashTests
{
    /// <summary>
    /// Verifies that every algorithm instance the computation creates is disposed exactly once, whatever the input
    /// shape.
    /// </summary>
    /// <param name="dataLength">The input length in bytes.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(4)]
    [DataRow(8)]
    [DataRow(12)]
    [DataRow(4_000)]
    public void MonitoringAlgorithm_WhenComputationCompletes_ShouldDisposeEveryInstanceExactlyOnce(int dataLength)
    {
        var instances = new ConcurrentBag<MonitoringHashAlgorithm>();
        var disposals = new ConcurrentDictionary<MonitoringHashAlgorithm, int>();
        Func<HashAlgorithm> factory = () =>
        {
            var instance = new MonitoringHashAlgorithm();
            instance.DisposeCalled += (sender, _) => disposals.AddOrUpdate((MonitoringHashAlgorithm)sender!, 1, (_, n) => n + 1);
            instances.Add(instance);
            return instance;
        };

        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);
        _ = hasher.ComputeHash(MakeData(dataLength));

        Assert.IsNotEmpty(instances, "the reduction alone creates an instance, even for an empty input");
        foreach (MonitoringHashAlgorithm instance in instances)
            Assert.AreEqual(1, disposals.GetValueOrDefault(instance), "each instance must be disposed exactly once");
    }

    /// <summary>
    /// Verifies that leaves and internal nodes alike are hashed through the one-shot span path — one
    /// <c>HashCore(ReadOnlySpan)</c> and one <c>TryHashFinal</c> per hash — and never through the array or
    /// <c>HashFinal</c> paths.
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_WhenHashing_ShouldUseTheOneShotSpanPathForEveryNode()
    {
        int spanCores = 0, tryFinals = 0, arrayCores = 0, finals = 0;
        Func<HashAlgorithm> factory = () =>
        {
            var instance = new MonitoringHashAlgorithm();
            instance.HashCoreSpanCalled += (_, _) => Interlocked.Increment(ref spanCores);
            instance.TryHashFinalCalled += (_, _) => Interlocked.Increment(ref tryFinals);
            instance.HashCoreCalled += (_, _) => Interlocked.Increment(ref arrayCores);
            instance.HashFinalCalled += (_, _) => Interlocked.Increment(ref finals);
            return instance;
        };

        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);
        _ = hasher.ComputeHash(MakeData(12));   // three leaves, one pair, one root

        Assert.AreEqual(5, tryFinals, "three leaves and two hashed internal nodes");
        Assert.AreEqual(tryFinals, spanCores, "exactly one span core per hash");
        Assert.AreEqual(0, arrayCores);
        Assert.AreEqual(0, finals);
    }

    /// <summary>
    /// Verifies the exact bytes hashed: one prefixed block per leaf and one prefixed group of child hashes per
    /// hashed internal node, with a promoted leftover costing nothing.
    /// </summary>
    /// <param name="dataLength">The input length in bytes.</param>
    /// <param name="fanOut">The fan-out.</param>
    /// <param name="expectedBytes">The total payload bytes the algorithm instances must see.</param>
    [TestMethod]
    [DataRow(8, 2, 19)]    // 2 leaves × (1 + 4) + root (1 + 2×4)
    [DataRow(12, 2, 33)]   // 3 leaves × 5 + pair 9 + root 9; the promoted third leaf is not hashed again
    [DataRow(5, 2, 16)]    // leaves (1 + 4) + (1 + 1) — the tail at its actual length — and the root 9
    [DataRow(12, 3, 28)]   // 3 leaves × 5 + one root of three children (1 + 3×4)
    [DataRow(16, 3, 42)]   // 4 leaves × 5 + a group of three 13; the fourth leaf is promoted and joins it in the root 9
    public void MonitoringAlgorithm_WhenHashing_ShouldProcessExactlyOnePrefixedPayloadPerLeafAndHashedNode(
        int dataLength, int fanOut, int expectedBytes)
    {
        long total = 0;
        Func<HashAlgorithm> factory = () =>
        {
            var instance = new MonitoringHashAlgorithm();
            instance.TryHashFinalCalled += (sender, _) => Interlocked.Add(ref total, ((MonitoringHashAlgorithm)sender!).BytesProcessed);
            return instance;
        };

        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut);
        _ = hasher.ComputeHash(MakeData(dataLength));

        Assert.AreEqual(expectedBytes, total);
    }

    /// <summary>
    /// Verifies that the root is the additive reference root, so the parallel workers' leaves are folded in input
    /// order rather than completion order.
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_WhenLeavesAreHashedInParallel_ShouldFoldThemInInputOrder()
    {
        byte[] data = MakeData(4_000);
        byte[] expected = ComputeAdditiveRoot(data, blockSize: 4, fanOut: 2);

        using var hasher = new ParallelMerkleTreeHash(static () => new MonitoringHashAlgorithm(), blockSize: 4, fanOut: 2);

        CollectionAssert.AreEqual(expected, hasher.ComputeHash(data));
    }
}
