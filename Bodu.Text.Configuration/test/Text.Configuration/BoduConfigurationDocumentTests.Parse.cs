// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDocumentTests.Parse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Infrastructure;

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
    /// the expected section and property.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsMinimalFixture_ShouldPopulateSingleSection()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Minimal);

        Assert.AreEqual(1, doc.Sections.Count);
        Assert.AreEqual("*", doc.Sections[0].Pattern);
        Assert.AreEqual("format.indent.size", doc.Sections[0].Properties[0].RawKey);
        Assert.AreEqual("4", doc.Sections[0].Properties[0].Value);
    }

    /// <summary>
    /// Verifies that LF and CRLF line endings produce the same document.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputUsesCrLf_ShouldProduceSameDocumentAsLf()
    {
        BoduConfigurationDocument lf = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Representative);
        BoduConfigurationDocument crlf = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Representative.Replace("\n", "\r\n"));

        Assert.AreEqual(lf.Sections.Count, crlf.Sections.Count);
        Assert.AreEqual(lf.Preamble.Properties.Count, crlf.Preamble.Properties.Count);
        Assert.AreEqual(lf.Sections[0].Properties.Count, crlf.Sections[0].Properties.Count);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.TryParse(string?, out BoduConfigurationDocument?)" />
    /// returns <see langword="true" /> with a populated document on success.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsValid_ShouldReturnTrueAndProduceDocument()
    {
        bool ok = BoduConfigurationDocument.TryParse(BoduConfigurationFixtures.Minimal, out BoduConfigurationDocument? doc);

        Assert.IsTrue(ok);
        Assert.IsNotNull(doc);
        Assert.AreEqual(1, doc!.Sections.Count);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.TryParse(string?, out BoduConfigurationDocument?)" />
    /// returns <see langword="false" /> with a <see langword="null" /> document on failure.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsMalformed_ShouldReturnFalse()
    {
        bool ok = BoduConfigurationDocument.TryParse("[*.cs]\nformat.indent.size\n", BoduConfigurationParseOptions.Strict, out BoduConfigurationDocument? doc);

        Assert.IsFalse(ok);
        Assert.IsNull(doc);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.TryParse(string?, out BoduConfigurationDocument?)" />
    /// returns <see langword="false" /> when the input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsNull_ShouldReturnFalse()
    {
        bool ok = BoduConfigurationDocument.TryParse(null, out BoduConfigurationDocument? doc);

        Assert.IsFalse(ok);
        Assert.IsNull(doc);
    }

    /// <summary>
    /// Verifies that under <see cref="BoduConfigurationDiagnosticMode.Collect" /> the document exposes the
    /// diagnostics it would otherwise have thrown.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDiagnosticModeIsCollect_ShouldExposeDiagnosticsOnDocument()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse("[*.cs]\nformat.indent.size\n", BoduConfigurationParseOptions.Relaxed);

        Assert.IsTrue(doc.Diagnostics.Length >= 1);
        Assert.AreEqual(BoduConfigurationDiagnosticCode.MissingEquals, doc.Diagnostics[0].Code);
    }
}
