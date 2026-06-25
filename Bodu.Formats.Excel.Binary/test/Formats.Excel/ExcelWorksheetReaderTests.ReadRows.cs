// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetReaderTests.ReadRows.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

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

        List<ExcelRow> rows = reader.ReadRows().ToList();

        Assert.HasCount(2, rows);
        Assert.AreEqual(0, rows[0].RowIndex);
        Assert.HasCount(2, rows[0].Cells);
        Assert.AreEqual(1, rows[1].RowIndex);
        Assert.HasCount(1, rows[1].Cells);
    }
}
