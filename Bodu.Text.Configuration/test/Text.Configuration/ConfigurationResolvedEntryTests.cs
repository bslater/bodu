// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolvedEntryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for the resolved-entry origin metadata API on <see cref="ConfigurationView" /> — the
/// "which section/file/line won for this key" debugging surface.
/// </summary>
[TestClass]
public class ConfigurationResolvedEntryTests
{
    /// <summary>
    /// Verifies that a preamble entry produces a <see cref="ConfigurationResolvedEntry" /> with no section
    /// pattern (preamble origins are signalled by <see cref="ConfigurationResolvedEntry.SectionPattern" />
    /// being <see langword="null" />).
    /// </summary>
    [TestMethod]
    public void GetEntry_WhenKeyFromPreamble_ShouldHaveNullSectionPattern()
    {
        const string fixture = """
service.name = Bodu
""";
        IniDocument doc = ConfigurationDocument.Parse(fixture);
        ConfigurationView view = doc.Resolve("any.cs");

        ConfigurationResolvedEntry? entry = view.GetEntry("service:name");

        Assert.IsNotNull(entry);
        Assert.AreEqual("service:name", entry!.Key);
        Assert.AreEqual("Bodu", entry.Value);
        Assert.IsNull(entry.SectionPattern);
    }

    /// <summary>
    /// Verifies that a section-sourced entry exposes the section pattern that won.
    /// </summary>
    [TestMethod]
    public void GetEntry_WhenKeyFromSection_ShouldExposeSectionPattern()
    {
        const string fixture = """
[*.cs]
format.indent.size = 4
""";
        IniDocument doc = ConfigurationDocument.Parse(fixture);
        ConfigurationView view = doc.Resolve("Foo.cs");

        ConfigurationResolvedEntry? entry = view.GetEntry("format:indent:size");

        Assert.IsNotNull(entry);
        Assert.AreEqual("*.cs", entry!.SectionPattern);
    }

    /// <summary>
    /// Verifies that when multiple sections supply the same key, the entry exposes the *last* matching
    /// section — last-wins precedence is the resolver's contract and origin metadata must reflect it.
    /// </summary>
    [TestMethod]
    public void GetEntry_WhenMultipleSectionsMatch_ShouldReturnLastWinningSection()
    {
        const string fixture = """
[*.cs]
format.indent.size = 4

[src/**/*.cs]
format.indent.size = 2
""";
        IniDocument doc = ConfigurationDocument.Parse(fixture);
        ConfigurationView view = doc.Resolve("src/Foo.cs");

        ConfigurationResolvedEntry? entry = view.GetEntry("format:indent:size");

        Assert.IsNotNull(entry);
        Assert.AreEqual("src/**/*.cs", entry!.SectionPattern);
        Assert.AreEqual("2", entry.Value);
    }

