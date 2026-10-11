// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSubstreamLoaderTests.ReadGlobalsSubstream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Biff;

public partial class BiffSubstreamLoaderTests
{
    /// <summary>
    /// Verifies that the globals end at their EOF record and leave the stream at the first sheet's substream.
    /// </summary>
    [TestMethod]
    public void ReadGlobalsSubstream_WhenEofIsPresent_ShouldStopAfterIt()
    {
        byte[] globals = Biff8TestWorkbook.Concat(
            Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofGlobals),
            Biff8TestWorkbook.DateMode(true),
            Biff8TestWorkbook.Eof());
        byte[] sheet = Biff8TestWorkbook.WorksheetSubstream(Biff8TestWorkbook.Number(0, 0, 1.0));
        using MemoryStream stream = new([.. globals, .. sheet]);

        byte[] substream = BiffSubstreamLoader.ReadGlobalsSubstream(stream);

        CollectionAssert.AreEqual(globals, substream);
        Assert.AreEqual(globals.Length, stream.Position);
    }

    /// <summary>
    /// Verifies that globals that have lost their EOF record end where the first sheet's BOF record begins, without that
    /// record or anything after it.
    /// </summary>
    [TestMethod]
    public void ReadGlobalsSubstream_WhenEofIsMissing_ShouldStopBeforeTheNextBof()
    {
        byte[] globals = Biff8TestWorkbook.Concat(Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofGlobals), Biff8TestWorkbook.DateMode(true));
        byte[] sheet = Biff8TestWorkbook.WorksheetSubstream(Biff8TestWorkbook.Number(0, 0, 1.0));
        using MemoryStream stream = new([.. globals, .. sheet, .. sheet]);

        byte[] substream = BiffSubstreamLoader.ReadGlobalsSubstream(stream);

        CollectionAssert.AreEqual(globals, substream);
    }

    /// <summary>
    /// Verifies that globals that the end of the stream cuts off, before their EOF record and with no sheet after them,
    /// are rejected.
    /// </summary>
    [TestMethod]
    public void ReadGlobalsSubstream_WhenStreamEndsBeforeEof_ShouldThrowExcelBinaryFormatException()
    {
        using MemoryStream stream = new(Biff8TestWorkbook.Concat(Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofGlobals), Biff8TestWorkbook.DateMode(true)));

        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = BiffSubstreamLoader.ReadGlobalsSubstream(stream);
        });
    }
}
