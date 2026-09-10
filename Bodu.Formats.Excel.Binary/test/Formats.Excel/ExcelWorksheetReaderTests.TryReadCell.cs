// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetReaderTests.TryReadCell.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel.Biff;
using Bodu.IO.Biff;

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

    /// <summary>
    /// Verifies that records carrying no cell value — ROW, BLANK, MULBLANK, DIMENSIONS, and records the codec does
    /// not name — are skipped between value cells.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenNonValueRecordsInterleaved_ShouldSkipThem()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(
            Biff8TestWorkbook.Dimensions(0, 2, 0, 4),
            Biff8TestWorkbook.Record(0x0208, new byte[16]),
            Biff8TestWorkbook.Number(0, 0, 1.0),
            Biff8TestWorkbook.Record(0x0201, new byte[6]),
            Biff8TestWorkbook.Record(0x00BE, [0, 0, 1, 0, 0, 0, 0, 0, 2, 0]),
            Biff8TestWorkbook.Record(0x0FFE, new byte[3]),
            Biff8TestWorkbook.Record(0x0FFF, default),
            Biff8TestWorkbook.Number(1, 0, 2.0));

        List<double?> values = reader.ReadCells().Select(c => c.NumberValue).ToList();

        CollectionAssert.AreEqual(new double?[] { 1.0, 2.0 }, values);
    }

    /// <summary>
    /// Verifies that a MULRK run declaring no cells yields nothing and does not disturb the cells that follow.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenMulRkHasNoCells_ShouldContinueWithNextRecord()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(
            Biff8TestWorkbook.Record(0x00BD, [0, 0, 3, 0, 2, 0]),
            Biff8TestWorkbook.Number(0, 5, 7.0));

        Assert.IsTrue(reader.TryReadCell(out ExcelCell cell));
        Assert.AreEqual(7.0, cell.NumberValue);
        Assert.IsFalse(reader.TryReadCell(out _));
    }

    /// <summary>
    /// Verifies that a MULRK run's cells are emitted one per call, in column order, before the following record.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenMulRkHasSeveralCells_ShouldEmitEachBeforeNextRecord()
    {
        uint rk1 = (1u << 2) | 0x02;
        uint rk2 = (2u << 2) | 0x02;
        uint rk3 = (3u << 2) | 0x02;
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(
            Biff8TestWorkbook.MulRk(4, 10, (0, rk1), (0, rk2), (0, rk3)),
            Biff8TestWorkbook.Number(4, 13, 4.0));

        var cells = new List<(int Column, double Value)>();
        while (reader.TryReadCell(out ExcelCell cell))
            cells.Add((cell.ColumnIndex, cell.NumberValue!.Value));

        CollectionAssert.AreEqual(new[] { (10, 1.0), (11, 2.0), (12, 3.0), (13, 4.0) }, cells);
    }

    /// <summary>
    /// Verifies that a decode failure is reported once and leaves the reader past the bad record, so the cells that
    /// follow are still read.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenRecordMalformed_ShouldSkipItOnNextCall()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(
            Biff8TestWorkbook.Record(0x0203, new byte[8]),
            Biff8TestWorkbook.Number(1, 1, 9.0));

        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = reader.TryReadCell(out _);
        });

        Assert.IsTrue(reader.TryReadCell(out ExcelCell cell));
        Assert.AreEqual(9.0, cell.NumberValue);
        Assert.IsFalse(reader.TryReadCell(out _));
    }

    /// <summary>
    /// Verifies that a format exception raised by the codec is wrapped with the codec exception as its inner
    /// exception.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenCodecRejectsRecord_ShouldPreserveInnerException()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(Biff8TestWorkbook.Record(0x027E, new byte[4]));

        var ex = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = reader.TryReadCell(out _);
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<BiffFormatException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that a STRING record with no preceding formula is ignored rather than surfaced as a cell.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenStringRecordHasNoFormula_ShouldIgnoreIt()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(
            Biff8TestWorkbook.CompressedString("orphan"),
            Biff8TestWorkbook.Label(0, 0, "real"));

        Assert.IsTrue(reader.TryReadCell(out ExcelCell cell));
        Assert.AreEqual("real", cell.StringValue);
        Assert.IsFalse(reader.TryReadCell(out _));
    }

    /// <summary>
    /// Verifies that a formula whose cached text is carried by a STRING record separated from it by a non-STRING
    /// record surfaces as an empty text cell, and the interposed record is still read.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenStringResultIsNotImmediatelyFollowed_ShouldNotConsumeLaterString()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(
            Biff8TestWorkbook.Formula(0, 0, [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF]),
            Biff8TestWorkbook.Number(0, 1, 1.0),
            Biff8TestWorkbook.CompressedString("late"));

        List<ExcelCell> cells = reader.ReadCells().ToList();

        Assert.HasCount(2, cells);
        Assert.AreEqual(string.Empty, cells[0].StringValue);
        Assert.AreEqual(1.0, cells[1].NumberValue);
    }

    /// <summary>
    /// Verifies that a formula with a wide (UTF-16) STRING result decodes the text.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenStringResultIsWide_ShouldDecodeUtf16()
    {
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(
            Biff8TestWorkbook.Formula(0, 0, [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF]),
            Biff8TestWorkbook.CompressedString("日本語"));

        Assert.IsTrue(reader.TryReadCell(out ExcelCell cell));
        Assert.AreEqual("日本語", cell.StringValue);
    }

    /// <summary>
    /// Verifies that a LABELSST record in a BIFF5 substream, where no shared string table exists, is rejected as a
    /// format error.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenLabelSstUnderBiff5_ShouldThrowFormatException()
    {
        using ExcelWorksheetReader reader = OpenBiff5WorksheetReader(1252, (ref BiffWriter w) => w.WriteRecord(BiffRecordType.LabelSst, new byte[10]));

        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = reader.TryReadCell(out _);
        });
    }

    /// <summary>
    /// Verifies that a CODEPAGE record inside a BIFF5 sheet substream changes the decoding of the labels that follow
    /// it within the same sheet.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenBiff5CodePageChangesMidSheet_ShouldDecodeLaterLabelsWithIt()
    {
        // 0xE9 is 'é' in Windows-1252 and 'Θ' in code page 437.
        byte[] label = [0, 0, 1, 0, 0, 0, 0x01, 0x00, 0xE9];
        using ExcelWorksheetReader reader = OpenBiff5WorksheetReader(1252, (ref BiffWriter w) =>
        {
            w.WriteRecord(BiffRecordType.Label, label);
            w.WriteCodePage(437);
            label[2] = 2;
            w.WriteRecord(BiffRecordType.Label, label);
        });

        List<string?> texts = reader.ReadCells().Select(c => c.StringValue).ToList();

        CollectionAssert.AreEqual(new[] { "é", "Θ" }, texts);
    }

    /// <summary>
    /// Verifies that BIFF5 cells of every kind decode through the streaming reader with the BIFF5 layouts.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenBiff5Substream_ShouldDecodeEveryCellKind()
    {
        Assert.IsTrue(BiffRk.TryEncode(2.5, out uint rk));
        using ExcelWorksheetReader reader = OpenBiff5WorksheetReader(1252, (ref BiffWriter w) =>
        {
            w.WriteDimensions(new BiffDimensionsRecord(0, 2, 0, 5));
            w.WriteRow(new BiffRowRecord(0, 0, 5, 0, 0, 0));
            w.WriteLabel(0, 0, 0, "ünïcode");
            w.WriteNumber(0, 1, 0, 1.5);
            w.WriteRk(0, 2, 0, rk);
            w.WriteMulRk(0, 3, [new BiffRkCell(0, 0x06), new BiffRkCell(0, 0x0A)]);
            w.WriteBoolean(1, 0, 0, false);
            w.WriteError(1, 1, 0, 0x17);
            w.WriteFormula(1, 2, 0, BiffCachedResultKind.Boolean, 1, default);
            w.WriteBlank(1, 3, 0);
            w.WriteMulBlank(1, 4, [0]);
        });

        List<ExcelCell> cells = reader.ReadCells().ToList();

        Assert.HasCount(8, cells);
        Assert.AreEqual("ünïcode", cells[0].StringValue);
        Assert.AreEqual(1.5, cells[1].NumberValue);
        Assert.AreEqual(2.5, cells[2].NumberValue);
        Assert.AreEqual(1.0, cells[3].NumberValue);
        Assert.AreEqual(2.0, cells[4].NumberValue);
        Assert.IsFalse(cells[5].BooleanValue);
        Assert.AreEqual(ExcelErrorCode.Reference, cells[6].ErrorValue);
        Assert.IsTrue(cells[7].BooleanValue);
    }

    /// <summary>
    /// Verifies that an EOF ends the sheet even when value records follow it in the buffer.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenRecordsFollowEof_ShouldStopAtEof()
    {
        byte[] substream = Biff8TestWorkbook.Concat(
            Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofWorksheet),
            Biff8TestWorkbook.Number(0, 0, 1.0),
            Biff8TestWorkbook.Eof(),
            Biff8TestWorkbook.Number(1, 0, 2.0));
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReaderRaw(substream);

        Assert.IsTrue(reader.TryReadCell(out _));
        Assert.IsFalse(reader.TryReadCell(out _));
        Assert.IsFalse(reader.TryReadCell(out _));
    }

    /// <summary>
    /// Verifies that a substream that ends without an EOF record still yields its cells and then reports the end.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenSubstreamLacksEof_ShouldEndAfterLastRecord()
    {
        byte[] substream = Biff8TestWorkbook.Concat(Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofWorksheet), Biff8TestWorkbook.Number(0, 0, 1.0));
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReaderRaw(substream);

        Assert.IsTrue(reader.TryReadCell(out _));
        Assert.IsFalse(reader.TryReadCell(out _));
    }

    /// <summary>
    /// Verifies that a shared-string index one past the table is rejected while the last valid index resolves.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenSstIndexAtTableEnd_ShouldAcceptLastAndRejectOnePast()
    {
        ExcelWorksheetInfo info = new("Sheet", 0, ExcelSheetVisibility.Visible, ExcelSheetType.Worksheet, default);
        byte[] substream = Biff8TestWorkbook.WorksheetSubstream(Biff8TestWorkbook.LabelSst(0, 0, 1), Biff8TestWorkbook.LabelSst(0, 1, 2));
        using var reader = new ExcelWorksheetReader(info, substream, ["a", "b"], BiffFormatTable.Empty, new BiffReaderOptions { Version = BiffVersion.Biff8 });

        Assert.IsTrue(reader.TryReadCell(out ExcelCell cell));
        Assert.AreEqual("b", cell.StringValue);
        var ex = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = reader.TryReadCell(out _);
        });
        Assert.IsTrue(ex.Message.Contains('2', StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that the format index resolved through the XF table is carried on each cell, with an XF index
    /// beyond the table falling back to the general format.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenXfIndexResolves_ShouldCarryFormatIndex()
    {
        ExcelWorksheetInfo info = new("Sheet", 0, ExcelSheetVisibility.Visible, ExcelSheetType.Worksheet, default);
        var formats = new BiffFormatTable([0, 14, 164], new Dictionary<ushort, string> { [164] = "0.00%" }, detectDateFormats: true);
        byte[] substream = Biff8TestWorkbook.WorksheetSubstream(
            Biff8TestWorkbook.Number(0, 0, 1.0, xfIndex: 1),
            Biff8TestWorkbook.Number(0, 1, 1.0, xfIndex: 2),
            Biff8TestWorkbook.Number(0, 2, 1.0, xfIndex: 99));
        using var reader = new ExcelWorksheetReader(info, substream, [], formats, new BiffReaderOptions { Version = BiffVersion.Biff8 });

        List<ExcelCell> cells = reader.ReadCells().ToList();

        Assert.AreEqual(14, cells[0].FormatIndex);
        Assert.IsTrue(cells[0].IsDateFormatted, "Built-in format 14 is a date.");
        Assert.AreEqual(164, cells[1].FormatIndex);
        Assert.IsFalse(cells[1].IsDateFormatted);
        Assert.AreEqual(0, cells[2].FormatIndex);
    }

    /// <summary>
    /// Opens a streaming reader over a BIFF5 worksheet substream written through a BIFF5 writer.
    /// </summary>
    /// <param name="codePage">The code page the writer encodes text in and the reader decodes with.</param>
    /// <param name="body">A callback writing the sheet body records.</param>
    /// <returns>The reader.</returns>
    private static ExcelWorksheetReader OpenBiff5WorksheetReader(int codePage, Biff5TestWorkbook.WriteAction body)
    {
        var output = new System.Buffers.ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, new BiffWriterOptions { Version = BiffVersion.Biff5, CodePage = codePage });
        writer.WriteBof(BiffSubstreamType.Worksheet);
        body(ref writer);
        writer.WriteEof();

        ExcelWorksheetInfo info = new("Sheet", 0, ExcelSheetVisibility.Visible, ExcelSheetType.Worksheet, default);
        return new ExcelWorksheetReader(info, output.WrittenSpan.ToArray(), [], BiffFormatTable.Empty, new BiffReaderOptions { Version = BiffVersion.Biff5, CodePage = codePage });
    }
}
