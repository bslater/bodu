// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTreeTests.VerifyBlockInclusion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Collections.Merkle;

/// <summary>
/// Tests for
/// <see cref="Rfc6962MerkleTree.VerifyBlockInclusion(ReadOnlySpan{byte}, long, int, long, ReadOnlySpan{byte}, IReadOnlyList{ReadOnlyMemory{byte}})" />,
/// the possession-check shape in which the tree size is derived rather than supplied.
/// </summary>
public partial class Rfc6962MerkleTreeTests
{
    /// <summary>The preimage length of the published authentication-path vector.</summary>
    private const int PathVectorLength = 4_198_400;

    /// <summary>The bound root of the published authentication-path vector.</summary>
    private const string PathVectorRoot = "faadb23a3a175030069185a8da081ae97c20503e86a5c936e9298ce787c90853";

    /// <summary>
    /// Gets the published authentication paths over one-mebibyte blocks of a 4 198 400-byte preimage — five blocks,
    /// the last of them 4 096 bytes.
    /// </summary>
    public static IEnumerable<object[]> BlockInclusionPaths =>
    [
        [new MerkleInclusionKat("blob leaf=0", 5, 0, ["df097c1755b1c8614312ed6afd2eeacb176433cff438abe81dcbc1e597053c11", "d67ee864a248824aa9c5c890d652a4fd9b105192a8d045b8d928456f4c93d792", "5a070a02145f400d7b47120ecbb537b7ab677bf32ddc76c18c48cbef711c8767"])],
        [new MerkleInclusionKat("blob leaf=1", 5, 1, ["1fb9102ff93bbc86947caa125fdc52cf0762333407d518506fed864899671536", "d67ee864a248824aa9c5c890d652a4fd9b105192a8d045b8d928456f4c93d792", "5a070a02145f400d7b47120ecbb537b7ab677bf32ddc76c18c48cbef711c8767"])],
        [new MerkleInclusionKat("blob leaf=2", 5, 2, ["dfe3cff18870ce0af202340a13a9a7f7c7443b71a0a9459743e7e696fb4af1e2", "8fcc293069494813b5da1d45f5d7d8107c05da7ae694a63742ed42435d2b7e3e", "5a070a02145f400d7b47120ecbb537b7ab677bf32ddc76c18c48cbef711c8767"])],
        [new MerkleInclusionKat("blob leaf=3", 5, 3, ["de9adf60ddb113ced877c6e8b5d9cbd1a400a45ad0c850095d943da151b986bd", "8fcc293069494813b5da1d45f5d7d8107c05da7ae694a63742ed42435d2b7e3e", "5a070a02145f400d7b47120ecbb537b7ab677bf32ddc76c18c48cbef711c8767"])],
        [new MerkleInclusionKat("blob leaf=4", 5, 4, ["1161c4a999636ce7d505a09d22e0dede201b229c3d2efdc9d781c42f41af8230"])],
    ];

    /// <summary>
    /// Verifies that the generated path matches the published one and that the block verifies against the published
    /// bound root, at the one-mebibyte block size a real consumer uses.
    /// </summary>
    /// <param name="kat">The block index and its expected path.</param>
    [TestMethod]
    [DynamicData(nameof(BlockInclusionPaths), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void VerifyBlockInclusion_WhenGivenAPublishedBlobPath_ShouldReproduceItAndAccept(MerkleInclusionKat kat)
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] preimage = CounterStream(PathVectorLength);
        byte[] boundRoot = Convert.FromHexString(PathVectorRoot);

        using var stream = new MemoryStream(preimage);
        MerkleComputation computation = tree.ComputeBlocked(stream, OneMebibyteBlock);

        Assert.AreEqual(kat.TreeSize, computation.LeafHashes.Count);
        Assert.AreEqual(PathVectorRoot, Hex(tree.BindRoot(computation.Root, computation.InputLength)));

        byte[][] path = tree.AuthenticationPath(computation.LeafHashes, kat.LeafIndex);
        CollectionAssert.AreEqual(kat.Path, path.Select(step => Hex(step)).ToArray(), "path steps must match in order");

        int offset = (int)MerkleBlocks.BlockOffset(kat.LeafIndex, OneMebibyteBlock);
        int length = MerkleBlocks.BlockLength(PathVectorLength, kat.LeafIndex, OneMebibyteBlock);

        Assert.IsTrue(
            tree.VerifyBlockInclusion(
                boundRoot, PathVectorLength, OneMebibyteBlock, kat.LeafIndex,
                preimage.AsSpan(offset, length), ToPath(path)));
    }

