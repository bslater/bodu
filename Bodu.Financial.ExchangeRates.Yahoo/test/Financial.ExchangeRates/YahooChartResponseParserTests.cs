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
    /// Builds a chart request for the AUD/USD fixture spanning January 2023.
    /// </summary>
    /// <returns>The chart request.</returns>
    private static YahooChartRequest CreateRequest() =>
        new(new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD), "AUDUSD=X", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));

    /// <summary>
    /// Verifies that a valid chart parses to the present close observations, skipping the null-close day.
    /// </summary>
    [TestMethod]
    public void Parse_WhenValidChart_ShouldReturnPresentObservations()
    {
        byte[] json = YahooFixtures.ReadBytes(YahooFixtures.AudUsd);

        YahooExchangeRateChart chart = YahooChartResponseParser.Parse(json, CreateRequest(), new YahooExchangeRateOptions());

        Assert.HasCount(3, chart.Observations);
        Assert.AreEqual("USD", chart.QuoteIsoCode);
        Assert.AreEqual(new DateOnly(2023, 1, 3), chart.Observations[0].Date);
        Assert.AreEqual(0.6828m, chart.Observations[0].Rate);
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
            _ = YahooChartResponseParser.Parse(json, CreateRequest(), new YahooExchangeRateOptions());
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
            _ = YahooChartResponseParser.Parse(json, CreateRequest(), new YahooExchangeRateOptions());
        });
    }
}
