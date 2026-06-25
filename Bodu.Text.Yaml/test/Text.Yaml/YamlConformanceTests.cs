// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlConformanceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Reproduces high-value edge cases drawn from the canonical yaml-test-suite and the YamlDotNet, VYaml, and
/// PyYAML/libyaml test corpora, covering block scalars, folding, plain-scalar rules, quoting, implicit typing,
/// flow collections, anchors/merge keys, and document markers.
/// </summary>
[TestClass]
[TestCategory("Regression")]
public sealed class YamlConformanceTests
{
    /// <summary>Parses YAML under the default (1.2 core) schema and returns the root element.</summary>
    /// <param name="yaml">The YAML source.</param>
    /// <returns>The parsed document, which the caller must dispose.</returns>
    private static YamlDocument Doc(string yaml) => YamlDocument.Parse(yaml);

    /// <summary>Parses YAML under the YAML 1.1 schema and returns the document.</summary>
    /// <param name="yaml">The YAML source.</param>
    /// <returns>The parsed document, which the caller must dispose.</returns>
    private static YamlDocument Doc11(string yaml) =>
        YamlDocument.Parse(yaml, new YamlDocumentOptions { SpecVersion = YamlSpecVersion.V1_1 });

    // ---- Block scalars: indentation and chomping ----

