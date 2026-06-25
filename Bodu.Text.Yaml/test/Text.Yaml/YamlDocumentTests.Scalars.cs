// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.Scalars.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies plain, quoted, and block scalar decoding — chomping, folding, escapes, and plain-scalar colon and hash
/// rules — produces the expected decoded string for the <c>text</c> member.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>
    /// Yields scalar-decoding scenarios whose <c>text</c> member must decode to the expected string, sweeping block
    /// scalar chomping and indentation, folding, double- and single-quoted escapes, line continuation, multi-line plain
    /// folding, and the plain-scalar colon and hash rules.
    /// </summary>
    /// <returns>One <c>[DynamicData]</c> row per scalar-decoding scenario.</returns>
    public static IEnumerable<object[]> ScalarDecodingCases()
    {
        static object[] Row(string name, string yaml, string expected) =>
            [new ValidKat<string, string>(name, yaml, expected)];

        // Block scalars: chomping and indentation.
        yield return Row("literal strip", "text: |-\n  line 1\n  line 2\n", "line 1\nline 2");
        yield return Row("literal keep", "text: |+\n  line 1\n  line 2\n\n", "line 1\nline 2\n\n");
        yield return Row("literal clip", "text: |\n  line1\n  line2\n", "line1\nline2\n");
        yield return Row("indent indicator", "text: |2\n   leading\n   spaces\n", " leading\n spaces\n");
        yield return Row("leading empty line", "text: |\n\n  content\n", "\ncontent\n");
        yield return Row("folded fold", "text: >\n  line 1\n  line 2\n  line 3\n", "line 1 line 2 line 3\n");
        yield return Row("folded more indented", "text: >\n  line 1\n    more indented\n  line 2\n", "line 1\n  more indented\nline 2\n");
        yield return Row("folded strip", "text: >-\n  line1\n  line2\n", "line1 line2");

        // Quoted scalar escapes and folding.
        yield return Row("double-quoted escapes", "text: \"a\\nb\\tc\"\n", "a\nb\tc");
        yield return Row("double-quoted escapes with unicode", "text: \"line1\\nline2\\ttab\\u0041\"\n", "line1\nline2\ttabA");
        yield return Row("unicode 16-bit escape", "text: \"\\u0041\\u0042\"\n", "AB");
        yield return Row("unicode 32-bit escape", "text: \"\\U00000043\"\n", "C");
        yield return Row("single-quoted no escapes", "text: 'a\\nb # literal'\n", "a\\nb # literal");
        yield return Row("backslash line continuation", "text: \"line1 \\\n  line2\"\n", "line1 line2");

        // Plain scalar colon and hash rules.
        yield return Row("multi-line plain fold", "text: this is\n  a long value\n", "this is a long value");
        yield return Row("plain colon without space", "text: http://example.com\n", "http://example.com");
        yield return Row("plain hash without space", "text: hash#tag\n", "hash#tag");
        yield return Row("plain hash comment truncates", "text: hello # comment\n", "hello");
        yield return Row("quoted hash is literal", "text: \"hello # not a comment\"\n", "hello # not a comment");
    }

    /// <summary>
    /// Verifies that each scalar-decoding scenario in the catalogue decodes the <c>text</c> member to its expected
    /// string.
    /// </summary>
    /// <param name="kat">The scalar-decoding scenario under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(ScalarDecodingCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenScalar_ShouldDecodeToExpectedString(ValidKat<string, string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        using var d = Doc(kat.Input);
        Assert.AreEqual(kat.Expected, d.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that single-quoted scalars decode doubled quotes and are never type-resolved.</summary>
    [TestMethod]
    public void Parse_WhenSingleQuoted_ShouldDecodeDoubledQuoteAndStayString()
    {
        using var doc = YamlDocument.Parse("a: 'it''s 42'\nb: '123'\n");
        Assert.AreEqual("it's 42", doc.RootElement.GetProperty("a").GetString());
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("b").ValueKind);
        Assert.AreEqual("123", doc.RootElement.GetProperty("b").GetString());
    }

    /// <summary>Verifies that quoted scalars that look like numbers remain strings.</summary>
    [TestMethod]
    public void Parse_WhenQuotedLooksLikeNumber_ShouldStayString()
    {
        using var d = Doc("id: \"007\"\nversion: \"1.2.3\"\n");
        Assert.AreEqual(YamlValueKind.String, d.RootElement.GetProperty("id").ValueKind);
        Assert.AreEqual("007", d.RootElement.GetProperty("id").GetString());
        Assert.AreEqual("1.2.3", d.RootElement.GetProperty("version").GetString());
    }
}
