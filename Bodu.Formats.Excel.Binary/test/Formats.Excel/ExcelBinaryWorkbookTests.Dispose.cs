// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Verifies that disposing a worksheet reader twice, and then its workbook twice, throws no exception.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        using MemoryStream xls = BuildOneNumberWorkbook();
        ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.Open(xls, new ExcelBinaryReaderOptions());
        ExcelWorksheetReader reader = workbook.OpenWorksheet(0);

        reader.Dispose();
        reader.Dispose();
        workbook.Dispose();
        workbook.Dispose();
    }

    /// <summary>
    /// Verifies that disposing a workbook opened with <see cref="ExcelBinaryWorkbook.Open" /> and options that do not
    /// set <see cref="ExcelBinaryReaderOptions.LeaveOpen" /> disposes the stream it was opened over.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenOpenedOverAStream_ShouldDisposeTheStream()
    {
        MemoryStream xls = BuildOneNumberWorkbook();
        ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.Open(xls, new ExcelBinaryReaderOptions());

        workbook.Dispose();

        Assert.IsFalse(xls.CanRead);
    }

    /// <summary>
    /// Builds a workbook whose one sheet holds the number 1 in A1.
    /// </summary>
    /// <returns>The compound file bytes, positioned at the start.</returns>
    private static MemoryStream BuildOneNumberWorkbook() =>
        Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Number(0, 0, 1)]));
}
