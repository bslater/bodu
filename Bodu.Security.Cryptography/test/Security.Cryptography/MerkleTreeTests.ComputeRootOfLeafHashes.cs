// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.ComputeRootOfLeafHashes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="MerkleTree.ComputeRootOfLeafHashes(IReadOnlyList{byte[]})" />.
/// </summary>
public partial class MerkleTreeTests
{
    /// <summary>
    /// Gets the pinned tree heads over the synthetic leaf hashes <c>leaf(0x00)</c> … <c>leaf(0x07)</c>, as the
    /// FallbackPlan conformance vectors publish them.
    /// </summary>
    public static IEnumerable<object[]> SyntheticLeafHashShapes =>
    [
        [new ValidKat<int, string>("shape leaves=1", 1, "96a296d224f285c67bee93c30f8a309157f0daa35dc5b87e410b78630a09cfc7")],
        [new ValidKat<int, string>("shape leaves=2", 2, "a20bf9a7cc2dc8a08f5f415a71b19f6ac427bab54d24eec868b5d3103449953a")],
        [new ValidKat<int, string>("shape leaves=3", 3, "3b6cccd7e3e023ff393006f030315ee7ad9eb111b022b41fba7e5b7a3973f688")],
        [new ValidKat<int, string>("shape leaves=4", 4, "9bcd51240af4005168f033121ba85be5a6ed4f0e6a5fac262066729b8fbfdecb")],
        [new ValidKat<int, string>("shape leaves=5", 5, "b855b42d6c30f5b087e05266783fbd6e394f7b926013ccaa67700a8b0c5a596f")],
        [new ValidKat<int, string>("shape leaves=6", 6, "bb36e7d3d4cee5720cbd323d02fab15962e2ba1dadf5f8fc6eeef4fd6ad056a8")],
        [new ValidKat<int, string>("shape leaves=7", 7, "3560191803028444b232018ac047fdb561c09c23a7a6876c85e08b5e4d48e9f3")],
        [new ValidKat<int, string>("shape leaves=8", 8, "ef7f49b620f6c7ea9b963a214da34b5021c6ded8ed57734380a311ab726aa907")],
    ];

    /// <summary>
    /// Returns the first <paramref name="count" /> synthetic leaf hashes, where leaf <em>i</em> is the leaf hash of
    /// the single byte <em>i</em>.
    /// </summary>
    /// <param name="count">The number of leaf hashes to produce.</param>
    /// <returns>The leaf hashes, in order.</returns>
    private static byte[][] SyntheticLeafHashes(int count)
    {
        MerkleTree tree = CreateTree();

        byte[][] leafHashes = new byte[count][];
        for (int index = 0; index < count; index++)
            leafHashes[index] = tree.HashLeaf([(byte)index]);

        return leafHashes;
    }

    /// <summary>
    /// Verifies that folding precomputed leaf hashes reproduces the published tree head for every leaf count from
    /// one to eight, including the non-power-of-two shapes.
    /// </summary>
    /// <param name="kat">The leaf count and its expected head.</param>
    [TestMethod]
    [DynamicData(nameof(SyntheticLeafHashShapes), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void ComputeRootOfLeafHashes_WhenGivenSyntheticLeafHashes_ShouldReproducePublishedHeads(
        ValidKat<int, string> kat)
    {
        MerkleTree tree = CreateTree();

        Assert.AreEqual(kat.Expected, Hex(tree.ComputeRootOfLeafHashes(SyntheticLeafHashes(kat.Input))));
    }

    /// <summary>
    /// Verifies that folding leaf hashes agrees with computing the root from the entries directly, for every entry
    /// count of the reference suite.
    /// </summary>
    /// <param name="entryCount">The number of reference entries to use.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(5)]
    [DataRow(7)]
    [DataRow(8)]
    public void ComputeRootOfLeafHashes_WhenGivenTheEntriesOwnLeafHashes_ShouldAgreeWithComputeRoot(int entryCount)
    {
        MerkleTree tree = CreateTree();

        byte[][] leafHashes = new byte[entryCount][];
        for (int index = 0; index < entryCount; index++)
            leafHashes[index] = tree.HashLeaf(ReferenceEntries[index]);

        Assert.AreEqual(
            Hex(tree.ComputeRoot(TakeEntries(entryCount))),
            Hex(tree.ComputeRootOfLeafHashes(leafHashes)));
    }

    /// <summary>
    /// Verifies that an empty leaf-hash list yields the hash of the empty string.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfLeafHashes_WhenListIsEmpty_ShouldReturnHashOfEmptyInput()
    {
        MerkleTree tree = CreateTree();

        Assert.AreEqual(Hex(SHA256.HashData([])), Hex(tree.ComputeRootOfLeafHashes([])));
    }

    /// <summary>
    /// Verifies that a single leaf hash is promoted unchanged as the tree's head rather than being re-hashed.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfLeafHashes_WhenListHasOneElement_ShouldReturnThatLeafHashUnchanged()
    {
        MerkleTree tree = CreateTree();
        byte[] leafHash = tree.HashLeaf([0x61, 0x62, 0x63]);

        Assert.AreEqual(Hex(leafHash), Hex(tree.ComputeRootOfLeafHashes([leafHash])));
    }

    /// <summary>
    /// Verifies that mutating the caller's array after the call cannot change a previously computed root, because
    /// the list is copied on entry.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfLeafHashes_WhenCallerMutatesTheListAfterwards_ShouldNotAffectTheComputedRoot()
    {
        MerkleTree tree = CreateTree();
        byte[][] leafHashes = SyntheticLeafHashes(3);

        string before = Hex(tree.ComputeRootOfLeafHashes(leafHashes));
        leafHashes[0] = tree.HashLeaf([0xFF]);

        Assert.AreEqual("3b6cccd7e3e023ff393006f030315ee7ad9eb111b022b41fba7e5b7a3973f688", before);
        Assert.AreNotEqual(before, Hex(tree.ComputeRootOfLeafHashes(leafHashes)));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> list is rejected with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfLeafHashes_WhenListIsNull_ShouldThrowArgumentNullException()
    {
        MerkleTree tree = CreateTree();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = tree.ComputeRootOfLeafHashes(null!);
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> element is rejected with <see cref="ArgumentException" /> naming the
    /// list and reporting the offending index.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfLeafHashes_WhenAnElementIsNull_ShouldThrowArgumentException()
    {
        MerkleTree tree = CreateTree();
        byte[][] leafHashes = [tree.HashLeaf([0x00]), null!, tree.HashLeaf([0x02])];

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = tree.ComputeRootOfLeafHashes(leafHashes);
        });

        Assert.AreEqual("leafHashes", ex.ParamName);
        Assert.IsTrue(ex.Message.Contains('1', StringComparison.Ordinal), "the message should report the index");
    }

    /// <summary>
    /// Verifies that an element of the wrong width is rejected with <see cref="ArgumentException" />, since a tree
    /// folded over mismatched widths would produce a meaningless head.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfLeafHashes_WhenAnElementIsNotDigestWidth_ShouldThrowArgumentException()
    {
        MerkleTree tree = CreateTree();
        byte[][] leafHashes = [tree.HashLeaf([0x00]), new byte[16]];

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = tree.ComputeRootOfLeafHashes(leafHashes);
        });

        Assert.AreEqual("leafHashes", ex.ParamName);
    }
}
