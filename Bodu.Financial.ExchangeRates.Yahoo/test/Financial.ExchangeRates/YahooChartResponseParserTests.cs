// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooChartResponseParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the behavior of <see cref="YahooChartResponseParser" /> over fixture and malformed input.
/// </summary>
[TestClass]
public class YahooChartResponseParserTests
{
    /// <summary>
    /// The ticker addressing the AUD/USD fixture chart.
    /// </summary>
    private const string Symbol = "AUDUSD=X";

    /// <summary>
    /// Builds a pair request for the AUD/USD fixture spanning January 2023.
    /// </summary>
    /// <returns>The pair request.</returns>
    private static CurrencyPairRequest CreateRequest() =>
        new(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

    /// <summary>
    /// Verifies that a valid chart parses to the present close observations, skipping the null-close day.
    /// </summary>
    [TestMethod]
    public void Parse_WhenValidChart_ShouldReturnPresentObservations()
    {
        byte[] json = YahooFixtures.ReadBytes(YahooFixtures.AudUsd);

        PairRateData<YahooSeriesInfo> data = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooRateProviderOptions());

        Assert.HasCount(3, data.Observations);
        Assert.AreEqual("USD", data.Series.QuoteIsoCode);
        Assert.AreEqual(new DateOnly(2023, 1, 3), data.Observations[0].Date);
        Assert.AreEqual(0.6828m, data.Observations[0].Rate);
    }

