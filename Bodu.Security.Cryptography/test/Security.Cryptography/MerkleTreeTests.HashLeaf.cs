// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.HashLeaf.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="MerkleTree.HashLeaf(ReadOnlySpan{byte})" />.
/// </summary>
public partial class MerkleTreeTests
{
    /// <summary>
    /// Verifies that a leaf hash is the digest of the <c>0x00</c> prefix followed by the entry, and never the
    /// entry's bare digest.
    /// </summary>
    [TestMethod]
    public void HashLeaf_WhenGivenAnEntry_ShouldPrefixWithTheLeafDomainByte()
    {
        MerkleTree tree = CreateTree();
        byte[] entry = [0x61, 0x62, 0x63];

        Assert.AreEqual("609f6e36d2405585188d5cfd761f407c7cc46a7d3f314c88270469dde315fcd1", Hex(tree.HashLeaf(entry)));
        Assert.AreEqual(Hex(SHA256.HashData([0x00, .. entry])), Hex(tree.HashLeaf(entry)));
        Assert.AreNotEqual(Hex(SHA256.HashData(entry)), Hex(tree.HashLeaf(entry)));
    }

    /// <summary>
    /// Verifies that the empty entry has a leaf hash distinct from the hash of zero bytes, so an empty leaf and an
    /// empty tree cannot be confused.
    /// </summary>
    [TestMethod]
    public void HashLeaf_WhenEntryIsEmpty_ShouldDifferFromTheEmptyTreeRoot()
    {
        MerkleTree tree = CreateTree();

        Assert.AreEqual("6e340b9cffb37a989ca544e6bb780a2c78901d3fb33738768511a30617afa01d", Hex(tree.HashLeaf([])));
        Assert.AreNotEqual(Hex(SHA256.HashData([])), Hex(tree.HashLeaf([])));
    }

    /// <summary>
    /// Verifies that a leaf hash is the configured digest width.
    /// </summary>
    [TestMethod]
    public void HashLeaf_WhenGivenAnEntry_ShouldReturnDigestWidthBytes()
    {
        MerkleTree tree = CreateTree();

        Assert.AreEqual(tree.HashLength, tree.HashLeaf([0x01, 0x02]).Length);
    }

    /// <summary>
    /// Verifies that entries differing only by trailing zeros produce different leaf hashes, because a short entry
    /// is hashed at its actual length and never padded.
    /// </summary>
    [TestMethod]
    public void HashLeaf_WhenEntriesDifferByTrailingZeros_ShouldProduceDifferentHashes()
    {
        MerkleTree tree = CreateTree();

        Assert.AreNotEqual(Hex(tree.HashLeaf([0x01])), Hex(tree.HashLeaf([0x01, 0x00])));
    }
}
