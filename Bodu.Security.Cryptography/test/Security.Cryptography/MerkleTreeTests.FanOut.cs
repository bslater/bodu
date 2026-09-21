// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.FanOut.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The non-RFC mode: a fan-out above two groups that many children per node with the same promotion rule, every root
/// computation agrees with a batch level-by-level reference, and the proof members refuse to operate.
/// </summary>
public partial class MerkleTreeTests
{
    private static readonly int[] WideFanOuts = [3, 4, 7];

    /// <summary>
    /// Reduces leaf hashes level by level in groups of <paramref name="fanOut" />, promoting a lone leftover unchanged
    /// and hashing a partial group — the reference the fold is held to.
    /// </summary>
    private static byte[] BatchReduce(byte[][] leafHashes, int fanOut)
    {
        if (leafHashes.Length == 0)
            return SHA256.HashData([]);

        byte[][] level = leafHashes;
        while (level.Length > 1)
        {
            var next = new List<byte[]>();
            for (int start = 0; start < level.Length; start += fanOut)
            {
                int count = Math.Min(fanOut, level.Length - start);
                if (count == 1)
                {
                    next.Add(level[start]);
                    continue;
                }

                byte[] payload = new byte[1 + (count * SHA256.HashSizeInBytes)];
                payload[0] = 0x01;
                for (int child = 0; child < count; child++)
                    level[start + child].CopyTo(payload, 1 + (child * SHA256.HashSizeInBytes));

                next.Add(SHA256.HashData(payload));
            }

            level = [.. next];
        }

        return level[0];
    }

    private static byte[][] LeafHashesOf(IReadOnlyList<ReadOnlyMemory<byte>> entries)
    {
        byte[][] leaves = new byte[entries.Count][];
        for (int index = 0; index < entries.Count; index++)
        {
            byte[] payload = new byte[1 + entries[index].Length];
            entries[index].Span.CopyTo(payload.AsSpan(1));
            leaves[index] = SHA256.HashData(payload);
        }

        return leaves;
    }

    /// <summary>
    /// Verifies that entry-mode roots at a wide fan-out equal the batch level-by-level reference for every entry
    /// count up to sixty-four, on the sequential and the parallel instance.
    /// </summary>
    /// <param name="fanOut">The fan-out under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    public void ComputeRoot_WhenFanOutIsWide_ShouldMatchTheBatchReduction(int fanOut)
    {
        var sequential = new MerkleTree(SHA256.Create, fanOut);
        var parallel = new MerkleTree(SHA256.Create, fanOut, maxDegreeOfParallelism: -1);

        for (int count = 0; count <= 64; count++)
        {
            IReadOnlyList<ReadOnlyMemory<byte>> entries = BlockModeEntries(count * VectorBlockSize);
            string expected = Hex(BatchReduce(LeafHashesOf(entries), fanOut));

            Assert.AreEqual(expected, Hex(sequential.ComputeRoot(entries)), $"sequential, {count} entries");
            Assert.AreEqual(expected, Hex(parallel.ComputeRoot(entries)), $"parallel, {count} entries");
            Assert.AreEqual(expected, Hex(sequential.ComputeRootOfLeafHashes(LeafHashesOf(entries))), $"leaf hashes, {count} entries");
        }
    }

    /// <summary>
    /// Verifies that every block-mode root path — stream, memory, span, root-only, async and the accumulator — agrees
    /// at a wide fan-out with the batch reference over the same blocks.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ComputeBlocked_WhenFanOutIsWide_ShouldAgreeAcrossEveryPath()
    {
        foreach (int fanOut in WideFanOuts)
        {
            var tree = new MerkleTree(SHA256.Create, fanOut);
            byte[] input = BlockModeInput((11 * VectorBlockSize) + 2);
            string expected = Hex(BatchReduce(LeafHashesOf(BlockModeEntries(input.Length)), fanOut));

            Assert.AreEqual(expected, Hex(tree.ComputeBlocked(new MemoryStream(input), VectorBlockSize).Root), $"stream, fan-out {fanOut}");
            Assert.AreEqual(expected, Hex(tree.ComputeBlocked(input.AsMemory(), VectorBlockSize).Root), $"memory, fan-out {fanOut}");
            Assert.AreEqual(expected, Hex(tree.ComputeBlocked(input.AsSpan(), VectorBlockSize).Root), $"span, fan-out {fanOut}");
            Assert.AreEqual(expected, Hex(tree.ComputeRootOfBlocks(new MemoryStream(input), VectorBlockSize)), $"root-only stream, fan-out {fanOut}");
            Assert.AreEqual(expected, Hex(tree.ComputeRootOfBlocks(input.AsSpan(), VectorBlockSize)), $"root-only span, fan-out {fanOut}");
            Assert.AreEqual(expected, Hex(await tree.ComputeRootOfBlocksAsync(new MemoryStream(input), VectorBlockSize)), $"async, fan-out {fanOut}");

            using MerkleBlockAccumulator accumulator = tree.CreateBlockAccumulator(VectorBlockSize);
            accumulator.Append(input);
            Assert.AreEqual(expected, Hex(accumulator.Finish()), $"accumulator, fan-out {fanOut}");
        }
    }

