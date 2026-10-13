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
    private static readonly OandaRateProviderOptions Options = new();

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
        var byDate = data.Observations.ToDictionary(o => o.Date, o => o.Rate);

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
    /// Verifies that a widget entry whose currency codes are not strings is passed over when the entry matching the
    /// requested pair is selected, rather than escaping as <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCurrencyCodeIsNotString_ShouldPassOverThatSeries()
    {
        byte[] json = Utf8(
            """
            {
              "widget": [
                { "baseCurrency": 36, "quoteCurrency": "USD", "data": [ [1672704000000, "0.6300"] ] },
                { "baseCurrency": "AUD", "quoteCurrency": "USD", "data": [ [1672704000000, "0.6828"] ] }
              ]
            }
            """);

        PairRateData<OandaSeriesInfo> data = OandaHistoryResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);

        Assert.HasCount(1, data.Observations);
        Assert.AreEqual(0.6828m, data.Observations[0].Rate);
    }

    /// <summary>
    /// Verifies that a widget whose only entry is not an object throws <see cref="ExchangeRateFormatException" />
    /// rather than <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSeriesIsNotObject_ShouldThrowFormatException()
    {
        byte[] json = Utf8("""{ "widget": [ 5 ] }""");

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = OandaHistoryResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);
        });
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

    /// <summary>
    /// Verifies that a row's timestamp is dated by its UTC day, its last millisecond included.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTimestampIsLastMillisecondOfADay_ShouldDateItByThatUtcDay()
    {
        byte[] json = Utf8(
            """
            { "widget": [ { "baseCurrency": "AUD", "quoteCurrency": "USD", "data": [ [1672790399999, "0.6828"] ] } ] }
            """);

        PairRateData<OandaSeriesInfo> data = OandaHistoryResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);

        Assert.HasCount(1, data.Observations);
        Assert.AreEqual(new DateOnly(2023, 1, 3), data.Observations[0].Date);
    }

    /// <summary>
    /// Verifies that rows timed at the first and last milliseconds a <see cref="DateTimeOffset" /> represents are dated
    /// by the first and last dates of its range, a timestamp given as a string included.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTimestampIsAtAnEndOfTheDateRange_ShouldReadIt()
    {
        byte[] json = Utf8(
            """
            {
              "widget": [
                {
                  "baseCurrency": "AUD",
                  "quoteCurrency": "USD",
                  "data": [ [-62135596800000, "0.5"], ["253402300799999", "0.9"] ]
                }
              ]
            }
            """);

        PairRateData<OandaSeriesInfo> data = OandaHistoryResponseParser.Parse(json, Request(DateOnly.MinValue, DateOnly.MaxValue), Options);

        var byDate = data.Observations.ToDictionary(o => o.Date, o => o.Rate);
        Assert.HasCount(2, data.Observations);
        Assert.AreEqual(0.5m, byDate[DateOnly.MinValue]);
        Assert.AreEqual(0.9m, byDate[DateOnly.MaxValue]);
    }

    /// <summary>
    /// Verifies that rows timed outside the range a <see cref="DateTimeOffset" /> represents, or at a fractional
    /// millisecond, are skipped rather than failing the response.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTimestampIsNotARepresentableTimestamp_ShouldSkipRow()
    {
        byte[] json = Utf8(
            """
            {
              "widget": [
                {
                  "baseCurrency": "AUD",
                  "quoteCurrency": "USD",
                  "data": [
                    [-62135596800001, "0.5"],
                    [253402300800000, "0.6"],
                    [9223372036854775807, "0.7"],
                    [1672704000000.5, "0.8"],
                    [1672704000000, "0.9"]
                  ]
                }
              ]
            }
            """);

        PairRateData<OandaSeriesInfo> data = OandaHistoryResponseParser.Parse(json, Request(DateOnly.MinValue, DateOnly.MaxValue), Options);

        Assert.HasCount(1, data.Observations);
        Assert.AreEqual(0.9m, data.Observations[0].Rate);
    }

    private static CurrencyPairRequest Request(DateOnly startDate, DateOnly endDate) =>
        new(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), startDate, endDate);

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);
}
