// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.LevelFold.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the internal <see cref="MerkleTree.LevelFold" /> to RFC 6962 at a fan-out of two and to a batch
/// level-by-level reference at wider fan-outs, and verifies what it records.
/// </summary>
/// <remarks>
/// The fold is the one reduction every root computation and the accumulator share, so these tests are what make "a
/// fan-out of two is RFC 6962's tree" a checked statement rather than a documented intention: the fold is compared with
/// the recursive <c>Mth</c> definition for every leaf count up to sixty-four, and with the published reference roots
/// for the eight-entry suite.
/// </remarks>
public partial class MerkleTreeTests
{
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
    public void LevelFold_WhenFanOutIsTwo_ShouldReproduceRfc6962ReferenceRoots(int entryCount, string expectedRoot)
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;
        var fold = new MerkleTree.LevelFold(hasher, hashLength, fanOut: 2);

        foreach (byte[] entry in ReferenceEntries.Take(entryCount))
            fold.Add(MerkleTree.HashWithPrefix(hasher, hashLength, MerkleTree.LeafPrefix, entry));

        Assert.AreEqual(expectedRoot, Hex(fold.Finish()));
    }

    /// <summary>
    /// Verifies that a fan-out of two agrees with the recursive Merkle Tree Hash definition for every leaf count up to
    /// sixty-four, so the level-by-level walk with promotion is RFC 6962's tree and not merely close to it.
    /// </summary>
    [TestMethod]
    public void LevelFold_WhenFanOutIsTwo_ShouldEqualMthForEveryLeafCountUpToSixtyFour()
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;

        for (int leafCount = 0; leafCount <= 64; leafCount++)
        {
            byte[][] leaves = SyntheticLeaves(hasher, hashLength, leafCount);
            string expected = leafCount == 0
                ? Hex(MerkleTree.HashEmpty(hasher, hashLength))
                : Hex(MerkleTree.Mth(leaves, hasher, hashLength));

            var fold = new MerkleTree.LevelFold(hasher, hashLength, fanOut: 2);
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
    public void LevelFold_WhenFanOutIsWider_ShouldEqualBatchReductionWithPromotion(int fanOut)
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;

        for (int leafCount = 0; leafCount <= 64; leafCount++)
        {
            byte[][] leaves = SyntheticLeaves(hasher, hashLength, leafCount);
            string expected = Hex(BatchReduce(hasher, hashLength, leaves, fanOut));

            var fold = new MerkleTree.LevelFold(hasher, hashLength, fanOut);
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
    public void LevelFold_WhenALevelHasALoneLeftover_ShouldPromoteItUnchanged()
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;
        byte[][] leaves = SyntheticLeaves(hasher, hashLength, 3);

        var fold = new MerkleTree.LevelFold(hasher, hashLength, fanOut: 2);
        foreach (byte[] leaf in leaves)
            fold.Add(leaf);

        byte[] pair = MerkleTree.HashChildren(hasher, hashLength, leaves[..2]);
        byte[] expected = MerkleTree.HashChildren(hasher, hashLength, [pair, leaves[2]]);

        Assert.AreEqual(Hex(expected), Hex(fold.Finish()));
    }

    /// <summary>
    /// Verifies that the recorder receives every leaf in order, one hashed node per group with its children, and
    /// nothing for a promoted node.
    /// </summary>
    [TestMethod]
    public void LevelFold_WhenRecorded_ShouldReportLeavesAndHashedNodesButNotPromotions()
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;
        byte[][] leaves = SyntheticLeaves(hasher, hashLength, 3);
        var diagnostics = new MerkleTreeDiagnostics();

        var fold = new MerkleTree.LevelFold(hasher, hashLength, fanOut: 2, diagnostics);
        foreach (byte[] leaf in leaves)
            fold.Add(leaf);
        byte[] root = fold.Finish();

        CollectionAssert.AreEqual(new long[] { 0, 1, 2 }, diagnostics.GetLevel(0).Select(node => node.Index).ToArray());

        List<MerkleTreeDiagnostics.Node> hashed = diagnostics.GetAllNodes().Where(node => !node.IsLeaf).ToList();
        Assert.HasCount(2, hashed, "one node for the first pair, one for the root — none for the promoted leaf");

        MerkleTreeDiagnostics.Node pair = hashed[0];
        Assert.AreEqual((1, 0L), (pair.Level, pair.Index));
        Assert.AreEqual(Hex(leaves[0]) + Hex(leaves[1]), Hex(pair.ChildHashes[0]) + Hex(pair.ChildHashes[1]));

        MerkleTreeDiagnostics.Node top = hashed[1];
        Assert.AreEqual((2, 0L), (top.Level, top.Index));
        Assert.AreEqual(Hex(pair.Hash), Hex(top.ChildHashes[0]), "the root's left child is the hashed pair");
        Assert.AreEqual(Hex(leaves[2]), Hex(top.ChildHashes[1]), "the root's right child is the promoted third leaf itself");
        Assert.AreEqual(Hex(root), Hex(top.Hash));
    }

    /// <summary>
    /// Verifies that folding nothing yields the empty tree's root, the hash of zero bytes.
    /// </summary>
    [TestMethod]
    public void LevelFold_WhenNoLeafWasAdded_ShouldReturnTheEmptyTreeRoot()
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;

        var fold = new MerkleTree.LevelFold(hasher, hashLength, fanOut: 3);

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
            leaves[i] = MerkleTree.HashWithPrefix(hasher, hashLength, MerkleTree.LeafPrefix, [(byte)i]);

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
            return MerkleTree.HashEmpty(hasher, hashLength);

        List<byte[]> level = [.. leaves];
        while (level.Count > 1)
        {
            List<byte[]> next = [];
            for (int i = 0; i < level.Count; i += fanOut)
            {
                int size = Math.Min(fanOut, level.Count - i);
                next.Add(size == 1
                    ? level[i]
                    : MerkleTree.HashChildren(hasher, hashLength, level.GetRange(i, size).ToArray()));
            }

            level = next;
        }

        return level[0];
    }
}
