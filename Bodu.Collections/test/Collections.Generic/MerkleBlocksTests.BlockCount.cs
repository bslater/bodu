// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlocksTests.BlockCount.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Tests for <see cref="MerkleBlocks.BlockCount(long, int)" />.
/// </summary>
public partial class MerkleBlocksTests
{
    /// <summary>
    /// Verifies that the block count is the input length divided by the block size, rounded up.
    /// </summary>
    /// <param name="inputLength">The input length in bytes.</param>
    /// <param name="blockSize">The block size in bytes.</param>
    /// <param name="expected">The expected number of blocks.</param>
    [TestMethod]
    [DataRow(0L, 4, 0L)]
    [DataRow(1L, 4, 1L)]
    [DataRow(3L, 4, 1L)]
    [DataRow(4L, 4, 1L)]
    [DataRow(5L, 4, 2L)]
    [DataRow(8L, 4, 2L)]
    [DataRow(9L, 4, 3L)]
    [DataRow(33L, 4, 9L)]
    [DataRow(1L, 1, 1L)]
    [DataRow(7L, 1, 7L)]
    public void BlockCount_WhenGivenALengthAndBlockSize_ShouldRoundUp(long inputLength, int blockSize, long expected) =>
        Assert.AreEqual(expected, MerkleBlocks.BlockCount(inputLength, blockSize));

    /// <summary>
    /// Verifies that a zero-length input has zero blocks rather than one empty block, so its root is the empty
    /// tree's rather than the hash of one empty leaf.
    /// </summary>
    [TestMethod]
    public void BlockCount_WhenInputLengthIsZero_ShouldReturnZeroRatherThanOne() =>
        Assert.AreEqual(0L, MerkleBlocks.BlockCount(0, OneMebibyte));

    /// <summary>
    /// Verifies that the count is exact at and either side of a one-mebibyte leaf boundary.
    /// </summary>
    /// <param name="inputLength">The input length in bytes.</param>
    /// <param name="expected">The expected number of blocks.</param>
    [TestMethod]
    [DataRow(1L, 1L)]
    [DataRow(1048575L, 1L)]
    [DataRow(1048576L, 1L)]
    [DataRow(1048577L, 2L)]
    [DataRow(2097153L, 3L)]
    [DataRow(4198400L, 5L)]
    [DataRow(536870912L, 512L)]
    public void BlockCount_WhenBlockSizeIsOneMebibyte_ShouldMatchTheRepositoryFormat(long inputLength, long expected) =>
        Assert.AreEqual(expected, MerkleBlocks.BlockCount(inputLength, OneMebibyte));

    /// <summary>
    /// Verifies that a count beyond <see cref="int.MaxValue" /> blocks is returned without overflowing, because the
    /// arithmetic is performed in 64 bits.
    /// </summary>
    [TestMethod]
    public void BlockCount_WhenCountExceedsInt32Range_ShouldNotOverflow() =>
        Assert.AreEqual(3_000_000_000L, MerkleBlocks.BlockCount(3_000_000_000L, 1));

    /// <summary>
    /// Verifies that a negative input length is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void BlockCount_WhenInputLengthIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = MerkleBlocks.BlockCount(-1, 4);
        });

        Assert.AreEqual("inputLength", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a non-positive block size is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="blockSize">The invalid block size.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void BlockCount_WhenBlockSizeIsNotPositive_ShouldThrowArgumentOutOfRangeException(int blockSize)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = MerkleBlocks.BlockCount(16, blockSize);
        });

        Assert.AreEqual("blockSize", ex.ParamName);
    }
}