    /// <summary>
    /// Verifies that when a section overrides a preamble key, the entry's origin shifts to the section.
    /// </summary>
    [TestMethod]
    public void GetEntry_WhenSectionOverridesPreamble_ShouldReturnSectionAsOrigin()
    {
        const string fixture = """
service.name = Preamble

[*.cs]
service.name = Section
""";
        IniDocument doc = ConfigurationDocument.Parse(fixture);
        ConfigurationView view = doc.Resolve("Foo.cs");

        ConfigurationResolvedEntry? entry = view.GetEntry("service:name");

        Assert.IsNotNull(entry);
        Assert.AreEqual("*.cs", entry!.SectionPattern);
        Assert.AreEqual("Section", entry.Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetEntry(string)" /> returns <see langword="null" /> for
    /// keys that are not present in the resolved view.
    /// </summary>
    [TestMethod]
    public void GetEntry_WhenKeyIsAbsent_ShouldReturnNull()
    {
        IniDocument doc = ConfigurationDocument.Parse("service.name = Bodu\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.IsNull(view.GetEntry("missing:key"));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetEntry(string)" /> accepts both dotted and colon-
    /// delimited key forms, matching the indexer's contract.
    /// </summary>
    [TestMethod]
    public void GetEntry_WhenKeyIsDottedForm_ShouldStillResolve()
    {
        IniDocument doc = ConfigurationDocument.Parse("service.name = Bodu\n");
        ConfigurationView view = doc.Resolve("any.cs");

        ConfigurationResolvedEntry? viaDotted = view.GetEntry("service.name");
        ConfigurationResolvedEntry? viaColon = view.GetEntry("service:name");

        Assert.IsNotNull(viaDotted);
        Assert.IsNotNull(viaColon);
        Assert.AreEqual(viaDotted!.Value, viaColon!.Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.GetEntry(string)" /> throws when given a
    /// <see langword="null" /> key, matching the indexer's nullability contract.
    /// </summary>
    [TestMethod]
    public void GetEntry_WhenKeyIsNull_ShouldThrowExactly()
    {
        IniDocument doc = ConfigurationDocument.Parse("service.name = Bodu\n");
        ConfigurationView view = doc.Resolve("any.cs");

        Assert.ThrowsExactly<ArgumentNullException>(() => _ = view.GetEntry(null!));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationView.Entries" /> enumerates every resolved key with its
    /// origin metadata and that the count matches <see cref="ConfigurationView.Count" />.
    /// </summary>
    [TestMethod]
    public void Entries_WhenViewResolved_ShouldEnumerateAllResolvedKeys()
    {
        const string fixture = """
service.name = Bodu

[*.cs]
format.indent.size = 4
""";
        IniDocument doc = ConfigurationDocument.Parse(fixture);
        ConfigurationView view = doc.Resolve("Foo.cs");

        var keys = new HashSet<string>();
        foreach (ConfigurationResolvedEntry entry in view.Entries)
            keys.Add(entry.Key);

        Assert.AreEqual(view.Count, keys.Count);
        Assert.IsTrue(keys.Contains("service:name"));
        Assert.IsTrue(keys.Contains("format:indent:size"));
    }

    /// <summary>
    /// Verifies that an entry's <see cref="ConfigurationResolvedEntry.SourceLocation" /> carries the
    /// line number from the originating <see cref="IniEntry" />.
    /// </summary>
    [TestMethod]
    public void GetEntry_WhenKeyResolved_ShouldExposeOriginatingLineNumber()
    {
        // Line 1: service.name = Bodu
        // Line 2: (blank)
        // Line 3: [*.cs]
        // Line 4: format.indent.size = 4
        const string fixture = "service.name = Bodu\n\n[*.cs]\nformat.indent.size = 4\n";
        IniDocument doc = ConfigurationDocument.Parse(fixture);
        ConfigurationView view = doc.Resolve("Foo.cs");

        ConfigurationResolvedEntry preamble = view.GetEntry("service:name")!;
        ConfigurationResolvedEntry section = view.GetEntry("format:indent:size")!;

        Assert.AreEqual(1, preamble.SourceLocation.LineNumber);
        Assert.AreEqual(4, section.SourceLocation.LineNumber);
    }

    /// <summary>
    /// Verifies that under <see cref="ConfigurationUnsetValueMode.RemoveEffectiveValue" />, an unset entry
    /// removes both the value and the origin metadata.
    /// </summary>
    [TestMethod]
    public void GetEntry_WhenKeyUnsetWithRemoveMode_ShouldReturnNull()
    {
        const string fixture = """
[*]
format.indent.size = 4

[generated/**]
format.indent.size = unset
""";
        IniDocument doc = ConfigurationDocument.Parse(fixture);
        ConfigurationResolveOptions options = new() { UnsetValueMode = ConfigurationUnsetValueMode.RemoveEffectiveValue };
        ConfigurationView view = doc.Resolve("generated/Foo.cs", options);

        Assert.IsNull(view["format:indent:size"]);
        Assert.IsNull(view.GetEntry("format:indent:size"));
    }
}
