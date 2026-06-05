// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.Serialization.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using System.Xml.Linq;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    private static readonly XNamespace SchemaNamespace = "urn:bodu:globalization:calendar";

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.ToXDocument()" /> emits the v2 root element in the schema
    /// namespace carrying the authored <c>schemaVersion</c> and <c>resourceId</c>.
    /// </summary>
    [TestMethod]
    public void ToXDocument_WhenDocumentAuthored_ShouldEmitV2RootElement()
    {
        XDocument document = SampleDocument().ToXDocument();

        Assert.IsNotNull(document.Root);
        Assert.AreEqual(SchemaNamespace + "NotableDateResource", document.Root!.Name);
        Assert.AreEqual("demo.sample", (string?)document.Root.Attribute("resourceId"));
        Assert.IsNotNull(document.Root.Attribute("schemaVersion"));
    }

    /// <summary>
    /// Verifies that the emitted XML is accepted by the canonical loader and assembles into a resource matching the
    /// directly built one.
    /// </summary>
    [TestMethod]
    public void ToXml_WhenLoadedByResourceLoader_ShouldMatchDirectBuild()
    {
        NotableDateDocumentBuilder builder = SampleDocument();

        NotableDateResource fromXml = NotableDateResourceLoader.Load(builder.ToXml());
        NotableDateResource direct = builder.Build();

        Assert.AreEqual(direct.ResourceId, fromXml.ResourceId);
        Assert.AreEqual(direct.NotableDates.Count, fromXml.NotableDates.Count);
    }

    /// <summary>
    /// Verifies that the emitted JSON is accepted by the canonical JSON loader.
    /// </summary>
    [TestMethod]
    public void ToJson_WhenLoadedByResourceLoader_ShouldAssembleEquivalentResource()
    {
        NotableDateDocumentBuilder builder = SampleDocument();

        NotableDateResource fromJson = NotableDateResourceLoader.LoadJson(builder.ToJson());

        Assert.AreEqual("demo.sample", fromJson.ResourceId);
        Assert.AreEqual(4, fromJson.NotableDates.Count);
    }

    /// <summary>
    /// Verifies that the XML and JSON serializations of the same document resolve identical occurrences for a year.
    /// </summary>
    [TestMethod]
    public void ToXmlAndToJson_WhenResolved_ShouldAgree()
    {
        NotableDateDocumentBuilder builder = SampleDocument();

        NotableDateService fromXml = new(NotableDateResourceLoader.Load(builder.ToXml()));
        NotableDateService fromJson = new(NotableDateResourceLoader.LoadJson(builder.ToJson()));

        IEnumerable<(string, DateOnly)> xmlDates = fromXml.Resolve(2026, "US")
            .Select(n => (n.NotableDateId, n.Date)).OrderBy(t => t.Item2);
        IEnumerable<(string, DateOnly)> jsonDates = fromJson.Resolve(2026, "US")
            .Select(n => (n.NotableDateId, n.Date)).OrderBy(t => t.Item2);

        CollectionAssert.AreEqual(xmlDates.ToList(), jsonDates.ToList());
    }

    /// <summary>
    /// Verifies that serializing a document that imports a catalogue to JSON throws
    /// <see cref="NotSupportedException" />, because the JSON form does not model imports.
    /// </summary>
    [TestMethod]
    public void ToJson_WhenDocumentUsesImports_ShouldThrowNotSupportedException()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("demo.import")
            .AddImport("global-core", i => i.Use("new-years-day", u => u.ForTerritory("US")));

        _ = Assert.ThrowsExactly<NotSupportedException>(() => builder.ToJson());
    }

    /// <summary>
    /// Verifies that serializing a document with a non-Gregorian calendar rule to JSON throws
    /// <see cref="NotSupportedException" />, because the JSON form only models the Gregorian calendar.
    /// </summary>
    [TestMethod]
    public void ToJson_WhenDocumentUsesNonGregorianCalendar_ShouldThrowNotSupportedException()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("demo.hijri")
            .AddNotableDate("eid", "Eid", NotableDateCategory.Religious, d => d
                .AddRule("default", r => r.ForCalendar(CalendarSystem.Hijri).Fixed(10, 1)));

        _ = Assert.ThrowsExactly<NotSupportedException>(() => builder.ToJson());
    }
}
