// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.Worksheets.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Verifies that the declared visibility and type of every sheet are surfaced, distinguishing hidden from very
    /// hidden and worksheets from chart and macro sheets.
    /// </summary>
    [TestMethod]
    public void Worksheets_WhenSheetsVaryInVisibilityAndType_ShouldReportEach()
    {
        byte[][] body = [Biff8TestWorkbook.Dimensions(0, 1, 0, 1)];
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec("Visible", 0x00, 0x00, body),
            new Biff8TestWorkbook.SheetSpec("Hidden", 0x00, 0x01, body),
            new Biff8TestWorkbook.SheetSpec("VeryHidden", 0x00, 0x02, body),
            new Biff8TestWorkbook.SheetSpec("Chart", 0x02, 0x00, body),
            new Biff8TestWorkbook.SheetSpec("Macro", 0x01, 0x00, body));

        using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual(ExcelSheetVisibility.Visible, workbook.Worksheets[0].Visibility);
        Assert.AreEqual(ExcelSheetVisibility.Hidden, workbook.Worksheets[1].Visibility);
        Assert.AreEqual(ExcelSheetVisibility.VeryHidden, workbook.Worksheets[2].Visibility);
        Assert.AreEqual(ExcelSheetType.Worksheet, workbook.Worksheets[0].Type);
        Assert.AreEqual(ExcelSheetType.Chart, workbook.Worksheets[3].Type);
        Assert.AreEqual(ExcelSheetType.MacroSheet, workbook.Worksheets[4].Type);
    }

    /// <summary>
    /// Verifies that the sample workbook reports both of its sheets, indexed in workbook order.
    /// </summary>
    [TestMethod]
    public void Worksheets_WhenSampleWorkbook_ShouldIndexSheetsInOrder()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        Assert.HasCount(2, workbook.Worksheets);
        Assert.AreEqual(0, workbook.Worksheets[0].Index);
        Assert.AreEqual(1, workbook.Worksheets[1].Index);
    }
}
