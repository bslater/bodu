// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensionsTests.ToMatrix.MoreCoverage.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class ArrayExtensionsTests
{
    /// <summary>
    /// Verifies that <c>ToMatrix</c> on rows that are themselves empty produces a matrix with zero columns.
    /// </summary>
    [TestMethod]
    public void ToMatrix_WhenInnerArraysAreEmpty_ShouldProduceZeroColumnMatrix()
    {
        int[][] source = { Array.Empty<int>(), Array.Empty<int>() };

        int[,] result = source.ToMatrix(transpose: false);

        Assert.AreEqual(2, result.GetLength(0));
        Assert.AreEqual(0, result.GetLength(1));
    }

    /// <summary>
    /// Verifies that <c>ToMatrix</c> on rows that are themselves empty, when transposed, produces a matrix
    /// with zero rows.
    /// </summary>
    [TestMethod]
    public void ToMatrix_WhenInnerArraysAreEmptyAndTransposed_ShouldProduceZeroRowMatrix()
    {
        int[][] source = { Array.Empty<int>(), Array.Empty<int>() };

        int[,] result = source.ToMatrix(transpose: true);

        Assert.AreEqual(0, result.GetLength(0));
        Assert.AreEqual(2, result.GetLength(1));
    }
}
