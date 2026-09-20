// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlockComputationTests.BlockOffset.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Collections.Specialized;

/// <summary>
/// Tests for <see cref="MerkleBlockComputation.BlockOffset(long)" />.
/// </summary>
public partial class MerkleBlockComputationTests
{
    /// <summary>
    /// Verifies that a block's offset is its index multiplied by the block size.
    /// </summary>
    /// <param name="blockIndex">The zero-based block index.</param>
    /// <param name="expected">The expected offset in bytes at a block size of four.</param>
    [TestMethod]
    [DataRow(0L, 0L)]
    [DataRow(1L, 4L)]
    [DataRow(2L, 8L)]
    public void BlockOffset_WhenGivenAnExistingBlock_ShouldReturnIndexTimesBlockSize(long blockIndex, long expected) =>
        Assert.AreEqual(expected, Compute(9).BlockOffset(blockIndex));

    /// <summary>
    /// Verifies that a negative block index is rejected with the parameter name.
    /// </summary>
    [TestMethod]
    public void BlockOffset_WhenBlockIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        MerkleBlockComputation computation = Compute(9);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = computation.BlockOffset(-1);
        });

        Assert.AreEqual("blockIndex", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a block index at or beyond the block count is rejected, unlike the pure arithmetic in
    /// <see cref="MerkleBlocks" />, because the computation knows how many blocks its input has.
    /// </summary>
    /// <param name="inputLength">The input length in bytes.</param>
    /// <param name="blockIndex">The zero-based block index, equal to or beyond the block count.</param>
    [TestMethod]
    [DataRow(0, 0L)]
    [DataRow(4, 1L)]
    [DataRow(9, 3L)]
    [DataRow(9, 99L)]
    public void BlockOffset_WhenBlockIndexIsNotLessThanBlockCount_ShouldThrowArgumentOutOfRangeException(
        int inputLength, long blockIndex)
    {
        MerkleBlockComputation computation = Compute(inputLength);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = computation.BlockOffset(blockIndex);
        });

        Assert.AreEqual("blockIndex", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the offset and length of every block address exactly the bytes its leaf hash was taken from, by
    /// re-hashing each addressed slice and comparing it with the retained leaf hash.
    /// </summary>
    [TestMethod]
    public void BlockOffset_WhenPairedWithBlockLength_ShouldAddressTheBytesEachLeafWasHashedFrom()
    {
        var tree = new Rfc6962MerkleTree(SHA256.Create);

        for (var inputLength = 0; inputLength <= 40; inputLength++)
        {
            byte[] input = Input(inputLength);
            MerkleBlockComputation computation = tree.ComputeBlocked(input, TestBlockSize);

            for (long blockIndex = 0; blockIndex < computation.BlockCount; blockIndex++)
            {
                var offset = (int)computation.BlockOffset(blockIndex);
                var length = computation.BlockLength(blockIndex);

                CollectionAssert.AreEqual(
                    tree.HashLeaf(input.AsSpan(offset, length)),
                    computation.LeafHashes[(int)blockIndex],
                    $"input length {inputLength}, block {blockIndex}");
            }
        }
    }
}
