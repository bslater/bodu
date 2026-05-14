// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensionsTests.ToMatrix.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class ArrayExtensionsTests
{
    /// <summary>
    /// Verifies that <c>ToMatrix(false)</c> copies the jagged array into a rectangular array with shape <c>[rows, cols]</c>.
    /// </summary>
    [TestMethod]
    public void ToMatrix_WithoutTranspose_ShouldProduceRowsByColsLayout()
    {
        int[][] source =
        {
            new[] { 1, 2, 3 },
            new[] { 4, 5, 6 },
        };

        int[,] result = source.ToMatrix(transpose: false);

        Assert.AreEqual(2, result.GetLength(0));
        Assert.AreEqual(3, result.GetLength(1));
        Assert.AreEqual(1, result[0, 0]);
        Assert.AreEqual(2, result[0, 1]);
        Assert.AreEqual(3, result[0, 2]);
        Assert.AreEqual(4, result[1, 0]);
        Assert.AreEqual(5, result[1, 1]);
        Assert.AreEqual(6, result[1, 2]);
    }

    /// <summary>
    /// Verifies that <c>ToMatrix(true)</c> transposes the layout to shape <c>[cols, rows]</c>.
    /// </summary>
    [TestMethod]
    public void ToMatrix_WithTranspose_ShouldProduceColsByRowsLayout()
    {
        int[][] source =
        {
            new[] { 1, 2, 3 },
            new[] { 4, 5, 6 },
        };

        int[,] result = source.ToMatrix(transpose: true);

        Assert.AreEqual(3, result.GetLength(0));
        Assert.AreEqual(2, result.GetLength(1));
        Assert.AreEqual(1, result[0, 0]);
        Assert.AreEqual(4, result[0, 1]);
        Assert.AreEqual(2, result[1, 0]);
        Assert.AreEqual(5, result[1, 1]);
        Assert.AreEqual(3, result[2, 0]);
        Assert.AreEqual(6, result[2, 1]);
    }

    /// <summary>
    /// Verifies that <c>ToMatrix</c> throws <see cref="ArgumentNullException"/> when the source is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ToMatrix_WhenSourceIsNull_ShouldThrowArgumentNullException()
    {
        int[][]? source = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source!.ToMatrix(false);
        });
    }

    /// <summary>
    /// Verifies that <c>ToMatrix</c> throws <see cref="ArgumentNullException"/> when any inner array is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ToMatrix_WhenInnerArrayIsNull_ShouldThrowArgumentNullException()
    {
        int[][] source = { new[] { 1, 2 }, null! };

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.ToMatrix(false);
        });
    }

    /// <summary>
    /// Verifies that <c>ToMatrix</c> throws <see cref="ArgumentException"/> when inner arrays have different lengths.
    /// </summary>
    [TestMethod]
    public void ToMatrix_WhenInnerArraysHaveDifferentLengths_ShouldThrowArgumentException()
    {
        int[][] source =
        {
            new[] { 1, 2, 3 },
            new[] { 4, 5 },
        };

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = source.ToMatrix(false);
        });
    }

    /// <summary>
    /// Verifies that <c>ToMatrix</c> throws <see cref="ArgumentException"/> when the source has no rows.
    /// </summary>
    [TestMethod]
    public void ToMatrix_WhenSourceHasNoRows_ShouldThrowArgumentException()
    {
        int[][] source = Array.Empty<int[]>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = source.ToMatrix(false);
        });
    }
}
