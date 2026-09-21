// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.Primitives.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the internal RFC 6962 primitives on <see cref="MerkleTree" /> — the prefixes, the split point, the recursive
/// <c>Mth</c> and the proof walks — to the published vectors.
/// </summary>
/// <remarks>
/// The public members validate arguments and read resources; the primitives are the stateless arithmetic underneath
/// them. A guard moved into a public member could leave a primitive accepting something it should not, so they are
/// driven here directly, through the same published values the public suite pins. The inputs are the eight entries of
/// the RFC 6962 reference test suite, so a failure here is a disagreement with the standard rather than with this
/// repository.
/// </remarks>
public partial class MerkleTreeTests
{
    /// <summary>
    /// Verifies that the recursive definition reproduces the published Merkle Tree Hash for entry counts zero through
    /// eight.
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
    public void Mth_ShouldReproduceRfc6962MerkleTreeHash(int entryCount, string expectedRoot)
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;

        string actual = entryCount == 0
            ? Hex(MerkleTree.HashEmpty(hasher, hashLength))
            : Hex(MerkleTree.Mth(ReferenceLeafHashes(hasher, hashLength, entryCount), hasher, hashLength));

        Assert.AreEqual(expectedRoot, actual);
    }

    /// <summary>
    /// Verifies that the prefixed hash applies RFC 6962's domain separation, so a one-entry tree's root is the entry's
    /// leaf hash and never its bare digest, and that the three prefixes carry the standard's values.
    /// </summary>
    [TestMethod]
    public void HashWithPrefix_ShouldDomainSeparateLeavesFromNodes()
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;
        byte[] entry = [0x61, 0x62, 0x63];

        byte[] leaf = MerkleTree.HashWithPrefix(hasher, hashLength, MerkleTree.LeafPrefix, entry);

        Assert.AreEqual("609f6e36d2405585188d5cfd761f407c7cc46a7d3f314c88270469dde315fcd1", Hex(leaf));
        Assert.AreNotEqual(Hex(SHA256.HashData(entry)), Hex(leaf));
        Assert.AreEqual(0x00, MerkleTree.LeafPrefix);
        Assert.AreEqual(0x01, MerkleTree.InternalNodePrefix);
        Assert.AreEqual(0x02, MerkleTree.RootPrefix);
    }

    /// <summary>
    /// Verifies that three entries split 2 + 1 rather than 1 + 2, the point on which RFC 6962 differs from a naive
    /// halving.
    /// </summary>
    [TestMethod]
    public void SplitPoint_ShouldSplitAtTheLargestPowerOfTwoBelowTheCount()
    {
        Assert.AreEqual(2, MerkleTree.SplitPoint(3));
        Assert.AreEqual(4, MerkleTree.SplitPoint(7));
        Assert.AreEqual(4, MerkleTree.SplitPoint(8));
        Assert.AreEqual(8, MerkleTree.SplitPoint(9));
    }

    /// <summary>
    /// Verifies that a path built by the primitives walks back to the same root, including the short path that leaf 6
    /// of a seven-entry tree carries.
    /// </summary>
    [TestMethod]
    public void WalkToHead_ShouldRoundTripEveryLeafOfASevenEntryTree()
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;

        byte[][] leaves = ReferenceLeafHashes(hasher, hashLength, 7);
        byte[] root = MerkleTree.Mth(leaves, hasher, hashLength);

        for (int leafIndex = 0; leafIndex < 7; leafIndex++)
        {
            List<byte[]> path = [];
            MerkleTree.AppendPath(leaves, leafIndex, path, hasher, hashLength);

            byte[]? head = MerkleTree.WalkToHead(
                7,
                leafIndex,
                leaves[leafIndex],
                path.Select(step => (ReadOnlyMemory<byte>)step).ToArray(),
                hasher,
                hashLength);

            Assert.IsNotNull(head, $"leaf {leafIndex} must walk to a head");
            Assert.AreEqual(Hex(root), Hex(head), $"leaf {leafIndex}");
            Assert.AreEqual(leafIndex == 6 ? 2 : 3, path.Count, $"leaf {leafIndex} path length");
        }
    }

    /// <summary>
    /// Verifies that the walk rejects a structurally invalid proof by returning <see langword="null" /> rather than
    /// throwing, which is what keeps the public verifiers total.
    /// </summary>
    [TestMethod]
    public void WalkToHead_WhenProofIsInvalid_ShouldReturnNullRatherThanThrow()
    {
        using HashAlgorithm hasher = SHA256.Create();
        int hashLength = hasher.HashSize / 8;
        byte[][] leaves = ReferenceLeafHashes(hasher, hashLength, 4);

        // Index at the tree size, a zero tree size, and a path longer than ceil(log2 n) all reject.
        Assert.IsNull(MerkleTree.WalkToHead(4, 4, leaves[0], [], hasher, hashLength));
        Assert.IsNull(MerkleTree.WalkToHead(0, 0, leaves[0], [], hasher, hashLength));
        Assert.IsNull(MerkleTree.WalkToHead(
            4, 0, leaves[0], Enumerable.Repeat((ReadOnlyMemory<byte>)new byte[32], 5).ToArray(), hasher, hashLength));
    }

    /// <summary>
    /// Returns the leaf hashes of the first <paramref name="count" /> reference entries.
    /// </summary>
    /// <param name="hasher">The algorithm to hash with.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <param name="count">The number of entries to hash.</param>
    /// <returns>The leaf hashes, in order.</returns>
    private static byte[][] ReferenceLeafHashes(HashAlgorithm hasher, int hashLength, int count)
    {
        byte[][] leaves = new byte[count][];
        for (int index = 0; index < count; index++)
            leaves[index] = MerkleTree.HashWithPrefix(hasher, hashLength, MerkleTree.LeafPrefix, ReferenceEntries[index]);

        return leaves;
    }
}
