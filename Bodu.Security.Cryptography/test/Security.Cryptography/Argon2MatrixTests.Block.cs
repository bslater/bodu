// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2MatrixTests.Block.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Argon2MatrixTests
{
    /// <summary>
    /// Verifies that a block index at or past the end of the matrix, or below zero, is rejected rather than addressing
    /// native memory outside it.
    /// </summary>
    /// <param name="index">The out-of-range block index.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(16)]
    [DataRow(int.MaxValue)]
    public void Block_WhenIndexIsOutsideTheMatrix_ShouldThrowArgumentOutOfRangeException(int index)
    {
        using Argon2Matrix matrix = Argon2Matrix.Rent(16, CreatePool());

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = matrix.Block(index);
        });

        Assert.AreEqual("index", ex.ParamName);
    }

    /// <summary>
    /// Verifies that consecutive blocks are 128 words apart, so a block's words never overlap its neighbor's.
    /// </summary>
    [TestMethod]
    public void BlockSpan_WhenBlocksAreWritten_ShouldNotOverlap()
    {
        using Argon2Matrix matrix = Argon2Matrix.Rent(4, CreatePool());
        for (int block = 0; block < matrix.BlockCount; block++)
            matrix.BlockSpan(block).Fill((ulong)block + 1);

        for (int block = 0; block < matrix.BlockCount; block++)
            Assert.IsFalse(matrix.BlockSpan(block).ContainsAnyExcept((ulong)block + 1), $"Block {block} was overwritten.");
    }
}
