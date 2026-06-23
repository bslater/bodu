// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetReaderTests.TryReadCell.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public partial class ExcelWorksheetReaderTests
{
    /// <summary>
    /// Verifies that reading from a worksheet with no value records reports the end of the worksheet.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenWorksheetHasNoCells_ShouldReturnFalse()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader();

        Assert.IsFalse(reader.TryReadCell(out ExcelCell cell));
        Assert.AreEqual(default, cell);
    }

    /// <summary>
    /// Verifies that reading past the final cell reports the end of the worksheet.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenPastLastCell_ShouldReturnFalse()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(Biff8TestWorkbook.Number(0, 0, 1.0));

        Assert.IsTrue(reader.TryReadCell(out _));
        Assert.IsFalse(reader.TryReadCell(out _));
    }
}
