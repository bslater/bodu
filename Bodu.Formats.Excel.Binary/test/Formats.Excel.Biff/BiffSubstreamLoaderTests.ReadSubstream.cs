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

        byte[] substream = BiffSubstreamLoader.ReadSubstream(stream);

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

        byte[] substream = BiffSubstreamLoader.ReadSubstream(stream);

        CollectionAssert.AreEqual(sheet, substream);
        Assert.AreEqual(sheet.Length, stream.Position);
    }
}
