// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDocumentTests.Parse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Infrastructure;
using Bodu.Text.Formats;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationDocumentTests
{
    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Parse(string)" /> throws an
    /// <see cref="ArgumentNullException" /> when the input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTextIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BoduConfigurationDocument.Parse(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Parse(string)" /> on the minimal fixture produces
    /// the expected section and entry.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsMinimalFixture_ShouldPopulateSingleSection()
    {
        IniDocument doc = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Minimal);

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
        IniDocument lf = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Representative);
        IniDocument crlf = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Representative.Replace("\n", "\r\n"));

        Assert.HasCount(lf.Sections.Count, crlf.Sections);
        Assert.HasCount(lf.GlobalSection.Entries.Count, crlf.GlobalSection.Entries);
        Assert.HasCount(lf.Sections[0].Entries.Count, crlf.Sections[0].Entries);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.TryParse(string?, out IniDocument?)" /> returns
    /// <see langword="true" /> with a populated document on success.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsValid_ShouldReturnTrueAndProduceDocument()
    {
        var ok = BoduConfigurationDocument.TryParse(BoduConfigurationFixtures.Minimal, out IniDocument? doc);

        Assert.IsTrue(ok);
        Assert.IsNotNull(doc);
        Assert.HasCount(1, doc!.Sections);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.TryParse(string?, BoduConfigurationParseOptions?, out IniDocument?)" />
    /// returns <see langword="false" /> with a <see langword="null" /> document on failure.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsMalformed_ShouldReturnFalse()
    {
        var ok = BoduConfigurationDocument.TryParse("[*.cs]\nformat.indent.size\n", BoduConfigurationParseOptions.Strict, out IniDocument? doc);

        Assert.IsFalse(ok);
        Assert.IsNull(doc);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.TryParse(string?, out IniDocument?)" /> returns
    /// <see langword="false" /> when the input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsNull_ShouldReturnFalse()
    {
        var ok = BoduConfigurationDocument.TryParse(null, out IniDocument? doc);

        Assert.IsFalse(ok);
        Assert.IsNull(doc);
    }

    /// <summary>
    /// Verifies that under <see cref="BoduConfigurationDiagnosticMode.Collect" /> the result exposes the
    /// diagnostics it would otherwise have thrown.
    /// </summary>
    [TestMethod]
    public void ParseWithDiagnostics_WhenDiagnosticModeIsCollect_ShouldExposeDiagnostics()
    {
        BoduConfigurationParseResult result = BoduConfigurationDocument.ParseWithDiagnostics(
            "[*.cs]\nformat.indent.size\n",
            BoduConfigurationParseOptions.Relaxed);

        Assert.IsGreaterThanOrEqualTo(1, result.Diagnostics.Length);
        Assert.AreEqual(BoduConfigurationDiagnosticCode.MissingEquals, result.Diagnostics[0].Code);
    }
}
