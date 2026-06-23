// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetReaderTests.Malformed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

public partial class ExcelWorksheetReaderTests
{
    /// <summary>
    /// Verifies that a truncated <c>NUMBER</c> record throws <see cref="ExcelBinaryFormatException" />.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenNumberRecordTruncated_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = ReadGrid(Biff8TestWorkbook.Record(0x0203, new byte[8]));
        });
    }

    /// <summary>
    /// Verifies that a truncated <c>RK</c> record throws <see cref="ExcelBinaryFormatException" />.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenRkRecordTruncated_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = ReadGrid(Biff8TestWorkbook.Record(0x027E, new byte[6]));
        });
    }

    /// <summary>
    /// Verifies that a truncated <c>LABELSST</c> record throws <see cref="ExcelBinaryFormatException" />.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenLabelSstRecordTruncated_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = ReadGrid(Biff8TestWorkbook.Record(0x00FD, new byte[6]));
        });
    }

    /// <summary>
    /// Verifies that a truncated <c>BOOLERR</c> record throws <see cref="ExcelBinaryFormatException" />.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenBoolErrRecordTruncated_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = ReadGrid(Biff8TestWorkbook.Record(0x0205, new byte[4]));
        });
    }

    /// <summary>
    /// Verifies that a truncated <c>FORMULA</c> record throws <see cref="ExcelBinaryFormatException" />.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenFormulaRecordTruncated_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = ReadGrid(Biff8TestWorkbook.Record(0x0006, new byte[8]));
        });
    }

    /// <summary>
    /// Verifies that a <c>MULRK</c> record whose cell run is not a whole number of entries throws
    /// <see cref="ExcelBinaryFormatException" />.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenMulRkRunNotWhole_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            // 6 header bytes + 5 run bytes is not a whole number of (ixfe + rk) entries.
            _ = ReadGrid(Biff8TestWorkbook.Record(0x00BD, new byte[11]));
        });
    }

    /// <summary>
    /// Verifies that a <c>LABELSST</c> cell whose shared-string index is out of range throws
    /// <see cref="ExcelBinaryFormatException" />.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenLabelSstIndexOutOfRange_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            // The reader's shared string table is empty, so index 0 is out of range.
            _ = ReadGrid(Biff8TestWorkbook.LabelSst(0, 0, 0));
        });
    }

    /// <summary>
    /// Verifies that a substream ending in a trailing fragment too short to form a record header throws
    /// <see cref="ExcelBinaryFormatException" />.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenTrailingBytesAfterLastRecord_ShouldThrowFormatException()
    {
        byte[] substream = Biff8TestWorkbook.Concat(
            Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofWorksheet),
            Biff8TestWorkbook.Number(0, 0, 1.0),
            new byte[] { 0x01, 0x02 });

        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReaderRaw(substream);

        Assert.IsTrue(reader.TryReadCell(out _));
        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = reader.TryReadCell(out _);
        });
    }
}
