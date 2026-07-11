// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfResponseParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="ImfResponseParser" /> maps IMF SDMX-JSON data messages to the expected daily observations
/// or exceptions.
/// </summary>
[TestClass]
public sealed class ImfResponseParserTests
{
    /// <summary>The SDMX series key recorded on the parsed series metadata.</summary>
    private const string SeriesKey = "D.GB.ENDE_XDC_USD_RATE";

    /// <summary>
    /// Verifies that a daily SDMX-JSON message yields one observation per in-range business day, keyed by the
    /// observation-dimension time values.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDailyMessage_ShouldReturnObservationsPerDay()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.USD, CurrencyCode.GBP),
            new DateOnly(2023, 1, 2),
            new DateOnly(2023, 1, 6));
        byte[] json = ImfFixtures.ReadBytes(ImfFixtures.UsdGbpDaily2023);

        PairRateData<ImfSeriesInfo> data = ImfResponseParser.Parse(json, request, SeriesKey);

        Assert.AreEqual(5, data.Observations.Count);
        Assert.AreEqual(0.8267m, data.Observations.Single(o => o.Date == new DateOnly(2023, 1, 3)).Rate);
        Assert.AreEqual(0.8280m, data.Observations.Single(o => o.Date == new DateOnly(2023, 1, 6)).Rate);
        Assert.AreEqual(SeriesKey, data.Series.SeriesKey);
    }

    /// <summary>
    /// Verifies that observations outside the requested inclusive range are dropped.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRangeNarrow_ShouldRestrictToRange()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.USD, CurrencyCode.GBP),
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 3));
        byte[] json = ImfFixtures.ReadBytes(ImfFixtures.UsdGbpDaily2023);

        PairRateData<ImfSeriesInfo> data = ImfResponseParser.Parse(json, request, SeriesKey);

        Assert.AreEqual(1, data.Observations.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 3), data.Observations[0].Date);
    }

    /// <summary>
    /// Verifies that the singular <c>data.structure</c> shape and a numeric-string observation value are tolerated.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSingularStructureAndStringValue_ShouldReturnObservation()
    {
        const string body = """
            {
              "data": {
                "structure": {
                  "dimensions": {
                    "observation": [
                      { "id": "TIME_PERIOD", "values": [ { "id": "2023-01-04" } ] }
                    ]
                  }
                },
                "dataSets": [
                  { "series": { "0:0:0": { "observations": { "0": [ "0.8300" ] } } } }
                ]
              }
            }
            """;
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.USD, CurrencyCode.GBP),
            new DateOnly(2023, 1, 1),
            new DateOnly(2023, 1, 31));

        PairRateData<ImfSeriesInfo> data = ImfResponseParser.Parse(Encoding.UTF8.GetBytes(body), request, SeriesKey);

        Assert.AreEqual(1, data.Observations.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 4), data.Observations[0].Date);
        Assert.AreEqual(0.8300m, data.Observations[0].Rate);
    }

    /// <summary>
    /// Verifies that malformed JSON is translated into an <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMalformedJson_ShouldThrowExchangeRateFormatException()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.USD, CurrencyCode.GBP),
            new DateOnly(2023, 1, 1),
            new DateOnly(2023, 1, 1));
        byte[] json = Encoding.UTF8.GetBytes("{ not json");

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = ImfResponseParser.Parse(json, request, SeriesKey);
        });
    }

    /// <summary>
    /// Verifies that a response missing the SDMX data path is translated into an
    /// <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDataPathAbsent_ShouldThrowExchangeRateFormatException()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.USD, CurrencyCode.GBP),
            new DateOnly(2023, 1, 1),
            new DateOnly(2023, 1, 1));
        byte[] json = ImfFixtures.ReadBytes(ImfFixtures.ErrorEmpty);

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = ImfResponseParser.Parse(json, request, SeriesKey);
        });
    }
}
