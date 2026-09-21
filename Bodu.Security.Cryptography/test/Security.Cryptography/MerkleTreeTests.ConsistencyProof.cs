// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.ConsistencyProof.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="MerkleTree.ConsistencyProof(IReadOnlyList{ReadOnlyMemory{byte}}, long)" /> and its
/// leaf-hash overload.
/// </summary>
public partial class MerkleTreeTests
{
    /// <summary>
    /// Gets the published consistency proofs between sizes of the reference tree (requirements appendix A4).
    /// </summary>
    public static IEnumerable<object[]> ReferenceConsistencyProofs =>
    [
        [new MerkleConsistencyKat("A4 1->1", 1, 1, [])],
        [new MerkleConsistencyKat("A4 1->8", 1, 8, ["96a296d224f285c67bee93c30f8a309157f0daa35dc5b87e410b78630a09cfc7", "5f083f0a1a33ca076a95279832580db3e0ef4584bdff1f54c8a360f50de3031e", "6b47aaf29ee3c2af9af889bc1fb9254dabd31177f16232dd6aab035ca39bf6e4"])],
        [new MerkleConsistencyKat("A4 2->5", 2, 5, ["5f083f0a1a33ca076a95279832580db3e0ef4584bdff1f54c8a360f50de3031e", "bc1a0643b12e4d2d7c77918f44e0f4f79a838b6cf9ec5b5c283e1f4d88599e6b"])],
        [new MerkleConsistencyKat("A4 3->7", 3, 7, ["0298d122906dcfc10892cb53a73992fc5b9f493ea4c9badb27b791b4127a7fe7", "07506a85fd9dd2f120eb694f86011e5bb4662e5c415a62917033d4a9624487e7", "fac54203e7cc696cf0dfcb42c92a1d9dbaf70ad9e621f4bd8d98662f00e3c125", "837dbb152e9b079010717e84e865da4ebc0fa198a806d59d31bf15accef22d0e"])],
        [new MerkleConsistencyKat("A4 4->8", 4, 8, ["6b47aaf29ee3c2af9af889bc1fb9254dabd31177f16232dd6aab035ca39bf6e4"])],
        [new MerkleConsistencyKat("A4 6->8", 6, 8, ["0ebc5d3437fbe2db158b9f126a1d118e308181031d0a949f8dededebc558ef6a", "ca854ea128ed050b41b35ffc1b87b8eb2bde461e9e3b5596ece6b9d5975a0ae0", "d37ee418976dd95753c1c73862b9398fa2a2cf9b4ff0fdfe8b30cd95209614b7"])],
        [new MerkleConsistencyKat("A4 7->8", 7, 8, ["b08693ec2e721597130641e8211e7eedccb4c26413963eee6c1e2ed16ffb1a5f", "46f6ffadd3d06a09ff3c5860d2755c8b9819db7df44251788c7d8e3180de8eb1", "0ebc5d3437fbe2db158b9f126a1d118e308181031d0a949f8dededebc558ef6a", "d37ee418976dd95753c1c73862b9398fa2a2cf9b4ff0fdfe8b30cd95209614b7"])],
    ];

    /// <summary>
    /// Verifies that the generated consistency proof matches the published one element for element and in order.
    /// </summary>
    /// <param name="kat">The two sizes and the expected proof.</param>
    [TestMethod]
    [DynamicData(nameof(ReferenceConsistencyProofs), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void ConsistencyProof_WhenGivenReferenceEntries_ShouldReproducePublishedProofs(MerkleConsistencyKat kat)
    {
        MerkleTree tree = CreateTree();

        byte[][] proof = tree.ConsistencyProof(TakeEntries(kat.SecondSize), kat.FirstSize);

        CollectionAssert.AreEqual(kat.Proof, proof.Select(step => Hex(step)).ToArray(), "proof steps must match in order");
    }

    /// <summary>
    /// Verifies that a tree consistent with itself yields an empty proof, because the sizes and roots establish it
    /// without evidence.
    /// </summary>
    /// <param name="size">The tree size.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(5)]
    [DataRow(8)]
    public void ConsistencyProof_WhenSizesAreEqual_ShouldReturnAnEmptyProof(int size)
    {
        MerkleTree tree = CreateTree();

        Assert.AreEqual(0, tree.ConsistencyProof(TakeEntries(size), size).Length);
    }

    /// <summary>
    /// Verifies that a first size of zero yields an empty proof, since every tree extends the empty tree.
    /// </summary>
    [TestMethod]
    public void ConsistencyProof_WhenFirstSizeIsZero_ShouldReturnAnEmptyProof()
    {
        MerkleTree tree = CreateTree();

        Assert.AreEqual(0, tree.ConsistencyProof(TakeEntries(8), 0).Length);
    }

    /// <summary>
    /// Verifies that the proof built from precomputed leaf hashes matches the proof built from the entries.
    /// </summary>
    /// <param name="firstSize">The earlier tree's size.</param>
    /// <param name="secondSize">The later tree's size.</param>
    [TestMethod]
    [DataRow(1, 8)]
    [DataRow(2, 5)]
    [DataRow(3, 7)]
    [DataRow(6, 8)]
    [DataRow(7, 8)]
    public void ConsistencyProofOfLeafHashes_WhenBuiltFromLeafHashes_ShouldAgreeWithTheEntryOverload(
        int firstSize, int secondSize)
    {
        MerkleTree tree = CreateTree();

        byte[][] leafHashes = new byte[secondSize][];
        for (int index = 0; index < secondSize; index++)
            leafHashes[index] = tree.HashLeaf(ReferenceEntries[index]);

        CollectionAssert.AreEqual(
            tree.ConsistencyProof(TakeEntries(secondSize), firstSize).Select(step => Hex(step)).ToArray(),
            tree.ConsistencyProofOfLeafHashes(leafHashes, firstSize).Select(step => Hex(step)).ToArray());
    }

    /// <summary>
    /// Verifies that a first size beyond the later tree is rejected with
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="firstSize">The out-of-range first size.</param>
    [TestMethod]
    [DataRow(-1L)]
    [DataRow(9L)]
    [DataRow(long.MaxValue)]
    public void ConsistencyProof_WhenFirstSizeIsOutOfRange_ShouldThrowArgumentOutOfRangeException(long firstSize)
    {
        MerkleTree tree = CreateTree();

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = tree.ConsistencyProof(TakeEntries(8), firstSize);
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> entry list is rejected with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ConsistencyProof_WhenEntriesIsNull_ShouldThrowArgumentNullException()
    {
        MerkleTree tree = CreateTree();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = tree.ConsistencyProof(null!, 0);
        });
    }
}
