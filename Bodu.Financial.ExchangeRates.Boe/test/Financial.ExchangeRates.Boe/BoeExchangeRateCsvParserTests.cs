// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateCsvParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Test;

namespace Bodu.Financial.ExchangeRates.Boe;

/// <summary>
/// Verifies that <see cref="BoeExchangeRateCsvParser" /> decodes the IADB CSV layout correctly and rejects responses
/// without the expected header.
/// </summary>
[TestClass]
public class BoeExchangeRateCsvParserTests
{
    /// <summary>
    /// Verifies that parsing the sample response yields one observation per present rate cell.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Parse_WhenSampleResponse_ShouldYieldOneObservationPerCell()
    {
        BoeExchangeRateTable table = ParseSample();

        Assert.AreEqual(6, table.Observations.Count);
    }

    /// <summary>
    /// Verifies that a parsed cell carries the daily spot rate quoted against the pound on its date.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSampleResponse_ShouldReadDateCurrencyAndRate()
    {
        BoeExchangeRateTable table = ParseSample();

        BoeExchangeRateObservation usd = table.Observations
            .Single(o => o.Date == new DateOnly(2023, 1, 3) && o.CurrencyCode == "USD");

        Assert.AreEqual(1.2065m, usd.Rate);
    }

    /// <summary>
    /// Verifies that the distinct quote currencies are surfaced as GBP-based series with their codes.
    /// </summary>
    [TestMethod]
    public void GetSeriesInfo_WhenSampleResponse_ShouldReturnGbpPairsWithCodes()
    {
        BoeExchangeRateTable table = ParseSample();

        BoeSeriesInfo usd = table.GetSeriesInfo().Single(s => s.QuoteIsoCode == "USD");

        Assert.AreEqual(new ExchangeRatePair("GBP", "USD"), usd.Pair);
        Assert.AreEqual("XUDLUSS", usd.SeriesCode);
    }

    /// <summary>
    /// Verifies that an empty rate cell is skipped while the rest of the row is read.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCellEmpty_ShouldSkipCell()
    {
        var csv =
            "\"DATE\",\"XUDLUSS\",\"XUDLJYS\"\n" +
            "\"03 Jan 2023\",\"\",\"159.10\"\n";

        BoeExchangeRateTable table = BoeExchangeRateCsvParser.Parse(csv, new BoeExchangeRateOptions());

        Assert.AreEqual(1, table.Observations.Count);
        Assert.AreEqual("JPY", table.Observations[0].CurrencyCode);
    }

    /// <summary>
    /// Verifies that a column whose series code is not configured is ignored.
    /// </summary>
    [TestMethod]
    public void Parse_WhenColumnNotConfigured_ShouldIgnoreColumn()
    {
        var csv =
            "\"DATE\",\"XUDLUSS\",\"XUDLZZZ\"\n" +
            "\"03 Jan 2023\",\"1.2065\",\"9.9999\"\n";

        BoeExchangeRateTable table = BoeExchangeRateCsvParser.Parse(csv, new BoeExchangeRateOptions());

        Assert.AreEqual(1, table.Observations.Count);
        Assert.AreEqual("USD", table.Observations[0].CurrencyCode);
    }

    /// <summary>
    /// Verifies that a response lacking the <c>DATE</c> header is reported as a format failure.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHeaderMissing_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<BoeExchangeRateFormatException>(() =>
        {
            _ = BoeExchangeRateCsvParser.Parse("<html><body>Service unavailable</body></html>", new BoeExchangeRateOptions());
        });
    }

    /// <summary>
    /// Parses the embedded sample response with default options.
    /// </summary>
    /// <returns>The parsed table.</returns>
    private static BoeExchangeRateTable ParseSample() =>
        BoeExchangeRateCsvParser.Parse(BoeFixtures.OpenStream(BoeFixtures.Sample), new BoeExchangeRateOptions());
}
