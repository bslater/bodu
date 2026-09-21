// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.AuthenticationPath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="MerkleTree.AuthenticationPath(IReadOnlyList{ReadOnlyMemory{byte}}, long)" /> and its
/// leaf-hash overload.
/// </summary>
public partial class MerkleTreeTests
{
    /// <summary>
    /// Gets the published authentication paths for every leaf of the seven- and eight-entry reference trees
    /// (requirements appendices A2 and A3).
    /// </summary>
    public static IEnumerable<object[]> ReferenceInclusionPaths =>
    [
        [new MerkleInclusionKat("A size=7 leaf=0", 7, 0, ["96a296d224f285c67bee93c30f8a309157f0daa35dc5b87e410b78630a09cfc7", "5f083f0a1a33ca076a95279832580db3e0ef4584bdff1f54c8a360f50de3031e", "837dbb152e9b079010717e84e865da4ebc0fa198a806d59d31bf15accef22d0e"])],
        [new MerkleInclusionKat("A size=7 leaf=1", 7, 1, ["6e340b9cffb37a989ca544e6bb780a2c78901d3fb33738768511a30617afa01d", "5f083f0a1a33ca076a95279832580db3e0ef4584bdff1f54c8a360f50de3031e", "837dbb152e9b079010717e84e865da4ebc0fa198a806d59d31bf15accef22d0e"])],
        [new MerkleInclusionKat("A size=7 leaf=2", 7, 2, ["07506a85fd9dd2f120eb694f86011e5bb4662e5c415a62917033d4a9624487e7", "fac54203e7cc696cf0dfcb42c92a1d9dbaf70ad9e621f4bd8d98662f00e3c125", "837dbb152e9b079010717e84e865da4ebc0fa198a806d59d31bf15accef22d0e"])],
        [new MerkleInclusionKat("A size=7 leaf=3", 7, 3, ["0298d122906dcfc10892cb53a73992fc5b9f493ea4c9badb27b791b4127a7fe7", "fac54203e7cc696cf0dfcb42c92a1d9dbaf70ad9e621f4bd8d98662f00e3c125", "837dbb152e9b079010717e84e865da4ebc0fa198a806d59d31bf15accef22d0e"])],
        [new MerkleInclusionKat("A size=7 leaf=4", 7, 4, ["4271a26be0d8a84f0bd54c8c302e7cb3a3b5d1fa6780a40bcce2873477dab658", "b08693ec2e721597130641e8211e7eedccb4c26413963eee6c1e2ed16ffb1a5f", "d37ee418976dd95753c1c73862b9398fa2a2cf9b4ff0fdfe8b30cd95209614b7"])],
        [new MerkleInclusionKat("A size=7 leaf=5", 7, 5, ["bc1a0643b12e4d2d7c77918f44e0f4f79a838b6cf9ec5b5c283e1f4d88599e6b", "b08693ec2e721597130641e8211e7eedccb4c26413963eee6c1e2ed16ffb1a5f", "d37ee418976dd95753c1c73862b9398fa2a2cf9b4ff0fdfe8b30cd95209614b7"])],
        [new MerkleInclusionKat("A size=7 leaf=6", 7, 6, ["0ebc5d3437fbe2db158b9f126a1d118e308181031d0a949f8dededebc558ef6a", "d37ee418976dd95753c1c73862b9398fa2a2cf9b4ff0fdfe8b30cd95209614b7"])],
        [new MerkleInclusionKat("A size=8 leaf=0", 8, 0, ["96a296d224f285c67bee93c30f8a309157f0daa35dc5b87e410b78630a09cfc7", "5f083f0a1a33ca076a95279832580db3e0ef4584bdff1f54c8a360f50de3031e", "6b47aaf29ee3c2af9af889bc1fb9254dabd31177f16232dd6aab035ca39bf6e4"])],
        [new MerkleInclusionKat("A size=8 leaf=1", 8, 1, ["6e340b9cffb37a989ca544e6bb780a2c78901d3fb33738768511a30617afa01d", "5f083f0a1a33ca076a95279832580db3e0ef4584bdff1f54c8a360f50de3031e", "6b47aaf29ee3c2af9af889bc1fb9254dabd31177f16232dd6aab035ca39bf6e4"])],
        [new MerkleInclusionKat("A size=8 leaf=2", 8, 2, ["07506a85fd9dd2f120eb694f86011e5bb4662e5c415a62917033d4a9624487e7", "fac54203e7cc696cf0dfcb42c92a1d9dbaf70ad9e621f4bd8d98662f00e3c125", "6b47aaf29ee3c2af9af889bc1fb9254dabd31177f16232dd6aab035ca39bf6e4"])],
        [new MerkleInclusionKat("A size=8 leaf=3", 8, 3, ["0298d122906dcfc10892cb53a73992fc5b9f493ea4c9badb27b791b4127a7fe7", "fac54203e7cc696cf0dfcb42c92a1d9dbaf70ad9e621f4bd8d98662f00e3c125", "6b47aaf29ee3c2af9af889bc1fb9254dabd31177f16232dd6aab035ca39bf6e4"])],
        [new MerkleInclusionKat("A size=8 leaf=4", 8, 4, ["4271a26be0d8a84f0bd54c8c302e7cb3a3b5d1fa6780a40bcce2873477dab658", "ca854ea128ed050b41b35ffc1b87b8eb2bde461e9e3b5596ece6b9d5975a0ae0", "d37ee418976dd95753c1c73862b9398fa2a2cf9b4ff0fdfe8b30cd95209614b7"])],
        [new MerkleInclusionKat("A size=8 leaf=5", 8, 5, ["bc1a0643b12e4d2d7c77918f44e0f4f79a838b6cf9ec5b5c283e1f4d88599e6b", "ca854ea128ed050b41b35ffc1b87b8eb2bde461e9e3b5596ece6b9d5975a0ae0", "d37ee418976dd95753c1c73862b9398fa2a2cf9b4ff0fdfe8b30cd95209614b7"])],
        [new MerkleInclusionKat("A size=8 leaf=6", 8, 6, ["46f6ffadd3d06a09ff3c5860d2755c8b9819db7df44251788c7d8e3180de8eb1", "0ebc5d3437fbe2db158b9f126a1d118e308181031d0a949f8dededebc558ef6a", "d37ee418976dd95753c1c73862b9398fa2a2cf9b4ff0fdfe8b30cd95209614b7"])],
        [new MerkleInclusionKat("A size=8 leaf=7", 8, 7, ["b08693ec2e721597130641e8211e7eedccb4c26413963eee6c1e2ed16ffb1a5f", "0ebc5d3437fbe2db158b9f126a1d118e308181031d0a949f8dededebc558ef6a", "d37ee418976dd95753c1c73862b9398fa2a2cf9b4ff0fdfe8b30cd95209614b7"])],
    ];

