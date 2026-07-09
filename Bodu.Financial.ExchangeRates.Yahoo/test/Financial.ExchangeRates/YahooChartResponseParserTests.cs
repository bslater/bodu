// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooChartResponseParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the behaviour of <see cref="YahooChartResponseParser" /> over fixture and malformed input.
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

        PairRateData<YahooSeriesInfo> data = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooExchangeRateOptions());

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
            _ = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooExchangeRateOptions());
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
            _ = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooExchangeRateOptions());
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

        PairRateData<YahooSeriesInfo> data = YahooChartResponseParser.Parse(json, CreateRequest(), Symbol, new YahooExchangeRateOptions());

        Assert.HasCount(1, data.Observations);
        Assert.AreEqual(new DateOnly(2023, 1, 3), data.Observations[0].Date);
        Assert.AreEqual(0.6828m, data.Observations[0].Rate);
    }
}
