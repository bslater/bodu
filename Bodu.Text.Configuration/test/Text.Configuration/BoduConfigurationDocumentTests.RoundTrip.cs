// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDocumentTests.RoundTrip.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Infrastructure;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationDocumentTests
{
    /// <summary>
    /// Verifies that parsing then emitting then re-parsing the representative fixture preserves the section
    /// count, preamble property count, and per-section property counts.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenRepresentativeFixture_ShouldPreserveCountsAndOrder()
    {
        BoduConfigurationDocument first = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Representative);
        string emitted = first.ToString();
        BoduConfigurationDocument second = BoduConfigurationDocument.Parse(emitted);

        Assert.AreEqual(first.Sections.Count, second.Sections.Count);
        Assert.AreEqual(first.Preamble.Properties.Count, second.Preamble.Properties.Count);

        for (int i = 0; i < first.Sections.Count; i++)
        {
            Assert.AreEqual(first.Sections[i].Pattern, second.Sections[i].Pattern);
            Assert.AreEqual(first.Sections[i].Properties.Count, second.Sections[i].Properties.Count);
        }
    }

    /// <summary>
    /// Verifies that leading comments survive a parse/emit/parse round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenLeadingCommentsPresent_ShouldPreserveLeadingComments()
    {
        BoduConfigurationDocument first = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.CommentsAndProperties);
        BoduConfigurationDocument second = BoduConfigurationDocument.Parse(first.ToString());

        Assert.AreEqual(first.Sections[0].LeadingComments.Count, second.Sections[0].LeadingComments.Count);
        Assert.AreEqual(first.Sections[0].Properties[0].LeadingComments.Count, second.Sections[0].Properties[0].LeadingComments.Count);
    }

    /// <summary>
    /// Verifies that inline comments survive a parse/emit/parse round-trip under the default Bodu profile.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenInlineCommentsPresent_ShouldPreserveInlineComments()
    {
        BoduConfigurationDocument first = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.InlineComments);
        BoduConfigurationDocument second = BoduConfigurationDocument.Parse(first.ToString());

        BoduConfigurationProperty original = first.Sections[0].Properties[0];
        BoduConfigurationProperty reparsed = second.Sections[0].Properties[0];

        Assert.AreEqual(original.Value, reparsed.Value);
        Assert.AreEqual(original.InlineComment.HasValue, reparsed.InlineComment.HasValue);
    }

    /// <summary>
    /// Verifies that the <c>root</c> flag survives a parse/emit/parse round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenPreambleContainsRoot_ShouldPreserveRoot()
    {
        BoduConfigurationDocument first = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Representative);
        BoduConfigurationDocument second = BoduConfigurationDocument.Parse(first.ToString());

        Assert.AreEqual(first.Root, second.Root);
    }

    /// <summary>
    /// Verifies that authoring a document programmatically and round-tripping it through the writer
    /// preserves property order and values.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenAuthoredProgrammatically_ShouldPreserveOrderAndValues()
    {
        BoduConfigurationDocument doc = new();
        doc.Preamble.Set("root", true);

        BoduConfigurationSection section = doc.GetOrAddSection("*.cs");
        section.Set("format.indent.size", 4);
        section.Set("format.indent.style", "space");

        BoduConfigurationDocument reparsed = BoduConfigurationDocument.Parse(doc.ToString());

        Assert.AreEqual(true, reparsed.Root);
        Assert.AreEqual(1, reparsed.Sections.Count);
        Assert.AreEqual("4", reparsed.Sections[0].Properties[0].Value);
        Assert.AreEqual("space", reparsed.Sections[0].Properties[1].Value);
    }
}