    /// <summary>
    /// Verifies that the generated path matches the published one element for element and in order.
    /// </summary>
    /// <param name="kat">The tree size, leaf index and expected path.</param>
    [TestMethod]
    [DynamicData(nameof(ReferenceInclusionPaths), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void AuthenticationPath_WhenGivenReferenceEntries_ShouldReproducePublishedPaths(MerkleInclusionKat kat)
    {
        MerkleTree tree = CreateTree();

        byte[][] path = tree.AuthenticationPath(TakeEntries(kat.TreeSize), kat.LeafIndex);

        CollectionAssert.AreEqual(kat.Path, path.Select(step => Hex(step)).ToArray(), "path steps must match in order");
    }

    /// <summary>
    /// Verifies that leaf 6 of a seven-entry tree has a two-step path where every other leaf has three, which is the
    /// case that catches a verifier assuming a uniform path length.
    /// </summary>
    [TestMethod]
    public void AuthenticationPath_WhenTreeIsNotPerfect_ShouldVaryPathLengthByLeafIndex()
    {
        MerkleTree tree = CreateTree();
        IReadOnlyList<ReadOnlyMemory<byte>> entries = TakeEntries(7);

        Assert.AreEqual(2, tree.AuthenticationPath(entries, 6).Length);

        for (int leafIndex = 0; leafIndex <= 5; leafIndex++)
            Assert.AreEqual(3, tree.AuthenticationPath(entries, leafIndex).Length, $"leaf {leafIndex}");
    }

    /// <summary>
    /// Verifies that a one-entry tree produces an empty path, because its leaf hash is already the root.
    /// </summary>
    [TestMethod]
    public void AuthenticationPath_WhenTreeHasOneEntry_ShouldReturnAnEmptyPath()
    {
        MerkleTree tree = CreateTree();

        Assert.AreEqual(0, tree.AuthenticationPath(TakeEntries(1), 0).Length);
    }

    /// <summary>
    /// Verifies that no path is longer than <c>ceil(log2(treeSize))</c> steps, the bound a verifier may reject on
    /// before walking.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void AuthenticationPath_WhenGeneratedForAnyLeaf_ShouldNeverExceedCeilingLog2OfTreeSize()
    {
        MerkleTree tree = CreateTree();

        for (int treeSize = 1; treeSize <= 64; treeSize++)
        {
            int bound = (int)Math.Ceiling(Math.Log2(treeSize));
            byte[][] leafHashes = SyntheticLeafHashes(treeSize);

            for (int leafIndex = 0; leafIndex < treeSize; leafIndex++)
            {
                int length = tree.AuthenticationPath(leafHashes, leafIndex).Length;
                Assert.IsTrue(
                    length <= bound,
                    $"size {treeSize} leaf {leafIndex}: {length} steps exceeds the bound of {bound}");
            }
        }
    }

    /// <summary>
    /// Verifies that the path built from precomputed leaf hashes matches the path built from the entries themselves.
    /// </summary>
    /// <param name="treeSize">The number of reference entries to use.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(5)]
    [DataRow(7)]
    [DataRow(8)]
    public void AuthenticationPath_WhenBuiltFromLeafHashes_ShouldAgreeWithTheEntryOverload(int treeSize)
    {
        MerkleTree tree = CreateTree();
        IReadOnlyList<ReadOnlyMemory<byte>> entries = TakeEntries(treeSize);

        byte[][] leafHashes = new byte[treeSize][];
        for (int index = 0; index < treeSize; index++)
            leafHashes[index] = tree.HashLeaf(ReferenceEntries[index]);

        for (int leafIndex = 0; leafIndex < treeSize; leafIndex++)
        {
            CollectionAssert.AreEqual(
                tree.AuthenticationPath(entries, leafIndex).Select(step => Hex(step)).ToArray(),
                tree.AuthenticationPath(leafHashes, leafIndex).Select(step => Hex(step)).ToArray(),
                $"leaf {leafIndex} of {treeSize}");
        }
    }

    /// <summary>
    /// Verifies that the leaf hashes retained by a streamed block-mode computation produce a path that verifies
    /// against that computation's root, so a party need not re-read the input to answer a challenge.
    /// </summary>
    [TestMethod]
    public void AuthenticationPath_WhenBuiltFromAStreamedComputation_ShouldVerifyAgainstItsRoot()
    {
        MerkleTree tree = CreateTree();
        byte[] input = BlockModeInput(17);
        using var stream = new MemoryStream(input);

        MerkleBlockComputation computation = tree.ComputeBlocked(stream, VectorBlockSize);

        for (long blockIndex = 0; blockIndex < computation.LeafHashes.Count; blockIndex++)
        {
            byte[][] path = tree.AuthenticationPath(computation.LeafHashes, blockIndex);
            int offset = (int)MerkleBlocks.BlockOffset(blockIndex, VectorBlockSize);
            int length = MerkleBlocks.BlockLength(input.Length, blockIndex, VectorBlockSize);

            Assert.IsTrue(
                tree.VerifyInclusion(
                    computation.Root,
                    computation.LeafHashes.Count,
                    blockIndex,
                    input.AsSpan(offset, length),
                    ToPath(path)),
                $"block {blockIndex} must verify");
        }
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> entry list is rejected with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AuthenticationPath_WhenEntriesIsNull_ShouldThrowArgumentNullException()
    {
        MerkleTree tree = CreateTree();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = tree.AuthenticationPath((IReadOnlyList<ReadOnlyMemory<byte>>)null!, 0);
        });
    }

    /// <summary>
    /// Verifies that a leaf index outside the tree is rejected with
    /// <see cref="ArgumentOutOfRangeException" />, because a caller asking for a path it cannot have has made a
    /// programming error rather than presented a wire value.
    /// </summary>
    /// <param name="treeSize">The number of entries in the tree.</param>
    /// <param name="leafIndex">The out-of-range leaf index.</param>
    [TestMethod]
    [DataRow(1, 1L)]
    [DataRow(3, 3L)]
    [DataRow(3, 4L)]
    [DataRow(8, -1L)]
    [DataRow(8, long.MaxValue)]
    public void AuthenticationPath_WhenLeafIndexIsOutsideTheTree_ShouldThrowArgumentOutOfRangeException(
        int treeSize, long leafIndex)
    {
        MerkleTree tree = CreateTree();

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = tree.AuthenticationPath(TakeEntries(treeSize), leafIndex);
        });
    }

    /// <summary>
    /// Converts a generated path to the read-only memory list the verification surface accepts.
    /// </summary>
    /// <param name="path">The path steps.</param>
    /// <returns>The path as a read-only memory list.</returns>
    private static IReadOnlyList<ReadOnlyMemory<byte>> ToPath(IReadOnlyList<byte[]> path) =>
        path.Select(step => (ReadOnlyMemory<byte>)step).ToArray();
}
