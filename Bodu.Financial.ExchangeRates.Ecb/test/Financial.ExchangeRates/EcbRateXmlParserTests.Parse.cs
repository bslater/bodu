// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateXmlParserTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using System.Xml;
using System.Xml.Linq;
using Bodu.Test;
using Bodu.Test.IO;

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateXmlParserTests
{
    /// <summary>
    /// Verifies that parsing the sample feed yields one observation per present currency cell.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Parse_WhenSampleFeed_ShouldYieldOneObservationPerCell()
    {
        EcbRateTable table = ParseSample();

        Assert.HasCount(6, table.Observations);
    }

    /// <summary>
    /// Verifies that a parsed cell carries the euro reference rate quoted against the cell's currency on its date.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSampleFeed_ShouldReadDateCurrencyAndRate()
    {
        EcbRateTable table = ParseSample();

        EcbRateObservation usd = table.Observations
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

        EcbRateTable table = Parse(xml);

        Assert.HasCount(1, table.Observations);
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
        EcbRateProviderOptions options = new();
        options.CurrencyAliases["SDR"] = "XDR";

        EcbRateTable table = EcbRateXmlParser.Parse(ToStream(xml), options);

        Assert.HasCount(1, table.Observations);
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

    /// <summary>
    /// Verifies that a <see langword="null" /> stream is rejected with <see cref="ArgumentNullException" /> naming it.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EcbRateXmlParser.Parse((Stream)null!, new EcbRateProviderOptions());
        });

        Assert.AreEqual("stream", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the stream overload rejects <see langword="null" /> options with
    /// <see cref="ArgumentNullException" /> naming them.
    /// </summary>
    [TestMethod]
    public void Parse_WhenOptionsIsNull_ForStream_ShouldThrowArgumentNullException()
    {
        using MemoryStream stream = EcbFixtures.OpenStream(EcbFixtures.Sample);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EcbRateXmlParser.Parse(stream, null!);
        });

        Assert.AreEqual("options", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a cell missing its currency or its rate, or carrying a blank one, is skipped rather than
    /// emitted, while the usable cell beside it is kept.
    /// </summary>
    /// <param name="cell">The unusable cell under test.</param>
    [TestMethod]
    [DataRow("<Cube rate=\"1.0545\"/>")]
    [DataRow("<Cube currency=\"USD\"/>")]
    [DataRow("<Cube currency=\"\" rate=\"1.0545\"/>")]
    [DataRow("<Cube currency=\"USD\" rate=\" \"/>")]
    public void Parse_WhenCellLacksCurrencyOrRate_ShouldSkipCell(string cell)
    {
        string xml =
            $"<gesmes:Envelope {Namespaces}><Cube><Cube time=\"2023-01-03\">" +
            cell + "<Cube currency=\"JPY\" rate=\"140.06\"/>" +
            "</Cube></Cube></gesmes:Envelope>";

        EcbRateTable table = Parse(xml);

        Assert.HasCount(1, table.Observations);
        Assert.AreEqual("JPY", table.Observations[0].CurrencyCode);
    }

    /// <summary>
    /// Verifies that a cell whose currency is not exactly three uppercase ASCII letters is skipped rather than
    /// emitted, whichever of the three positions or the length breaks the shape.
    /// </summary>
    /// <param name="currency">The malformed currency label under test.</param>
    [TestMethod]
    [DataRow("US")]
    [DataRow("USDX")]
    [DataRow("usd")]
    [DataRow("U$D")]
    [DataRow("USd")]
    public void Parse_WhenCurrencyIsNotAnIsoCode_ShouldSkipCell(string currency)
    {
        string xml =
            $"<gesmes:Envelope {Namespaces}><Cube><Cube time=\"2023-01-03\">" +
            $"<Cube currency=\"{currency}\" rate=\"1.0545\"/><Cube currency=\"JPY\" rate=\"140.06\"/>" +
            "</Cube></Cube></gesmes:Envelope>";

        EcbRateTable table = Parse(xml);

        Assert.HasCount(1, table.Observations);
        Assert.AreEqual("JPY", table.Observations[0].CurrencyCode);
    }

    /// <summary>
    /// Verifies that a day whose <c>time</c> attribute is not a calendar date is skipped with its cells, while the
    /// well-formed day beside it is kept.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDayTimeIsUnparsable_ShouldSkipTheDay()
    {
        string xml =
            $"<gesmes:Envelope {Namespaces}><Cube>" +
            "<Cube time=\"2023-13-45\"><Cube currency=\"USD\" rate=\"1.0600\"/></Cube>" +
            "<Cube time=\"2023-01-03\"><Cube currency=\"JPY\" rate=\"140.06\"/></Cube>" +
            "</Cube></gesmes:Envelope>";

        EcbRateTable table = Parse(xml);

        Assert.HasCount(1, table.Observations);
        Assert.AreEqual(new EcbRateObservation(new DateOnly(2023, 1, 3), "JPY", 140.06m), table.Observations[0]);
    }

    /// <summary>
    /// Verifies that an empty day element, one with no cells, counts as a dated row and yields nothing, so a feed
    /// of such days is not reported as a layout failure.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDayCubeIsEmpty_ShouldYieldNoObservationsForIt()
    {
        string xml =
            $"<gesmes:Envelope {Namespaces}><Cube>" +
            "<Cube time=\"2023-01-04\"/>" +
            "<Cube time=\"2023-01-03\"><Cube currency=\"JPY\" rate=\"140.06\"/></Cube>" +
            "</Cube></gesmes:Envelope>";

        EcbRateTable table = Parse(xml);

        Assert.HasCount(1, table.Observations);
        Assert.AreEqual(new DateOnly(2023, 1, 3), table.Observations[0].Date);
    }

    /// <summary>
    /// Verifies that a stream the parser cannot seek, which it reads through the document model rather than
    /// forward, yields the same table as the seekable parse of the same bytes.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStreamCannotSeek_ShouldYieldTheSameTableAsASeekableStream()
    {
        using NonSeekableStream stream = new(EcbFixtures.ReadBytes(EcbFixtures.Sample));

        EcbRateTable table = EcbRateXmlParser.Parse(stream, new EcbRateProviderOptions());

        CollectionAssert.AreEqual(ParseSample().Observations.ToArray(), table.Observations.ToArray());
    }

    /// <summary>
    /// Verifies that a stream the parser cannot seek still reports content that is not well-formed XML as a format
    /// failure carrying the XML error, since the document load is what reads such a stream.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStreamCannotSeekAndXmlIsMalformed_ShouldThrowFormatException()
    {
        using NonSeekableStream stream = new(Encoding.UTF8.GetBytes("<gesmes:Envelope><Cube>"));

        var ex = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = EcbRateXmlParser.Parse(stream, new EcbRateProviderOptions());
        });

        Assert.IsInstanceOfType<XmlException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that a feed whose day elements nest inside one another, which the forward read declines, is read
    /// through the document model: every day element is found wherever it sits, each day's cells are the
    /// <c>Cube</c> elements directly inside it, and the result is what parsing the loaded document yields.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDayCubesNest_ShouldYieldTheDocumentModelParse()
    {
        string xml =
            $"<gesmes:Envelope {Namespaces}><Cube><Cube time=\"2023-01-04\">" +
            "<Cube currency=\"USD\" rate=\"1.0600\"/>" +
            "<Cube time=\"2023-01-03\"><Cube currency=\"USD\" rate=\"1.0545\"/></Cube>" +
            "</Cube></Cube></gesmes:Envelope>";
        EcbRateObservation[] expected =
        [
            new(new DateOnly(2023, 1, 4), "USD", 1.0600m),
            new(new DateOnly(2023, 1, 3), "USD", 1.0545m),
        ];

        EcbRateTable table = Parse(xml);

        CollectionAssert.AreEqual(expected, table.Observations.ToArray());
        CollectionAssert.AreEqual(
            EcbRateXmlParser.Parse(XDocument.Parse(xml), new EcbRateProviderOptions()).Observations.ToArray(),
            table.Observations.ToArray());
    }

    /// <summary>
    /// Verifies that a day element nested below another element inside an open day, a shape the forward read also
    /// declines, is read through the document model, which finds the inner day and takes the outer one as having no
    /// cells of its own.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDayCubeNestsBelowAnotherElementInsideADay_ShouldYieldTheDocumentModelParse()
    {
        string xml =
            $"<gesmes:Envelope {Namespaces}><Cube><Cube time=\"2023-01-04\"><Group>" +
            "<Cube time=\"2023-01-03\"><Cube currency=\"USD\" rate=\"1.0545\"/></Cube>" +
            "</Group></Cube></Cube></gesmes:Envelope>";

        EcbRateTable table = Parse(xml);

        Assert.HasCount(1, table.Observations);
        Assert.AreEqual(new EcbRateObservation(new DateOnly(2023, 1, 3), "USD", 1.0545m), table.Observations[0]);
    }

    /// <summary>
    /// Verifies that when the forward read declines a seekable stream, the document load starts where the stream was
    /// positioned when parsing began rather than at its origin.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDayCubesNestAndStreamStartsPastItsOrigin_ShouldRewindToWhereParsingBegan()
    {
        byte[] prefix = Encoding.UTF8.GetBytes("not part of the feed ");
        byte[] feed = Encoding.UTF8.GetBytes(
            $"<gesmes:Envelope {Namespaces}><Cube><Cube time=\"2023-01-04\">" +
            "<Cube currency=\"USD\" rate=\"1.0600\"/>" +
            "<Cube time=\"2023-01-03\"><Cube currency=\"USD\" rate=\"1.0545\"/></Cube>" +
            "</Cube></Cube></gesmes:Envelope>");
        using MemoryStream stream = new([.. prefix, .. feed], writable: false);
        stream.Position = prefix.Length;

        EcbRateTable table = EcbRateXmlParser.Parse(stream, new EcbRateProviderOptions());

        Assert.HasCount(2, table.Observations);
    }

    /// <summary>
    /// Verifies that the document overload rejects a <see langword="null" /> document with
    /// <see cref="ArgumentNullException" /> naming it.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDocumentIsNull_ForXDocument_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EcbRateXmlParser.Parse((XDocument)null!, new EcbRateProviderOptions());
        });

        Assert.AreEqual("document", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the document overload rejects <see langword="null" /> options with
    /// <see cref="ArgumentNullException" /> naming them.
    /// </summary>
    [TestMethod]
    public void Parse_WhenOptionsIsNull_ForXDocument_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EcbRateXmlParser.Parse(new XDocument(), null!);
        });

        Assert.AreEqual("options", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a document with no root element is reported as a missing envelope.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDocumentHasNoRoot_ForXDocument_ShouldThrowFormatException()
    {
        var ex = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = EcbRateXmlParser.Parse(new XDocument(), new EcbRateProviderOptions());
        });

        Assert.AreEqual(EcbResourceStrings.Format_Invalid_EcbEnvelopeMissing, ex.Message);
    }

    /// <summary>
    /// Verifies that a document with no dated day element is reported as a layout failure with no data rows.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNoDatedRows_ForXDocument_ShouldThrowFormatException()
    {
        XDocument document = XDocument.Parse($"<gesmes:Envelope {Namespaces}><Cube/></gesmes:Envelope>");

        var ex = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = EcbRateXmlParser.Parse(document, new EcbRateProviderOptions());
        });

        Assert.AreEqual(EcbResourceStrings.Format_Invalid_EcbNoDataRows, ex.Message);
    }

    /// <summary>
    /// Verifies that parsing the loaded sample document yields the table the forward read of the same feed yields.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSampleFeed_ForXDocument_ShouldYieldTheStreamingParse()
    {
        using MemoryStream stream = EcbFixtures.OpenStream(EcbFixtures.Sample);
        XDocument document = XDocument.Load(stream);

        EcbRateTable table = EcbRateXmlParser.Parse(document, new EcbRateProviderOptions());

        CollectionAssert.AreEqual(ParseSample().Observations.ToArray(), table.Observations.ToArray());
    }

    /// <summary>
    /// Verifies that the document overload skips a day whose <c>time</c> attribute is not a calendar date, with its
    /// cells, while keeping the well-formed day beside it.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDayTimeIsUnparsable_ForXDocument_ShouldSkipTheDay()
    {
        XDocument document = XDocument.Parse(
            $"<gesmes:Envelope {Namespaces}><Cube>" +
            "<Cube time=\"2023-13-45\"><Cube currency=\"USD\" rate=\"1.0600\"/></Cube>" +
            "<Cube time=\"2023-01-03\"><Cube currency=\"JPY\" rate=\"140.06\"/></Cube>" +
            "</Cube></gesmes:Envelope>");

        EcbRateTable table = EcbRateXmlParser.Parse(document, new EcbRateProviderOptions());

        Assert.HasCount(1, table.Observations);
        Assert.AreEqual(new EcbRateObservation(new DateOnly(2023, 1, 3), "JPY", 140.06m), table.Observations[0]);
    }

    /// <summary>
    /// Verifies that the document overload skips a cell it cannot parse, one missing its currency or its rate, with
    /// a non-numeric or non-positive rate, or with a currency that is not an ISO code, while keeping the usable cell
    /// beside it.
    /// </summary>
    /// <param name="cell">The unusable cell under test.</param>
    [TestMethod]
    [DataRow("<Cube rate=\"1.0545\"/>")]
    [DataRow("<Cube currency=\"USD\"/>")]
    [DataRow("<Cube currency=\"USD\" rate=\"n/a\"/>")]
    [DataRow("<Cube currency=\"USD\" rate=\"0\"/>")]
    [DataRow("<Cube currency=\"usd\" rate=\"1.0545\"/>")]
    public void Parse_WhenCellIsUnparsable_ForXDocument_ShouldSkipTheCell(string cell)
    {
        XDocument document = XDocument.Parse(
            $"<gesmes:Envelope {Namespaces}><Cube><Cube time=\"2023-01-03\">" +
            cell + "<Cube currency=\"JPY\" rate=\"140.06\"/>" +
            "</Cube></Cube></gesmes:Envelope>");

        EcbRateTable table = EcbRateXmlParser.Parse(document, new EcbRateProviderOptions());

        Assert.HasCount(1, table.Observations);
        Assert.AreEqual("JPY", table.Observations[0].CurrencyCode);
    }

    /// <summary>
    /// Verifies that the document overload maps an applicable currency alias before validating the ISO code, as the
    /// forward read does.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCurrencyAliasConfigured_ForXDocument_ShouldMapToIsoCode()
    {
        XDocument document = XDocument.Parse(
            $"<gesmes:Envelope {Namespaces}><Cube><Cube time=\"2023-01-03\">" +
            "<Cube currency=\"SDR\" rate=\"1.2500\"/></Cube></Cube></gesmes:Envelope>");
        EcbRateProviderOptions options = new();
        options.CurrencyAliases["SDR"] = "XDR";

        EcbRateTable table = EcbRateXmlParser.Parse(document, options);

        Assert.HasCount(1, table.Observations);
        Assert.AreEqual("XDR", table.Observations[0].CurrencyCode);
    }
}
