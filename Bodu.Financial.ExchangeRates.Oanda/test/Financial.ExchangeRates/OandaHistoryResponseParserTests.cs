// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OandaHistoryResponseParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="OandaHistoryResponseParser" /> maps an OANDA response into dated observations, restricting
/// them to the requested range and rejecting malformed or empty payloads.
/// </summary>
[TestClass]
public class OandaHistoryResponseParserTests
{
    private static readonly OandaExchangeRateOptions Options = new();

    /// <summary>
    /// Verifies that a well-formed response yields the in-range observations with the expected dates and rates, while
    /// out-of-range rows are excluded.
    /// </summary>
    [TestMethod]
    public void Parse_WhenValid_ShouldReturnObservationsInRange()
    {
        byte[] json = Utf8(
            """
            {
              "widget": [
                {
                  "baseCurrency": "AUD",
                  "quoteCurrency": "USD",
                  "data": [
                    [1675209600000, "0.7000"],
                    [1672704000000, "0.6828"],
                    [1672617600000, "0.6800"]
                  ]
                }
              ]
            }
            """);

        PairRateData<OandaSeriesInfo> data = OandaHistoryResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);

        // The feed returns rows newest-first and the parser preserves that order, so assert by date rather than index.
        Dictionary<DateOnly, decimal> byDate = data.Observations.ToDictionary(o => o.Date, o => o.Rate);

        Assert.HasCount(2, data.Observations);
        Assert.AreEqual(0.6800m, byDate[new DateOnly(2023, 1, 2)]);
        Assert.AreEqual(0.6828m, byDate[new DateOnly(2023, 1, 3)]);
    }

    /// <summary>
    /// Verifies that a timestamp supplied as a JSON number and a rate supplied as a JSON number are both parsed.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRateIsNumber_ShouldParseIt()
    {
        byte[] json = Utf8(
            """
            { "widget": [ { "baseCurrency": "AUD", "quoteCurrency": "USD", "data": [ [1672704000000, 0.6828] ] } ] }
            """);

        PairRateData<OandaSeriesInfo> data = OandaHistoryResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);

        Assert.HasCount(1, data.Observations);
        Assert.AreEqual(0.6828m, data.Observations[0].Rate);
    }

    /// <summary>
    /// Verifies that the widget entry matching the requested pair is selected when several are present.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMultipleSeries_ShouldSelectMatchingPair()
    {
        byte[] json = Utf8(
            """
            {
              "widget": [
                { "baseCurrency": "AUD", "quoteCurrency": "EUR", "data": [ [1672704000000, "0.6300"] ] },
                { "baseCurrency": "AUD", "quoteCurrency": "USD", "data": [ [1672704000000, "0.6828"] ] }
              ]
            }
            """);

        PairRateData<OandaSeriesInfo> data = OandaHistoryResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);

        Assert.HasCount(1, data.Observations);
        Assert.AreEqual(0.6828m, data.Observations[0].Rate);
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
            _ = OandaHistoryResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);
        });
    }

    /// <summary>
    /// Verifies that a response without a <c>widget</c> array throws <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMissingWidget_ShouldThrowFormatException()
    {
        byte[] json = Utf8("""{ "frequency": "daily" }""");

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = OandaHistoryResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);
        });
    }

    /// <summary>
    /// Verifies that rows with a missing or non-positive rate are skipped.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRateNotPositive_ShouldSkipRow()
    {
        byte[] json = Utf8(
            """
            {
              "widget": [
                {
                  "baseCurrency": "AUD",
                  "quoteCurrency": "USD",
                  "data": [
                    [1672617600000, "0"],
                    [1672704000000],
                    [1672790400000, "0.6850"]
                  ]
                }
              ]
            }
            """);

        PairRateData<OandaSeriesInfo> data = OandaHistoryResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);

        Assert.HasCount(1, data.Observations);
        Assert.AreEqual(new DateOnly(2023, 1, 4), data.Observations[0].Date);
        Assert.AreEqual(0.6850m, data.Observations[0].Rate);
    }

    private static ExchangeRatePairRequest Request(DateOnly startDate, DateOnly endDate) =>
        new(new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD), startDate, endDate);

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);
}
