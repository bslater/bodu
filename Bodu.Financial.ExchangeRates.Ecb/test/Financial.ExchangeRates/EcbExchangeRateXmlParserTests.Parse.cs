// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateXmlParserTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Test;

namespace Bodu.Financial.ExchangeRates;

public partial class EcbExchangeRateXmlParserTests
{
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
    /// Verifies that a cell with a non-positive rate is skipped rather than emitted.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRateIsNonPositive_ShouldSkipCell()
    {
        string xml =
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
        string xml =
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
        string xml = $"<gesmes:Envelope {Namespaces}><Cube></Cube></gesmes:Envelope>";

        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
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
        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = Parse("<gesmes:Envelope><Cube>");
        });
    }
}
