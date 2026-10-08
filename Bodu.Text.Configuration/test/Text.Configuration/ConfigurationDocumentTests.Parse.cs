// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDocumentTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Infrastructure;

namespace Bodu.Text.Configuration;

public partial class ConfigurationDocumentTests
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.Parse(string)" /> throws an
    /// <see cref="ArgumentNullException" /> when the input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTextIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ConfigurationDocument.Parse(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.Parse(string)" /> on the minimal fixture produces
    /// the expected section and entry.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsMinimalFixture_ShouldPopulateSingleSection()
    {
        var doc = ConfigurationDocument.Parse(ConfigurationFixtures.Minimal);

        Assert.HasCount(1, doc.Sections);
        Assert.AreEqual("*", doc.Sections[0].Name);
        Assert.AreEqual("format.indent.size", doc.Sections[0].Entries[0].Key);
        Assert.AreEqual("4", doc.Sections[0].Entries[0].Value);
    }

    /// <summary>
    /// Verifies that LF and CRLF line endings produce equivalent documents.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputUsesCrLf_ShouldProduceSameDocumentAsLf()
    {
        var lf = ConfigurationDocument.Parse(ConfigurationFixtures.Representative);
        var crlf = ConfigurationDocument.Parse(ConfigurationFixtures.Representative.Replace("\n", "\r\n"));

        Assert.HasCount(lf.Sections.Count, crlf.Sections);
        Assert.HasCount(lf.GlobalSection.Entries.Count, crlf.GlobalSection.Entries);
        Assert.HasCount(lf.Sections[0].Entries.Count, crlf.Sections[0].Entries);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.TryParse(string?, out IniDocument?)" /> returns
    /// <see langword="true" /> with a populated document on success.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsValid_ShouldReturnTrueAndProduceDocument()
    {
        bool ok = ConfigurationDocument.TryParse(ConfigurationFixtures.Minimal, out ConfigurationDocument? doc);

        Assert.IsTrue(ok);
        Assert.IsNotNull(doc);
        Assert.HasCount(1, doc!.Sections);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.TryParse(string?, ConfigurationParseOptions?, out IniDocument?)" />
    /// returns <see langword="false" /> with a <see langword="null" /> document on failure.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsMalformed_ShouldReturnFalse()
    {
        bool ok = ConfigurationDocument.TryParse("[*.cs]\nformat.indent.size\n", ConfigurationParseOptions.Strict, out ConfigurationDocument? doc);

        Assert.IsFalse(ok);
        Assert.IsNull(doc);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.TryParse(string?, out IniDocument?)" /> returns
    /// <see langword="false" /> when the input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsNull_ShouldReturnFalse()
    {
        bool ok = ConfigurationDocument.TryParse(null, out ConfigurationDocument? doc);

        Assert.IsFalse(ok);
        Assert.IsNull(doc);
    }

    /// <summary>
    /// Verifies that under <see cref="ConfigurationDiagnosticMode.Collect" /> the result exposes the
    /// diagnostics it would otherwise have thrown.
    /// </summary>
    [TestMethod]
    public void ParseWithDiagnostics_WhenDiagnosticModeIsCollect_ShouldExposeDiagnostics()
    {
        ConfigurationParseResult result = ConfigurationDocument.ParseWithDiagnostics(
            "[*.cs]\nformat.indent.size\n",
            ConfigurationParseOptions.Relaxed);

        Assert.IsGreaterThanOrEqualTo(1, result.Diagnostics.Length);
        Assert.AreEqual(ConfigurationDiagnosticCode.MissingEquals, result.Diagnostics[0].Code);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.Parse(string, ConfigurationParseOptions?)" /> ignores a byte order
    /// mark before a comment line under every profile, so the document resolves as the same file loaded from disk does
    /// (EditorConfig core-test <c>bom_at_head</c>).
    /// </summary>
    /// <param name="profile">The profile whose parse and resolve options apply.</param>
    [TestMethod]
    [DataRow(ConfigurationProfile.Bodu)]
    [DataRow(ConfigurationProfile.EditorConfigCompatible)]
    [DataRow(ConfigurationProfile.Strict)]
    [DataRow(ConfigurationProfile.Relaxed)]
    public void Parse_WhenByteOrderMarkPrecedesCommentLine_ShouldIgnoreTheMark(ConfigurationProfile profile)
    {
        var doc = ConfigurationDocument.Parse(
            "\uFEFF; test EditorConfig files with BOM\n\nroot = true\n\n[*]\nkey = value\n",
            ConfigurationParseOptions.For(profile));

        ConfigurationView view = doc.Resolve("a.c", ConfigurationResolveOptions.For(profile));

        Assert.AreEqual("value", view.GetString("key"));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.Parse(string, ConfigurationParseOptions?)" /> ignores a byte order
    /// mark before a section header under every profile, reading the header as the first section.
    /// </summary>
    /// <param name="profile">The profile whose parse options apply.</param>
    [TestMethod]
    [DataRow(ConfigurationProfile.Bodu)]
    [DataRow(ConfigurationProfile.EditorConfigCompatible)]
    [DataRow(ConfigurationProfile.Strict)]
    [DataRow(ConfigurationProfile.Relaxed)]
    public void Parse_WhenByteOrderMarkPrecedesSectionHeader_ShouldReadTheSection(ConfigurationProfile profile)
    {
        var doc = ConfigurationDocument.Parse("\uFEFF[*]\nkey = value\n", ConfigurationParseOptions.For(profile));

        Assert.HasCount(1, doc.Sections);
        Assert.AreEqual("*", doc.Sections[0].Name);
        Assert.AreEqual("value", doc.Sections[0].Entries[0].Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.Parse(string)" /> ignores only the first of two leading byte order
    /// marks: the second stays content, so the first line is a property line without <c>=</c> and the parse throws
    /// <see cref="ConfigurationParseException" /> for <see cref="ConfigurationDiagnosticCode.MissingEquals" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTextStartsWithTwoByteOrderMarks_ShouldIgnoreOnlyTheFirst()
    {
        var ex = Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationDocument.Parse("\uFEFF\uFEFF[*]\nkey = value\n");
        });

        Assert.AreEqual(ConfigurationDiagnosticCode.MissingEquals, ex.Diagnostic?.Code);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.Parse(string)" /> keeps a U+FEFF that does not start the text as
    /// part of the value it appears in.
    /// </summary>
    [TestMethod]
    public void Parse_WhenByteOrderMarkIsInsideTheText_ShouldKeepItAsContent()
    {
        var doc = ConfigurationDocument.Parse("[*]\nkey = a\uFEFFb\n");

        Assert.AreEqual("a\uFEFFb", doc.Sections[0].Entries[0].Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.TryParse(string?, out ConfigurationDocument?)" /> ignores a leading
    /// byte order mark and returns <see langword="true" /> with the section read.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenTextStartsWithByteOrderMark_ShouldReturnTrueAndReadTheSection()
    {
        bool parsed = ConfigurationDocument.TryParse("\uFEFF[*]\nkey = value\n", out ConfigurationDocument? doc);

        Assert.IsTrue(parsed);
        Assert.IsNotNull(doc);
        Assert.AreEqual("*", doc.Sections[0].Name);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.ParseWithDiagnostics(string, ConfigurationParseOptions?)" /> ignores
    /// a leading byte order mark, reporting no diagnostic and reading the section.
    /// </summary>
    [TestMethod]
    public void ParseWithDiagnostics_WhenTextStartsWithByteOrderMark_ShouldReportNoDiagnostics()
    {
        ConfigurationParseResult result = ConfigurationDocument.ParseWithDiagnostics(
            "\uFEFF[*]\nkey = value\n",
            ConfigurationParseOptions.Relaxed);

        Assert.IsEmpty(result.Diagnostics);
        Assert.AreEqual("*", result.Document.Sections[0].Name);
    }

    /// <summary>
    /// Verifies that a parsed document has no root: an absolute target is matched as given, so an anchored section does
    /// not apply to it, while the same target given relative to the configuration's directory does match.
    /// </summary>
    [TestMethod]
    public void Parse_WhenResolvedWithoutPathRoot_ShouldMatchTheTargetAsGiven()
    {
        var doc = ConfigurationDocument.Parse("[src/*.cs]\nformat.indent.size = 4\n");

        Assert.IsNull(doc.Resolve(Path.Combine(Path.GetTempPath(), "src", "a.cs"))["format:indent:size"]);
        Assert.AreEqual("4", doc.Resolve("src/a.cs")["format:indent:size"]);
    }
}
