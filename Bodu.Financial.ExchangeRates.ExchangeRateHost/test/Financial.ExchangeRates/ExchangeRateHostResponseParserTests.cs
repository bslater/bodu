// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateHostResponseParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="ExchangeRateHostResponseParser" /> maps exchangerate.host time-series, single-date, and
/// error responses to the expected observations or exceptions.
/// </summary>
[TestClass]
public sealed class ExchangeRateHostResponseParserTests
{
    /// <summary>The options used while parsing fixtures.</summary>
    private static readonly ExchangeRateHostRateProviderOptions Options = new() { ApiKey = "test-key" };

    /// <summary>
    /// Verifies that a time-series response yields one observation per in-range date.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTimeSeries_ShouldReturnObservationsInRange()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD),
            new DateOnly(2023, 1, 2),
            new DateOnly(2023, 1, 6));
        byte[] json = ExchangeRateHostFixtures.ReadBytes(ExchangeRateHostFixtures.EurUsdTimeSeries);

        PairRateData<ExchangeRateHostSeriesInfo> data = ExchangeRateHostResponseParser.Parse(json, request, "EUR", "USD", Options);

        Assert.AreEqual(5, data.Observations.Count);
        Assert.AreEqual(1.0546m, data.Observations.Single(o => o.Date == new DateOnly(2023, 1, 3)).Rate);
        Assert.AreEqual("EUR", data.Series.SourceIsoCode);
    }

    /// <summary>
    /// Verifies that the example <c>timeframe</c> response the exchangerate.host API documents (the <c>timeframe</c>
    /// flag, the source currency, the inclusive bounds, and quotes keyed by date) yields the requested pair's rate for
    /// every date it carries.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDocumentedTimeframeResponse_ShouldReturnEveryDatedRateForThePair()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.USD, CurrencyCode.CHF),
            new DateOnly(2012, 5, 1),
            new DateOnly(2012, 5, 3));
        byte[] json = ExchangeRateHostFixtures.ReadBytes(ExchangeRateHostFixtures.UsdTimeframeExample);

        PairRateData<ExchangeRateHostSeriesInfo> data = ExchangeRateHostResponseParser.Parse(json, request, "USD", "CHF", Options);

        CollectionAssert.AreEqual(
            new[]
            {
                new RateObservation(new DateOnly(2012, 5, 1), 0.91957m),
                new RateObservation(new DateOnly(2012, 5, 2), 0.92036m),
                new RateObservation(new DateOnly(2012, 5, 3), 0.91525m),
            },
            data.Observations.OrderBy(o => o.Date).ToArray());
        Assert.AreEqual("USD", data.Series.SourceIsoCode);
        Assert.AreEqual("CHF", data.Series.QuoteIsoCode);
    }

    /// <summary>
    /// Verifies that a time-series response drops dates outside the requested inclusive range.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTimeSeriesRangeNarrow_ShouldRestrictToRange()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD),
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 3));
        byte[] json = ExchangeRateHostFixtures.ReadBytes(ExchangeRateHostFixtures.EurUsdTimeSeries);

        PairRateData<ExchangeRateHostSeriesInfo> data = ExchangeRateHostResponseParser.Parse(json, request, "EUR", "USD", Options);

        Assert.AreEqual(1, data.Observations.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 3), data.Observations[0].Date);
    }

    /// <summary>
    /// Verifies that a single-date response yields the one dated observation.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSingleDate_ShouldReturnOneObservation()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD),
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 3));
        byte[] json = ExchangeRateHostFixtures.ReadBytes(ExchangeRateHostFixtures.EurUsdHistorical);

        PairRateData<ExchangeRateHostSeriesInfo> data = ExchangeRateHostResponseParser.Parse(json, request, "EUR", "USD", Options);

        Assert.AreEqual(1, data.Observations.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 3), data.Observations[0].Date);
        Assert.AreEqual(1.0546m, data.Observations[0].Rate);
    }

    /// <summary>
    /// Verifies that an error response is translated into an <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenErrorResponse_ShouldThrowExchangeRateFormatException()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD),
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 3));
        byte[] json = ExchangeRateHostFixtures.ReadBytes(ExchangeRateHostFixtures.ErrorInvalidKey);

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = ExchangeRateHostResponseParser.Parse(json, request, "EUR", "USD", Options);
        });
    }

    /// <summary>
    /// Verifies that malformed JSON is translated into an <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMalformedJson_ShouldThrowExchangeRateFormatException()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD),
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 3));
        byte[] json = Encoding.UTF8.GetBytes("{ not json");

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = ExchangeRateHostResponseParser.Parse(json, request, "EUR", "USD", Options);
        });
    }

    /// <summary>
    /// Verifies that a time-series rate given as a number or a numeric string is read, and one that is not positive
    /// or not numeric is skipped.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTimeSeriesRatesVaryInKind_ShouldReadOnlyPositiveNumbers()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD),
            new DateOnly(2023, 1, 1),
            new DateOnly(2023, 1, 31));
        byte[] json = Encoding.UTF8.GetBytes(
            """
            {
              "success": true,
              "timeseries": true,
              "source": "EUR",
              "quotes": {
                "2023-01-02": { "EURUSD": "1.0668" },
                "2023-01-03": { "EURUSD": 1.0546 },
                "2023-01-04": { "EURUSD": 0 },
                "2023-01-05": { "EURUSD": -1.05 },
                "2023-01-06": { "EURUSD": null },
                "2023-01-07": { "EURUSD": "soon" },
                "2023-01-08": { "EURUSD": true }
              }
            }
            """);

        PairRateData<ExchangeRateHostSeriesInfo> data = ExchangeRateHostResponseParser.Parse(json, request, "EUR", "USD", Options);

        Assert.AreEqual(2, data.Observations.Count);
        Assert.AreEqual(1.0668m, data.Observations.Single(o => o.Date == new DateOnly(2023, 1, 2)).Rate);
        Assert.AreEqual(1.0546m, data.Observations.Single(o => o.Date == new DateOnly(2023, 1, 3)).Rate);
    }

    /// <summary>
    /// Verifies that a single-date rate given as a numeric string is read.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSingleDateRateIsNumericString_ShouldReadIt()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD),
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 3));
        byte[] json = Encoding.UTF8.GetBytes(
            """
            { "success": true, "historical": true, "date": "2023-01-03", "source": "EUR", "quotes": { "EURUSD": "1.0546" } }
            """);

        PairRateData<ExchangeRateHostSeriesInfo> data = ExchangeRateHostResponseParser.Parse(json, request, "EUR", "USD", Options);

        Assert.AreEqual(1, data.Observations.Count);
        Assert.AreEqual(1.0546m, data.Observations[0].Rate);
    }
}
