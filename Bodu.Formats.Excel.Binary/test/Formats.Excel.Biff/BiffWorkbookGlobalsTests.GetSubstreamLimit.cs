// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWorkbookGlobalsTests.GetSubstreamLimit.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Biff;

public partial class BiffWorkbookGlobalsTests
{
    /// <summary>
    /// Verifies that each sheet's substream ends at the nearest offset past its own at which another sheet's substream
    /// begins, whatever the order of the sheet directory and with two sheets sharing an offset, and that the last
    /// substream in the stream ends at the stream's end.
    /// </summary>
    [TestMethod]
    public void GetSubstreamLimit_WhenDirectoryIsOutOfOrder_ShouldReturnTheNearestLaterOffset()
    {
        BiffWorkbookGlobals globals = ParseGlobals(300, 100, 200, 100);

        long[] limits = [.. Enumerable.Range(0, 4).Select(index => globals.GetSubstreamLimit(index, 400))];

        CollectionAssert.AreEqual(new long[] { 400, 200, 300, 200 }, limits);
    }

    /// <summary>
    /// Verifies that a sheet's substream ends no later than the end of the stream, even where the next sheet's offset
    /// lies beyond it.
    /// </summary>
    [TestMethod]
    public void GetSubstreamLimit_WhenNextOffsetLiesPastTheStream_ShouldReturnTheStreamLength()
    {
        BiffWorkbookGlobals globals = ParseGlobals(100, 900);

        Assert.AreEqual(250, globals.GetSubstreamLimit(0, 250));
        Assert.AreEqual(250, globals.GetSubstreamLimit(1, 250));
    }

    /// <summary>
    /// Parses a globals substream whose sheet directory points worksheets at the specified offsets.
    /// </summary>
    /// <param name="offsets">The substream offset of each sheet, in directory order.</param>
    /// <returns>The parsed globals.</returns>
    private static BiffWorkbookGlobals ParseGlobals(params long[] offsets)
    {
        List<byte[]> records = [Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofGlobals)];
        for (int i = 0; i < offsets.Length; i++)
            records.Add(Biff8TestWorkbook.BoundSheet(offsets[i], 0, 0, $"Sheet{i + 1}"));

        records.Add(Biff8TestWorkbook.Eof());
        return BiffWorkbookGlobals.Parse(Biff8TestWorkbook.Concat([.. records]), new ExcelBinaryReaderOptions());
    }
}