    /// <summary>Verifies literal block scalar with strip chomping removes the final newline.</summary>
    [TestMethod]
    public void BlockScalar_LiteralStrip()
    {
        using var d = Doc("text: |-\n  line 1\n  line 2\n");
        Assert.AreEqual("line 1\nline 2", d.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies literal block scalar with keep chomping preserves trailing newlines.</summary>
    [TestMethod]
    public void BlockScalar_LiteralKeep()
    {
        using var d = Doc("text: |+\n  line 1\n  line 2\n\n");
        Assert.AreEqual("line 1\nline 2\n\n", d.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies an explicit indentation indicator preserves leading spaces as content.</summary>
    [TestMethod]
    public void BlockScalar_IndentIndicator()
    {
        using var d = Doc("text: |2\n   leading\n   spaces\n");
        Assert.AreEqual(" leading\n spaces\n", d.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies a leading empty line in a literal block is preserved.</summary>
    [TestMethod]
    public void BlockScalar_LeadingEmptyLine()
    {
        using var d = Doc("text: |\n\n  content\n");
        Assert.AreEqual("\ncontent\n", d.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies a folded block scalar folds single newlines into spaces.</summary>
    [TestMethod]
    public void BlockScalar_FoldedFold()
    {
        using var d = Doc("text: >\n  line 1\n  line 2\n  line 3\n");
        Assert.AreEqual("line 1 line 2 line 3\n", d.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies a folded block scalar keeps newlines around more-indented lines.</summary>
    [TestMethod]
    public void BlockScalar_FoldedMoreIndented()
    {
        using var d = Doc("text: >\n  line 1\n    more indented\n  line 2\n");
        Assert.AreEqual("line 1\n  more indented\nline 2\n", d.RootElement.GetProperty("text").GetString());
    }

    // ---- Empty values and nulls ----

    /// <summary>Verifies an empty mapping value resolves to null.</summary>
    [TestMethod]
    public void Empty_MappingValueIsNull()
    {
        using var d = Doc("key:\nother: value\n");
        Assert.AreEqual(YamlValueKind.Null, d.RootElement.GetProperty("key").ValueKind);
        Assert.AreEqual("value", d.RootElement.GetProperty("other").GetString());
    }

    /// <summary>Verifies the empty flow collections resolve to empty sequence and mapping.</summary>
    [TestMethod]
    public void Empty_FlowCollections()
    {
        using var d = Doc("items: []\nconfig: {}\n");
        Assert.AreEqual(YamlValueKind.Sequence, d.RootElement.GetProperty("items").ValueKind);
        Assert.AreEqual(0, d.RootElement.GetProperty("items").GetSequenceLength());
        Assert.AreEqual(YamlValueKind.Mapping, d.RootElement.GetProperty("config").ValueKind);
    }

    /// <summary>Verifies an explicit complex key with an empty value.</summary>
    [TestMethod]
    public void Empty_ExplicitKeyNullValue()
    {
        using var d = Doc("? key\n:\n");
        Assert.AreEqual(YamlValueKind.Null, d.RootElement.GetProperty("key").ValueKind);
    }

    // ---- Plain scalar colon and hash rules ----

    /// <summary>Verifies a colon without a following space stays within a plain scalar (a URL).</summary>
    [TestMethod]
    public void Plain_ColonWithoutSpace()
    {
        using var d = Doc("url: http://example.com\n");
        Assert.AreEqual("http://example.com", d.RootElement.GetProperty("url").GetString());
    }

    /// <summary>Verifies a hash without a preceding space does not start a comment.</summary>
    [TestMethod]
    public void Plain_HashWithoutSpace()
    {
        using var d = Doc("text: hash#tag\n");
        Assert.AreEqual("hash#tag", d.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies a space before a hash starts a comment that truncates the value.</summary>
    [TestMethod]
    public void Plain_HashCommentTruncates()
    {
        using var d = Doc("text: hello # comment\n");
        Assert.AreEqual("hello", d.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies a hash inside a quoted string is literal.</summary>
    [TestMethod]
    public void Quoted_HashIsLiteral()
    {
        using var d = Doc("text: \"hello # not a comment\"\n");
        Assert.AreEqual("hello # not a comment", d.RootElement.GetProperty("text").GetString());
    }

    // ---- Quoted scalar escapes and folding ----

    /// <summary>Verifies double-quoted escape sequences are decoded.</summary>
    [TestMethod]
    public void Quoted_DoubleEscapes()
    {
        using var d = Doc("text: \"a\\nb\\tc\"\n");
        Assert.AreEqual("a\nb\tc", d.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies 16-bit and 32-bit Unicode escapes are decoded.</summary>
    [TestMethod]
    public void Quoted_UnicodeEscapes()
    {
        using var d = Doc("a: \"\\u0041\\u0042\"\nb: \"\\U00000043\"\n");
        Assert.AreEqual("AB", d.RootElement.GetProperty("a").GetString());
        Assert.AreEqual("C", d.RootElement.GetProperty("b").GetString());
    }

    /// <summary>Verifies single-quoted scalars do not process escapes but collapse doubled quotes.</summary>
    [TestMethod]
    public void Quoted_SingleNoEscapes()
    {
        using var d = Doc("text: 'a\\nb # literal'\n");
        Assert.AreEqual("a\\nb # literal", d.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies a backslash before a line break folds without inserting a space.</summary>
    [TestMethod]
    public void Quoted_BackslashLineContinuation()
    {
        using var d = Doc("text: \"line1 \\\n  line2\"\n");
        Assert.AreEqual("line1 line2", d.RootElement.GetProperty("text").GetString());
    }

    // ---- Implicit typing ----

    /// <summary>Verifies that boolean case variants are recognized under the core schema.</summary>
    [TestMethod]
    public void Typing_BooleanVariants()
    {
        using var d = Doc("a: true\nb: True\nc: TRUE\nd: false\ne: False\n");
        Assert.IsTrue(d.RootElement.GetProperty("a").GetBoolean());
        Assert.IsTrue(d.RootElement.GetProperty("b").GetBoolean());
        Assert.IsTrue(d.RootElement.GetProperty("c").GetBoolean());
        Assert.IsFalse(d.RootElement.GetProperty("d").GetBoolean());
        Assert.IsFalse(d.RootElement.GetProperty("e").GetBoolean());
    }

    /// <summary>Verifies that yes/no remain strings under 1.2 but are booleans under 1.1.</summary>
    [TestMethod]
    public void Typing_NorwayProblem()
    {
        using var d12 = Doc("a: NO\nb: YES\n");
        Assert.AreEqual(YamlValueKind.String, d12.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual(YamlValueKind.String, d12.RootElement.GetProperty("b").ValueKind);

        using var d11 = Doc11("a: NO\nb: YES\n");
        Assert.IsFalse(d11.RootElement.GetProperty("a").GetBoolean());
        Assert.IsTrue(d11.RootElement.GetProperty("b").GetBoolean());
    }

    /// <summary>Verifies hexadecimal and octal integer forms resolve.</summary>
    [TestMethod]
    public void Typing_HexAndOctal()
    {
        using var d = Doc("hex: 0x1A\noct: 0o12\n");
        Assert.AreEqual(26L, d.RootElement.GetProperty("hex").GetInt64());
        Assert.AreEqual(10L, d.RootElement.GetProperty("oct").GetInt64());
    }

    /// <summary>Verifies binary integers and underscore digit groups resolve under 1.1.</summary>
    [TestMethod]
    public void Typing_BinaryAndUnderscore_Under11()
    {
        using var d = Doc11("bin: 0b1010\nbig: 1_000\nflt: 1_000.5\n");
        Assert.AreEqual(10L, d.RootElement.GetProperty("bin").GetInt64());
        Assert.AreEqual(1000L, d.RootElement.GetProperty("big").GetInt64());
        Assert.AreEqual(1000.5, d.RootElement.GetProperty("flt").GetDouble());
    }

    /// <summary>Verifies the special floating-point forms resolve.</summary>
    [TestMethod]
    public void Typing_SpecialFloats()
    {
        using var d = Doc("a: .inf\nb: -.inf\nc: .Inf\nd: .nan\n");
        Assert.IsTrue(double.IsPositiveInfinity(d.RootElement.GetProperty("a").GetDouble()));
        Assert.IsTrue(double.IsNegativeInfinity(d.RootElement.GetProperty("b").GetDouble()));
        Assert.IsTrue(double.IsPositiveInfinity(d.RootElement.GetProperty("c").GetDouble()));
        Assert.IsTrue(double.IsNaN(d.RootElement.GetProperty("d").GetDouble()));
    }

    /// <summary>Verifies quoted scalars that look like numbers remain strings.</summary>
    [TestMethod]
    public void Typing_QuotedLooksLikeNumber()
    {
        using var d = Doc("id: \"007\"\nversion: \"1.2.3\"\n");
        Assert.AreEqual(YamlValueKind.String, d.RootElement.GetProperty("id").ValueKind);
        Assert.AreEqual("007", d.RootElement.GetProperty("id").GetString());
        Assert.AreEqual("1.2.3", d.RootElement.GetProperty("version").GetString());
    }

    // ---- Flow collections ----

    /// <summary>Verifies nested flow collections parse.</summary>
    [TestMethod]
    public void Flow_Nested()
    {
        using var d = Doc("data: [{x: 1}, {x: 2}]\n");
        var data = d.RootElement.GetProperty("data");
        Assert.AreEqual(2, data.GetSequenceLength());
        Assert.AreEqual(1L, data[0].GetProperty("x").GetInt64());
        Assert.AreEqual(2L, data[1].GetProperty("x").GetInt64());
    }

    /// <summary>Verifies a flow collection spanning multiple lines parses.</summary>
    [TestMethod]
    public void Flow_Multiline()
    {
        using var d = Doc("items: [\n  1,\n  2,\n  3,\n]\n");
        Assert.AreEqual(3, d.RootElement.GetProperty("items").GetSequenceLength());
        Assert.AreEqual(2L, d.RootElement.GetProperty("items")[1].GetInt64());
    }

    /// <summary>Verifies a quoted flow key containing colons parses.</summary>
    [TestMethod]
    public void Flow_QuotedKeyWithColons()
    {
        using var d = Doc("config: {\"a:b:c\": value}\n");
        Assert.AreEqual("value", d.RootElement.GetProperty("config").GetProperty("a:b:c").GetString());
    }

    // ---- Anchors, aliases, merge keys ----

    /// <summary>Verifies an anchored mapping aliased elsewhere resolves to the same content.</summary>
    [TestMethod]
    public void Anchor_AliasResolves()
    {
        using var d = Doc("defaults: &x\n  a: 1\n  b: 2\nconfig: *x\n");
        Assert.AreEqual(1L, d.RootElement.GetProperty("config").GetProperty("a").GetInt64());
        Assert.AreEqual(2L, d.RootElement.GetProperty("config").GetProperty("b").GetInt64());
    }

    /// <summary>Verifies a merge key inserts defaults and explicit keys override them.</summary>
    [TestMethod]
    public void Merge_Override()
    {
        using var d = Doc("defaults: &x\n  a: 1\n  b: 2\nconfig:\n  <<: *x\n  a: 999\n");
        Assert.AreEqual(999L, d.RootElement.GetProperty("config").GetProperty("a").GetInt64());
        Assert.AreEqual(2L, d.RootElement.GetProperty("config").GetProperty("b").GetInt64());
    }

    /// <summary>Verifies an alias to an undefined anchor is rejected.</summary>
    [TestMethod]
    public void Anchor_UndefinedAliasThrows()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var d = Doc("config: *undefined\n");
        });
    }

    // ---- Document markers ----

    /// <summary>Verifies inline content after a document-start marker parses.</summary>
    [TestMethod]
    public void Document_InlineAfterStartMarker()
    {
        using var d = Doc("--- key: value\n");
        Assert.AreEqual("value", d.RootElement.GetProperty("key").GetString());
    }

    /// <summary>Verifies a single document followed by a document-end marker parses.</summary>
    [TestMethod]
    public void Document_EndMarker()
    {
        using var d = Doc("key: value\n...\n");
        Assert.AreEqual("value", d.RootElement.GetProperty("key").GetString());
    }

    /// <summary>Verifies multiple documents are returned in order.</summary>
    [TestMethod]
    public void Document_MultipleDocuments()
    {
        var docs = YamlDocument.ParseAllDocuments("---\nkey1: value1\n---\nkey2: value2\n");
        Assert.AreEqual(2, docs.Count);
        Assert.AreEqual("value1", docs[0].RootElement.GetProperty("key1").GetString());
        Assert.AreEqual("value2", docs[1].RootElement.GetProperty("key2").GetString());
    }

    // ---- Indentation and block structure ----

    /// <summary>Verifies a mapping value that is a block sequence at the key column parses.</summary>
    [TestMethod]
    public void Block_MixedMappingSequence()
    {
        using var d = Doc("key1: value1\nkey2:\n  - item1\n  - item2\n");
        Assert.AreEqual("value1", d.RootElement.GetProperty("key1").GetString());
        Assert.AreEqual(2, d.RootElement.GetProperty("key2").GetSequenceLength());
        Assert.AreEqual("item1", d.RootElement.GetProperty("key2")[0].GetString());
    }

    /// <summary>Verifies a sequence of mappings with multiple keys per entry parses.</summary>
    [TestMethod]
    public void Block_SequenceOfMultiKeyMappings()
    {
        using var d = Doc("- name: a\n  age: 1\n- name: b\n  age: 2\n");
        Assert.AreEqual("a", d.RootElement[0].GetProperty("name").GetString());
        Assert.AreEqual(2L, d.RootElement[1].GetProperty("age").GetInt64());
    }
}
