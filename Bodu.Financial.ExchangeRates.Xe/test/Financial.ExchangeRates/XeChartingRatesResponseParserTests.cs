// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeChartingRatesResponseParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="XeChartingRatesResponseParser" /> decodes the XE delta-encoded series into dated
/// observations, restricting them to the requested range and rejecting malformed or empty payloads.
/// </summary>
[TestClass]
public class XeChartingRatesResponseParserTests
{
    private static readonly XeExchangeRateOptions Options = new();

    /// <summary>
    /// Verifies that a well-formed response decodes each subsequent rate as its delta from the baseline, dating the
    /// first decoded point at <c>startTime</c> and advancing by <c>interval</c>.
    /// </summary>
    [TestMethod]
    public void Parse_WhenValid_ShouldDecodeDeltasAndDatePoints()
    {
        byte[] json = Utf8(
            """
            {
              "batchList": [
                { "startTime": 1672617600000, "interval": 86400000, "rates": [100, 100.68, 100.6828, 100.685] }
              ]
            }
            """);

        PairRateData<XeSeriesInfo> data = XeChartingRatesResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);

        Assert.HasCount(3, data.Observations);
        Assert.AreEqual(new DateOnly(2023, 1, 2), data.Observations[0].Date);
        Assert.AreEqual(0.68m, data.Observations[0].Rate);
        Assert.AreEqual(new DateOnly(2023, 1, 3), data.Observations[1].Date);
        Assert.AreEqual(0.6828m, data.Observations[1].Rate);
        Assert.AreEqual(new DateOnly(2023, 1, 4), data.Observations[2].Date);
        Assert.AreEqual(0.685m, data.Observations[2].Rate);
    }

    /// <summary>
    /// Verifies that points the server returns outside the request's inclusive range are excluded.
    /// </summary>
    [TestMethod]
    public void Parse_WhenPointsOutsideRange_ShouldExcludeThem()
    {
        byte[] json = XeFixtures.ReadBytes(XeFixtures.AudUsd);

        PairRateData<XeSeriesInfo> data = XeChartingRatesResponseParser.Parse(json, Request(new(2023, 1, 3), new(2023, 1, 4)), Options);

        Assert.HasCount(2, data.Observations);
        Assert.IsTrue(data.Observations.All(o => o.Date >= new DateOnly(2023, 1, 3) && o.Date <= new DateOnly(2023, 1, 4)));
    }

    /// <summary>
    /// Verifies that malformed JSON throws <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMalformedJson_ShouldThrowFormatException()
    {
        byte[] json = Utf8("{ not json");

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = XeChartingRatesResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);
        });
    }

    /// <summary>
    /// Verifies that a response with an empty <c>batchList</c> array throws <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBatchListEmpty_ShouldThrowFormatException()
    {
        byte[] json = XeFixtures.ReadBytes(XeFixtures.EmptyBatchList);

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = XeChartingRatesResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);
        });
    }

    /// <summary>
    /// Verifies that a response without a <c>batchList</c> array throws <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMissingBatchList_ShouldThrowFormatException()
    {
        byte[] json = Utf8("""{ "somethingElse": 1 }""");

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = XeChartingRatesResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);
        });
    }

    /// <summary>
    /// Verifies that a batch whose <c>rates</c> array carries only the baseline element yields no observations.
    /// </summary>
    [TestMethod]
    public void Parse_WhenOnlyBaselinePresent_ShouldReturnNoObservations()
    {
        byte[] json = Utf8(
            """
            { "batchList": [ { "startTime": 1672617600000, "interval": 86400000, "rates": [100] } ] }
            """);

        PairRateData<XeSeriesInfo> data = XeChartingRatesResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);

        Assert.IsEmpty(data.Observations);
    }

    /// <summary>
    /// Verifies that when several intraday points fall on the same calendar day, the last point of that day is the one
    /// retained after accumulation, matching the upsert-by-date behaviour of the in-memory store.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSubDailyGranularity_ShouldEmitChronologicalPointsPerDay()
    {
        // Two points per day (interval of 12 hours): the parser emits each point in chronological order, and the
        // accumulator keeps the last one for each calendar day.
        byte[] json = Utf8(
            """
            { "batchList": [ { "startTime": 1672617600000, "interval": 43200000, "rates": [100, 100.50, 100.68, 100.70] } ] }
            """);

        PairRateData<XeSeriesInfo> data = XeChartingRatesResponseParser.Parse(json, Request(new(2023, 1, 2), new(2023, 1, 2)), Options);

        Assert.HasCount(2, data.Observations);
        Assert.IsTrue(data.Observations.All(o => o.Date == new DateOnly(2023, 1, 2)));
        Assert.AreEqual(0.50m, data.Observations[0].Rate);
        Assert.AreEqual(0.68m, data.Observations[1].Rate);
    }

    private static ExchangeRatePairRequest Request(DateOnly startDate, DateOnly endDate) =>
        new(new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD), startDate, endDate);

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);
}
