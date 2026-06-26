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
}