    /// <summary>
    /// Verifies that a chart-error response throws <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenChartError_ShouldThrowFormatException()
    {
        byte[] json = YahooFixtures.ReadBytes(YahooFixtures.ErrorNotFound);

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooRateProviderOptions());
        });
    }

    /// <summary>
    /// Verifies that malformed JSON throws <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMalformedJson_ShouldThrowFormatException()
    {
        byte[] json = Encoding.UTF8.GetBytes("{ not json");

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooRateProviderOptions());
        });
    }

    /// <summary>
    /// Verifies that a Unix timestamp outside the range representable by <see cref="DateTimeOffset" /> causes only
    /// that point to be skipped, rather than crashing the fetch with an uncaught
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenTimestampOutOfRange_ShouldSkipThatPointNotThrow()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            """
            {
              "chart": {
                "result": [
                  {
                    "meta": { "currency": "USD" },
                    "timestamp": [ 999999999999, 1672704000 ],
                    "indicators": { "quote": [ { "close": [ 0.5, 0.6828 ] } ] }
                  }
                ],
                "error": null
              }
            }
            """);

        PairRateData<YahooSeriesInfo> data = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooRateProviderOptions());

        Assert.HasCount(1, data.Observations);
        Assert.AreEqual(new DateOnly(2023, 1, 3), data.Observations[0].Date);
        Assert.AreEqual(0.6828m, data.Observations[0].Rate);
    }

    /// <summary>
    /// Verifies that a close of a kind other than a number or a numeric string (a boolean, a non-numeric string, or an
    /// object) causes only that point to be skipped, rather than escaping as <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCloseIsNotNumeric_ShouldSkipThatPoint()
    {
        byte[] json = Chart("[ 1672617600, 1672704000, 1672790400, 1672876800 ]", "[ true, \"n/a\", { \"value\": 1 }, 0.6828 ]");

        PairRateData<YahooSeriesInfo> data = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooRateProviderOptions());

        Assert.HasCount(1, data.Observations);
        Assert.AreEqual(new DateOnly(2023, 1, 5), data.Observations[0].Date);
        Assert.AreEqual(0.6828m, data.Observations[0].Rate);
    }

    /// <summary>
    /// Verifies that a close written as a numeric string is read as the number it spells.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCloseIsNumericString_ShouldParseIt()
    {
        byte[] json = Chart("[ 1672704000 ]", "[ \"0.6828\" ]");

        PairRateData<YahooSeriesInfo> data = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooRateProviderOptions());

        Assert.HasCount(1, data.Observations);
        Assert.AreEqual(0.6828m, data.Observations[0].Rate);
    }

    /// <summary>
    /// Verifies that a timestamp of a kind other than a number or a numeric string causes only that point to be
    /// skipped, rather than escaping as <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTimestampIsNotNumeric_ShouldSkipThatPoint()
    {
        byte[] json = Chart("[ true, \"soon\", null, 1672704000 ]", "[ 0.5, 0.6, 0.7, 0.6828 ]");

        PairRateData<YahooSeriesInfo> data = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooRateProviderOptions());

        Assert.HasCount(1, data.Observations);
        Assert.AreEqual(new DateOnly(2023, 1, 3), data.Observations[0].Date);
    }

    /// <summary>
    /// Verifies that a timestamp written as a numeric string is read as the Unix time it spells.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTimestampIsNumericString_ShouldParseIt()
    {
        byte[] json = Chart("[ \"1672704000\" ]", "[ 0.6828 ]");

        PairRateData<YahooSeriesInfo> data = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooRateProviderOptions());

        Assert.HasCount(1, data.Observations);
        Assert.AreEqual(new DateOnly(2023, 1, 3), data.Observations[0].Date);
    }

    /// <summary>
    /// Verifies that a <c>chart</c> member that is not an object throws <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenChartIsNotObject_ShouldThrowFormatException()
    {
        byte[] json = Encoding.UTF8.GetBytes("""{ "chart": 5 }""");

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooRateProviderOptions());
        });
    }

    /// <summary>
    /// Verifies that a chart result that is not an object throws <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenResultIsNotObject_ShouldThrowFormatException()
    {
        byte[] json = Encoding.UTF8.GetBytes("""{ "chart": { "result": [ 5 ], "error": null } }""");

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooRateProviderOptions());
        });
    }

    /// <summary>
    /// Verifies that a quote entry that is not an object throws <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenQuoteIsNotObject_ShouldThrowFormatException()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            """{ "chart": { "result": [ { "timestamp": [ 1672704000 ], "indicators": { "quote": [ 5 ] } } ], "error": null } }""");

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooRateProviderOptions());
        });
    }

    /// <summary>
    /// Verifies that a <c>meta</c> member that is not an object is ignored, and the requested quote currency is used.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMetaIsNotObject_ShouldUseRequestedQuoteCurrency()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            """{ "chart": { "result": [ { "meta": 5, "timestamp": [ 1672704000 ], "indicators": { "quote": [ { "close": [ 0.6828 ] } ] } } ], "error": null } }""");

        PairRateData<YahooSeriesInfo> data = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooRateProviderOptions());

        Assert.AreEqual("USD", data.Series.QuoteIsoCode);
        Assert.HasCount(1, data.Observations);
    }

    /// <summary>
    /// Verifies that daily bars Yahoo stamps at midnight British Summer Time, 23:00 UTC on the previous day, are dated
    /// on their trading day in the exchange's time zone rather than on the UTC date of the stamp.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBarsAreStampedAtLondonSummerMidnight_ShouldDateEachBarInExchangeTimeZone()
    {
        byte[] json = YahooFixtures.ReadBytes(YahooFixtures.AudUsdSummer);
        CurrencyPairRequest request = new(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), new DateOnly(2023, 7, 1), new DateOnly(2023, 7, 31));

        PairRateData<YahooSeriesInfo> data = YahooChartResponseParser.Parse(json, request, Symbol, new YahooRateProviderOptions());

        CollectionAssert.AreEqual(
            new[]
            {
                new RateObservation(new DateOnly(2023, 7, 3), 0.6656m),
                new RateObservation(new DateOnly(2023, 7, 4), 0.6680m),
                new RateObservation(new DateOnly(2023, 7, 5), 0.6705m),
            },
            data.Observations.ToArray());
    }

    /// <summary>
    /// Verifies that a bar dated on its exchange trading day is kept by a request for that day alone, although the
    /// UTC date of its stamp is the day before.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRequestCoversOnlyTheBarsExchangeDate_ShouldKeepTheBar()
    {
        byte[] json = YahooFixtures.ReadBytes(YahooFixtures.AudUsdSummer);
        CurrencyPairRequest request = new(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), new DateOnly(2023, 7, 3), new DateOnly(2023, 7, 3));

        PairRateData<YahooSeriesInfo> data = YahooChartResponseParser.Parse(json, request, Symbol, new YahooRateProviderOptions());

        CollectionAssert.AreEqual(new[] { new RateObservation(new DateOnly(2023, 7, 3), 0.6656m) }, data.Observations.ToArray());
    }

    /// <summary>
    /// Verifies that when the exchange time zone is not one the system knows, each bar is dated by the response's
    /// <c>gmtoffset</c>.
    /// </summary>
    [TestMethod]
    public void Parse_WhenExchangeTimeZoneIsUnknown_ShouldDateBarsByGmtOffset()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            """
            {
              "chart": {
                "result": [
                  {
                    "meta": { "currency": "USD", "gmtoffset": 3600, "exchangeTimezoneName": "Bodu/NoSuchZone" },
                    "timestamp": [ 1688338800 ],
                    "indicators": { "quote": [ { "close": [ 0.6656 ] } ] }
                  }
                ],
                "error": null
              }
            }
            """);
        CurrencyPairRequest request = new(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), new DateOnly(2023, 7, 1), new DateOnly(2023, 7, 31));

        PairRateData<YahooSeriesInfo> data = YahooChartResponseParser.Parse(json, request, Symbol, new YahooRateProviderOptions());

        CollectionAssert.AreEqual(new[] { new RateObservation(new DateOnly(2023, 7, 3), 0.6656m) }, data.Observations.ToArray());
    }

    /// <summary>
    /// Verifies that a bar is dated by the exchange time zone's offset on its own date rather than by
    /// <c>gmtoffset</c>, which is the offset when the response was produced: a summer bar in a response produced in
    /// winter is still dated on its London trading day.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGmtOffsetDiffersFromTheBarsOffset_ShouldDateBarByExchangeTimeZone()
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById("Europe/London", out _))
            Assert.Inconclusive("The Europe/London time zone is not available on this system.");

        byte[] json = Encoding.UTF8.GetBytes(
            """
            {
              "chart": {
                "result": [
                  {
                    "meta": { "currency": "USD", "gmtoffset": 0, "timezone": "GMT", "exchangeTimezoneName": "Europe/London" },
                    "timestamp": [ 1688338800 ],
                    "indicators": { "quote": [ { "close": [ 0.6656 ] } ] }
                  }
                ],
                "error": null
              }
            }
            """);
        CurrencyPairRequest request = new(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), new DateOnly(2023, 7, 1), new DateOnly(2023, 7, 31));

        PairRateData<YahooSeriesInfo> data = YahooChartResponseParser.Parse(json, request, Symbol, new YahooRateProviderOptions());

        CollectionAssert.AreEqual(new[] { new RateObservation(new DateOnly(2023, 7, 3), 0.6656m) }, data.Observations.ToArray());
    }

    /// <summary>
    /// Builds a single-result chart response from the supplied timestamp and close arrays.
    /// </summary>
    /// <param name="timestamps">The JSON text of the <c>timestamp</c> array.</param>
    /// <param name="closes">The JSON text of the <c>close</c> array.</param>
    /// <returns>The UTF-8 response bytes.</returns>
    private static byte[] Chart(string timestamps, string closes) =>
        Encoding.UTF8.GetBytes(
            "{ \"chart\": { \"result\": [ { \"meta\": { \"currency\": \"USD\" }, \"timestamp\": " + timestamps
            + ", \"indicators\": { \"quote\": [ { \"close\": " + closes + " } ] } } ], \"error\": null } }");
}
