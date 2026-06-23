// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelCellReferenceTests.ColumnName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public partial class ExcelCellReferenceTests
{
    /// <summary>
    /// Verifies that a zero-based column index converts to its A1 column name.
    /// </summary>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <param name="expected">The expected column name.</param>
    [TestMethod]
    [DataRow(0, "A")]
    [DataRow(25, "Z")]
    [DataRow(26, "AA")]
    [DataRow(27, "AB")]
    [DataRow(51, "AZ")]
    [DataRow(701, "ZZ")]
    [DataRow(702, "AAA")]
    public void ColumnName_WhenIndex_ShouldReturnExpectedName(int columnIndex, string expected)
    {
        Assert.AreEqual(expected, ExcelCellReference.ColumnName(columnIndex));
    }

    /// <summary>
    /// Verifies that a negative column index throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ColumnName_WhenIndexNegative_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = ExcelCellReference.ColumnName(-1);
        });
    }
}
