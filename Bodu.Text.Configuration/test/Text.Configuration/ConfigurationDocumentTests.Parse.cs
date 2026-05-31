// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDocumentTests.Parse.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Infrastructure;
using Bodu.Text.Ini;

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
        IniDocument doc = ConfigurationDocument.Parse(ConfigurationFixtures.Minimal);

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
        IniDocument lf = ConfigurationDocument.Parse(ConfigurationFixtures.Representative);
        IniDocument crlf = ConfigurationDocument.Parse(ConfigurationFixtures.Representative.Replace("\n", "\r\n"));

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
        var ok = ConfigurationDocument.TryParse(ConfigurationFixtures.Minimal, out IniDocument? doc);

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
        var ok = ConfigurationDocument.TryParse("[*.cs]\nformat.indent.size\n", ConfigurationParseOptions.Strict, out IniDocument? doc);

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
        var ok = ConfigurationDocument.TryParse(null, out IniDocument? doc);

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
}
