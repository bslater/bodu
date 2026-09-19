// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTreeTests.ComputeRoot.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="Rfc6962MerkleTree.ComputeRoot(IReadOnlyList{ReadOnlyMemory{byte}})" />.
/// </summary>
public partial class Rfc6962MerkleTreeTests
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
        Rfc6962MerkleTree tree = CreateTree();

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
        Rfc6962MerkleTree tree = CreateTree();

        Assert.AreEqual(kat.Expected, Hex(tree.ComputeRoot(BlockModeEntries(kat.Input))));
    }

    /// <summary>
    /// Verifies that a tree of zero entries has the hash of the empty string as its root, rather than raising an
    /// exception as the level-by-level types do.
    /// </summary>
    [TestMethod]
    public void ComputeRoot_WhenEntriesAreEmpty_ShouldReturnHashOfEmptyInput()
    {
        Rfc6962MerkleTree tree = CreateTree();

        Assert.AreEqual(Hex(SHA256.HashData([])), Hex(tree.ComputeRoot([])));
    }

    /// <summary>
    /// Verifies that a one-entry tree's root is the entry's leaf hash and specifically <em>not</em> the entry's bare
    /// digest — the property the leaf-domain prefix exists to provide.
    /// </summary>
    [TestMethod]
    public void ComputeRoot_WhenGivenOneEntry_ShouldReturnLeafHashAndNotTheBareDigest()
    {
        Rfc6962MerkleTree tree = CreateTree();
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
        Rfc6962MerkleTree tree = CreateTree();
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
    /// Verifies that a lone subtree root is promoted unchanged rather than re-hashed as a one-child node. This is
    /// the exact point on which <see cref="MerkleTreeHash" />'s level-by-level reduction differs.
    /// </summary>
    [TestMethod]
    public void ComputeRoot_WhenASubtreeIsLone_ShouldPromoteItUnchangedRatherThanReHashIt()
    {
        Rfc6962MerkleTree tree = CreateTree();
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
    /// Verifies that a <see langword="null" /> entry list is rejected with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ComputeRoot_WhenEntriesIsNull_ShouldThrowArgumentNullException()
    {
        Rfc6962MerkleTree tree = CreateTree();

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
        var tree = new Rfc6962MerkleTree(SHA512.Create);

        Assert.AreEqual(64, tree.HashLength);
        Assert.AreEqual(64, tree.ComputeRoot(TakeEntries(5)).Length);
        Assert.AreEqual(Hex(SHA512.HashData([])), Hex(tree.ComputeRoot([])));
    }
}
