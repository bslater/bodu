// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.Open.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

namespace Bodu.Formats.Excel;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Verifies that a workbook opened over a seekable, read-only stream whose every read returns at most one byte
    /// yields the same cells as the same workbook opened over a read-only <see cref="MemoryStream" />.
    /// </summary>
    [TestMethod]
    public void Open_WhenStreamReturnsOneByteAtATime_ShouldReadEveryCell()
    {
        byte[][] body =
        [
            Biff8TestWorkbook.LabelSst(0, 0, 0),
            Biff8TestWorkbook.Number(0, 1, 1.5),
            Biff8TestWorkbook.LabelSst(1, 0, 1),
        ];
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [Biff8TestWorkbook.Sst("abc", "def")],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, body));
        byte[] file = xls.ToArray();

        var oneByteAtATime = new FixedChunkStream(file, chunkSize: 1, writable: false);
        Assert.IsTrue(oneByteAtATime.CanSeek);
        Assert.IsFalse(oneByteAtATime.CanWrite);

        string expected = ReadFirstSheet(new MemoryStream(file, writable: false));
        string actual = ReadFirstSheet(oneByteAtATime);

        Assert.AreEqual("0,0,String,abc;0,1,Number,1.5;1,0,String,def", expected);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Opens a workbook with <see cref="ExcelBinaryWorkbook.Open" /> and describes the cells of its first sheet.
    /// </summary>
    /// <param name="stream">The stream to open; the workbook disposes it.</param>
    /// <returns>
    /// The cells, described by <see cref="DescribeCell" /> and separated by semicolons, in reading order.
    /// </returns>
    private static string ReadFirstSheet(Stream stream)
    {
        using var workbook = ExcelBinaryWorkbook.Open(stream, new ExcelBinaryReaderOptions());
        using ExcelWorksheetReader reader = workbook.OpenWorksheet(0);

        return string.Join(";", reader.ReadCells().Select(DescribeCell));
    }
}
