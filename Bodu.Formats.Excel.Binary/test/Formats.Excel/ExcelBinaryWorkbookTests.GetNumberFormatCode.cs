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
}
