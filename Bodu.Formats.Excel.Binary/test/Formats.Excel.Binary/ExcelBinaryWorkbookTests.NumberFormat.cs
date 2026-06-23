// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.NumberFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

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
        using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.Open(
            ExcelBinaryFixtures.OpenStream(ExcelBinaryFixtures.SampleBiff8),
            new ExcelBinaryReaderOptions { DetectDateFormats = false });

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Data");

        Assert.IsFalse(grid[(11, 0)].IsDateFormatted);
    }

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
