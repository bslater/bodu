// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.ReadWorksheet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Biff;

namespace Bodu.Formats.Excel;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Verifies that a materialized worksheet supports random access by position.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenByName_ShouldSupportRandomAccess()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        ExcelWorksheet sheet = workbook.ReadWorksheet("Data");

        Assert.IsTrue(sheet.TryGetCell(10, 1, out ExcelCell seriesId));
        Assert.AreEqual("SER-A", seriesId.StringValue);
        Assert.IsFalse(sheet.TryGetCell(100000, 100000, out _));
    }

    /// <summary>
    /// Verifies that a materialized worksheet exposes its name, index, and used range.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenByIndex_ShouldExposeSheetMetadata()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        ExcelWorksheet sheet = workbook.ReadWorksheet(0);

        Assert.AreEqual("Data", sheet.Name);
        Assert.AreEqual(0, sheet.Index);
        Assert.AreEqual(111, sheet.Dimensions.RowCount);
    }

    /// <summary>
    /// Verifies that a materialized worksheet groups cells into rows in ascending row and column order.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenGroupingRows_ShouldOrderRowsAndCells()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        ExcelWorksheet sheet = workbook.ReadWorksheet("Data");
        var rowIndexes = sheet.Rows.Select(r => r.RowIndex).ToList();

        CollectionAssert.AreEqual(rowIndexes.OrderBy(r => r).ToList(), rowIndexes);
        foreach (ExcelRow row in sheet.Rows)
        {
            var columns = row.Cells.Select(c => c.ColumnIndex).ToList();
            CollectionAssert.AreEqual(columns.OrderBy(c => c).ToList(), columns);
            Assert.IsTrue(row.Cells.All(c => c.RowIndex == row.RowIndex));
        }
    }

    /// <summary>
    /// Verifies that requesting an unknown worksheet name throws <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenNameMissing_ShouldThrowKeyNotFoundException()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = workbook.ReadWorksheet("NoSuchSheet");
        });
    }

    /// <summary>
    /// Verifies that requesting a worksheet index outside the range throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenIndexOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = workbook.ReadWorksheet(99);
        });
    }

    /// <summary>
    /// Verifies that reading a sheet whose BOF declares BIFF4 (version <c>0x0400</c>) in a BIFF8 workbook throws
    /// <see cref="ExcelBinaryUnsupportedException" />, with the codec's exception as its inner exception, as the open does
    /// for the workbook globals.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenSheetBofDeclaresUnsupportedVersion_ShouldThrowExcelBinaryUnsupportedException()
    {
        byte[] sheet = Biff8TestWorkbook.Concat(
            Biff8TestWorkbook.Record(0x0809, [0x00, 0x04, 0x10, 0x00, 0xBB, 0x0D, 0xCC, 0x07]),
            Biff8TestWorkbook.Number(0, 0, 9),
            Biff8TestWorkbook.Eof());
        byte[] globals = Biff8TestWorkbook.Concat(Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofGlobals), Biff8TestWorkbook.BoundSheet(0, 0, 0, "Odd"), Biff8TestWorkbook.Eof());
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(globals.AsSpan(20 + 4), (uint)globals.Length);
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile([.. globals, .. sheet], "Workbook");

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);
        var ex = Assert.ThrowsExactly<ExcelBinaryUnsupportedException>(() =>
        {
            _ = workbook.ReadWorksheet(0);
        });

        Assert.IsInstanceOfType<BiffUnsupportedVersionException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that reading a chart sheet yields no cells, though its substream holds the chart's data cache in NUMBER
    /// and LABEL records ([MS-XLS] 2.2.3.2), and that the worksheet after it still reads its own cell.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenSheetIsAChart_ShouldYieldNoCells()
    {
        byte[] chart = Biff8TestWorkbook.Concat(
            Biff8TestWorkbook.Bof(0x0020),
            Biff8TestWorkbook.Record(0x1001, [0x00, 0x00]),
            Biff8TestWorkbook.Record(0x1009, new byte[20]),
            Biff8TestWorkbook.Dimensions(0, 2, 0, 1),
            Biff8TestWorkbook.Record(0x1065, [0x01, 0x00]),
            Biff8TestWorkbook.Number(0, 0, 40.0),
            Biff8TestWorkbook.Number(1, 0, 50.0),
            Biff8TestWorkbook.Record(0x1065, [0x02, 0x00]),
            Biff8TestWorkbook.Label(0, 0, "Q1"),
            Biff8TestWorkbook.Label(1, 0, "Q2"),
            Biff8TestWorkbook.Record(0x1065, [0x03, 0x00]),
            Biff8TestWorkbook.Eof());
        byte[] sheet = Biff8TestWorkbook.WorksheetSubstream(Biff8TestWorkbook.Number(0, 0, 1.0));
        int globalsLength = Biff8TestWorkbook.Concat(
            Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofGlobals),
            Biff8TestWorkbook.BoundSheet(0, 2, 0, "Chart1"),
            Biff8TestWorkbook.BoundSheet(0, 0, 0, "Sheet1"),
            Biff8TestWorkbook.Eof()).Length;
        byte[] globals = Biff8TestWorkbook.Concat(
            Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofGlobals),
            Biff8TestWorkbook.BoundSheet(globalsLength, 2, 0, "Chart1"),
            Biff8TestWorkbook.BoundSheet(globalsLength + chart.Length, 0, 0, "Sheet1"),
            Biff8TestWorkbook.Eof());
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile([.. globals, .. chart, .. sheet], "Workbook");
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        ExcelWorksheet chartSheet = workbook.ReadWorksheet(0);
        ExcelWorksheet dataSheet = workbook.ReadWorksheet(1);

        Assert.AreEqual(ExcelSheetType.Chart, workbook.Worksheets[0].Type);
        Assert.IsEmpty(chartSheet.Cells);
        Assert.AreEqual(1.0, dataSheet.Cells.Single().NumberValue);
    }

    /// <summary>
    /// Verifies that a worksheet holding embedded charts, each a chart substream nested after its OBJ record ([MS-XLS]
    /// 2.1.7.20.6), reads its own cells, those after the charts included, and none of the charts' data cache, and that
    /// the sheet after it reads its own cell.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenWorksheetEmbedsCharts_ShouldReadOnlyItsOwnCells()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec(
                "Sheet1",
                0,
                0,
                [
                    Biff8TestWorkbook.Dimensions(0, 3, 0, 1),
                    Biff8TestWorkbook.Number(0, 0, 1.0),
                    Biff8TestWorkbook.Number(1, 0, 2.0),
                    Biff8TestWorkbook.EmbeddedChart(1, 10.0, 20.0, 30.0),
                    Biff8TestWorkbook.EmbeddedChart(2, 40.0, 50.0),
                    Biff8TestWorkbook.Number(2, 0, 3.0),
                    Biff8TestWorkbook.Record((ushort)BiffRecordType.Window2, new byte[18]),
                ]),
            new Biff8TestWorkbook.SheetSpec("Sheet2", 0, 0, [Biff8TestWorkbook.Number(0, 0, 4.0)]));
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        ExcelWorksheet first = workbook.ReadWorksheet(0);
        ExcelWorksheet second = workbook.ReadWorksheet(1);

        Assert.AreEqual("0,0,Number,1;1,0,Number,2;2,0,Number,3", string.Join(";", first.Cells.Select(DescribeCell)));
        Assert.AreEqual("0,0,Number,4", string.Join(";", second.Cells.Select(DescribeCell)));
    }

    /// <summary>
    /// Verifies that a worksheet whose substream has no EOF record ends where the next sheet's substream begins, at that
    /// sheet's BOUNDSHEET offset, so it reads its own cells, those after an embedded chart's BOF included, and no
    /// others, and that the next sheet reads its own cell.
    /// </summary>
    /// <param name="embedsChart">Whether the worksheet without an EOF record holds an embedded chart.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ReadWorksheet_WhenWorksheetLacksEof_ShouldEndWhereTheNextSheetBegins(bool embedsChart)
    {
        List<byte[]> records = [Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofWorksheet), Biff8TestWorkbook.Number(0, 0, 1.0)];
        if (embedsChart)
            records.Add(Biff8TestWorkbook.EmbeddedChart(1, 10.0, 20.0));

        records.Add(Biff8TestWorkbook.Number(1, 0, 2.0));
        byte[] stream = Biff8TestWorkbook.BuildRawWorkbookStream(
            [Biff8TestWorkbook.Concat([.. records]), Biff8TestWorkbook.WorksheetSubstream(Biff8TestWorkbook.Number(0, 0, 3.0))],
            ("Sheet1", 0),
            ("Sheet2", 1));
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile(stream, "Workbook");
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        ExcelWorksheet first = workbook.ReadWorksheet(0);
        ExcelWorksheet second = workbook.ReadWorksheet(1);

        Assert.AreEqual("0,0,Number,1;1,0,Number,2", string.Join(";", first.Cells.Select(DescribeCell)));
        Assert.AreEqual("0,0,Number,3", string.Join(";", second.Cells.Select(DescribeCell)));
    }

    /// <summary>
    /// Verifies that a worksheet whose substream has no EOF record ends at the nearest substream that begins after it,
    /// whatever the order of the sheet directory, which here lists the substreams last to first.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenSheetDirectoryIsOutOfStreamOrder_ShouldEndAtTheNearestLaterSheet()
    {
        byte[] stream = Biff8TestWorkbook.BuildRawWorkbookStream(
            [
                Biff8TestWorkbook.Concat(Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofWorksheet), Biff8TestWorkbook.Number(0, 0, 1.0)),
                Biff8TestWorkbook.Concat(Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofWorksheet), Biff8TestWorkbook.Number(0, 0, 2.0)),
                Biff8TestWorkbook.WorksheetSubstream(Biff8TestWorkbook.Number(0, 0, 3.0)),
            ],
            ("Last", 2),
            ("Middle", 1),
            ("First", 0));
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile(stream, "Workbook");
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        ExcelWorksheet first = workbook.ReadWorksheet("First");
        ExcelWorksheet middle = workbook.ReadWorksheet("Middle");
        ExcelWorksheet last = workbook.ReadWorksheet("Last");

        Assert.AreEqual("0,0,Number,1", string.Join(";", first.Cells.Select(DescribeCell)));
        Assert.AreEqual("0,0,Number,2", string.Join(";", middle.Cells.Select(DescribeCell)));
        Assert.AreEqual("0,0,Number,3", string.Join(";", last.Cells.Select(DescribeCell)));
    }

    /// <summary>
    /// Verifies that the last substream in the workbook stream, when it has no EOF record, ends at the end of the
    /// stream, and its worksheet reads its cells.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenLastSheetLacksEof_ShouldEndAtTheEndOfTheStream()
    {
        byte[] stream = Biff8TestWorkbook.BuildRawWorkbookStream(
            [
                Biff8TestWorkbook.WorksheetSubstream(Biff8TestWorkbook.Number(0, 0, 1.0)),
                Biff8TestWorkbook.Concat(
                    Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofWorksheet),
                    Biff8TestWorkbook.Number(0, 0, 2.0),
                    Biff8TestWorkbook.Number(1, 0, 3.0)),
            ],
            ("Sheet1", 0),
            ("Sheet2", 1));
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile(stream, "Workbook");
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        ExcelWorksheet last = workbook.ReadWorksheet(1);

        Assert.AreEqual("0,0,Number,2;1,0,Number,3", string.Join(";", last.Cells.Select(DescribeCell)));
    }

    /// <summary>
    /// Verifies that a shared string whose surrogate pair is split between the SST record and the CONTINUE record after
    /// it is read as the character.
    /// </summary>
    [TestMethod]
    public void ReadWorksheet_WhenSharedStringSplitsASurrogatePair_ShouldReadTheCharacter()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [
                Biff8TestWorkbook.Record(0x00FC, [0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, 0x3D, 0xD8]),
                Biff8TestWorkbook.Record(0x003C, [0x01, 0x00, 0xDE]),
            ],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.LabelSst(0, 0, 0)]));
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        ExcelCell cell = workbook.ReadWorksheet(0).Cells.Single();

        Assert.AreEqual("\uD83D\uDE00", cell.StringValue);
    }
}
