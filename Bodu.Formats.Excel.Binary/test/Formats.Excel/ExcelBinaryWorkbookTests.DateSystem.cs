// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.DateSystem.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Verifies that the sample workbook reports the 1900 date system.
    /// </summary>
    [TestMethod]
    public void DateSystem_WhenSampleWorkbook_ShouldBe1900()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        Assert.AreEqual(ExcelDateSystem.Excel1900, workbook.DateSystem);
    }

    /// <summary>
    /// Verifies that a workbook declaring the 1904 date system reports it.
    /// </summary>
    [TestMethod]
    public void DateSystem_When1904Declared_ShouldReport1904()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [Biff8TestWorkbook.DateMode(is1904: true)],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Dimensions(0, 1, 0, 1)]));

        using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual(ExcelDateSystem.Excel1904, workbook.DateSystem);
    }
}
