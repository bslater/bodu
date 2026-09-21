// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.BlockOffset.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="MerkleTree.BlockOffset(long, int)" />.
/// </summary>
public partial class MerkleTreeTests
{
    /// <summary>
    /// Verifies that a block's offset is its index multiplied by the block size.
    /// </summary>
    /// <param name="blockIndex">The zero-based block index.</param>
    /// <param name="blockSize">The block size in bytes.</param>
    /// <param name="expected">The expected offset in bytes.</param>
    [TestMethod]
    [DataRow(0L, 4, 0L)]
    [DataRow(1L, 4, 4L)]
    [DataRow(8L, 4, 32L)]
    [DataRow(0L, 1048576, 0L)]
    [DataRow(1L, 1048576, 1048576L)]
    [DataRow(4L, 1048576, 4194304L)]
    public void BlockOffset_WhenGivenAnIndex_ShouldReturnIndexTimesBlockSize(
        long blockIndex, int blockSize, long expected) =>
        Assert.AreEqual(expected, MerkleTree.BlockOffset(blockIndex, blockSize));

    /// <summary>
    /// Verifies that an offset beyond <see cref="int.MaxValue" /> is computed in 64-bit arithmetic rather than
    /// wrapping, which a one-mebibyte leaf size reaches at 2048 blocks.
    /// </summary>
    [TestMethod]
    public void BlockOffset_WhenOffsetExceedsInt32Range_ShouldNotOverflow()
    {
        Assert.AreEqual(2_147_483_648L, MerkleTree.BlockOffset(2048, OneMebibyteBlock));
        Assert.AreEqual(1_099_511_627_776L, MerkleTree.BlockOffset(1_048_576, OneMebibyteBlock));
    }

    /// <summary>
    /// Verifies that a negative block index is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void BlockOffset_WhenBlockIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = MerkleTree.BlockOffset(-1, 4);
        });

        Assert.AreEqual("blockIndex", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a non-positive block size is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="blockSize">The invalid block size.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-4)]
    public void BlockOffset_WhenBlockSizeIsNotPositive_ShouldThrowArgumentOutOfRangeException(int blockSize)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = MerkleTree.BlockOffset(1, blockSize);
        });

        Assert.AreEqual("blockSize", ex.ParamName);
    }
}
