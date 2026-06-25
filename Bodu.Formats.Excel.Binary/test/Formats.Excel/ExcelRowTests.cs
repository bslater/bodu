// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelRowTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// Verifies the behavior of <see cref="ExcelRow" />, the materialized row of populated cells.
/// </summary>
[TestClass]
public class ExcelRowTests
{
    /// <summary>
    /// Verifies that a row exposes its index and the cells it was constructed with.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCellsSupplied_ShouldExposeIndexAndCells()
    {
        ExcelCell[] cells = [ExcelCell.Number(7, 0, 1.0), ExcelCell.Number(7, 1, 2.0)];

        ExcelRow row = new(7, cells);

        Assert.AreEqual(7, row.RowIndex);
        Assert.HasCount(2, row.Cells);
        Assert.AreEqual(0, row.Cells[0].ColumnIndex);
        Assert.AreEqual(1, row.Cells[1].ColumnIndex);
    }

    /// <summary>
    /// Verifies that a row with no cells exposes an empty cell list.
    /// </summary>
    [TestMethod]
    public void Cells_WhenNoCells_ShouldBeEmpty()
    {
        ExcelRow row = new(0, []);

        Assert.IsEmpty(row.Cells);
    }
}
