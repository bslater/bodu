// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationReaderTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration;

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for the configuration reader's line-by-line behaviour, covering sections, properties, comments,
/// and diagnostic routing.
/// </summary>
[TestClass]
public partial class BoduConfigurationReaderTests
{
    /// <summary>
    /// Verifies that an empty input produces an empty document with no sections.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsEmpty_ShouldProduceEmptyDocument()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(string.Empty);

        Assert.AreEqual(0, doc.Sections.Count);
        Assert.AreEqual(0, doc.Preamble.Properties.Count);
        Assert.IsNull(doc.Root);
    }

    /// <summary>
    /// Verifies that preamble properties land on <see cref="BoduConfigurationDocument.Preamble" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenPropertiesPrecedeSection_ShouldPopulatePreamble()
    {
        const string input = "root = true\napplication.name = Bodu\n[*]\nformat.indent.size = 4\n";

        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(input);

        Assert.AreEqual(2, doc.Preamble.Properties.Count);
        Assert.AreEqual("root", doc.Preamble.Properties[0].RawKey);
        Assert.AreEqual("application.name", doc.Preamble.Properties[1].RawKey);
        Assert.AreEqual(1, doc.Sections.Count);
    }

    /// <summary>
    /// Verifies that full-line comments are recognised under <c>#</c> and <c>;</c> prefixes and attached as
    /// leading comments to the next section or property.
    /// </summary>
    [TestMethod]
    public void Parse_WhenFullLineComments_ShouldAttachAsLeadingComments()
    {
        const string input = "# preamble comment\nroot = true\n; section comment\n[*.cs]\nformat.indent.size = 4\n";

        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(input);

        Assert.AreEqual(1, doc.Preamble.Properties[0].LeadingComments.Count);
        Assert.AreEqual('#', doc.Preamble.Properties[0].LeadingComments[0].Prefix);

        Assert.AreEqual(1, doc.Sections[0].LeadingComments.Count);
        Assert.AreEqual(';', doc.Sections[0].LeadingComments[0].Prefix);
    }

    /// <summary>
    /// Verifies that under the default Bodu profile a whitespace-introduced <c>#</c> is treated as an inline
    /// comment and the value is trimmed accordingly.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInlineCommentIsWhitespaceIntroduced_ShouldStripCommentFromValue()
    {
        const string input = "[*]\nformat.indent.size = 4 # default\n";

        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(input);
        BoduConfigurationProperty property = doc.Sections[0].Properties[0];

        Assert.AreEqual("4", property.Value);
        Assert.IsNotNull(property.InlineComment);
        Assert.AreEqual("#", property.InlineComment!.Value.Prefix.ToString());
    }

    /// <summary>
    /// Verifies that under the EditorConfig-compatible profile a <c>#</c> in a value is treated as part of
    /// the value, matching EditorConfig 0.17.2 semantics.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInlineCommentDisabled_ShouldTreatHashAsLiteral()
    {
        const string input = "[*]\nurl = https://example.com/a#fragment\n";

        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(input, BoduConfigurationParseOptions.EditorConfigCompatible);

        BoduConfigurationProperty property = doc.Sections[0].Properties[0];
        Assert.AreEqual("https://example.com/a#fragment", property.Value);
        Assert.IsNull(property.InlineComment);
    }

    /// <summary>
    /// Verifies that a property line missing the <c>=</c> separator throws a
    /// <see cref="BoduConfigurationParseException" /> under the default Throw diagnostic mode.
    /// </summary>
    [TestMethod]
    public void Parse_WhenPropertyLineHasNoEquals_ShouldThrowExactly()
    {
        const string input = "[*.cs]\nformat.indent.size\n";

        BoduConfigurationParseException ex = Assert.ThrowsExactly<BoduConfigurationParseException>(() =>
        {
            _ = BoduConfigurationDocument.Parse(input);
        });

        Assert.IsNotNull(ex.Diagnostic);
        Assert.AreEqual(BoduConfigurationDiagnosticCode.MissingEquals, ex.Diagnostic!.Code);
    }

    /// <summary>
    /// Verifies that a value with an empty payload after <c>=</c> is captured as the empty string.
    /// </summary>
    [TestMethod]
    public void Parse_WhenValueIsEmpty_ShouldProduceEmptyStringValue()
    {
        const string input = "[*]\nformat.indent.size =\n";

        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(input);

        Assert.AreEqual(string.Empty, doc.Sections[0].Properties[0].Value);
    }

    /// <summary>
    /// Verifies that a section header containing inner brackets is parsed using the last <c>]</c> as the
    /// terminator.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSectionContainsInnerBrackets_ShouldUseLastClosingBracket()
    {
        const string input = "[[weird].txt]\nkey = value\n";

        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(input);

        Assert.AreEqual("[weird].txt", doc.Sections[0].Pattern);
    }

    /// <summary>
    /// Verifies that under <see cref="BoduConfigurationDuplicateKeyMode.Reject" /> a duplicate key throws.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyAndStrict_ShouldThrowExactly()
    {
        const string input = "[*]\nformat.indent.size = 4\nformat.indent.size = 2\n";

        Assert.ThrowsExactly<BoduConfigurationParseException>(() =>
        {
            _ = BoduConfigurationDocument.Parse(input, BoduConfigurationParseOptions.Strict);
        });
    }

    /// <summary>
    /// Verifies that under <see cref="BoduConfigurationDiagnosticMode.Collect" /> recoverable errors are
    /// gathered on the document rather than thrown.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDiagnosticModeIsCollect_ShouldNotThrow()
    {
        const string input = "[*]\nformat.indent.size\n";

        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(input, BoduConfigurationParseOptions.Relaxed);

        Assert.AreEqual(0, doc.Sections[0].Properties.Count);
    }
}