    /// <summary>
    /// Verifies that a single flipped byte in the block is rejected while the path is left untouched — the path is not
    /// the proof, so a holder that kept only the path cannot answer with it.
    /// </summary>
    /// <param name="kat">The block index and its published path.</param>
    [TestMethod]
    [DynamicData(nameof(BlockInclusionPaths), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void VerifyBlockInclusion_WhenBlockIsTamperedButPathIsIntact_ShouldReject(MerkleInclusionKat kat)
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] preimage = CounterStream(PathVectorLength);
        byte[] boundRoot = Convert.FromHexString(PathVectorRoot);
        IReadOnlyList<ReadOnlyMemory<byte>> path = kat.Path
            .Select(step => (ReadOnlyMemory<byte>)Convert.FromHexString(step)).ToArray();

        int offset = (int)MerkleBlocks.BlockOffset(kat.LeafIndex, OneMebibyteBlock);
        int length = MerkleBlocks.BlockLength(PathVectorLength, kat.LeafIndex, OneMebibyteBlock);

        byte[] tampered = preimage.AsSpan(offset, length).ToArray();
        tampered[^1] ^= 0x01;

        Assert.IsFalse(
            tree.VerifyBlockInclusion(
                boundRoot, PathVectorLength, OneMebibyteBlock, kat.LeafIndex, tampered, path));
    }

