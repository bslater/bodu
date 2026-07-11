// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FredResponseParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="FredResponseParser" /> maps FRED series-observations responses to the expected
/// observations or exceptions.
/// </summary>
[TestClass]
public sealed class FredResponseParserTests
{
    /// <summary>
    /// Verifies that an observations response skips FRED's <c>"."</c> missing-value rows.
    /// </summary>
    [TestMethod]
    public void Parse_WhenValueIsMissingMarker_ShouldSkipObservation()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD),
            new DateOnly(2023, 1, 2),
            new DateOnly(2023, 1, 6));
        byte[] json = FredFixtures.ReadBytes(FredFixtures.DexUsEu);

        PairRateData<FredSeriesInfo> data = FredResponseParser.Parse(json, request, "DEXUSEU");

        // The 2023-01-02 row carries "." and is dropped; four valid rows remain.
        Assert.AreEqual(4, data.Observations.Count);
        Assert.IsFalse(data.Observations.Any(o => o.Date == new DateOnly(2023, 1, 2)));
        Assert.AreEqual(1.0546m, data.Observations.Single(o => o.Date == new DateOnly(2023, 1, 3)).Rate);
        Assert.AreEqual("DEXUSEU", data.Series.SeriesId);
    }

    /// <summary>
    /// Verifies that an observations response drops dates outside the requested inclusive range.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRangeNarrow_ShouldRestrictToRange()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD),
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 3));
        byte[] json = FredFixtures.ReadBytes(FredFixtures.DexUsEu);

        PairRateData<FredSeriesInfo> data = FredResponseParser.Parse(json, request, "DEXUSEU");

        Assert.AreEqual(1, data.Observations.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 3), data.Observations[0].Date);
        Assert.AreEqual(1.0546m, data.Observations[0].Rate);
    }

    /// <summary>
    /// Verifies that only strictly positive values are retained.
    /// </summary>
    [TestMethod]
    public void Parse_WhenValueNotPositive_ShouldSkipObservation()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD),
            new DateOnly(2023, 1, 2),
            new DateOnly(2023, 1, 3));
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"observations\":[" +
            "{\"date\":\"2023-01-02\",\"value\":\"0\"}," +
            "{\"date\":\"2023-01-03\",\"value\":\"1.0546\"}]}");

        PairRateData<FredSeriesInfo> data = FredResponseParser.Parse(json, request, "DEXUSEU");

        Assert.AreEqual(1, data.Observations.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 3), data.Observations[0].Date);
    }

    /// <summary>
    /// Verifies that a response omitting the observations array is translated into an
    /// <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenObservationsMissing_ShouldThrowExchangeRateFormatException()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD),
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 3));
        byte[] json = FredFixtures.ReadBytes(FredFixtures.ErrorEmpty);

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = FredResponseParser.Parse(json, request, "DEXUSEU");
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
            _ = FredResponseParser.Parse(json, request, "DEXUSEU");
        });
    }
}
