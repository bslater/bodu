// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.BiffVersion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Biff;

namespace Bodu.Formats.Excel;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Verifies that a BIFF8 workbook reports its version.
    /// </summary>
    [TestMethod]
    public void BiffVersion_WhenBiff8Workbook_ShouldBeBiff8()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook([], new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, []));
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual(BiffVersion.Biff8, workbook.BiffVersion);
    }

    /// <summary>
    /// Verifies that a BIFF5 workbook reports its version.
    /// </summary>
    [TestMethod]
    public void BiffVersion_WhenBiff5Workbook_ShouldBeBiff5()
    {
        using MemoryStream xls = Biff5TestWorkbook.BuildWorkbook(1252, null, ("Sheet1", (ref BiffWriter w) => { }));
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual(BiffVersion.Biff5, workbook.BiffVersion);
    }

    /// <summary>
    /// Verifies that the real-world sample fixture is BIFF8.
    /// </summary>
    [TestMethod]
    public void BiffVersion_WhenSampleWorkbook_ShouldBeBiff8()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        Assert.AreEqual(BiffVersion.Biff8, workbook.BiffVersion);
    }
}
