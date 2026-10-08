// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.BlockScalars.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies block-scalar parsing: literal (<c>|</c>) and folded (<c>&gt;</c>) styles, chomping indicators, the
/// indentation indicator, and folding of more-indented and empty lines.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that a literal block scalar with strip chomping removes the final newline.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenLiteralBlockWithStripChomping_ShouldRemoveFinalNewline()
    {
        using var doc = YamlDocument.Parse("text: |-\n  line 1\n  line 2\n");
        Assert.AreEqual("line 1\nline 2", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that a literal block scalar with keep chomping preserves trailing newlines.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenLiteralBlockWithKeepChomping_ShouldPreserveTrailingNewlines()
    {
        using var doc = YamlDocument.Parse("text: |+\n  line 1\n  line 2\n\n");
        Assert.AreEqual("line 1\nline 2\n\n", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that a literal block scalar with clip chomping keeps a single trailing newline.</summary>
    [TestMethod]
    public void Parse_WhenLiteralBlockScalar_ShouldPreserveLineBreaks()
    {
        using var doc = YamlDocument.Parse("text: |\n  line1\n  line2\n");
        Assert.AreEqual("line1\nline2\n", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that an explicit indentation indicator preserves leading spaces as content.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenLiteralBlockHasIndentationIndicator_ShouldPreserveLeadingSpaces()
    {
        using var doc = YamlDocument.Parse("text: |2\n   leading\n   spaces\n");
        Assert.AreEqual(" leading\n spaces\n", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that a leading empty line in a literal block is preserved.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenLiteralBlockHasLeadingEmptyLine_ShouldPreserveIt()
    {
        using var doc = YamlDocument.Parse("text: |\n\n  content\n");
        Assert.AreEqual("\ncontent\n", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that a folded block scalar folds single newlines into spaces.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenFoldedBlock_ShouldFoldSingleNewlinesIntoSpaces()
    {
        using var doc = YamlDocument.Parse("text: >\n  line 1\n  line 2\n  line 3\n");
        Assert.AreEqual("line 1 line 2 line 3\n", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that a folded block scalar with strip chomping folds and removes the final newline.</summary>
    [TestMethod]
    public void Parse_WhenFoldedBlockScalarStripped_ShouldFold()
    {
        using var doc = YamlDocument.Parse("text: >-\n  line1\n  line2\n");
        Assert.AreEqual("line1 line2", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that a folded block scalar keeps newlines around more-indented lines.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenFoldedBlockHasMoreIndentedLines_ShouldPreserveNewlines()
    {
        using var doc = YamlDocument.Parse("text: >\n  line 1\n    more indented\n  line 2\n");
        Assert.AreEqual("line 1\n  more indented\nline 2\n", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>
    /// Verifies that a block scalar that begins on a sequence entry's line takes the entry's dash column as its parent
    /// indentation, so an indentation indicator counts from the dash rather than from the column after it.
    /// </summary>
    /// <param name="yaml">A sequence whose first entry is a block scalar with an indentation indicator.</param>
    /// <param name="expected">The content the entry is expected to hold.</param>
    [TestMethod]
    [DataRow("- |1\n  x\n", " x\n", DisplayName = "literal, indicator 1")]
    [DataRow("- |2\n  x\n", "x\n", DisplayName = "literal, indicator 2")]
    [DataRow("- |1\n  x\n#c\n", " x\n", DisplayName = "literal, indicator 1, then a comment")]
    [DataRow("- >1\n  folded\n", " folded\n", DisplayName = "folded, indicator 1")]
    [DataRow("  - |1\n    x\n", " x\n", DisplayName = "indented sequence, literal, indicator 1")]
    public void Parse_WhenSequenceEntryBlockScalarHasIndentationIndicator_ShouldCountFromDash(
        string yaml,
        string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, document.RootElement[0].GetString());
    }

    /// <summary>
    /// Verifies that a block scalar that begins on a nested sequence entry's line takes the inner entry's dash column
    /// as its parent indentation.
    /// </summary>
    /// <param name="yaml">A sequence holding a sequence whose first entry is a block scalar with an indicator.</param>
    /// <param name="expected">The content the inner entry is expected to hold.</param>
    [TestMethod]
    [DataRow("- - |1\n    x\n", " x\n", DisplayName = "compact nested sequence, indicator 1")]
    [DataRow("-\n  - |2\n    x\n", "x\n", DisplayName = "nested sequence on the next line, indicator 2")]
    public void Parse_WhenNestedSequenceEntryBlockScalarHasIndentationIndicator_ShouldCountFromInnerDash(
        string yaml,
        string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, document.RootElement[0][0].GetString());
    }

    /// <summary>
    /// Verifies that a block scalar that begins on a sequence entry's line detects content indented one column past the
    /// dash, rather than ending before it.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSequenceEntryBlockScalarContentIsOneColumnPastDash_ShouldDetectIt()
    {
        using var document = YamlDocument.Parse("- |\n x\n- >\n y\n");

        CollectionAssert.AreEqual(new[] { "x\n", "y\n" }, StringsOf(document.RootElement));
    }

    /// <summary>
    /// Verifies that YAML specification example 8.1 (block scalar header), block scalars with headers and comments as
    /// sequence entries, reads as the specification gives.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSpecificationExample8Point1_ShouldReadEachBlockScalar()
    {
        using var document = YamlDocument.Parse(
            "- | # Empty header\n literal\n- >1 # Indentation indicator\n  folded\n- |+ # Chomping indicator\n keep\n\n"
            + "- >1- # Both indicators\n  strip\n");

        CollectionAssert.AreEqual(
            new[] { "literal\n", " folded\n", "keep\n\n", " strip" },
            StringsOf(document.RootElement));
    }

    /// <summary>
    /// Verifies that YAML specification example 8.2 (block indentation indicator), block scalars as sequence entries,
    /// reads as the specification gives, a tab after the indentation of a more-indented line being content.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSpecificationExample8Point2_ShouldReadEachBlockScalar()
    {
        using var document = YamlDocument.Parse(
            "- |\n detected\n- >\n \n  \n  # detected\n- |1\n  explicit\n- >\n \t\n detected\n");

        CollectionAssert.AreEqual(
            new[] { "detected\n", "\n\n# detected\n", " explicit\n", "\t\ndetected\n" },
            StringsOf(document.RootElement));
    }

    /// <summary>
    /// Returns the string value of each entry of a sequence.
    /// </summary>
    /// <param name="sequence">The sequence element.</param>
    /// <returns>The entries' string values, in order.</returns>
    private static string[] StringsOf(YamlElement sequence) =>
        [.. sequence.EnumerateSequence().Select(entry => entry.GetString())];

    /// <summary>
    /// Verifies that a block scalar that begins on the line after a sequence entry's dash takes the entry's dash column
    /// as its parent indentation, so an indentation indicator counts from the dash, as when it begins on the dash's
    /// line.
    /// </summary>
    /// <param name="yaml">A sequence whose entry is a block scalar on the line after the dash.</param>
    /// <param name="expected">The content the entry is expected to hold.</param>
    [TestMethod]
    [DataRow("-\n  |1\n   x\n", "  x\n", DisplayName = "literal, indicator 1")]
    [DataRow("-\n  |2\n   x\n", " x\n", DisplayName = "literal, indicator 2")]
    [DataRow("-\n  >1\n   folded\n", "  folded\n", DisplayName = "folded, indicator 1")]
    [DataRow("  -\n    |1\n     x\n", "  x\n", DisplayName = "indented sequence, literal, indicator 1")]
    [DataRow("-\n |1\n  x\n", " x\n", DisplayName = "one column past the dash, indicator 1")]
    [DataRow("-\n  |\n   x\n", "x\n", DisplayName = "detected indentation")]
    public void Parse_WhenSequenceEntryBlockScalarStartsOnNextLine_ShouldCountFromDash(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, document.RootElement[0].GetString());
    }
}
