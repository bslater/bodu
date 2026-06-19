// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8WorkbookReaderTests.Cells.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

public partial class Biff8WorkbookReaderTests
{
    /// <summary>
    /// Verifies that a shared-string cell (the USD series identifier on the Series ID row) decodes to its text value.
    /// </summary>
    [TestMethod]
    public void ReadSheetCells_WhenDataSheet_ShouldDecodeSharedStringCell()
    {
        Biff8WorkbookReader reader = OpenSample();

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(reader, "Data");

        ExcelCell seriesId = grid[(10, 1)];
        Assert.AreEqual(ExcelCellKind.String, seriesId.Kind);
        Assert.AreEqual("FXRUSD", seriesId.StringValue);
    }

    /// <summary>
    /// Verifies that the header title cell, which uses an inline label, decodes to its text value.
    /// </summary>
    [TestMethod]
    public void ReadSheetCells_WhenDataSheet_ShouldDecodeTitleCell()
    {
        Biff8WorkbookReader reader = OpenSample();

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(reader, "Data");

        Assert.AreEqual("A$1=USD", grid[(1, 1)].StringValue);
        Assert.AreEqual("USD", grid[(5, 1)].StringValue);
    }

    /// <summary>
    /// Verifies that the first data row decodes its serial-date and rate numbers (covering the RK and NUMBER records).
    /// </summary>
    [TestMethod]
    public void ReadSheetCells_WhenDataSheet_ShouldDecodeFirstDataRowNumbers()
    {
        Biff8WorkbookReader reader = OpenSample();

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(reader, "Data");

        ExcelCell date = grid[(11, 0)];
        Assert.AreEqual(ExcelCellKind.Number, date.Kind);
        Assert.AreEqual(44929.0, date.NumberValue!.Value, 0.0);
        Assert.AreEqual(new DateOnly(2023, 1, 3), ExcelSerialDate.FromSerialDate(date.NumberValue.Value));

        Assert.AreEqual(0.6828, grid[(11, 1)].NumberValue!.Value, 1e-10);
    }

    /// <summary>
    /// Verifies that the final data row decodes the most recent USD and JPY rates (covering the MULRK expansion).
    /// </summary>
    [TestMethod]
    public void ReadSheetCells_WhenDataSheet_ShouldDecodeFinalDataRowNumbers()
    {
        Biff8WorkbookReader reader = OpenSample();

        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(reader, "Data");

        ExcelCell lastDate = grid[(874, 0)];
        Assert.AreEqual(new DateOnly(2026, 6, 12), ExcelSerialDate.FromSerialDate(lastDate.NumberValue!.Value));

        Assert.AreEqual(0.7029, grid[(874, 1)].NumberValue!.Value, 1e-10);
        Assert.AreEqual(112.71, grid[(874, 4)].NumberValue!.Value, 1e-10);
    }

    /// <summary>
    /// Verifies that requesting a sheet index outside the range throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ReadSheetCells_WhenSheetIndexOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        Biff8WorkbookReader reader = OpenSample();

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = reader.ReadSheetCells(99).ToList();
        });
    }

    /// <summary>
    /// Verifies that requesting an unknown sheet name throws <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void ReadSheetCells_WhenSheetNameMissing_ShouldThrowKeyNotFoundException()
    {
        Biff8WorkbookReader reader = OpenSample();

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = reader.ReadSheetCells("NoSuchSheet").ToList();
        });
    }
}