    /// <summary>
    /// Verifies that a block of the wrong length is rejected, a check the entry-mode overloads cannot make because a
    /// variable-length entry has no expected length.
    /// </summary>
    /// <param name="suppliedLength">The wrong block length to present.</param>
    [TestMethod]
    [DataRow(3)]
    [DataRow(5)]
    [DataRow(0)]
    public void VerifyBlockInclusion_WhenBlockLengthDisagreesWithItsPosition_ShouldReject(int suppliedLength)
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] input = BlockModeInput(17);
        IReadOnlyList<ReadOnlyMemory<byte>> entries = BlockModeEntries(17);
        byte[] boundRoot = tree.BindRoot(tree.ComputeRoot(entries), 17);
        IReadOnlyList<ReadOnlyMemory<byte>> path = ToPath(tree.AuthenticationPath(entries, 0));

        // Block 0 must be exactly four bytes at this block size.
        Assert.IsTrue(tree.VerifyBlockInclusion(boundRoot, 17, VectorBlockSize, 0, input.AsSpan(0, 4), path));
        Assert.IsFalse(
            tree.VerifyBlockInclusion(boundRoot, 17, VectorBlockSize, 0, input.AsSpan(0, suppliedLength), path));
    }

    /// <summary>
    /// Verifies that the short final block must be presented at its actual length, and that padding it to a full block
    /// is rejected.
    /// </summary>
    [TestMethod]
    public void VerifyBlockInclusion_WhenFinalBlockIsPaddedToFullWidth_ShouldReject()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] input = BlockModeInput(17);
        IReadOnlyList<ReadOnlyMemory<byte>> entries = BlockModeEntries(17);
        byte[] boundRoot = tree.BindRoot(tree.ComputeRoot(entries), 17);
        IReadOnlyList<ReadOnlyMemory<byte>> path = ToPath(tree.AuthenticationPath(entries, 4));

        byte[] actualTail = input.AsSpan(16, 1).ToArray();
        byte[] paddedTail = [actualTail[0], 0x00, 0x00, 0x00];

        Assert.IsTrue(tree.VerifyBlockInclusion(boundRoot, 17, VectorBlockSize, 4, actualTail, path));
        Assert.IsFalse(tree.VerifyBlockInclusion(boundRoot, 17, VectorBlockSize, 4, paddedTail, path));
    }

    /// <summary>
    /// Verifies that a block index at or beyond the derived tree size is rejected rather than throwing.
    /// </summary>
    /// <param name="blockIndex">The out-of-range block index.</param>
    [TestMethod]
    [DataRow(5L)]
    [DataRow(6L)]
    [DataRow(-1L)]
    [DataRow(long.MaxValue)]
    public void VerifyBlockInclusion_WhenBlockIndexIsOutsideTheDerivedTree_ShouldReject(long blockIndex)
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] boundRoot = tree.BindRoot(tree.ComputeRoot(BlockModeEntries(17)), 17);

        Assert.IsFalse(tree.VerifyBlockInclusion(boundRoot, 17, VectorBlockSize, blockIndex, [0x00], []));
    }

    /// <summary>
    /// Verifies that a non-positive input length or block size is rejected rather than throwing.
    /// </summary>
    /// <param name="inputLength">The claimed input length.</param>
    /// <param name="blockSize">The claimed block size.</param>
    [TestMethod]
    [DataRow(0L, 4)]
    [DataRow(-1L, 4)]
    [DataRow(16L, 0)]
    [DataRow(16L, -4)]
    public void VerifyBlockInclusion_WhenLengthOrBlockSizeIsNotPositive_ShouldReject(long inputLength, int blockSize)
    {
        Rfc6962MerkleTree tree = CreateTree();

        Assert.IsFalse(tree.VerifyBlockInclusion(new byte[32], inputLength, blockSize, 0, [0x00], []));
    }

    /// <summary>
    /// Verifies that verification returns a boolean and never throws for any combination of malformed root, size,
    /// index and path, which is the property a verifier sitting behind untrusted input must hold.
    /// </summary>
    /// <remarks>
    /// Randomized over a fixed seed so a failure is reproducible. This stands in for the fuzzing that production
    /// RFC 6962 implementations run against their proof verifiers: enumerated cases cannot cover the combinations, and
    /// an exception where a <see langword="false" /> belongs turns a failed proof into a denial of service.
    /// </remarks>
    [TestMethod]
    [TestCategory("Regression")]
    public void VerifyInclusion_WhenGivenArbitraryMalformedInput_ShouldReturnFalseAndNeverThrow()
    {
        Rfc6962MerkleTree tree = CreateTree();
        var random = new Random(Seed: 20260919);

        long[] sizes = [-1, 0, 1, 2, 3, 7, 8, long.MaxValue];
        long[] indices = [-1, 0, 1, 6, 7, 8, long.MaxValue];
        int[] widths = [0, 1, 16, 31, 32, 33, 64];

        for (int iteration = 0; iteration < 4000; iteration++)
        {
            byte[] root = new byte[widths[random.Next(widths.Length)]];
            random.NextBytes(root);

            byte[] entry = new byte[random.Next(0, 40)];
            random.NextBytes(entry);

            ReadOnlyMemory<byte>[] path = new ReadOnlyMemory<byte>[random.Next(0, 6)];
            for (int step = 0; step < path.Length; step++)
            {
                byte[] element = new byte[widths[random.Next(widths.Length)]];
                random.NextBytes(element);
                path[step] = element;
            }

            long treeSize = sizes[random.Next(sizes.Length)];
            long leafIndex = indices[random.Next(indices.Length)];

            try
            {
                // The outcome is not asserted: almost every input is invalid and a chance acceptance would be a
                // genuine proof. What is asserted is that nothing throws.
                _ = tree.VerifyInclusion(root, treeSize, leafIndex, entry, path);
                _ = tree.VerifyInclusionOfLeafHash(root, treeSize, leafIndex, entry, path);
                _ = tree.VerifyInclusionBound(root, treeSize, treeSize, leafIndex, entry, path);
                _ = tree.VerifyBlockInclusion(root, treeSize, 4, leafIndex, entry, path);
            }
            catch (Exception ex)
            {
                Assert.Fail(
                    $"iteration {iteration} threw {ex.GetType().Name}: root={root.Length}B " +
                    $"size={treeSize} index={leafIndex} entry={entry.Length}B steps={path.Length}");
            }
        }
    }
}
