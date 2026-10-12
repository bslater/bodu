// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSubstreamLoaderTests.ReadSubstream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Biff;

namespace Bodu.Formats.Excel.Biff;

public partial class BiffSubstreamLoaderTests
{
    /// <summary>
    /// Verifies that the substream of a worksheet holding embedded charts, each a chart substream nested after its OBJ
    /// record, ends at the EOF that matches the worksheet's BOF, past the charts' own EOF records, and leaves the stream
    /// at the next substream.
    /// </summary>
    /// <param name="charts">The number of embedded charts in the worksheet.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public void ReadSubstream_WhenSubstreamNestsCharts_ShouldStopAtItsMatchingEof(int charts)
    {
        List<byte[]> body = [Biff8TestWorkbook.Number(0, 0, 1.0)];
        for (int i = 1; i <= charts; i++)
            body.Add(Biff8TestWorkbook.EmbeddedChart((ushort)i, 10.0, 20.0));

        body.Add(Biff8TestWorkbook.Number(1, 0, 2.0));
        byte[] sheet = Biff8TestWorkbook.WorksheetSubstream([.. body]);
        byte[] next = Biff8TestWorkbook.WorksheetSubstream(Biff8TestWorkbook.Number(0, 0, 3.0));
        using MemoryStream stream = new([.. sheet, .. next]);

        byte[] substream = BiffSubstreamLoader.ReadSubstream(stream, stream.Length);

        CollectionAssert.AreEqual(sheet, substream);
        Assert.AreEqual(sheet.Length, stream.Position);
    }

    /// <summary>
    /// Verifies that a substream nested in a nested substream closes only itself, so the worksheet's substream ends at
    /// the EOF that matches its own BOF.
    /// </summary>
    [TestMethod]
    public void ReadSubstream_WhenNestedSubstreamNestsAnother_ShouldStopAtItsMatchingEof()
    {
        byte[] sheet = Biff8TestWorkbook.WorksheetSubstream(
            Biff8TestWorkbook.Bof((ushort)BiffSubstreamType.Chart),
            Biff8TestWorkbook.EmbeddedChart(1, 10.0),
            Biff8TestWorkbook.Eof(),
            Biff8TestWorkbook.Number(0, 0, 1.0));
        byte[] next = Biff8TestWorkbook.WorksheetSubstream(Biff8TestWorkbook.Number(0, 0, 3.0));
        using MemoryStream stream = new([.. sheet, .. next]);

        byte[] substream = BiffSubstreamLoader.ReadSubstream(stream, stream.Length);

        CollectionAssert.AreEqual(sheet, substream);
        Assert.AreEqual(sheet.Length, stream.Position);
    }

    /// <summary>
    /// Verifies that a substream without an EOF record ends at the limit, where the next substream begins, and leaves
    /// the stream there.
    /// </summary>
    [TestMethod]
    public void ReadSubstream_WhenSubstreamLacksEof_ShouldStopAtTheLimit()
    {
        byte[] sheet = Biff8TestWorkbook.Concat(Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofWorksheet), Biff8TestWorkbook.Number(0, 0, 1.0));
        byte[] next = Biff8TestWorkbook.WorksheetSubstream(Biff8TestWorkbook.Number(0, 0, 2.0));
        using MemoryStream stream = new([.. sheet, .. next]);

        byte[] substream = BiffSubstreamLoader.ReadSubstream(stream, sheet.Length);

        CollectionAssert.AreEqual(sheet, substream);
        Assert.AreEqual(sheet.Length, stream.Position);
    }

    /// <summary>
    /// Verifies that a substream without an EOF record that holds an embedded chart ends at the limit, not at the
    /// chart's BOF or EOF.
    /// </summary>
    [TestMethod]
    public void ReadSubstream_WhenSubstreamLacksEofAndNestsAChart_ShouldStopAtTheLimit()
    {
        byte[] sheet = Biff8TestWorkbook.Concat(
            Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofWorksheet),
            Biff8TestWorkbook.Number(0, 0, 1.0),
            Biff8TestWorkbook.EmbeddedChart(1, 10.0),
            Biff8TestWorkbook.Number(1, 0, 2.0));
        byte[] next = Biff8TestWorkbook.WorksheetSubstream(Biff8TestWorkbook.Number(0, 0, 3.0));
        using MemoryStream stream = new([.. sheet, .. next]);

        byte[] substream = BiffSubstreamLoader.ReadSubstream(stream, sheet.Length);

        CollectionAssert.AreEqual(sheet, substream);
    }

    /// <summary>
    /// Verifies that the last substream in the stream, without an EOF record, ends at the end of the stream when the
    /// limit is the stream's length.
    /// </summary>
    [TestMethod]
    public void ReadSubstream_WhenLimitIsTheStreamLength_ShouldReadToTheEndOfTheStream()
    {
        byte[] sheet = Biff8TestWorkbook.Concat(
            Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofWorksheet),
            Biff8TestWorkbook.Number(0, 0, 1.0),
            Biff8TestWorkbook.Number(1, 0, 2.0));
        using MemoryStream stream = new(sheet);

        byte[] substream = BiffSubstreamLoader.ReadSubstream(stream, stream.Length);

        CollectionAssert.AreEqual(sheet, substream);
    }

    /// <summary>
    /// Verifies that a substream without an EOF record is rejected when the limit is <see cref="long.MaxValue" />,
    /// which requires the matching EOF record.
    /// </summary>
    [TestMethod]
    public void ReadSubstream_WhenLimitIsMaxValueAndEofIsMissing_ShouldThrowExcelBinaryFormatException()
    {
        using MemoryStream stream = new(Biff8TestWorkbook.Concat(Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofGlobals), Biff8TestWorkbook.DateMode(true)));

        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = BiffSubstreamLoader.ReadSubstream(stream, long.MaxValue);
        });
    }

    /// <summary>
    /// Verifies that a substream that the end of the stream cuts inside a record, in its header or its payload, is
    /// rejected even when the limit is the stream's length.
    /// </summary>
    /// <param name="cut">The number of bytes removed from the end of the substream.</param>
    [TestMethod]
    [DataRow(2)]
    [DataRow(7)]
    public void ReadSubstream_WhenStreamEndsInsideARecord_ShouldThrowExcelBinaryFormatException(int cut)
    {
        byte[] sheet = Biff8TestWorkbook.Concat(Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofWorksheet), Biff8TestWorkbook.Number(0, 0, 1.0));
        using MemoryStream stream = new(sheet.AsSpan(0, sheet.Length - cut).ToArray());

        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = BiffSubstreamLoader.ReadSubstream(stream, stream.Length);
        });
    }

    /// <summary>
    /// Verifies that a substream that begins at the end of the stream is rejected rather than read as empty.
    /// </summary>
    [TestMethod]
    public void ReadSubstream_WhenSubstreamBeginsAtTheEndOfTheStream_ShouldThrowExcelBinaryFormatException()
    {
        using MemoryStream stream = new(Biff8TestWorkbook.WorksheetSubstream(Biff8TestWorkbook.Number(0, 0, 1.0)));
        stream.Seek(0, SeekOrigin.End);

        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = BiffSubstreamLoader.ReadSubstream(stream, stream.Length);
        });
    }
}
