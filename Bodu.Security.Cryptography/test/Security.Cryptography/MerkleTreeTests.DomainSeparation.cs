// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.DomainSeparation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The domain-separation contract across the block-mode roots: leaves are length-bound, a node's preimage cannot be
/// replayed as a leaf, and the empty input is the empty tree.
/// </summary>
public partial class MerkleTreeTests
{
    /// <summary>
    /// Verifies that inputs differing only by a trailing zero byte produce different roots, because a leaf is hashed
    /// at its actual length.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfBlocks_WhenTrailingZeroByteAppended_ShouldProduceDifferentRoot()
    {
        MerkleTree tree = CreateTree();

        Assert.AreNotEqual(
            Hex(tree.ComputeRootOfBlocks(new byte[] { 0x41 }, VectorBlockSize)),
            Hex(tree.ComputeRootOfBlocks(new byte[] { 0x41, 0x00 }, VectorBlockSize)),
            "Inputs differing only by a trailing zero produced the same root — the leaf is not length-bound.");
    }

    /// <summary>
    /// Verifies that a partial tail block and the same tail zero-padded to a full block produce different roots.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfBlocks_WhenPartialTailPaddedToFullBlock_ShouldProduceDifferentRoot()
    {
        MerkleTree tree = CreateTree();
        byte[] partialTail = [1, 2, 3, 4, 5, 6, 7];
        byte[] fullTail = [1, 2, 3, 4, 5, 6, 7, 0];

        Assert.AreNotEqual(
            Hex(tree.ComputeRootOfBlocks(partialTail, VectorBlockSize)),
            Hex(tree.ComputeRootOfBlocks(fullTail, VectorBlockSize)),
            "A zero-padded partial tail collided with an explicit trailing zero — leaf length is not bound.");
    }

    /// <summary>
    /// Verifies that the concatenation of two leaf hashes, presented as a single leaf, does not reproduce the root over
    /// the two blocks — leaves and nodes occupy different hash domains.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfBlocks_WhenLeafHashConcatenationFedAsSingleLeaf_ShouldNotReproduceRoot()
    {
        MerkleTree tree = CreateTree();
        byte[] leaf0 = tree.HashLeaf([1, 2, 3, 4]);
        byte[] leaf1 = tree.HashLeaf([5, 6, 7, 8]);
        byte[] concatenated = [.. leaf0, .. leaf1];

        byte[] root = tree.ComputeRootOfBlocks(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, VectorBlockSize);
        byte[] asSingleLeaf = tree.ComputeRootOfBlocks(concatenated, concatenated.Length);

        Assert.AreNotEqual(Hex(root), Hex(asSingleLeaf), "An internal node's child-hash concatenation reproduced the root when replayed as leaf data.");
        Assert.AreEqual(Hex(tree.HashNode(leaf0, leaf1)), Hex(root));
    }

    /// <summary>
    /// Verifies that an empty input yields the empty tree's root, the hash of zero bytes, on every root path.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfBlocks_WhenInputIsEmpty_ShouldReturnTheEmptyTreeRoot()
    {
        MerkleTree tree = CreateTree();
        string expected = Hex(SHA256.HashData([]));

        Assert.AreEqual(expected, Hex(tree.ComputeRootOfBlocks([], VectorBlockSize)), "array");
        Assert.AreEqual(expected, Hex(tree.ComputeRootOfBlocks(ReadOnlySpan<byte>.Empty, VectorBlockSize)), "span");
        Assert.AreEqual(expected, Hex(tree.ComputeRootOfBlocks(new MemoryStream(), VectorBlockSize)), "stream");
        Assert.AreEqual(expected, Hex(tree.ComputeBlocked([], VectorBlockSize).Root), "blocked");
    }
}
