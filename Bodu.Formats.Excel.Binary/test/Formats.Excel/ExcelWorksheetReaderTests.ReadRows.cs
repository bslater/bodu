// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetReaderTests.ReadRows.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public partial class ExcelWorksheetReaderTests
{
    /// <summary>
    /// Verifies that <see cref="ExcelWorksheetReader.ReadRows" /> groups cells by row in the order they appear.
    /// </summary>
    [TestMethod]
    public void ReadRows_WhenCellsSpanRows_ShouldGroupByRow()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(
            Biff8TestWorkbook.Number(0, 0, 1.0),
            Biff8TestWorkbook.Number(0, 1, 2.0),
            Biff8TestWorkbook.Number(1, 0, 3.0));

        var rows = reader.ReadRows().ToList();

        Assert.HasCount(2, rows);
        Assert.AreEqual(0, rows[0].RowIndex);
        Assert.HasCount(2, rows[0].Cells);
        Assert.AreEqual(1, rows[1].RowIndex);
        Assert.HasCount(1, rows[1].Cells);
    }

    /// <summary>
    /// Verifies that a row index that goes backwards starts a new group rather than merging into the earlier row,
    /// since grouping follows record order without buffering.
    /// </summary>
    [TestMethod]
    public void ReadRows_WhenRowIndexGoesBackwards_ShouldStartNewGroup()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(
            Biff8TestWorkbook.Number(1, 0, 1.0),
            Biff8TestWorkbook.Number(0, 0, 2.0),
            Biff8TestWorkbook.Number(1, 1, 3.0));

        List<ExcelRow> rows = reader.ReadRows().ToList();

        CollectionAssert.AreEqual(new[] { 1, 0, 1 }, rows.Select(r => r.RowIndex).ToList());
        Assert.IsTrue(rows.All(r => r.Cells.Count == 1));
    }

    /// <summary>
    /// Verifies that a MULRK run and the cells around it on the same row are grouped into one row.
    /// </summary>
    [TestMethod]
    public void ReadRows_WhenRowMixesMulRkAndSingleCells_ShouldGroupWholeRow()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(
            Biff8TestWorkbook.Label(2, 0, "a"),
            Biff8TestWorkbook.MulRk(2, 1, (0, 0x06), (0, 0x0A)),
            Biff8TestWorkbook.Number(2, 3, 3.0),
            Biff8TestWorkbook.Number(3, 0, 4.0));

        List<ExcelRow> rows = reader.ReadRows().ToList();

        Assert.HasCount(2, rows);
        Assert.HasCount(4, rows[0].Cells);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, rows[0].Cells.Select(c => c.ColumnIndex).ToList());
    }

    /// <summary>
    /// Verifies that grouping resumes from the current position after cells were read directly.
    /// </summary>
    [TestMethod]
    public void ReadRows_WhenCalledAfterTryReadCell_ShouldGroupRemainingCells()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(
            Biff8TestWorkbook.Number(0, 0, 1.0),
            Biff8TestWorkbook.Number(0, 1, 2.0),
            Biff8TestWorkbook.Number(1, 0, 3.0));
        Assert.IsTrue(reader.TryReadCell(out _));

        List<ExcelRow> rows = reader.ReadRows().ToList();

        Assert.HasCount(2, rows);
        Assert.HasCount(1, rows[0].Cells);
        Assert.AreEqual(2.0, rows[0].Cells[0].NumberValue);
    }

    /// <summary>
    /// Verifies that an empty worksheet yields no rows.
    /// </summary>
    [TestMethod]
    public void ReadRows_WhenWorksheetHasNoCells_ShouldBeEmpty()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(Biff8TestWorkbook.Dimensions(0, 0, 0, 0));

        Assert.IsEmpty(reader.ReadRows());
    }
}
