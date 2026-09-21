// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests._MonitoringHashAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Security.Cryptography;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// What the tree does with the algorithms it creates, observed through <see cref="MonitoringHashAlgorithm" />: exact
/// disposal, the one-shot span path, exactly one prefixed payload per hashed node, and in-order folding of leaves hashed
/// in parallel.
/// </summary>
public partial class MerkleTreeTests
{
    /// <summary>
    /// Verifies that every algorithm a computation creates is disposed exactly once, on both instances, for inputs
    /// from empty to many batches.
    /// </summary>
    /// <param name="dataLength">The input length in bytes.</param>
    /// <param name="maxDegreeOfParallelism">The degree of parallelism.</param>
    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(4, 1)]
    [DataRow(12, 1)]
    [DataRow(4_000, 1)]
    [DataRow(0, -1)]
    [DataRow(8, -1)]
    [DataRow(4_000, -1)]
    public void MonitoringAlgorithm_WhenComputationCompletes_ShouldDisposeEveryInstanceExactlyOnce(int dataLength, int maxDegreeOfParallelism)
    {
        var instances = new ConcurrentBag<MonitoringHashAlgorithm>();
        var disposals = new ConcurrentDictionary<MonitoringHashAlgorithm, int>();
        var tree = new MerkleTree(
            () =>
            {
                var instance = new MonitoringHashAlgorithm();
                instance.DisposeCalled += (sender, _) => disposals.AddOrUpdate((MonitoringHashAlgorithm)sender!, 1, (_, n) => n + 1);
                instances.Add(instance);
                return instance;
            },
            maxDegreeOfParallelism: maxDegreeOfParallelism);

        _ = tree.ComputeRootOfBlocks(new MemoryStream(MakeData(dataLength)), 4);

        Assert.IsGreaterThan(1, instances.Count, "the probe and the computation each create an instance, even for an empty input");
        foreach (MonitoringHashAlgorithm instance in instances)
            Assert.AreEqual(1, disposals.GetValueOrDefault(instance), "each instance must be disposed exactly once");
    }

    /// <summary>
    /// Verifies that every leaf and node is hashed through the one-shot span path, never the array path.
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_WhenHashing_ShouldUseTheOneShotSpanPathForEveryNode()
    {
        int spanCores = 0, tryFinals = 0, arrayCores = 0, finals = 0;
        var tree = new MerkleTree(() =>
        {
            var instance = new MonitoringHashAlgorithm();
            instance.HashCoreSpanCalled += (_, _) => Interlocked.Increment(ref spanCores);
            instance.TryHashFinalCalled += (_, _) => Interlocked.Increment(ref tryFinals);
            instance.HashCoreCalled += (_, _) => Interlocked.Increment(ref arrayCores);
            instance.HashFinalCalled += (_, _) => Interlocked.Increment(ref finals);
            return instance;
        });

        _ = tree.ComputeRootOfBlocks(MakeData(12), 4);   // three leaves, one pair, one root

        Assert.AreEqual(5, tryFinals, "three leaves and two hashed internal nodes");
        Assert.AreEqual(tryFinals, spanCores, "exactly one span core per hash");
        Assert.AreEqual(0, arrayCores);
        Assert.AreEqual(0, finals);
    }

    /// <summary>
    /// Verifies that exactly one prefixed payload is hashed per leaf and per hashed node — a promoted node is never
    /// re-hashed, and the tail is hashed at its actual length — on both instances.
    /// </summary>
    /// <param name="dataLength">The input length in bytes.</param>
    /// <param name="fanOut">The fan-out.</param>
    /// <param name="expectedBytes">The total bytes the algorithms must process.</param>
    [TestMethod]
    [DataRow(8, 2, 19)]    // 2 leaves × (1 + 4) + root (1 + 2×4)
    [DataRow(12, 2, 33)]   // 3 leaves × 5 + pair 9 + root 9; the promoted third leaf is not hashed again
    [DataRow(5, 2, 16)]    // leaves (1 + 4) + (1 + 1) — the tail at its actual length — and the root 9
    [DataRow(12, 3, 28)]   // 3 leaves × 5 + one root of three children (1 + 3×4)
    [DataRow(16, 3, 42)]   // 4 leaves × 5 + a group of three 13; the fourth leaf is promoted and joins it in the root 9
    public void MonitoringAlgorithm_WhenHashing_ShouldProcessExactlyOnePrefixedPayloadPerLeafAndHashedNode(int dataLength, int fanOut, int expectedBytes)
    {
        foreach (int degree in (int[])[1, -1])
        {
            long total = 0;
            var tree = new MerkleTree(
                () =>
                {
                    var instance = new MonitoringHashAlgorithm();
                    instance.TryHashFinalCalled += (sender, _) => Interlocked.Add(ref total, ((MonitoringHashAlgorithm)sender!).BytesProcessed);
                    return instance;
                },
                fanOut,
                degree);

            _ = tree.ComputeRootOfBlocks(MakeData(dataLength), 4);

            Assert.AreEqual(expectedBytes, total, $"degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that leaves hashed in parallel are folded in input order — the additive oracle is order-insensitive
    /// per group but the tree shape over 1,000 leaves is not.
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_WhenLeavesAreHashedInParallel_ShouldFoldThemInInputOrder()
    {
        byte[] data = MakeData(4_000);
        byte[] expected = ComputeAdditiveRoot(data, blockSize: 4, fanOut: 2);
        var tree = new MerkleTree(static () => new MonitoringHashAlgorithm(), maxDegreeOfParallelism: -1);

        CollectionAssert.AreEqual(expected, tree.ComputeRootOfBlocks(data, 4));
        CollectionAssert.AreEqual(expected, tree.ComputeRootOfBlocks(new MemoryStream(data), 4));
    }

    /// <summary>
    /// Verifies that a parallel instance hands every worker a distinct algorithm instance.
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_WhenFactoryInvokedConcurrently_ShouldReceiveDistinctInstances()
    {
        var instances = new ConcurrentBag<HashAlgorithm>();
        var tree = new MerkleTree(
            () =>
            {
                var instance = new MonitoringHashAlgorithm();
                instances.Add(instance);
                return instance;
            },
            maxDegreeOfParallelism: -1);

        _ = tree.ComputeRootOfBlocks(MakeData(64), 4);

        var list = instances.ToList();
        Assert.HasCount(list.Count, list.Distinct(ReferenceEqualityComparer.Instance), "the factory returned a shared instance");
    }
}
