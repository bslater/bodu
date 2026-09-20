// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleLevelFoldTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Security.Cryptography;

namespace Bodu.Collections.Specialized.SourceOnly;

/// <summary>
/// Holds the source-compiled <c>MerkleLevelFold</c> to RFC 6962 at a fan-out of two and to a batch level-by-level
/// reference at wider fan-outs, and verifies what it reports to an observer.
/// </summary>
/// <remarks>
/// The fold is the one reduction every Merkle type in the solution now shares, so these tests are what make "a
/// fan-out of two is RFC 6962's tree" a checked statement rather than a documented intention: the fold is compared
/// with the recursive <c>Mth</c> definition for every leaf count up to sixty-four, and with the published reference
/// roots for the eight-entry suite.
/// </remarks>
[TestClass]
public sealed class MerkleLevelFoldTests
{
    /// <summary>The eight RFC 6962 reference entries, in order.</summary>
    private static readonly byte[][] ReferenceEntries =
    [
        [],
        [0x00],
        [0x10],
        [0x20, 0x21],
        [0x30, 0x31],
        [0x40, 0x41, 0x42, 0x43],
        [0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57],
        [0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f],
    ];

    /// <summary>
    /// Verifies that a fan-out of two folds the reference entries to the published RFC 6962 roots.
    /// </summary>
    /// <param name="entryCount">The number of reference entries to fold.</param>
    /// <param name="expectedRoot">The published root, lowercase hex.</param>
    [TestMethod]
    [DataRow(0, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [DataRow(1, "6e340b9cffb37a989ca544e6bb780a2c78901d3fb33738768511a30617afa01d")]
    [DataRow(2, "fac54203e7cc696cf0dfcb42c92a1d9dbaf70ad9e621f4bd8d98662f00e3c125")]
    [DataRow(3, "aeb6bcfe274b70a14fb067a5e5578264db0fa9b51af5e0ba159158f329e06e77")]
    [DataRow(4, "d37ee418976dd95753c1c73862b9398fa2a2cf9b4ff0fdfe8b30cd95209614b7")]
    [DataRow(5, "4e3bbb1f7b478dcfe71fb631631519a3bca12c9aefca1612bfce4c13a86264d4")]
    [DataRow(6, "76e67dadbcdf1e10e1b74ddc608abd2f98dfb16fbce75277b5232a127f2087ef")]
    [DataRow(7, "ddb89be403809e325750d3d263cd78929c2942b7942a34b77e122c9594a74c8c")]
    [DataRow(8, "5dc9da79a70659a9ad559cb701ded9a2ab9d823aad2f4960cfe370eff4604328")]
    public void Finish_WhenFanOutIsTwo_ShouldReproduceRfc6962ReferenceRoots(int entryCount, string expectedRoot)
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;
        var fold = new MerkleLevelFold(hasher, hashLength, fanOut: 2);

        foreach (byte[] entry in ReferenceEntries.Take(entryCount))
            fold.Add(MerkleTreeCore.HashWithPrefix(hasher, hashLength, MerkleTreeFormat.LeafPrefix, entry));

        Assert.AreEqual(expectedRoot, Hex(fold.Finish()));
    }

    /// <summary>
    /// Verifies that a fan-out of two agrees with the recursive Merkle Tree Hash definition for every leaf count up to
    /// sixty-four, so the level-by-level walk with promotion is RFC 6962's tree and not merely close to it.
    /// </summary>
    [TestMethod]
    public void Finish_WhenFanOutIsTwo_ShouldEqualMthForEveryLeafCountUpToSixtyFour()
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;

        for (int leafCount = 0; leafCount <= 64; leafCount++)
        {
            byte[][] leaves = SyntheticLeaves(hasher, hashLength, leafCount);
            string expected = leafCount == 0
                ? Hex(MerkleTreeCore.HashEmpty(hasher, hashLength))
                : Hex(MerkleTreeCore.Mth(leaves, hasher, hashLength));

            var fold = new MerkleLevelFold(hasher, hashLength, fanOut: 2);
            foreach (byte[] leaf in leaves)
                fold.Add(leaf);

            Assert.AreEqual(expected, Hex(fold.Finish()), $"leaf count {leafCount}");
        }
    }

