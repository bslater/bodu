// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxSpotRateHistoryResponseParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Ofx;

/// <summary>
/// Verifies that <see cref="OfxSpotRateHistoryResponseParser" /> maps an OFX response into dated observations,
/// restricting them to the requested range and rejecting malformed or empty payloads.
/// </summary>
[TestClass]
public class OfxSpotRateHistoryResponseParserTests
{
    private static readonly OfxExchangeRateOptions Options = new();

    /// <summary>
    /// Verifies that a well-formed response yields the in-range observations with the expected dates and rates, while
    /// out-of-range points are excluded.
    /// </summary>
    [TestMethod]
    public void Parse_WhenValid_ShouldReturnObservationsInRange()
    {
        byte[] json = Utf8(
            """
            {
              "HistoricalPoints": [
                { "PointInTime": 1672617600000, "InterbankRate": 0.6800 },
                { "PointInTime": 1672704000000, "InterbankRate": 0.6828 },
                { "PointInTime": 1675209600000, "InterbankRate": 0.7000 }
              ]
            }
            """);

        PairRateData<OfxSeriesInfo> data = OfxSpotRateHistoryResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);

        Assert.AreEqual(2, data.Observations.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 2), data.Observations[0].Date);
        Assert.AreEqual(0.6800m, data.Observations[0].Rate);
        Assert.AreEqual(new DateOnly(2023, 1, 3), data.Observations[1].Date);
        Assert.AreEqual(0.6828m, data.Observations[1].Rate);
    }

    /// <summary>
    /// Verifies that a numeric value supplied as a JSON string is still parsed.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRateIsNumericString_ShouldParseIt()
    {
        byte[] json = Utf8(
            """
            { "HistoricalPoints": [ { "PointInTime": "1672704000000", "InterbankRate": "0.6828" } ] }
            """);

        PairRateData<OfxSeriesInfo> data = OfxSpotRateHistoryResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);

        Assert.AreEqual(1, data.Observations.Count);
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
            _ = OfxSpotRateHistoryResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);
        });
    }

    /// <summary>
    /// Verifies that a response without a <c>HistoricalPoints</c> array throws <see cref="ExchangeRateFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMissingHistoricalPoints_ShouldThrowFormatException()
    {
        byte[] json = Utf8("""{ "CurrentInterbankRate": 0.6828 }""");

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = OfxSpotRateHistoryResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);
        });
    }

    /// <summary>
    /// Verifies that points with a missing or non-positive rate are skipped.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRateNotPositive_ShouldSkipPoint()
    {
        byte[] json = Utf8(
            """
            {
              "HistoricalPoints": [
                { "PointInTime": 1672617600000, "InterbankRate": 0 },
                { "PointInTime": 1672704000000 },
                { "PointInTime": 1672790400000, "InterbankRate": 0.6850 }
              ]
            }
            """);

        PairRateData<OfxSeriesInfo> data = OfxSpotRateHistoryResponseParser.Parse(json, Request(new(2023, 1, 1), new(2023, 1, 31)), Options);

        Assert.AreEqual(1, data.Observations.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 4), data.Observations[0].Date);
        Assert.AreEqual(0.6850m, data.Observations[0].Rate);
    }

    private static ExchangeRatePairRequest Request(DateOnly startDate, DateOnly endDate) =>
        new(new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD), startDate, endDate);

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);
}
