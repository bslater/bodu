// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelCellReferenceTests.ToA1.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public partial class ExcelCellReferenceTests
{
    /// <summary>
    /// Verifies that zero-based coordinates convert to their A1 reference.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index.</param>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <param name="expected">The expected A1 reference.</param>
    [TestMethod]
    [DataRow(0, 0, "A1")]
    [DataRow(9, 27, "AB10")]
    [DataRow(99, 0, "A100")]
    public void ToA1_WhenCoordinates_ShouldReturnExpectedReference(int rowIndex, int columnIndex, string expected)
    {
        Assert.AreEqual(expected, ExcelCellReference.ToA1(rowIndex, columnIndex));
    }

    /// <summary>
    /// Verifies that the column name and A1 conversions round-trip through parsing.
    /// </summary>
    [TestMethod]
    public void ToA1_WhenRoundTrippedThroughParse_ShouldRecoverCoordinates()
    {
        string reference = ExcelCellReference.ToA1(123, 456);

        Assert.IsTrue(ExcelCellReference.TryParseA1(reference, out int rowIndex, out int columnIndex));
        Assert.AreEqual(123, rowIndex);
        Assert.AreEqual(456, columnIndex);
    }
}
