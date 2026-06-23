// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.ReadWorksheet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Formats.Excel.Binary;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Verifies that a materialized worksheet supports random access by position.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenByName_ShouldSupportRandomAccess()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        ExcelWorksheet sheet = workbook.ReadWorksheet("Data");

        Assert.IsTrue(sheet.TryGetCell(10, 1, out ExcelCell seriesId));
        Assert.AreEqual("FXRUSD", seriesId.StringValue);
        Assert.IsFalse(sheet.TryGetCell(100000, 100000, out _));
    }

    /// <summary>
    /// Verifies that a materialized worksheet exposes its name, index, and used range.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenByIndex_ShouldExposeSheetMetadata()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        ExcelWorksheet sheet = workbook.ReadWorksheet(0);

        Assert.AreEqual("Data", sheet.Name);
        Assert.AreEqual(0, sheet.Index);
        Assert.AreEqual(2186, sheet.Dimensions.RowCount);
    }

    /// <summary>
    /// Verifies that a materialized worksheet groups cells into rows in ascending row and column order.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenGroupingRows_ShouldOrderRowsAndCells()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        ExcelWorksheet sheet = workbook.ReadWorksheet("Data");
        List<int> rowIndexes = sheet.Rows.Select(r => r.RowIndex).ToList();

        CollectionAssert.AreEqual(rowIndexes.OrderBy(r => r).ToList(), rowIndexes);
        foreach (ExcelRow row in sheet.Rows)
        {
            List<int> columns = row.Cells.Select(c => c.ColumnIndex).ToList();
            CollectionAssert.AreEqual(columns.OrderBy(c => c).ToList(), columns);
            Assert.IsTrue(row.Cells.All(c => c.RowIndex == row.RowIndex));
        }
    }

    /// <summary>
    /// Verifies that requesting an unknown worksheet name throws <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenNameMissing_ShouldThrowKeyNotFoundException()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = workbook.ReadWorksheet("NoSuchSheet");
        });
    }

    /// <summary>
    /// Verifies that requesting a worksheet index outside the range throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenIndexOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = workbook.ReadWorksheet(99);
        });
    }
}
