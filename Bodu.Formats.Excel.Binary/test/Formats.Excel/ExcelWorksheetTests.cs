// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// Verifies the behavior of <see cref="ExcelWorksheet" />, the materialized, randomly addressable worksheet view.
/// </summary>
[TestClass]
public class ExcelWorksheetTests
{
    /// <summary>
    /// Verifies that a materialized worksheet exposes its descriptor's name, index, and used range.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBuilt_ShouldExposeSheetMetadata()
    {
        ExcelWorksheet sheet = CreateWorksheet();

        Assert.AreEqual("Sheet1", sheet.Name);
        Assert.AreEqual(2, sheet.Index);
        Assert.AreEqual(0, sheet.Dimensions.FirstRowIndex);
        Assert.AreEqual(3, sheet.Dimensions.RowCount);
    }

    /// <summary>
    /// Verifies that the cells are exposed in row-major order regardless of construction order.
    /// </summary>
    [TestMethod]
    public void Cells_WhenBuiltFromUnorderedCells_ShouldBeRowMajor()
    {
        ExcelWorksheet sheet = CreateWorksheet();

        List<(int Row, int Column)> positions = sheet.Cells.Select(c => (c.RowIndex, c.ColumnIndex)).ToList();

        CollectionAssert.AreEqual(new[] { (0, 0), (0, 1), (2, 0) }, positions);
    }

    /// <summary>
    /// Verifies that <see cref="ExcelWorksheet.TryGetCell(int, int, out ExcelCell)" /> returns a populated cell.
    /// </summary>
    [TestMethod]
    public void TryGetCell_WhenCellPopulated_ShouldReturnTrueAndCell()
    {
        ExcelWorksheet sheet = CreateWorksheet();

        Assert.IsTrue(sheet.TryGetCell(0, 1, out ExcelCell cell));
        Assert.AreEqual(2.0, cell.NumberValue!.Value, 0.0);
    }

    /// <summary>
    /// Verifies that <see cref="ExcelWorksheet.TryGetCell(int, int, out ExcelCell)" /> reports an absent cell.
    /// </summary>
    [TestMethod]
    public void TryGetCell_WhenCellAbsent_ShouldReturnFalse()
    {
        ExcelWorksheet sheet = CreateWorksheet();

        Assert.IsFalse(sheet.TryGetCell(5, 5, out _));
    }

    /// <summary>
    /// Verifies that the cells are grouped into rows in ascending row order, omitting rows with no populated cells.
    /// </summary>
    [TestMethod]
    public void Rows_WhenBuilt_ShouldGroupPopulatedRowsInOrder()
    {
        ExcelWorksheet sheet = CreateWorksheet();

        var rowIndexes = sheet.Rows.Select(r => r.RowIndex).ToList();

        CollectionAssert.AreEqual(new[] { 0, 2 }, rowIndexes);
        Assert.HasCount(2, sheet.Rows[0].Cells);
        Assert.HasCount(1, sheet.Rows[1].Cells);
    }

    /// <summary>
    /// Builds a worksheet of three cells across two rows, supplied out of order to exercise sorting.
    /// </summary>
    /// <returns>A materialized worksheet for use by the tests.</returns>
    private static ExcelWorksheet CreateWorksheet()
    {
        ExcelWorksheetInfo info = new("Sheet1", 2, ExcelSheetVisibility.Visible, ExcelSheetType.Worksheet, new ExcelWorksheetDimensions(0, 3, 0, 2));
        ExcelCell[] cells =
        [
            ExcelCell.Number(2, 0, 3.0),
            ExcelCell.Number(0, 1, 2.0),
            ExcelCell.Number(0, 0, 1.0),
        ];

        return new ExcelWorksheet(info, cells);
    }
}
