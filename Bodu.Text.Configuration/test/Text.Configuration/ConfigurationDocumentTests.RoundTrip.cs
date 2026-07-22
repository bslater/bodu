// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDocumentTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Infrastructure;

namespace Bodu.Text.Configuration;

/// <summary>
/// Parse / emit / re-parse coverage exercised through the static
/// <see cref="ConfigurationDocument" /> facade over the library's own INI document model.
/// </summary>
[TestClass]
public partial class ConfigurationDocumentTests
{
    internal static string Emit(IniDocumentBase document)
    {
        using StringWriter sw = new();
        ConfigurationDocument.Save(document, sw);
        return sw.ToString();
    }

    /// <summary>
    /// Verifies that parsing then emitting then re-parsing the representative fixture preserves the section
    /// count, preamble entry count, and per-section entry counts.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenRepresentativeFixture_ShouldPreserveCountsAndOrder()
    {
        var first = ConfigurationDocument.Parse(ConfigurationFixtures.Representative);
        string emitted = Emit(first);
        var second = ConfigurationDocument.Parse(emitted);

        Assert.HasCount(first.Sections.Count, second.Sections);
        Assert.HasCount(first.GlobalSection.Entries.Count, second.GlobalSection.Entries);

        for (int i = 0; i < first.Sections.Count; i++)
        {
            Assert.AreEqual(first.Sections[i].Name, second.Sections[i].Name);
            Assert.HasCount(first.Sections[i].Entries.Count, second.Sections[i].Entries);
        }
    }

    /// <summary>
    /// Verifies that leading comments survive a parse/emit/parse round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenLeadingCommentsPresent_ShouldPreserveLeadingComments()
    {
        var first = ConfigurationDocument.Parse(ConfigurationFixtures.CommentsAndProperties);
        var second = ConfigurationDocument.Parse(Emit(first));

        Assert.HasCount(first.Sections[0].LeadingComments.Count, second.Sections[0].LeadingComments);
        Assert.HasCount(first.Sections[0].Entries[0].LeadingComments.Count, second.Sections[0].Entries[0].LeadingComments);
    }

    /// <summary>
    /// Verifies that inline comments survive a parse/emit/parse round-trip under the default Bodu profile.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenInlineCommentsPresent_ShouldPreserveInlineComments()
    {
        var first = ConfigurationDocument.Parse(ConfigurationFixtures.InlineComments);
        var second = ConfigurationDocument.Parse(Emit(first));

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
        var first = ConfigurationDocument.Parse(ConfigurationFixtures.Representative);
        var second = ConfigurationDocument.Parse(Emit(first));

        Assert.AreEqual(first.GlobalSection["root"], second.GlobalSection["root"]);
    }

    /// <summary>
    /// Verifies that authoring a document programmatically through the mutable Ini surface and round-tripping
    /// it through <see cref="ConfigurationDocument.Save(IniDocument, System.IO.TextWriter, ConfigurationWriteOptions?)" />
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

        var reparsed = ConfigurationDocument.Parse(Emit(doc));

        Assert.AreEqual("true", reparsed.GlobalSection["root"]);
        Assert.HasCount(1, reparsed.Sections);
        Assert.AreEqual("4", reparsed.Sections[0].Entries[0].Value);
        Assert.AreEqual("space", reparsed.Sections[0].Entries[1].Value);
    }
}
