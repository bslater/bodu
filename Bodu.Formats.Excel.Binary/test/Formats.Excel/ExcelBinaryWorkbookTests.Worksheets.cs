// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.Worksheets.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Biff;

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

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

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

    /// <summary>
    /// Verifies that a sheet without a DIMENSIONS record reports the default, empty used range while its cells
    /// remain readable.
    /// </summary>
    [TestMethod]
    public void Worksheets_WhenDimensionsRecordMissing_ShouldReportDefaultDimensions()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Number(3, 4, 1.0)]));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual(default, workbook.Worksheets[0].Dimensions);
        Assert.AreEqual(1.0, ReadCellGrid(workbook, "Sheet1")[(3, 4)].NumberValue);
    }

    /// <summary>
    /// Verifies that a DIMENSIONS record too short for its layout yields the default used range rather than an
    /// error, because the used range is descriptive metadata.
    /// </summary>
    [TestMethod]
    public void Worksheets_WhenDimensionsRecordMalformed_ShouldReportDefaultDimensions()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Record(0x0200, new byte[6]), Biff8TestWorkbook.Number(0, 0, 1.0)]));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual(default, workbook.Worksheets[0].Dimensions);
        Assert.AreEqual(1.0, ReadCellGrid(workbook, "Sheet1")[(0, 0)].NumberValue);
    }

    /// <summary>
    /// Verifies that a DIMENSIONS record placed after the first cell record is not found, since the scan stops at
    /// the sheet body.
    /// </summary>
    [TestMethod]
    public void Worksheets_WhenDimensionsRecordFollowsCells_ShouldReportDefaultDimensions()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Number(0, 0, 1.0), Biff8TestWorkbook.Dimensions(0, 10, 0, 10)]));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual(default, workbook.Worksheets[0].Dimensions);
    }

    /// <summary>
    /// Verifies that a DIMENSIONS record preceded by non-cell records, including ones the codec does not name, is
    /// still found.
    /// </summary>
    [TestMethod]
    public void Worksheets_WhenDimensionsFollowsHeaderRecords_ShouldReportDeclaredRange()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0,
            [
                Biff8TestWorkbook.Record(0x020B, new byte[20]),
                Biff8TestWorkbook.Record(0x0FFE, new byte[3]),
                Biff8TestWorkbook.Record(0x0055, [8, 0]),
                Biff8TestWorkbook.Dimensions(2, 7, 1, 4),
            ]));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        ExcelWorksheetDimensions dimensions = workbook.Worksheets[0].Dimensions;
        Assert.AreEqual(2, dimensions.FirstRowIndex);
        Assert.AreEqual(5, dimensions.RowCount);
        Assert.AreEqual(1, dimensions.FirstColumnIndex);
        Assert.AreEqual(3, dimensions.ColumnCount);
    }

    /// <summary>
    /// Verifies that a worksheet with no DIMENSIONS record and no cells of its own reports the default dimensions, not
    /// those of a chart embedded in it, whose substream is skipped through its matching EOF.
    /// </summary>
    [TestMethod]
    public void Worksheets_WhenOnlyAnEmbeddedChartDeclaresDimensions_ShouldReportDefaultDimensions()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec(
                "Sheet1",
                0,
                0,
                [
                    Biff8TestWorkbook.Bof((ushort)BiffSubstreamType.Chart),
                    Biff8TestWorkbook.Dimensions(0, 10, 0, 4),
                    Biff8TestWorkbook.Eof(),
                ]));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual(default, workbook.Worksheets[0].Dimensions);
    }

    /// <summary>
    /// Verifies that a worksheet with no DIMENSIONS record, no cells and no EOF record reports the default dimensions,
    /// its scan ending where the next sheet's substream begins, an embedded chart's notwithstanding, and that the next
    /// sheet reports its own.
    /// </summary>
    /// <param name="embedsChart">Whether the worksheet without an EOF record holds an embedded chart.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Worksheets_WhenWorksheetLacksEofAndDimensions_ShouldReportDefaultDimensions(bool embedsChart)
    {
        List<byte[]> records = [Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofWorksheet)];
        if (embedsChart)
        {
            records.Add(Biff8TestWorkbook.Bof((ushort)BiffSubstreamType.Chart));
            records.Add(Biff8TestWorkbook.Dimensions(0, 10, 0, 4));
            records.Add(Biff8TestWorkbook.Eof());
        }

        byte[] stream = Biff8TestWorkbook.BuildRawWorkbookStream(
            [
                Biff8TestWorkbook.Concat([.. records]),
                Biff8TestWorkbook.WorksheetSubstream(Biff8TestWorkbook.Dimensions(0, 5, 0, 1), Biff8TestWorkbook.Number(0, 0, 2.0)),
            ],
            ("Sheet1", 0),
            ("Sheet2", 1));
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile(stream, "Workbook");

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual(default, workbook.Worksheets[0].Dimensions);
        Assert.AreEqual(5, workbook.Worksheets[1].Dimensions.RowCount);
    }

    /// <summary>
    /// Verifies that a chart sheet's dimensions are not scanned and report the default.
    /// </summary>
    [TestMethod]
    public void Worksheets_WhenSheetIsChart_ShouldReportDefaultDimensions()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec("Chart", 2, 0, [Biff8TestWorkbook.Dimensions(0, 10, 0, 10)]));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual(ExcelSheetType.Chart, workbook.Worksheets[0].Type);
        Assert.AreEqual(default, workbook.Worksheets[0].Dimensions);
    }

    /// <summary>
    /// Verifies that a sheet name outside the 8-bit character range is written as UTF-16 by the fixture and read
    /// back intact.
    /// </summary>
    [TestMethod]
    public void Worksheets_WhenSheetNameIsWide_ShouldDecodeUtf16Name()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec("売上データ", 0, 0, [Biff8TestWorkbook.Dimensions(0, 1, 0, 1)]),
            new Biff8TestWorkbook.SheetSpec("Résumé", 0, 0, [Biff8TestWorkbook.Dimensions(0, 1, 0, 1)]));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual("売上データ", workbook.Worksheets[0].Name);
        Assert.AreEqual("Résumé", workbook.Worksheets[1].Name);
        Assert.AreEqual("Résumé", workbook.OpenWorksheet("Résumé").Worksheet.Name);
    }

    /// <summary>
    /// Verifies that an unrecognized sheet-type byte maps to the unknown sheet type and a visibility byte above the
    /// defined states maps to very hidden.
    /// </summary>
    [TestMethod]
    public void Worksheets_WhenSheetTypeUnknown_ShouldMapToUnknown()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec("Odd", 0x40, 0x02, [Biff8TestWorkbook.Dimensions(0, 1, 0, 1)]));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual(ExcelSheetType.Unknown, workbook.Worksheets[0].Type);
        Assert.AreEqual(ExcelSheetVisibility.VeryHidden, workbook.Worksheets[0].Visibility);
        Assert.AreEqual(default, workbook.Worksheets[0].Dimensions);
    }

    /// <summary>
    /// Verifies that a workbook with no bound sheets opens with an empty sheet list.
    /// </summary>
    [TestMethod]
    public void Worksheets_WhenWorkbookHasNoSheets_ShouldBeEmpty()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook([]);

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.IsEmpty(workbook.Worksheets);
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = workbook.OpenWorksheet(0);
        });
    }

    /// <summary>
    /// Verifies that a sheet whose substream offset lies beyond the end of the stream fails when the sheet is
    /// opened, not when the workbook is.
    /// </summary>
    [TestMethod]
    public void Worksheets_WhenSheetOffsetBeyondStream_ShouldFailOnOpen()
    {
        byte[] globals = Biff8TestWorkbook.Concat(
            Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofGlobals),
            Biff8TestWorkbook.BoundSheet(100000, 0, 0, "Far"),
            Biff8TestWorkbook.Eof());
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile(globals, "Workbook");

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.HasCount(1, workbook.Worksheets);
        Assert.AreEqual(default, workbook.Worksheets[0].Dimensions);
        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = workbook.OpenWorksheet(0);
        });
    }
}
