// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.Tags.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies core-tag composition: the <c>!!str</c> tag forces a string, the <c>!!int</c> tag reinterprets a quoted
/// scalar as an integer, <c>%</c> escapes and verbatim tags resolve, and a node with two tags is rejected.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that the <c>!!str</c> tag forces a numeric-looking scalar to remain a string.</summary>
    [TestMethod]
    public void Parse_WhenStrTag_ShouldForceString()
    {
        using var doc = YamlDocument.Parse("a: !!str 123\nb: !!str true\n");
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual("123", doc.RootElement.GetProperty("a").GetString());
        Assert.AreEqual("true", doc.RootElement.GetProperty("b").GetString());
    }

    /// <summary>Verifies that the <c>!!int</c> tag reinterprets a quoted scalar as an integer.</summary>
    [TestMethod]
    public void Parse_WhenIntTag_ShouldForceInteger()
    {
        using var doc = YamlDocument.Parse("a: !!int \"42\"\n");
        Assert.AreEqual(YamlValueKind.Integer, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual(42L, doc.RootElement.GetProperty("a").GetInt64());
    }

    /// <summary>Verifies that the <c>!!int</c> tag accepts a hexadecimal literal, resolving it under the schema.</summary>
    [TestMethod]
    public void Parse_WhenIntTagHasHexLiteral_ShouldResolveValue()
    {
        using var doc = YamlDocument.Parse("a: !!int 0x2A\n");
        Assert.AreEqual(YamlValueKind.Integer, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual(42L, doc.RootElement.GetProperty("a").GetInt64());
    }

    /// <summary>Verifies that the <c>!!float</c> tag widens an integral literal to a floating-point value.</summary>
    [TestMethod]
    public void Parse_WhenFloatTagHasIntegralLiteral_ShouldWidenToFloat()
    {
        using var doc = YamlDocument.Parse("a: !!float 42\n");
        Assert.AreEqual(YamlValueKind.Float, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual(42.0, doc.RootElement.GetProperty("a").GetDouble());
    }

    /// <summary>Verifies that the <c>!!int</c> tag with non-integer content is rejected.</summary>
    [TestMethod]
    public void Parse_WhenIntTagHasNonIntegerContent_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("a: !!int abc\n");
        });
    }

    /// <summary>Verifies that the <c>!!bool</c> tag with invalid content is rejected.</summary>
    [TestMethod]
    public void Parse_WhenBoolTagHasInvalidContent_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("a: !!bool not-a-bool\n");
        });
    }

    /// <summary>Verifies that the <c>!!float</c> tag with non-numeric content is rejected.</summary>
    [TestMethod]
    public void Parse_WhenFloatTagHasInvalidContent_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("a: !!float abc\n");
        });
    }

    /// <summary>Verifies that the <c>!!str</c> tag wrapping a boolean-like value keeps it a string.</summary>
    [TestMethod]
    public void Parse_WhenStrTagWrapsBoolLikeValue_ShouldRemainString()
    {
        using var doc = YamlDocument.Parse("a: !!str true\n");
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual("true", doc.RootElement.GetProperty("a").GetString());
    }

    /// <summary>Verifies that the <c>!!null</c> tag forces the null kind onto an otherwise non-null scalar.</summary>
    [TestMethod]
    public void Parse_WhenNullTag_ShouldForceNull()
    {
        using var doc = YamlDocument.Parse("a: !!null\nb: !!null value\n");
        Assert.AreEqual(YamlValueKind.Null, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual(YamlValueKind.Null, doc.RootElement.GetProperty("b").ValueKind);
    }

    /// <summary>
    /// Verifies that a node with two tags is rejected, rather than the second tag being read as the start of a plain
    /// scalar.
    /// </summary>
    /// <param name="yaml">A document with a node that carries two tags.</param>
    [TestMethod]
    [DataRow("! ! ! ...a", DisplayName = "three non-specific tags")]
    [DataRow("!!str !!int 5", DisplayName = "two core tags")]
    [DataRow("- !!str !!str x", DisplayName = "a sequence entry")]
    [DataRow("[ !!str !!str x ]", DisplayName = "a flow sequence entry")]
    [DataRow("k: !!str !!str x", DisplayName = "a mapping value")]
    public void Parse_WhenNodeHasTwoTags_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that a <c>%</c>-escaped character in a tag is decoded before the tag is resolved, so an escaped core
    /// tag applies.
    /// </summary>
    /// <param name="yaml">A document whose tag holds a <c>%</c> escape.</param>
    /// <param name="expected">The document expected, as JSON.</param>
    [TestMethod]
    [DataRow("- !!%73tr 1\n", """["1"]""", DisplayName = "a secondary-handle tag")]
    [DataRow("%TAG !e! tag:yaml.org,2002:\n--- !e!%73tr 1", "\"1\"", DisplayName = "a named-handle tag")]
    [DataRow("!<tag:yaml.org,2002:%73tr> 1", "\"1\"", DisplayName = "a verbatim tag")]
    public void Parse_WhenTagHasPercentEscape_ShouldDecodeIt(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that a verbatim tag runs to its closing <c>&gt;</c>, so a comma inside it, as in every core tag URI,
    /// does not end it.
    /// </summary>
    /// <param name="yaml">A document with a verbatim core tag.</param>
    /// <param name="expected">The document expected, as JSON.</param>
    [TestMethod]
    [DataRow("!<tag:yaml.org,2002:str> 12", "\"12\"", DisplayName = "on the root")]
    [DataRow("[ !<tag:yaml.org,2002:str> 12, 3 ]", """["12",3]""", DisplayName = "in a flow sequence")]
    [DataRow("a: !<tag:yaml.org,2002:int> \"7\"", """{"a":7}""", DisplayName = "on a mapping value")]
    public void Parse_WhenVerbatimTagHoldsComma_ShouldReadWholeTag(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that a tag with a malformed <c>%</c> escape, or a verbatim tag with no closing <c>&gt;</c>, is
    /// rejected.
    /// </summary>
    /// <param name="yaml">A document with a malformed tag.</param>
    [TestMethod]
    [DataRow("!!%7 1", DisplayName = "an escape with one hexadecimal digit")]
    [DataRow("!!%zz 1", DisplayName = "an escape with no hexadecimal digits")]
    [DataRow("!!s%FFtr 1", DisplayName = "an escape that is not UTF-8")]
    [DataRow("!<tag:yaml.org,2002:str 1", DisplayName = "an unterminated verbatim tag")]
    public void Parse_WhenTagIsMalformed_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }
}
