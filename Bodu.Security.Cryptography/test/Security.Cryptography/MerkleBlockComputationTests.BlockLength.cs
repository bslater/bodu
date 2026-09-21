// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlockComputationTests.BlockLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="MerkleBlockComputation.BlockLength(long)" />.
/// </summary>
public partial class MerkleBlockComputationTests
{
    /// <summary>
    /// Verifies that every block but the last is full and the last is short whenever the input length is not a whole
    /// multiple of the block size.
    /// </summary>
    /// <param name="inputLength">The input length in bytes.</param>
    /// <param name="blockIndex">The zero-based block index.</param>
    /// <param name="expected">The expected block length in bytes at a block size of four.</param>
    [TestMethod]
    [DataRow(4, 0L, 4)]
    [DataRow(1, 0L, 1)]
    [DataRow(5, 0L, 4)]
    [DataRow(5, 1L, 1)]
    [DataRow(8, 1L, 4)]
    [DataRow(9, 2L, 1)]
    public void BlockLength_WhenGivenAnExistingBlock_ShouldReturnFullLengthExceptForAShortFinalBlock(
        int inputLength, long blockIndex, int expected) =>
        Assert.AreEqual(expected, Compute(inputLength).BlockLength(blockIndex));

    /// <summary>
    /// Verifies that the block lengths of an input sum to its length, so no byte is dropped or counted twice.
    /// </summary>
    [TestMethod]
    public void BlockLength_WhenSummedOverEveryBlock_ShouldEqualInputLength()
    {
        for (var inputLength = 0; inputLength <= 40; inputLength++)
        {
            MerkleBlockComputation computation = Compute(inputLength);
            long total = 0;

            for (long blockIndex = 0; blockIndex < computation.BlockCount; blockIndex++)
                total += computation.BlockLength(blockIndex);

            Assert.AreEqual(computation.InputLength, total, $"input length {inputLength}");
        }
    }

    /// <summary>
    /// Verifies that a negative block index is rejected with the parameter name.
    /// </summary>
    [TestMethod]
    public void BlockLength_WhenBlockIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        MerkleBlockComputation computation = Compute(9);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = computation.BlockLength(-1);
        });

        Assert.AreEqual("blockIndex", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a block index at or beyond the block count is rejected rather than reported as a zero-length
    /// block, because the computation knows how many blocks its input has.
    /// </summary>
    /// <param name="inputLength">The input length in bytes.</param>
    /// <param name="blockIndex">The zero-based block index, equal to or beyond the block count.</param>
    [TestMethod]
    [DataRow(0, 0L)]
    [DataRow(4, 1L)]
    [DataRow(9, 3L)]
    [DataRow(9, 99L)]
    public void BlockLength_WhenBlockIndexIsNotLessThanBlockCount_ShouldThrowArgumentOutOfRangeException(
        int inputLength, long blockIndex)
    {
        MerkleBlockComputation computation = Compute(inputLength);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = computation.BlockLength(blockIndex);
        });

        Assert.AreEqual("blockIndex", ex.ParamName);
    }
}
