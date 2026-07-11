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
}
