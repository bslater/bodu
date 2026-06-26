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
}
