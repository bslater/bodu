// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensionsTests.ToMatrix.Additional.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class ArrayExtensionsTests
{
    /// <summary>
    /// Verifies that <c>ToMatrix</c> on a single-row jagged array produces a one-row rectangular array.
    /// </summary>
    [TestMethod]
    public void ToMatrix_WithSingleRow_ShouldProduceOneRowMatrix()
    {
        int[][] source = { new[] { 1, 2, 3 } };

        int[,] result = source.ToMatrix(transpose: false);

        Assert.AreEqual(1, result.GetLength(0));
        Assert.AreEqual(3, result.GetLength(1));
        Assert.AreEqual(1, result[0, 0]);
        Assert.AreEqual(2, result[0, 1]);
        Assert.AreEqual(3, result[0, 2]);
    }

    /// <summary>
    /// Verifies that <c>ToMatrix</c> with transpose on a single-row jagged array produces a one-column matrix.
    /// </summary>
    [TestMethod]
    public void ToMatrix_WithSingleRowTransposed_ShouldProduceOneColumnMatrix()
    {
        int[][] source = { new[] { 1, 2, 3 } };

        int[,] result = source.ToMatrix(transpose: true);

        Assert.AreEqual(3, result.GetLength(0));
        Assert.AreEqual(1, result.GetLength(1));
        Assert.AreEqual(1, result[0, 0]);
        Assert.AreEqual(2, result[1, 0]);
        Assert.AreEqual(3, result[2, 0]);
    }

    /// <summary>
    /// Verifies that <c>ToMatrix</c> on a single-column jagged array produces a one-column matrix.
    /// </summary>
    [TestMethod]
    public void ToMatrix_WithSingleColumn_ShouldProduceOneColumnMatrix()
    {
        int[][] source =
        {
            new[] { 1 },
            new[] { 2 },
            new[] { 3 },
        };

        int[,] result = source.ToMatrix(transpose: false);

        Assert.AreEqual(3, result.GetLength(0));
        Assert.AreEqual(1, result.GetLength(1));
        Assert.AreEqual(1, result[0, 0]);
        Assert.AreEqual(2, result[1, 0]);
        Assert.AreEqual(3, result[2, 0]);
    }

    /// <summary>
    /// Verifies that <c>ToMatrix</c> on a reference-type jagged array preserves element identity.
    /// </summary>
    [TestMethod]
    public void ToMatrix_WithReferenceTypes_ShouldPreserveElementIdentity()
    {
        var a = new object();
        var b = new object();
        var c = new object();
        var d = new object();

        object[][] source =
        {
            new[] { a, b },
            new[] { c, d },
        };

        object[,] result = source.ToMatrix(transpose: false);

        Assert.AreSame(a, result[0, 0]);
        Assert.AreSame(b, result[0, 1]);
        Assert.AreSame(c, result[1, 0]);
        Assert.AreSame(d, result[1, 1]);
    }

    /// <summary>
    /// Verifies that <c>ToMatrix</c> with a single inner array containing a single value produces a 1x1 matrix.
    /// </summary>
    [TestMethod]
    public void ToMatrix_WithSingleElement_ShouldProduceOneByOneMatrix()
    {
        int[][] source = { new[] { 42 } };

        int[,] result = source.ToMatrix(transpose: false);

        Assert.AreEqual(1, result.GetLength(0));
        Assert.AreEqual(1, result.GetLength(1));
        Assert.AreEqual(42, result[0, 0]);
    }
}
