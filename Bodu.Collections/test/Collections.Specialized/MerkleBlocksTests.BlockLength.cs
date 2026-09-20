// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlocksTests.BlockLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Specialized;

/// <summary>
/// Tests for <see cref="MerkleBlocks.BlockLength(long, long, int)" />.
/// </summary>
public partial class MerkleBlocksTests
{
    /// <summary>
    /// Verifies that every block but the last is full and the last is short whenever the input length is not a whole
    /// multiple of the block size.
    /// </summary>
    /// <param name="inputLength">The input length in bytes.</param>
    /// <param name="blockIndex">The zero-based block index.</param>
    /// <param name="expected">The expected block length in bytes.</param>
    [TestMethod]
    [DataRow(4L, 0L, 4)]
    [DataRow(1L, 0L, 1)]
    [DataRow(5L, 0L, 4)]
    [DataRow(5L, 1L, 1)]
    [DataRow(8L, 1L, 4)]
    [DataRow(9L, 2L, 1)]
    [DataRow(33L, 8L, 1)]
    [DataRow(32L, 7L, 4)]
    public void BlockLength_WhenGivenABlockIndex_ShouldReturnFullLengthExceptForAShortFinalBlock(
        long inputLength, long blockIndex, int expected) =>
        Assert.AreEqual(expected, MerkleBlocks.BlockLength(inputLength, blockIndex, 4));

    /// <summary>
    /// Verifies that a block beginning at or beyond the input length has zero length rather than a negative one.
    /// </summary>
    /// <param name="inputLength">The input length in bytes.</param>
    /// <param name="blockIndex">The zero-based block index.</param>
    [TestMethod]
    [DataRow(0L, 0L)]
    [DataRow(4L, 1L)]
    [DataRow(5L, 2L)]
    [DataRow(8L, 99L)]
    public void BlockLength_WhenBlockBeginsAtOrBeyondTheInput_ShouldReturnZero(long inputLength, long blockIndex) =>
        Assert.AreEqual(0, MerkleBlocks.BlockLength(inputLength, blockIndex, 4));

    /// <summary>
    /// Verifies the final block's length at a one-mebibyte leaf size, including the 4096-byte tail the FallbackPlan
    /// authentication-path vector pins.
    /// </summary>
    /// <param name="inputLength">The input length in bytes.</param>
    /// <param name="blockIndex">The zero-based block index.</param>
    /// <param name="expected">The expected block length in bytes.</param>
    [TestMethod]
    [DataRow(4198400L, 0L, 1048576)]
    [DataRow(4198400L, 3L, 1048576)]
    [DataRow(4198400L, 4L, 4096)]
    [DataRow(1048576L, 0L, 1048576)]
    [DataRow(1048577L, 1L, 1)]
    [DataRow(2109497L, 2L, 12345)]
    public void BlockLength_WhenBlockSizeIsOneMebibyte_ShouldMatchTheRepositoryFormat(
        long inputLength, long blockIndex, int expected) =>
        Assert.AreEqual(expected, MerkleBlocks.BlockLength(inputLength, blockIndex, OneMebibyte));

    /// <summary>
    /// Verifies that the block lengths sum to the input length, so no byte is covered twice or left out.
    /// </summary>
    /// <param name="inputLength">The input length in bytes.</param>
    [TestMethod]
    [DataRow(0L)]
    [DataRow(1L)]
    [DataRow(17L)]
    [DataRow(28L)]
    [DataRow(33L)]
    [DataRow(1000L)]
    public void BlockLength_WhenSummedOverEveryBlock_ShouldEqualTheInputLength(long inputLength)
    {
        long total = 0;
        long count = MerkleBlocks.BlockCount(inputLength, 4);
        for (long index = 0; index < count; index++)
            total += MerkleBlocks.BlockLength(inputLength, index, 4);

        Assert.AreEqual(inputLength, total);
    }

    /// <summary>
    /// Verifies that a block's offset plus its length never exceeds the input length, so a caller slicing by these
    /// values cannot read past the end.
    /// </summary>
    /// <param name="inputLength">The input length in bytes.</param>
    [TestMethod]
    [DataRow(1L)]
    [DataRow(9L)]
    [DataRow(20L)]
    [DataRow(33L)]
    public void BlockLength_WhenAddedToTheBlockOffset_ShouldNeverExceedTheInputLength(long inputLength)
    {
        long count = MerkleBlocks.BlockCount(inputLength, 4);
        for (long index = 0; index < count; index++)
        {
            long end = MerkleBlocks.BlockOffset(index, 4) + MerkleBlocks.BlockLength(inputLength, index, 4);
            Assert.IsTrue(end <= inputLength, $"block {index} ends at {end}, past {inputLength}");
        }
    }

    /// <summary>
    /// Verifies that a negative input length is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void BlockLength_WhenInputLengthIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = MerkleBlocks.BlockLength(-1, 0, 4);
        });

        Assert.AreEqual("inputLength", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a negative block index is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void BlockLength_WhenBlockIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = MerkleBlocks.BlockLength(16, -1, 4);
        });

        Assert.AreEqual("blockIndex", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a non-positive block size is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="blockSize">The invalid block size.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-2)]
    public void BlockLength_WhenBlockSizeIsNotPositive_ShouldThrowArgumentOutOfRangeException(int blockSize)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = MerkleBlocks.BlockLength(16, 0, blockSize);
        });

        Assert.AreEqual("blockSize", ex.ParamName);
    }
}