    /// <summary>
    /// Verifies that a wider fan-out agrees with a batch level-by-level reduction that hashes full and partial groups
    /// and promotes a lone leftover unchanged, for every leaf count up to sixty-four.
    /// </summary>
    /// <param name="fanOut">The fan-out under test.</param>
    [TestMethod]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(7)]
    public void Finish_WhenFanOutIsWider_ShouldEqualBatchReductionWithPromotion(int fanOut)
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;

        for (int leafCount = 0; leafCount <= 64; leafCount++)
        {
            byte[][] leaves = SyntheticLeaves(hasher, hashLength, leafCount);
            string expected = Hex(BatchReduce(hasher, hashLength, leaves, fanOut));

            var fold = new MerkleLevelFold(hasher, hashLength, fanOut);
            foreach (byte[] leaf in leaves)
                fold.Add(leaf);

            Assert.AreEqual(expected, Hex(fold.Finish()), $"fan-out {fanOut}, leaf count {leafCount}");
        }
    }

    /// <summary>
    /// Verifies that a lone leftover is promoted unchanged rather than hashed as a one-child node: three leaves at a
    /// fan-out of two hash the first pair, then hash that parent with the third <em>leaf</em>.
    /// </summary>
    [TestMethod]
    public void Finish_WhenALevelHasALoneLeftover_ShouldPromoteItUnchanged()
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;
        byte[][] leaves = SyntheticLeaves(hasher, hashLength, 3);

        var fold = new MerkleLevelFold(hasher, hashLength, fanOut: 2);
        foreach (byte[] leaf in leaves)
            fold.Add(leaf);

        byte[] pair = MerkleTreeCore.HashChildren(hasher, hashLength, leaves[..2]);
        byte[] expected = MerkleTreeCore.HashChildren(hasher, hashLength, [pair, leaves[2]]);

        Assert.AreEqual(Hex(expected), Hex(fold.Finish()));
    }

    /// <summary>
    /// Verifies that the observer receives every leaf in order, one hashed node per group with its children, and
    /// nothing for a promoted node.
    /// </summary>
    [TestMethod]
    public void Finish_WhenObserved_ShouldReportLeavesAndHashedNodesButNotPromotions()
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;
        byte[][] leaves = SyntheticLeaves(hasher, hashLength, 3);
        var observer = new RecordingObserver();

        var fold = new MerkleLevelFold(hasher, hashLength, fanOut: 2, observer);
        foreach (byte[] leaf in leaves)
            fold.Add(leaf);
        byte[] root = fold.Finish();

        CollectionAssert.AreEqual(new long[] { 0, 1, 2 }, observer.LeafIndices);
        Assert.AreEqual(
            2,
            observer.Nodes.Count,
            "one node for the first pair, one for the root — none for the promoted leaf");

        (int level, long index, byte[][] children, byte[] hash) = observer.Nodes[0];
        Assert.AreEqual((1, 0L), (level, index));
        Assert.AreEqual(Hex(leaves[0]) + Hex(leaves[1]), Hex(children[0]) + Hex(children[1]));

        (level, index, children, hash) = observer.Nodes[1];
        Assert.AreEqual((2, 0L), (level, index));
        Assert.AreEqual(Hex(observer.Nodes[0].Hash), Hex(children[0]), "the root's left child is the hashed pair");
        Assert.AreEqual(Hex(leaves[2]), Hex(children[1]), "the root's right child is the promoted third leaf itself");
        Assert.AreEqual(Hex(root), Hex(hash));
    }

    /// <summary>
    /// Verifies that folding nothing yields the empty tree's root, the hash of zero bytes.
    /// </summary>
    [TestMethod]
    public void Finish_WhenNoLeafWasAdded_ShouldReturnTheEmptyTreeRoot()
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;

        var fold = new MerkleLevelFold(hasher, hashLength, fanOut: 3);

        Assert.AreEqual(Hex(SHA256.HashData([])), Hex(fold.Finish()));
    }

    /// <summary>
    /// Produces distinct leaf hashes for a leaf count, each the leaf hash of its index as a single byte.
    /// </summary>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length.</param>
    /// <param name="count">The number of leaves.</param>
    /// <returns>The leaf hashes.</returns>
    private static byte[][] SyntheticLeaves(HashAlgorithm hasher, int hashLength, int count)
    {
        byte[][] leaves = new byte[count][];
        for (int i = 0; i < count; i++)
            leaves[i] = MerkleTreeCore.HashWithPrefix(hasher, hashLength, MerkleTreeFormat.LeafPrefix, [(byte)i]);

        return leaves;
    }

    /// <summary>
    /// An independent batch reduction: group each level left to right, hash a group of two or more, promote a lone one.
    /// </summary>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length.</param>
    /// <param name="leaves">The leaf hashes.</param>
    /// <param name="fanOut">The group width.</param>
    /// <returns>The root.</returns>
    private static byte[] BatchReduce(HashAlgorithm hasher, int hashLength, byte[][] leaves, int fanOut)
    {
        if (leaves.Length == 0)
            return MerkleTreeCore.HashEmpty(hasher, hashLength);

        List<byte[]> level = [.. leaves];
        while (level.Count > 1)
        {
            List<byte[]> next = [];
            for (int i = 0; i < level.Count; i += fanOut)
            {
                int size = Math.Min(fanOut, level.Count - i);
                next.Add(size == 1
                    ? level[i]
                    : MerkleTreeCore.HashChildren(hasher, hashLength, level.GetRange(i, size).ToArray()));
            }

            level = next;
        }

        return level[0];
    }

    /// <summary>
    /// Formats a hash as lowercase hex.
    /// </summary>
    /// <param name="hash">The hash.</param>
    /// <returns>The lowercase hex string.</returns>
    private static string Hex(byte[] hash) =>
        Convert.ToHexString(hash).ToLowerInvariant();

    /// <summary>
    /// Records everything the fold reports.
    /// </summary>
    private sealed class RecordingObserver
        : IMerkleTreeObserver
    {
        /// <summary>The leaf indices, in report order.</summary>
        public List<long> LeafIndices { get; } = [];

        /// <summary>The hashed nodes, in report order.</summary>
        public List<(int Level, long Index, byte[][] Children, byte[] Hash)> Nodes { get; } = [];

        /// <inheritdoc />
        public void OnLeaf(long index, byte[] hash) =>
            LeafIndices.Add(index);

        /// <inheritdoc />
        public void OnNode(int level, long index, byte[][] children, byte[] hash) =>
            Nodes.Add((level, index, children, hash));
    }
}
