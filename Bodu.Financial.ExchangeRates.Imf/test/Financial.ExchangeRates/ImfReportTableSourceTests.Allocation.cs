// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfReportTableSourceTests.Allocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

public partial class ImfReportTableSourceTests
{
    /// <summary>
    /// The most a cached report may cost to parse, in bytes allocated per byte of report. Decoding the report to a
    /// string and encoding it back to bytes before parsing cost about 19.8; parsing its bytes directly costs about 14.7.
    /// </summary>
    private const int MaxAllocatedBytesPerReportByte = 17;

    /// <summary>
    /// Verifies that a long cached report is parsed from its bytes, without first being decoded to a string and
    /// encoded back to bytes.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenCachedReportIsLong_ShouldParseItsBytesWithoutDecodingThem()
    {
        ImfReportTableSource source = CreateSource(out StubHttpMessageHandler handler, out RecordingCache cache);
        string month = ImfFixtures.ReadText(ImfFixtures.Rep202604);
        byte[] report = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat(month, 40)));
        ImfReportMonth warmUp = new(2026, 3);
        cache.Seed(Month, report);
        cache.Seed(warmUp, Encoding.UTF8.GetBytes(month));
        _ = await source.GetTableAsync(warmUp);

        long before = GC.GetAllocatedBytesForCurrentThread();
        ImfRateTable table = await source.GetTableAsync(Month);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0, handler.RequestCount);
        Assert.IsNotEmpty(table.Observations);
        Assert.IsLessThan(
            (long)report.Length * MaxAllocatedBytesPerReportByte,
            allocated,
            $"Parsing a {report.Length}-byte report allocated {allocated} bytes.");
    }
}
