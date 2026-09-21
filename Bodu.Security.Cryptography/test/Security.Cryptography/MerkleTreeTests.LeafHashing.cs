// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.LeafHashing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the internal leaf-hashing loops on <see cref="MerkleTree" /> — the sequential, asynchronous and parallel
/// stream loops and the in-memory parallel hasher — to the sequential loop's leaf sequence.
/// </summary>
public partial class MerkleTreeTests
{
    /// <summary>The seed the leaf-loop inputs are generated from.</summary>
    private const int LeafLoopSeed = 0x6962;

    /// <summary>
    /// Verifies that the parallel stream loop hands out exactly the leaf sequence and byte count the sequential loop
    /// produces, at every degree of parallelism and across a batch boundary.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The degree of parallelism to request.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(-1)]
    public void ForEachLeafHashParallel_WhenComparedWithTheSequentialLoop_ShouldReportTheSameLeavesInOrder(int maxDegreeOfParallelism)
    {
        byte[] input = MakeData(300 * 64 + 37, LeafLoopSeed);
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;

        List<byte[]> expected = [];
        long expectedLength = MerkleTree.ForEachLeafHash(new MemoryStream(input), 64, hasher, hashLength, expected.Add, CancellationToken.None);

        List<byte[]> actual = [];
        long actualLength = MerkleTree.ForEachLeafHashParallel(
            new MemoryStream(input), 64, SHA256.Create, hashLength, maxDegreeOfParallelism, actual.Add, CancellationToken.None);

        Assert.AreEqual(expectedLength, actualLength);
        Assert.AreEqual(input.Length, actualLength);
        CollectionAssert.AreEqual(expected.Select(h => Hex(h)).ToList(), actual.Select(h => Hex(h)).ToList());
    }

    /// <summary>
    /// Verifies that the asynchronous loops, sequential and parallel, hand out the same leaf sequence and byte count as
    /// the synchronous sequential loop.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The degree of parallelism to request, or zero for the sequential loop.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(-1)]
    public async Task ForEachLeafHashAsync_WhenComparedWithTheSequentialLoop_ShouldReportTheSameLeavesInOrder(int maxDegreeOfParallelism)
    {
        byte[] input = MakeData(300 * 64 + 37, LeafLoopSeed);
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;

        List<byte[]> expected = [];
        long expectedLength = MerkleTree.ForEachLeafHash(new MemoryStream(input), 64, hasher, hashLength, expected.Add, CancellationToken.None);

        List<byte[]> actual = [];
        long actualLength = maxDegreeOfParallelism == 0
            ? await MerkleTree.ForEachLeafHashAsync(new MemoryStream(input), 64, hasher, hashLength, actual.Add, CancellationToken.None)
            : await MerkleTree.ForEachLeafHashParallelAsync(
                new MemoryStream(input), 64, SHA256.Create, hashLength, maxDegreeOfParallelism, actual.Add, CancellationToken.None);

        Assert.AreEqual(expectedLength, actualLength);
        CollectionAssert.AreEqual(expected.Select(h => Hex(h)).ToList(), actual.Select(h => Hex(h)).ToList());
    }

    /// <summary>
    /// Verifies that hashing a buffer's blocks in parallel yields the same leaves as the sequential stream loop, and
    /// that an empty buffer yields no leaves.
    /// </summary>
    [TestMethod]
    public void HashLeavesParallel_WhenGivenABuffer_ShouldReportTheSameLeavesAsTheStreamLoop()
    {
        byte[] input = MakeData(300 * 64 + 37, LeafLoopSeed);
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;

        List<byte[]> expected = [];
        _ = MerkleTree.ForEachLeafHash(new MemoryStream(input), 64, hasher, hashLength, expected.Add, CancellationToken.None);

        byte[][] actual = MerkleTree.HashLeavesParallel(input, 64, SHA256.Create, hashLength, -1, CancellationToken.None);

        CollectionAssert.AreEqual(expected.Select(h => Hex(h)).ToList(), actual.Select(h => Hex(h)).ToList());
        Assert.AreEqual(0, MerkleTree.HashLeavesParallel(ReadOnlyMemory<byte>.Empty, 64, SHA256.Create, hashLength, -1, CancellationToken.None).Length);
    }

    /// <summary>
    /// Verifies that a leaf algorithm faulting inside a parallel worker surfaces its own exception rather than an
    /// <see cref="AggregateException" />.
    /// </summary>
    [TestMethod]
    public void HashLeavesParallel_WhenAWorkerFaults_ShouldSurfaceTheInnerException()
    {
        byte[] input = MakeData(8 * 64, LeafLoopSeed);

        // Every block's prefixed payload is 65 bytes, so every worker faults; the first fault is what surfaces.
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = MerkleTree.HashLeavesParallel(input, 64, static () => new FaultingHashAlgorithm(65, static () => { }), 32, -1, CancellationToken.None);
        });

        Assert.AreEqual("Hashing failed.", ex.Message);
    }
}
