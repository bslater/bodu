// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTreeTests.HashNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="Rfc6962MerkleTree.HashNode(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />.
/// </summary>
public partial class Rfc6962MerkleTreeTests
{
    /// <summary>
    /// Verifies that a node hash is the digest of the <c>0x01</c> prefix followed by the two child hashes in order.
    /// </summary>
    [TestMethod]
    public void HashNode_WhenGivenTwoChildren_ShouldPrefixWithTheNodeDomainByte()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] left = tree.HashLeaf([0x61, 0x62, 0x63]);
        byte[] right = tree.HashLeaf([0x64, 0x65, 0x66]);

        Assert.AreEqual(Hex(SHA256.HashData([0x01, .. left, .. right])), Hex(tree.HashNode(left, right)));
    }

    /// <summary>
    /// Verifies that the node hash of two known leaves reproduces the value published by the FallbackPlan
    /// conformance vectors, which pin <c>node(leaf("abc"), leaf("def"))</c>.
    /// </summary>
    [TestMethod]
    public void HashNode_WhenGivenThePinnedLeafPair_ShouldReproduceThePublishedNodeHash()
    {
        Rfc6962MerkleTree tree = CreateTree();

        Assert.AreEqual(
            "75c0b5328c14ebdab04b24f779011d375a1b54e89a3fd0f842d7ef449735c92f",
            Hex(tree.HashNode(tree.HashLeaf("abc"u8), tree.HashLeaf("def"u8))));
    }

    /// <summary>
    /// Verifies that child order is significant, so a tree cannot be rebuilt with its subtrees swapped.
    /// </summary>
    [TestMethod]
    public void HashNode_WhenChildrenAreSwapped_ShouldProduceADifferentHash()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] left = tree.HashLeaf([0x61]);
        byte[] right = tree.HashLeaf([0x62]);

        Assert.AreNotEqual(Hex(tree.HashNode(left, right)), Hex(tree.HashNode(right, left)));
    }

    /// <summary>
    /// Verifies that a node hash is distinguishable from a leaf hash over the same bytes, which is what the separate
    /// domain prefixes exist to guarantee.
    /// </summary>
    [TestMethod]
    public void HashNode_WhenComparedWithALeafOverTheSameBytes_ShouldDiffer()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] left = tree.HashLeaf([0x61]);
        byte[] right = tree.HashLeaf([0x62]);

        Assert.AreNotEqual(Hex(tree.HashLeaf([.. left, .. right])), Hex(tree.HashNode(left, right)));
    }

    /// <summary>
    /// Verifies that a child hash of the wrong width is rejected with <see cref="ArgumentException" /> naming the
    /// offending parameter.
    /// </summary>
    /// <param name="leftLength">The left child's length in bytes.</param>
    /// <param name="rightLength">The right child's length in bytes.</param>
    /// <param name="expectedParamName">The parameter expected to be named.</param>
    [TestMethod]
    [DataRow(31, 32, "left")]
    [DataRow(33, 32, "left")]
    [DataRow(32, 31, "right")]
    [DataRow(0, 32, "left")]
    public void HashNode_WhenAChildIsNotDigestWidth_ShouldThrowArgumentException(
        int leftLength, int rightLength, string expectedParamName)
    {
        Rfc6962MerkleTree tree = CreateTree();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = tree.HashNode(new byte[leftLength], new byte[rightLength]);
        });

        Assert.AreEqual(expectedParamName, ex.ParamName);
    }
}
