// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateXmlParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using System.Text;
using Bodu.Test;

namespace Bodu.Financial.ExchangeRates.Ecb;

/// <summary>
/// Verifies that <see cref="EcbExchangeRateXmlParser" /> decodes the ECB <c>eurofxref</c> XML layout correctly and
/// rejects malformed or empty feeds.
/// </summary>
[TestClass]
public class EcbExchangeRateXmlParserTests
{
    /// <summary>
    /// The ECB <c>eurofxref</c> namespace declarations shared by the inline fixtures.
    /// </summary>
    private const string Namespaces =
        "xmlns:gesmes=\"http://www.gesmes.org/xml/2002-08-01\" xmlns=\"http://www.ecb.int/vocabulary/2002-08-01/eurofxref\"";

    /// <summary>
    /// Verifies that parsing the sample feed yields one observation per present currency cell.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Parse_WhenSampleFeed_ShouldYieldOneObservationPerCell()
    {
        EcbExchangeRateTable table = ParseSample();

        Assert.AreEqual(6, table.Observations.Count);
    }

    /// <summary>
    /// Verifies that a parsed cell carries the euro reference rate quoted against the cell's currency on its date.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSampleFeed_ShouldReadDateCurrencyAndRate()
    {
        EcbExchangeRateTable table = ParseSample();

        EcbExchangeRateObservation usd = table.Observations
            .Single(o => o.Date == new DateOnly(2023, 1, 3) && o.CurrencyCode == "USD");

        Assert.AreEqual(1.0545m, usd.Rate);
    }

    /// <summary>
    /// Verifies that the distinct quote currencies are surfaced as EUR-based series.
    /// </summary>
    [TestMethod]
    public void GetSeriesInfo_WhenSampleFeed_ShouldReturnDistinctEurPairs()
    {
        EcbExchangeRateTable table = ParseSample();

        var pairs = table.GetSeriesInfo().Select(s => s.Pair).ToList();

        Assert.AreEqual(3, pairs.Count);
        CollectionAssert.Contains(pairs, new ExchangeRatePair("EUR", "USD"));
        CollectionAssert.Contains(pairs, new ExchangeRatePair("EUR", "JPY"));
        CollectionAssert.Contains(pairs, new ExchangeRatePair("EUR", "GBP"));
    }

    /// <summary>
    /// Verifies that a cell with a non-positive rate is skipped rather than emitted.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRateIsNonPositive_ShouldSkipCell()
    {
        var xml =
            $"<gesmes:Envelope {Namespaces}><Cube><Cube time=\"2023-01-03\">" +
            "<Cube currency=\"USD\" rate=\"0\"/><Cube currency=\"JPY\" rate=\"140.06\"/>" +
            "</Cube></Cube></gesmes:Envelope>";

        EcbExchangeRateTable table = Parse(xml);

        Assert.AreEqual(1, table.Observations.Count);
        Assert.AreEqual("JPY", table.Observations[0].CurrencyCode);
    }

    /// <summary>
    /// Verifies that an applicable currency alias is mapped before the ISO code is validated.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCurrencyAliasConfigured_ShouldMapToIsoCode()
    {
        var xml =
            $"<gesmes:Envelope {Namespaces}><Cube><Cube time=\"2023-01-03\">" +
            "<Cube currency=\"SDR\" rate=\"1.2500\"/></Cube></Cube></gesmes:Envelope>";
        EcbExchangeRateOptions options = new();
        options.CurrencyAliases["SDR"] = "XDR";

        EcbExchangeRateTable table = EcbExchangeRateXmlParser.Parse(ToStream(xml), options);

        Assert.AreEqual(1, table.Observations.Count);
        Assert.AreEqual("XDR", table.Observations[0].CurrencyCode);
    }

    /// <summary>
    /// Verifies that a feed with no dated rows is reported as a layout failure.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNoDatedRows_ShouldThrowFormatException()
    {
        var xml = $"<gesmes:Envelope {Namespaces}><Cube></Cube></gesmes:Envelope>";

        _ = Assert.ThrowsExactly<EcbExchangeRateFormatException>(() =>
        {
            _ = Parse(xml);
        });
    }

    /// <summary>
    /// Verifies that content which is not well-formed XML is reported as a format failure.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMalformedXml_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<EcbExchangeRateFormatException>(() =>
        {
            _ = Parse("<gesmes:Envelope><Cube>");
        });
    }

    /// <summary>
    /// Parses the embedded sample feed with default options.
    /// </summary>
    /// <returns>The parsed table.</returns>
    private static EcbExchangeRateTable ParseSample() =>
        EcbExchangeRateXmlParser.Parse(EcbFixtures.OpenStream(EcbFixtures.Sample), new EcbExchangeRateOptions());

    /// <summary>
    /// Parses inline XML with default options.
    /// </summary>
    /// <param name="xml">The XML to parse.</param>
    /// <returns>The parsed table.</returns>
    private static EcbExchangeRateTable Parse(string xml) =>
        EcbExchangeRateXmlParser.Parse(ToStream(xml), new EcbExchangeRateOptions());

    /// <summary>
    /// Materializes XML text as a UTF-8 stream.
    /// </summary>
    /// <param name="xml">The XML text.</param>
    /// <returns>A readable stream over the encoded XML.</returns>
    private static MemoryStream ToStream(string xml) =>
        new(Encoding.UTF8.GetBytes(xml), writable: false);
}