    /// <summary>
    /// Verifies that a wide fan-out produces a different root from the binary tree over the same three leaves — the
    /// mode is a distinct commitment, not a relabelling.
    /// </summary>
    [TestMethod]
    public void ComputeRoot_WhenFanOutIsWide_ShouldDifferFromTheBinaryRoot()
    {
        IReadOnlyList<ReadOnlyMemory<byte>> entries = TakeEntries(3);

        Assert.AreNotEqual(Hex(CreateTree().ComputeRoot(entries)), Hex(new MerkleTree(SHA256.Create, fanOut: 3).ComputeRoot(entries)));
    }

    /// <summary>
    /// Verifies that a lone leftover at a wide fan-out is promoted rather than re-hashed: the root over exactly one
    /// group plus one leaf hashes the group's parent with the promoted leaf.
    /// </summary>
    [TestMethod]
    public void ComputeRoot_WhenFanOutIsWideAndALeafIsLeftOver_ShouldPromoteItUnchanged()
    {
        var tree = new MerkleTree(SHA256.Create, fanOut: 3);
        byte[][] leaves = LeafHashesOf(TakeEntries(4));

        byte[] payload = new byte[1 + (3 * SHA256.HashSizeInBytes)];
        payload[0] = 0x01;
        for (int child = 0; child < 3; child++)
            leaves[child].CopyTo(payload, 1 + (child * SHA256.HashSizeInBytes));

        byte[] group = SHA256.HashData(payload);
        byte[] expected = tree.HashNode(group, leaves[3]);

        Assert.AreEqual(Hex(expected), Hex(tree.ComputeRoot(TakeEntries(4))));
    }

    /// <summary>
    /// Verifies that every proof and verify member throws <see cref="NotSupportedException" /> on a non-binary
    /// instance, before touching its arguments, while <c>BindRoot</c>, <c>HashLeaf</c> and <c>HashNode</c> still work.
    /// </summary>
    [TestMethod]
    public void ProofMembers_WhenFanOutIsWide_ShouldThrowNotSupportedException()
    {
        var tree = new MerkleTree(SHA256.Create, fanOut: 3);
        IReadOnlyList<ReadOnlyMemory<byte>> entries = TakeEntries(4);
        byte[][] leaves = LeafHashesOf(entries);
        byte[] root = tree.ComputeRoot(entries);
        ReadOnlyMemory<byte>[] path = [];

        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = tree.AuthenticationPath(entries, 0); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = tree.AuthenticationPath(leaves, 0); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = tree.VerifyInclusion(root, 4, 0, entries[0].Span, path); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = tree.VerifyInclusionOfLeafHash(root, 4, 0, leaves[0], path); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = tree.VerifyInclusionBound(root, 4, 4, 0, entries[0].Span, path); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = tree.VerifyBlockInclusion(root, 16, 4, 0, entries[0].Span, path); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = tree.ConsistencyProof(entries, 2); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = tree.ConsistencyProofOfLeafHashes(leaves, 2); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = tree.VerifyConsistency(root, 2, root, 4, path); });

        Assert.AreEqual(32, tree.BindRoot(root, 4).Length);
        Assert.AreEqual(32, tree.HashLeaf(entries[0].Span).Length);
        Assert.AreEqual(32, tree.HashNode(leaves[0], leaves[1]).Length);
    }
}
