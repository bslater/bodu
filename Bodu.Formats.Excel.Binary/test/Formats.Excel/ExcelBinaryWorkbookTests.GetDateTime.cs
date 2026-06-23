// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.GetDateTime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Verifies that converting a date-formatted cell with the workbook's date system yields the expected date.
    /// </summary>
    [TestMethod]
    public void GetDateTime_WhenDateFormattedCell_ShouldReturnExpectedDate()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Data");
        DateTime? converted = workbook.GetDateTime(grid[(11, 0)]);

        Assert.IsNotNull(converted);
        Assert.AreEqual(new DateTime(2023, 1, 3), converted.Value.Date);
    }

    /// <summary>
    /// Verifies that converting a non-numeric cell returns <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetDateTime_WhenCellIsText_ShouldReturnNull()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Data");

        Assert.IsNull(workbook.GetDateTime(grid[(10, 1)]));
    }
}
