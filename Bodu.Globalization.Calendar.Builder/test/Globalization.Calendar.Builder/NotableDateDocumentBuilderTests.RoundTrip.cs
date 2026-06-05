// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// Resolves the four sample notable dates for 2026 in the United States and returns them as comparable tuples.
    /// </summary>
    /// <param name="resource">The resource to resolve over.</param>
    /// <returns>An ordered list of <c>(id, date)</c> tuples.</returns>
    private static List<(string Id, DateOnly Date)> ResolvedSample(NotableDateResource resource) =>
        new NotableDateService(resource)
            .Resolve(2026, "US")
            .Select(n => (n.NotableDateId, n.Date))
            .OrderBy(t => t.Item2)
            .ToList();

    /// <summary>
    /// Verifies that re-serializing a document parsed from its own XML reproduces the original XML byte-for-byte.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenParsedFromXmlAndReserialized_ShouldReproduceXml()
    {
        string xml = SampleDocument().ToXml();

        string reserialized = NotableDateDocumentBuilder.FromXml(xml).ToXml();

        Assert.AreEqual(xml, reserialized);
    }

    /// <summary>
    /// Verifies that a document parsed from XML resolves the same occurrences as the original.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenParsedFromXml_ShouldPreserveResolution()
    {
        NotableDateDocumentBuilder original = SampleDocument();

        NotableDateDocumentBuilder reparsed = NotableDateDocumentBuilder.FromXml(original.ToXml());

        CollectionAssert.AreEqual(ResolvedSample(original.Build()), ResolvedSample(reparsed.Build()));
    }

    /// <summary>
    /// Verifies that re-serializing a document parsed from its own JSON reproduces the original JSON.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenParsedFromJsonAndReserialized_ShouldReproduceJson()
    {
        string json = SampleDocument().ToJson();

        string reserialized = NotableDateDocumentBuilder.FromJson(json).ToJson();

        Assert.AreEqual(json, reserialized);
    }

    /// <summary>
    /// Verifies that a document parsed from JSON resolves the same occurrences as the original.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenParsedFromJson_ShouldPreserveResolution()
    {
        NotableDateDocumentBuilder original = SampleDocument();

        NotableDateDocumentBuilder reparsed = NotableDateDocumentBuilder.FromJson(original.ToJson());

        CollectionAssert.AreEqual(ResolvedSample(original.Build()), ResolvedSample(reparsed.Build()));
    }
}
