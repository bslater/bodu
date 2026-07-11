// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfResponseParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="ImfResponseParser" /> maps IMF SDMX-JSON CompactData responses to the expected monthly
/// observations or exceptions.
/// </summary>
[TestClass]
public sealed class ImfResponseParserTests
{
    /// <summary>The SDMX series key recorded on the parsed series metadata.</summary>
    private const string SeriesKey = "M.GB.ENDE_XDC_USD_RATE";

    /// <summary>
    /// Verifies that a monthly Obs array yields one observation per in-range month, mapped to the month start.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMonthlyObsArray_ShouldReturnObservationsAtMonthStart()
    {
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.USD, CurrencyCode.GBP),
            new DateOnly(2023, 1, 1),
            new DateOnly(2023, 3, 1));
        byte[] json = ImfFixtures.ReadBytes(ImfFixtures.UsdGbp2023);

        PairRateData<ImfSeriesInfo> data = ImfResponseParser.Parse(json, request, SeriesKey);

        Assert.AreEqual(3, data.Observations.Count);
        Assert.AreEqual(0.8267m, data.Observations.Single(o => o.Date == new DateOnly(2023, 1, 1)).Rate);
        Assert.AreEqual(new DateOnly(2023, 3, 1), data.Observations.Single(o => o.Date == new DateOnly(2023, 3, 1)).Date);
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
            new DateOnly(2023, 1, 1),
            new DateOnly(2023, 1, 1));
        byte[] json = ImfFixtures.ReadBytes(ImfFixtures.UsdGbp2023);

        PairRateData<ImfSeriesInfo> data = ImfResponseParser.Parse(json, request, SeriesKey);

        Assert.AreEqual(1, data.Observations.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 1), data.Observations[0].Date);
    }

    /// <summary>
    /// Verifies that a single Obs object (not an array) yields the one dated observation.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSingleObsObject_ShouldReturnOneObservation()
    {
        const string body = """
            {
              "CompactData": {
                "DataSet": {
                  "Series": {
                    "@FREQ": "M",
                    "@REF_AREA": "GB",
                    "Obs": { "@TIME_PERIOD": "2023-02", "@OBS_VALUE": "0.8321" }
                  }
                }
              }
            }
            """;
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.USD, CurrencyCode.GBP),
            new DateOnly(2023, 1, 1),
            new DateOnly(2023, 3, 1));

        PairRateData<ImfSeriesInfo> data = ImfResponseParser.Parse(Encoding.UTF8.GetBytes(body), request, SeriesKey);

        Assert.AreEqual(1, data.Observations.Count);
        Assert.AreEqual(new DateOnly(2023, 2, 1), data.Observations[0].Date);
        Assert.AreEqual(0.8321m, data.Observations[0].Rate);
    }

    /// <summary>
    /// Verifies that a numeric (non-string) <c>@OBS_VALUE</c> is tolerated.
    /// </summary>
    [TestMethod]
    public void Parse_WhenObsValueIsNumber_ShouldReturnObservation()
    {
        const string body = """
            {
              "CompactData": {
                "DataSet": {
                  "Series": {
                    "Obs": { "@TIME_PERIOD": "2023-01", "@OBS_VALUE": 0.8267 }
                  }
                }
              }
            }
            """;
        CurrencyPairRequest request = new(
            new CurrencyPair(CurrencyCode.USD, CurrencyCode.GBP),
            new DateOnly(2023, 1, 1),
            new DateOnly(2023, 1, 1));

        PairRateData<ImfSeriesInfo> data = ImfResponseParser.Parse(Encoding.UTF8.GetBytes(body), request, SeriesKey);

        Assert.AreEqual(1, data.Observations.Count);
        Assert.AreEqual(0.8267m, data.Observations[0].Rate);
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
    /// Verifies that a response missing the CompactData path is translated into an
    /// <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCompactDataPathAbsent_ShouldThrowExchangeRateFormatException()
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
