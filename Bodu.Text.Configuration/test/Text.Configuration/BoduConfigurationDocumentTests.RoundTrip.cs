// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDocumentTests.RoundTrip.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using Bodu.Text.Configuration.Infrastructure;
using Bodu.Text.Formats;

namespace Bodu.Text.Configuration;

/// <summary>
/// Parse / emit / re-parse coverage exercised through the static
/// <see cref="BoduConfigurationDocument" /> facade. The underlying storage is <see cref="IniDocument" /> from
/// <c>Bodu.Text.Formats</c>.
/// </summary>
[TestClass]
public partial class BoduConfigurationDocumentTests
{
    internal static string Emit(IniDocument document)
    {
        using StringWriter sw = new();
        BoduConfigurationDocument.Save(document, sw);
        return sw.ToString();
    }

    /// <summary>
    /// Verifies that parsing then emitting then re-parsing the representative fixture preserves the section
    /// count, preamble entry count, and per-section entry counts.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenRepresentativeFixture_ShouldPreserveCountsAndOrder()
    {
        IniDocument first = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Representative);
        string emitted = Emit(first);
        IniDocument second = BoduConfigurationDocument.Parse(emitted);

        Assert.AreEqual(first.Sections.Count, second.Sections.Count);
        Assert.AreEqual(first.GlobalSection.Entries.Count, second.GlobalSection.Entries.Count);

        for (int i = 0; i < first.Sections.Count; i++)
        {
            Assert.AreEqual(first.Sections[i].Name, second.Sections[i].Name);
            Assert.AreEqual(first.Sections[i].Entries.Count, second.Sections[i].Entries.Count);
        }
    }

    /// <summary>
    /// Verifies that leading comments survive a parse/emit/parse round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenLeadingCommentsPresent_ShouldPreserveLeadingComments()
    {
        IniDocument first = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.CommentsAndProperties);
        IniDocument second = BoduConfigurationDocument.Parse(Emit(first));

        Assert.AreEqual(first.Sections[0].LeadingComments.Count, second.Sections[0].LeadingComments.Count);
        Assert.AreEqual(first.Sections[0].Entries[0].LeadingComments.Count, second.Sections[0].Entries[0].LeadingComments.Count);
    }

    /// <summary>
    /// Verifies that inline comments survive a parse/emit/parse round-trip under the default Bodu profile.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenInlineCommentsPresent_ShouldPreserveInlineComments()
    {
        IniDocument first = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.InlineComments);
        IniDocument second = BoduConfigurationDocument.Parse(Emit(first));

        IniEntry original = first.Sections[0].Entries[0];
        IniEntry reparsed = second.Sections[0].Entries[0];

        Assert.AreEqual(original.Value, reparsed.Value);
        Assert.AreEqual(original.InlineComment.HasValue, reparsed.InlineComment.HasValue);
    }

    /// <summary>
    /// Verifies that the <c>root</c> flag survives a parse/emit/parse round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenPreambleContainsRoot_ShouldPreserveRoot()
    {
        IniDocument first = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Representative);
        IniDocument second = BoduConfigurationDocument.Parse(Emit(first));

        Assert.AreEqual(first.GlobalSection["root"], second.GlobalSection["root"]);
    }

    /// <summary>
    /// Verifies that authoring a document programmatically through the mutable Ini surface and round-tripping
    /// it through <see cref="BoduConfigurationDocument.Save(IniDocument, System.IO.TextWriter, BoduConfigurationWriteOptions?)" />
    /// preserves entry order and values.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenAuthoredProgrammatically_ShouldPreserveOrderAndValues()
    {
        IniDocument doc = new();
        doc.GlobalSection.SetEntry("root", "true");
        IniSection section = doc.GetOrAddSection("*.cs");
        section.SetEntry("format.indent.size", "4");
        section.SetEntry("format.indent.style", "space");

        IniDocument reparsed = BoduConfigurationDocument.Parse(Emit(doc));

        Assert.AreEqual("true", reparsed.GlobalSection["root"]);
        Assert.AreEqual(1, reparsed.Sections.Count);
        Assert.AreEqual("4", reparsed.Sections[0].Entries[0].Value);
        Assert.AreEqual("space", reparsed.Sections[0].Entries[1].Value);
    }
}
