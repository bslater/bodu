// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlockComputationTests.BlockCount.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="MerkleBlockComputation.BlockCount" />.
/// </summary>
public partial class MerkleBlockComputationTests
{
    /// <summary>
    /// Verifies that the block count is the input length divided by the block size, rounded up, and zero for an empty
    /// input.
    /// </summary>
    /// <param name="inputLength">The input length in bytes.</param>
    /// <param name="expected">The expected number of blocks at a block size of four.</param>
    [TestMethod]
    [DataRow(0, 0L)]
    [DataRow(1, 1L)]
    [DataRow(4, 1L)]
    [DataRow(5, 2L)]
    [DataRow(8, 2L)]
    [DataRow(9, 3L)]
    [DataRow(33, 9L)]
    public void BlockCount_WhenGivenAnInputLength_ShouldRoundUp(int inputLength, long expected) =>
        Assert.AreEqual(expected, Compute(inputLength).BlockCount);

    /// <summary>
    /// Verifies that the block count always equals the number of leaf hashes retained, since the tree hashes exactly
    /// one leaf per block.
    /// </summary>
    [TestMethod]
    public void BlockCount_WhenComparedWithLeafHashes_ShouldMatchTheirCount()
    {
        for (var inputLength = 0; inputLength <= 40; inputLength++)
        {
            MerkleBlockComputation computation = Compute(inputLength);

            Assert.AreEqual(computation.LeafHashes.Count, computation.BlockCount, $"input length {inputLength}");
        }
    }

    /// <summary>
    /// Verifies that the block count agrees with <see cref="MerkleTree.BlockCount(long, int)" /> for the
    /// computation's own input length and block size.
    /// </summary>
    [TestMethod]
    public void BlockCount_WhenComparedWithMerkleBlocks_ShouldAgree()
    {
        MerkleBlockComputation computation = Compute(2109, blockSize: 64);

        Assert.AreEqual(MerkleTree.BlockCount(2109, 64), computation.BlockCount);
    }
}
