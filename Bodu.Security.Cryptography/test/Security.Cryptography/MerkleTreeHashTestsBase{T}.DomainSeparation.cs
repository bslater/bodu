// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeHashTestsBase{T}.DomainSeparation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests that pin the tree's <b>domain separation</b> and <b>length-binding</b> guarantees: a leaf and an
/// internal node must hash into distinct domains, and a leaf must bind the exact byte count of its input so
/// trailing-zero variations cannot collide.
/// </summary>
/// <remarks>
/// <para>
/// These scenarios require a real, non-commutative hash (<see cref="SHA256" />). The additive
/// <c>MonitoringHashAlgorithm</c> used elsewhere cannot express them — a <c>0x00</c> leaf-domain prefix and a
/// dropped zero-pad byte both contribute nothing to a commutative byte-sum, so the additive oracle is blind to
/// exactly the properties under test here.
/// </para>
/// <para>
/// Declared as a partial extension of <see cref="MerkleTreeHashTestsBase{THasher}" /> so both the sequential
/// and parallel implementations inherit the suite and are held to identical output.
/// </para>
/// </remarks>
public abstract partial class MerkleTreeHashTestsBase<THasher>
{
    /// <summary>
    /// Verifies that appending a trailing zero byte to a single-leaf input changes the root, confirming the
    /// leaf binds its real length rather than zero-padding to the block size.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenTrailingZeroByteAppended_ShouldProduceDifferentRoot()
    {
        byte[] shorter = [0x41];
        byte[] longer = [0x41, 0x00];

        using THasher h1 = Construct(Sha256Factory, blockSize: 4, fanOut: 2);
        using THasher h2 = Construct(Sha256Factory, blockSize: 4, fanOut: 2);

        CollectionAssert.AreNotEqual(
            ComputeHash(h1, shorter),
            ComputeHash(h2, longer),
            "Inputs differing only by a trailing zero produced the same root — the leaf is not length-bound.");
    }

    /// <summary>
    /// Verifies that a partial tail leaf and a full-length tail leaf whose extra byte is zero produce different
    /// roots, confirming the partial tail is hashed at its actual length rather than zero-padded to a full block.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenPartialTailPaddedToFullBlock_ShouldProduceDifferentRoot()
    {
        // blockSize 4: first leaf {1,2,3,4}; tail leaf {5,6,7} (length 3) versus {5,6,7,0} (length 4).
        // Under length-ambiguous zero padding both tails collapse to {5,6,7,0} and the roots collide.
        byte[] partialTail = [1, 2, 3, 4, 5, 6, 7];
        byte[] fullTail = [1, 2, 3, 4, 5, 6, 7, 0];

        using THasher h1 = Construct(Sha256Factory, blockSize: 4, fanOut: 2);
        using THasher h2 = Construct(Sha256Factory, blockSize: 4, fanOut: 2);

        CollectionAssert.AreNotEqual(
            ComputeHash(h1, partialTail),
            ComputeHash(h2, fullTail),
            "A zero-padded partial tail collided with an explicit trailing zero — leaf length is not bound.");
    }

    /// <summary>
    /// Verifies that feeding the concatenation of two leaf hashes as a single leaf does not reproduce the root of
    /// the two-leaf tree, confirming leaves and internal nodes are hashed in separate domains (second-preimage /
    /// node-confusion resistance).
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenLeafHashConcatenationFedAsSingleLeaf_ShouldNotReproduceRoot()
    {
        // Obtain the two leaf hashes by hashing each 4-byte block as its own single-leaf tree — a single-leaf
        // tree's root is exactly the leaf hash in every valid tree format, so this does not assume the leaf
        // encoding.
        using THasher leafHasher0 = Construct(Sha256Factory, blockSize: 4, fanOut: 2);
        using THasher leafHasher1 = Construct(Sha256Factory, blockSize: 4, fanOut: 2);
        byte[] leaf0 = ComputeHash(leafHasher0, [1, 2, 3, 4]);
        byte[] leaf1 = ComputeHash(leafHasher1, [5, 6, 7, 8]);

        byte[] concatenated = new byte[leaf0.Length + leaf1.Length];
        Buffer.BlockCopy(leaf0, 0, concatenated, 0, leaf0.Length);
        Buffer.BlockCopy(leaf1, 0, concatenated, leaf0.Length, leaf1.Length);

        using THasher treeHasher = Construct(Sha256Factory, blockSize: 4, fanOut: 2);
        using THasher confusionHasher = Construct(Sha256Factory, blockSize: concatenated.Length, fanOut: 2);

        byte[] root = ComputeHash(treeHasher, [1, 2, 3, 4, 5, 6, 7, 8]);
        byte[] asSingleLeaf = ComputeHash(confusionHasher, concatenated);

        CollectionAssert.AreNotEqual(
            root,
            asSingleLeaf,
            "An internal node's child-hash concatenation reproduced the root when replayed as leaf data — " +
            "leaves and internal nodes share a hash domain.");
    }

    /// <summary>
    /// Verifies that empty input raises <see cref="InvalidOperationException" /> — zero bytes yield no leaves, so
    /// the implementation rejects the call rather than returning a synthetic zero-leaf root.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsEmpty_ShouldThrowExactly()
    {
        using THasher hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            ComputeHash(hasher, Array.Empty<byte>());
        });
    }
}
