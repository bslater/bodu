// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.OpenWorksheet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Verifies that a shared-string cell (the first series identifier on the Series ID row) decodes to its text value.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenDataSheet_ShouldDecodeSharedStringCell()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Data");

        ExcelCell seriesId = grid[(10, 1)];
        Assert.AreEqual(ExcelCellKind.String, seriesId.Kind);
        Assert.AreEqual("SER-A", seriesId.StringValue);
    }

    /// <summary>
    /// Verifies that the cells that use an inline label, the title and a series unit, decode to their text values.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenDataSheet_ShouldDecodeTitleCell()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Data");

        Assert.AreEqual("Sample daily values", grid[(0, 0)].StringValue);
        Assert.AreEqual("Points", grid[(5, 1)].StringValue);
    }

    /// <summary>
    /// Verifies that the first data row decodes its serial-date and rate numbers (covering the RK and NUMBER records).
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenDataSheet_ShouldDecodeFirstDataRowNumbers()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Data");

        ExcelCell date = grid[(11, 0)];
        Assert.AreEqual(ExcelCellKind.Number, date.Kind);
        Assert.AreEqual(45292.0, date.NumberValue!.Value, 0.0);
        Assert.AreEqual(new DateOnly(2024, 1, 1), ExcelSerialDate.FromSerialDate(date.NumberValue.Value));

        Assert.AreEqual(1.2345678, grid[(11, 1)].NumberValue!.Value, 1e-10);
    }

    /// <summary>
    /// Verifies that the final data row decodes its date and values (covering the MULRK expansion).
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenDataSheet_ShouldDecodeFinalDataRowNumbers()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Data");

        ExcelCell lastDate = grid[(110, 0)];
        Assert.AreEqual(new DateOnly(2024, 4, 9), ExcelSerialDate.FromSerialDate(lastDate.NumberValue!.Value));

        Assert.AreEqual(0.75, grid[(110, 1)].NumberValue!.Value, 1e-10);
        Assert.AreEqual(112.71, grid[(110, 4)].NumberValue!.Value, 1e-10);
    }

    /// <summary>
    /// Verifies that a shared-string cell and an inline-label cell decode to their text values.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenSharedAndInlineStrings_ShouldDecodeBoth()
    {
        byte[][] body =
        [
            Biff8TestWorkbook.LabelSst(0, 0, 1),
            Biff8TestWorkbook.Label(0, 1, "Inline"),
            Biff8TestWorkbook.Number(1, 0, 3.5),
        ];
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [Biff8TestWorkbook.Sst("Alpha", "Beta")],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, body));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);
        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Sheet1");

        Assert.AreEqual("Beta", grid[(0, 0)].StringValue);
        Assert.AreEqual("Inline", grid[(0, 1)].StringValue);
        Assert.AreEqual(3.5, grid[(1, 0)].NumberValue!.Value, 0.0);
    }

    /// <summary>
    /// Verifies that two worksheet readers from the same workbook can be read independently.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenTwoSheetsOpened_ShouldReadIndependently()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        using ExcelWorksheetReader data = workbook.OpenWorksheet("Data");
        using ExcelWorksheetReader notes = workbook.OpenWorksheet("Notes");

        Assert.IsTrue(data.TryReadCell(out _));
        Assert.IsTrue(notes.TryReadCell(out _));
        Assert.AreEqual("Data", data.Worksheet.Name);
        Assert.AreEqual("Notes", notes.Worksheet.Name);
    }

    /// <summary>
    /// Verifies that the serial-date column is flagged as date-formatted while the rate column is not.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenDataSheet_ShouldFlagDateFormattedColumn()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Data");

        Assert.IsTrue(grid[(11, 0)].IsDateFormatted, "The first data column holds serial dates.");
        Assert.IsFalse(grid[(11, 1)].IsDateFormatted, "The rate column holds plain numbers.");
    }

    /// <summary>
    /// Verifies that disabling date-format detection leaves every numeric cell unflagged.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenDateDetectionDisabled_ShouldNotFlagDateColumn()
    {
        using var workbook = ExcelBinaryWorkbook.Open(
            ExcelBinaryFixtures.OpenSample(),
            new ExcelBinaryReaderOptions { DetectDateFormats = false });

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Data");

        Assert.IsFalse(grid[(11, 0)].IsDateFormatted);
    }

    /// <summary>
    /// Verifies that requesting a sheet index outside the range throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenSheetIndexOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = workbook.OpenWorksheet(99);
        });
    }

    /// <summary>
    /// Verifies that requesting an unknown sheet name throws <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenSheetNameMissing_ShouldThrowKeyNotFoundException()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = workbook.OpenWorksheet("NoSuchSheet");
        });
    }

    /// <summary>
    /// Verifies that reading from a disposed workbook throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenWorkbookDisposed_ShouldThrowObjectDisposedException()
    {
        ExcelBinaryWorkbook workbook = OpenSample();
        workbook.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = workbook.OpenWorksheet(0);
        });
    }

    /// <summary>
    /// Verifies that, in a workbook whose sheets <c>DATA</c> and <c>data</c> differ only in case, each name opens the
    /// sheet it matches ordinally.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenNamesDifferOnlyInCase_ShouldOpenTheOrdinalMatch()
    {
        using MemoryStream xls = BuildCaseVariantWorkbook();
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        using ExcelWorksheetReader lower = workbook.OpenWorksheet("data");
        using ExcelWorksheetReader upper = workbook.OpenWorksheet("DATA");

        Assert.AreEqual(1, lower.Worksheet.Index);
        Assert.AreEqual(2.0, lower.ReadCells().Single().NumberValue!.Value, 0.0);
        Assert.AreEqual(0, upper.Worksheet.Index);
        Assert.AreEqual(1.0, upper.ReadCells().Single().NumberValue!.Value, 0.0);
    }

    /// <summary>
    /// Verifies that opening <c>SHEET1</c> in a workbook whose only sheet is named <c>Sheet1</c> throws
    /// <see cref="KeyNotFoundException" />, because sheet names are compared ordinally.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenNameDiffersInCase_ShouldThrowKeyNotFoundException()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Number(0, 0, 1)]));
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = workbook.OpenWorksheet("SHEET1");
        });
    }

    /// <summary>
    /// Verifies that opening <c>Data</c> in a workbook whose sheets are <c>DATA</c> and <c>data</c> throws
    /// <see cref="KeyNotFoundException" />, because no sheet name matches it ordinally.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenNameMatchesOnlyIgnoringCase_ShouldThrowKeyNotFoundException()
    {
        using MemoryStream xls = BuildCaseVariantWorkbook();
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = workbook.OpenWorksheet("Data");
        });
    }

    /// <summary>
    /// Verifies that opening a one-sheet workbook's sheet at index 1 or at index -1 throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenIndexIsOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Number(0, 0, 1)]));
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = workbook.OpenWorksheet(1);
        });
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = workbook.OpenWorksheet(-1);
        });
    }

    /// <summary>
    /// Builds a workbook whose two sheets are named <c>DATA</c> (A1 = 1) and <c>data</c> (A1 = 2).
    /// </summary>
    /// <returns>The compound file bytes, positioned at the start.</returns>
    private static MemoryStream BuildCaseVariantWorkbook() =>
        Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec("DATA", 0, 0, [Biff8TestWorkbook.Number(0, 0, 1)]),
            new Biff8TestWorkbook.SheetSpec("data", 0, 0, [Biff8TestWorkbook.Number(0, 0, 2)]));
}
