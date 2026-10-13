// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeCsvRateTableSourceTests.Allocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

public sealed partial class BoeCsvRateTableSourceTests
{
    /// <summary>
    /// The most a cached response may cost to parse, in bytes allocated per byte of response. Decoding the response to a
    /// string and encoding it back to bytes before parsing cost about 33.8; parsing its bytes directly costs about 28.8.
    /// </summary>
    private const int MaxAllocatedBytesPerResponseByte = 31;

    /// <summary>
    /// Verifies that a long cached response is parsed from its bytes, without first being decoded to a string and
    /// encoded back to bytes.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task GetTableAsync_WhenCachedResponseIsLong_ShouldParseItsBytesWithoutDecodingThem()
    {
        BoeRateProviderOptions options = new();
        BoeCsvRateTableSource source = CreateSource(out StubHttpMessageHandler handler, out RecordingCache cache, options);
        byte[] response = Encoding.UTF8.GetBytes(LongResponse(options, rows: 2000));
        cache.Seed((s_from, s_to), response);
        cache.Seed((s_from, s_from), Encoding.UTF8.GetBytes(LongResponse(options, rows: 2)));
        _ = await source.GetTableAsync(s_from, s_from);

        long before = GC.GetAllocatedBytesForCurrentThread();
        BoeRateTable table = await source.GetTableAsync(s_from, s_to);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0, handler.RequestCount);
        Assert.HasCount(2000 * options.Series.Count, table.Observations);
        Assert.IsLessThan(
            (long)response.Length * MaxAllocatedBytesPerResponseByte,
            allocated,
            $"Parsing a {response.Length}-byte response allocated {allocated} bytes.");
    }

    /// <summary>
    /// Builds an IADB column response of the given number of rows over every configured series.
    /// </summary>
    /// <param name="options">The options supplying the series.</param>
    /// <param name="rows">The number of dated rows.</param>
    /// <returns>The CSV text.</returns>
    private static string LongResponse(BoeRateProviderOptions options, int rows)
    {
        StringBuilder csv = new();
        csv.Append("\"DATE\"");
        foreach (BoeSeries series in options.Series)
            csv.Append(",\"").Append(series.SeriesCode).Append('"');

        csv.Append('\n');
        DateOnly first = new(2000, 1, 3);
        for (var row = 0; row < rows; row++)
        {
            csv.Append('"').Append(first.AddDays(row).ToString("dd MMM yyyy", CultureInfo.InvariantCulture)).Append('"');
            for (var column = 0; column < options.Series.Count; column++)
                csv.Append(",\"1.").Append((row + column).ToString("D4", CultureInfo.InvariantCulture)).Append('"');

            csv.Append('\n');
        }

        return csv.ToString();
    }
}
