// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.ComputeRoot.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="MerkleTree.ComputeRoot(IReadOnlyList{ReadOnlyMemory{byte}})" />.
/// </summary>
public partial class MerkleTreeTests
{
    /// <summary>
    /// Verifies that the root over the RFC 6962 reference entries reproduces the published Merkle Tree Hash for
    /// every entry count from zero to eight.
    /// </summary>
    /// <param name="kat">The entry count and its expected root.</param>
    [TestMethod]
    [DynamicData(nameof(EntryModeRoots), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void ComputeRoot_WhenGivenReferenceEntries_ShouldReproduceRfc6962MerkleTreeHash(ValidKat<int, string> kat)
    {
        MerkleTree tree = CreateTree();

        Assert.AreEqual(kat.Expected, Hex(tree.ComputeRoot(TakeEntries(kat.Input))));
    }

    /// <summary>
    /// Verifies that the root over fixed-size blocks of a byte stream reproduces the published Merkle Tree Hash,
    /// including a short final block and a zero-length input.
    /// </summary>
    /// <param name="kat">The input length and its expected root.</param>
    [TestMethod]
    [DynamicData(nameof(BlockModeRoots), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void ComputeRoot_WhenGivenFixedSizeBlocks_ShouldReproduceRfc6962MerkleTreeHash(ValidKat<int, string> kat)
    {
        MerkleTree tree = CreateTree();

        Assert.AreEqual(kat.Expected, Hex(tree.ComputeRoot(BlockModeEntries(kat.Input))));
    }

    /// <summary>
    /// Verifies that a tree of zero entries has the hash of the empty string as its root, rather than raising an
    /// exception as the level-by-level types do.
    /// </summary>
    [TestMethod]
    public void ComputeRoot_WhenEntriesAreEmpty_ShouldReturnHashOfEmptyInput()
    {
        MerkleTree tree = CreateTree();

        Assert.AreEqual(Hex(SHA256.HashData([])), Hex(tree.ComputeRoot([])));
    }

    /// <summary>
    /// Verifies that a one-entry tree's root is the entry's leaf hash and specifically <em>not</em> the entry's bare
    /// digest — the property the leaf-domain prefix exists to provide.
    /// </summary>
    [TestMethod]
    public void ComputeRoot_WhenGivenOneEntry_ShouldReturnLeafHashAndNotTheBareDigest()
    {
        MerkleTree tree = CreateTree();
        byte[] entry = [0x61, 0x62, 0x63];

        string root = Hex(tree.ComputeRoot([entry]));

        Assert.AreEqual(Hex(tree.HashLeaf(entry)), root);
        Assert.AreNotEqual(Hex(SHA256.HashData(entry)), root);
    }

    /// <summary>
    /// Verifies that three entries split 2 + 1 rather than 1 + 2, which is the difference between RFC 6962's split
    /// at the largest power of two below the count and a naive halving.
    /// </summary>
    [TestMethod]
    public void ComputeRoot_WhenGivenThreeEntries_ShouldSplitAtTheLargestPowerOfTwoBelowTheCount()
    {
        MerkleTree tree = CreateTree();
        byte[] a = [0x61];
        byte[] b = [0x62];
        byte[] c = [0x63];

        byte[] leafA = tree.HashLeaf(a);
        byte[] leafB = tree.HashLeaf(b);
        byte[] leafC = tree.HashLeaf(c);

        string root = Hex(tree.ComputeRoot([a, b, c]));

        Assert.AreEqual(Hex(tree.HashNode(tree.HashNode(leafA, leafB), leafC)), root);
        Assert.AreNotEqual(Hex(tree.HashNode(leafA, tree.HashNode(leafB, leafC))), root);
    }

    /// <summary>
    /// Verifies that a lone subtree root is promoted unchanged rather than re-hashed as a one-child node — the rule
    /// that lets the level-by-level fold reproduce the recursive definition.
    /// </summary>
    [TestMethod]
    public void ComputeRoot_WhenASubtreeIsLone_ShouldPromoteItUnchangedRatherThanReHashIt()
    {
        MerkleTree tree = CreateTree();
        byte[] a = [0x61];
        byte[] b = [0x62];
        byte[] c = [0x63];

        byte[] leafC = tree.HashLeaf(c);
        byte[] pairAb = tree.HashNode(tree.HashLeaf(a), tree.HashLeaf(b));

        // The level-by-level shape wraps the lone third leaf as the single-child node H(0x01 || leafC) before
        // combining it, which HashNode cannot express because it always takes two children.
        byte[] oneChildPreimage = [0x01, .. leafC];
        byte[] oneChildNode = SHA256.HashData(oneChildPreimage);

        string root = Hex(tree.ComputeRoot([a, b, c]));

        Assert.AreEqual(Hex(tree.HashNode(pairAb, leafC)), root);
        Assert.AreNotEqual(Hex(tree.HashNode(pairAb, oneChildNode)), root);
    }

    /// <summary>
    /// Verifies that an odd trailing leaf is not duplicated and paired with itself, the construction behind
    /// Bitcoin's CVE-2012-2459, under which two different leaf lists can share a root.
    /// </summary>
    /// <remarks>
    /// There are two well-known wrong ways to reduce an odd node: re-hash it alone as a one-child node, which the
    /// package's former level-by-level hasher did, or duplicate it and hash it against itself, which is what Bitcoin
    /// does. RFC 6962 does neither — it promotes the subtree root unchanged — and this pins the root against both
    /// alternatives. Under the duplicating construction a three-leaf tree collides with a four-leaf tree whose last
    /// leaf repeats the third, which is the forgery the CVE describes.
    /// </remarks>
    [TestMethod]
    public void ComputeRoot_WhenTrailingLeafIsOdd_ShouldNotDuplicateItAgainstItself()
    {
        MerkleTree tree = CreateTree();
        byte[] a = [0x61];
        byte[] b = [0x62];
        byte[] c = [0x63];

        byte[] pairAb = tree.HashNode(tree.HashLeaf(a), tree.HashLeaf(b));
        byte[] leafC = tree.HashLeaf(c);

        string threeLeafRoot = Hex(tree.ComputeRoot([a, b, c]));

        // Bitcoin's rebalancing would pair the lone leaf with a copy of itself.
        byte[] duplicatedLeaf = tree.HashNode(leafC, leafC);

        Assert.AreEqual(Hex(tree.HashNode(pairAb, leafC)), threeLeafRoot);
        Assert.AreNotEqual(Hex(tree.HashNode(pairAb, duplicatedLeaf)), threeLeafRoot);

        // And the collision the CVE turns on does not exist here: repeating the third leaf is a different tree.
        Assert.AreNotEqual(threeLeafRoot, Hex(tree.ComputeRoot([a, b, c, c])));
    }

    /// <summary>
    /// Verifies that an internal node's preimage cannot be passed off as leaf data, which is the second-preimage
    /// attack the leaf and node domain prefixes exist to prevent.
    /// </summary>
    /// <remarks>
    /// Without domain separation a two-leaf tree's root is <c>H(l0 || l1)</c>, so an attacker could present the
    /// concatenation of the two child hashes as a single entry and obtain the same root from a one-entry tree. With
    /// the prefixes the two preimages are structurally distinct and no such entry exists.
    /// </remarks>
    [TestMethod]
    public void ComputeRoot_WhenAnInternalNodePreimageIsOfferedAsAnEntry_ShouldNotCollideWithTheTwoLeafRoot()
    {
        MerkleTree tree = CreateTree();
        byte[] first = [0x61];
        byte[] second = [0x62];

        byte[] twoLeafRoot = tree.ComputeRoot([first, second]);
        byte[] nodePreimage = [.. tree.HashLeaf(first), .. tree.HashLeaf(second)];

        Assert.AreNotEqual(Hex(twoLeafRoot), Hex(tree.ComputeRoot([nodePreimage])));
        Assert.AreNotEqual(Hex(twoLeafRoot), Hex(tree.HashLeaf(nodePreimage)));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> entry list is rejected with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ComputeRoot_WhenEntriesIsNull_ShouldThrowArgumentNullException()
    {
        MerkleTree tree = CreateTree();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = tree.ComputeRoot(null!);
        });
    }

    /// <summary>
    /// Verifies that a SHA-512 tree produces 64-byte roots, confirming nothing in the construction assumes a
    /// 32-byte digest.
    /// </summary>
    [TestMethod]
    public void ComputeRoot_WhenAlgorithmIsSha512_ShouldProduceDigestWidthRoots()
    {
        var tree = new MerkleTree(SHA512.Create);

        Assert.AreEqual(64, tree.HashLength);
        Assert.AreEqual(64, tree.ComputeRoot(TakeEntries(5)).Length);
        Assert.AreEqual(Hex(SHA512.HashData([])), Hex(tree.ComputeRoot([])));
    }
}
