// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.GetNumberFormatCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Verifies that a date-formatted cell resolves to a non-empty number-format code.
    /// </summary>
    [TestMethod]
    public void GetNumberFormatCode_WhenDateFormattedCell_ShouldReturnNonEmptyCode()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Data");
        string? code = workbook.GetNumberFormatCode(grid[(11, 0)].FormatIndex);

        Assert.IsFalse(string.IsNullOrEmpty(code));
    }

    /// <summary>
    /// Verifies that a FORMAT record for an index in a built-in date range decides the index's code and its date
    /// classification: a number code there is reported, and its cells are not date formatted.
    /// </summary>
    [TestMethod]
    public void GetNumberFormatCode_WhenFormatRecordDefinesBuiltInDateIndex_ShouldReturnItsCode()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [Biff8TestWorkbook.Format(50, "0.00"), Biff8TestWorkbook.Xf(50)],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Number(0, 0, 1.5)]));
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        ExcelCell cell = workbook.ReadWorksheet(0).Cells.Single();

        Assert.AreEqual("0.00", workbook.GetNumberFormatCode(50));
        Assert.IsFalse(cell.IsDateFormatted);
    }

    /// <summary>
    /// Verifies that a FORMAT record for a built-in number format index decides the index's code and its date
    /// classification: a date code there is reported, and its cells are date formatted.
    /// </summary>
    [TestMethod]
    public void GetNumberFormatCode_WhenFormatRecordDefinesBuiltInNumberIndex_ShouldReturnItsCode()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [Biff8TestWorkbook.Format(1, "yyyy"), Biff8TestWorkbook.Xf(1)],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Number(0, 0, 1.5)]));
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        ExcelCell cell = workbook.ReadWorksheet(0).Cells.Single();

        Assert.AreEqual("yyyy", workbook.GetNumberFormatCode(1));
        Assert.IsTrue(cell.IsDateFormatted);
    }

    /// <summary>
    /// Verifies that when two FORMAT records define one index, the later record decides the index's code and its date
    /// classification.
    /// </summary>
    [TestMethod]
    public void GetNumberFormatCode_WhenFormatIndexIsRepeated_ShouldReturnTheLastRecordsCode()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [Biff8TestWorkbook.Format(164, "0.00"), Biff8TestWorkbook.Format(164, "yyyy-mm-dd"), Biff8TestWorkbook.Xf(164)],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Number(0, 0, 1.5)]));
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        ExcelCell cell = workbook.ReadWorksheet(0).Cells.Single();

        Assert.AreEqual("yyyy-mm-dd", workbook.GetNumberFormatCode(164));
        Assert.IsTrue(cell.IsDateFormatted);
    }
}
