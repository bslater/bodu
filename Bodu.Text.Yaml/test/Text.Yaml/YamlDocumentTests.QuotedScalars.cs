// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.QuotedScalars.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies quoted-scalar parsing: double-quoted escape and Unicode decoding, line-break folding, single-quoted
/// literal handling with doubled-quote collapse, and number-like quoted scalars staying strings.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that double-quoted control escapes and a 16-bit Unicode escape are decoded.</summary>
    [TestMethod]
    public void Parse_WhenDoubleQuotedEscapes_ShouldDecode()
    {
        using var doc = YamlDocument.Parse("text: \"line1\\nline2\\ttab\\u0041\"\n");
        Assert.AreEqual("line1\nline2\ttabA", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that double-quoted escape sequences for newline and tab are decoded.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenDoubleQuotedControlEscapes_ShouldDecode()
    {
        using var doc = YamlDocument.Parse("text: \"a\\nb\\tc\"\n");
        Assert.AreEqual("a\nb\tc", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that 16-bit and 32-bit Unicode escapes are decoded.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenUnicodeEscapes_ShouldDecode()
    {
        using var doc = YamlDocument.Parse("a: \"\\u0041\\u0042\"\nb: \"\\U00000043\"\n");
        Assert.AreEqual("AB", doc.RootElement.GetProperty("a").GetString());
        Assert.AreEqual("C", doc.RootElement.GetProperty("b").GetString());
    }

    /// <summary>Verifies that a hash inside a double-quoted string is literal and not a comment.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenHashInsideDoubleQuotes_ShouldBeLiteral()
    {
        using var doc = YamlDocument.Parse("text: \"hello # not a comment\"\n");
        Assert.AreEqual("hello # not a comment", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that a backslash before a line break folds without inserting a space.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenDoubleQuotedBackslashLineBreak_ShouldFoldWithoutSpace()
    {
        using var doc = YamlDocument.Parse("text: \"line1 \\\n  line2\"\n");
        Assert.AreEqual("line1 line2", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that single-quoted scalars collapse doubled quotes and are never type-resolved.</summary>
    [TestMethod]
    public void Parse_WhenSingleQuoted_ShouldDecodeDoubledQuoteAndStayString()
    {
        using var doc = YamlDocument.Parse("a: 'it''s 42'\nb: '123'\n");
        Assert.AreEqual("it's 42", doc.RootElement.GetProperty("a").GetString());
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("b").ValueKind);
        Assert.AreEqual("123", doc.RootElement.GetProperty("b").GetString());
    }

    /// <summary>Verifies that single-quoted scalars do not process escape sequences.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenSingleQuotedWithBackslash_ShouldNotProcessEscapes()
    {
        using var doc = YamlDocument.Parse("text: 'a\\nb # literal'\n");
        Assert.AreEqual("a\\nb # literal", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that quoted scalars that look like numbers remain strings.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenQuotedScalarLooksLikeNumber_ShouldStayString()
    {
        using var doc = YamlDocument.Parse("id: \"007\"\nversion: \"1.2.3\"\n");
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("id").ValueKind);
        Assert.AreEqual("007", doc.RootElement.GetProperty("id").GetString());
        Assert.AreEqual("1.2.3", doc.RootElement.GetProperty("version").GetString());
    }

    /// <summary>
    /// Verifies that a continuation line following an escaped line break must satisfy the same indentation rule as an
    /// ordinary folded continuation line.
    /// </summary>
    [TestMethod]
    public void Parse_WhenEscapedLineBreakContinuationUnderIndented_ShouldThrowYamlFormatException()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var doc = YamlDocument.Parse("text: \"line1 \\\nline2\"\n");
        });
    }

    /// <summary>
    /// Verifies that a continuation line following an escaped line break may not begin a document boundary marker,
    /// matching the rule applied to ordinary folded continuation lines.
    /// </summary>
    [TestMethod]
    public void Parse_WhenEscapedLineBreakContinuationAtDocumentBoundary_ShouldThrowYamlFormatException()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var doc = YamlDocument.Parse("text: \"line1 \\\n--- x\"\n");
        });
    }

    /// <summary>
    /// Verifies that the ordinary folded continuation line rejects under-indentation, pinning the behavior the
    /// escaped-line-break branch is required to match.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenFoldedContinuationUnderIndented_ShouldThrowYamlFormatException()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var doc = YamlDocument.Parse("text: \"line1\nline2\"\n");
        });
    }

    /// <summary>
    /// Verifies that a backslash followed by a literal tab in a double-quoted scalar is the escaped tab, as a backslash
    /// followed by <c>t</c> is, wherever it falls among the scalar's lines; trailing white space after it still folds.
    /// </summary>
    /// <param name="yaml">A document with a backslash and a literal tab inside double quotes.</param>
    /// <param name="expected">The document expected, as JSON.</param>
    [TestMethod]
    [DataRow("- \"\\\t\"", """["\t"]""", DisplayName = "alone")]
    [DataRow("\"2 inline\\\ttab\"\n", "\"2 inline\\ttab\"", DisplayName = "inside a line")]
    [DataRow("\"2 leading\n    \\\ttab\"\n", "\"2 leading \\ttab\"", DisplayName = "opening a continuation line")]
    [DataRow("\"5 leading\n    \\\t  tab\"\n", "\"5 leading \\t  tab\"", DisplayName = "before spaces, opening a line")]
    [DataRow("\"3 trailing\\\t\n    tab\"\n", "\"3 trailing\\t tab\"", DisplayName = "ending a line")]
    [DataRow("\"4 trailing\\\t  \n    tab\"\n", "\"4 trailing\\t tab\"", DisplayName = "before trailing spaces")]
    public void Parse_WhenBackslashPrecedesLiteralTab_ShouldDecodeTab(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that a high-surrogate escape followed directly by a low-surrogate escape, as JSON writes a character
    /// outside the Basic Multilingual Plane, decodes to that one character.
    /// </summary>
    /// <param name="yaml">A document with an escaped surrogate pair in double quotes.</param>
    /// <param name="prefix">The text expected before the character.</param>
    [TestMethod]
    [DataRow("\"\\uD83D\\uDC4D\"", "", DisplayName = "alone")]
    [DataRow("Test: \"x\\uD83D\\uDC4D\"\n", "x", DisplayName = "as a mapping value, after text")]
    public void Parse_WhenEscapedSurrogatePair_ShouldDecodeOneCharacter(string yaml, string prefix)
    {
        using var document = YamlDocument.Parse(yaml);

        YamlElement value = document.RootElement.ValueKind == YamlValueKind.Mapping
            ? document.RootElement.GetProperty("Test")
            : document.RootElement;
        Assert.AreEqual(prefix + char.ConvertFromUtf32(0x1F44D), value.GetString());
    }

    /// <summary>
    /// Verifies that a surrogate escape that is not a high surrogate followed directly by a low surrogate is rejected.
    /// </summary>
    /// <param name="yaml">A document with an unpaired surrogate escape.</param>
    [TestMethod]
    [DataRow("\"\\uD83D\"", DisplayName = "a lone high surrogate")]
    [DataRow("\"\\uDC4D\"", DisplayName = "a lone low surrogate")]
    [DataRow("\"\\uDC4D\\uD83D\"", DisplayName = "a reversed pair")]
    [DataRow("\"\\uD83Dabc\"", DisplayName = "a high surrogate followed by text")]
    [DataRow("\"\\uD83D\\u0041\"", DisplayName = "a high surrogate followed by another escape")]
    [DataRow("\"\\uD83D\\uD83D\"", DisplayName = "two high surrogates")]
    public void Parse_WhenSurrogateEscapeIsUnpaired_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }
}
