// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.Structure.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies block structural parsing and document markers: mixed mapping/sequence nesting, multi-key mapping
/// entries, explicit <c>?</c> keys and their <c>:</c> values, and the <c>---</c> / <c>...</c> document markers.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that a mapping value that is a block sequence at the key column parses.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenMappingValueIsBlockSequence_ShouldParse()
    {
        using var doc = YamlDocument.Parse("key1: value1\nkey2:\n  - item1\n  - item2\n");
        Assert.AreEqual("value1", doc.RootElement.GetProperty("key1").GetString());
        Assert.AreEqual(2, doc.RootElement.GetProperty("key2").GetSequenceLength());
        Assert.AreEqual("item1", doc.RootElement.GetProperty("key2")[0].GetString());
    }

    /// <summary>Verifies that a sequence of mappings with multiple keys per entry parses.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenSequenceOfMultiKeyMappings_ShouldParse()
    {
        using var doc = YamlDocument.Parse("- name: a\n  age: 1\n- name: b\n  age: 2\n");
        Assert.AreEqual("a", doc.RootElement[0].GetProperty("name").GetString());
        Assert.AreEqual(2L, doc.RootElement[1].GetProperty("age").GetInt64());
    }

    /// <summary>Verifies that an inline scalar after a document-start marker parses.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenInlineScalarFollowsStartMarker_ShouldParse()
    {
        using var doc = YamlDocument.Parse("--- value\n");
        Assert.AreEqual("value", doc.RootElement.GetString());
    }

    /// <summary>Verifies that a block mapping beginning on the document-start line is rejected.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenInlineMappingFollowsStartMarker_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("--- key: value\n");
        });
    }

    /// <summary>Verifies that a single document followed by a document-end marker parses.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenDocumentEndMarkerFollows_ShouldParseBody()
    {
        using var doc = YamlDocument.Parse("key: value\n...\n");
        Assert.AreEqual("value", doc.RootElement.GetProperty("key").GetString());
    }

    /// <summary>Verifies that a simple block mapping resolves scalar value kinds correctly.</summary>
    [TestMethod]
    public void Parse_WhenBlockMapping_ShouldResolveScalarKinds()
    {
        using var doc = YamlDocument.Parse("a: 1\nb: two\nc: true\nd: 3.5\ne: null\nf: ~");
        YamlElement root = doc.RootElement;

        Assert.AreEqual(YamlValueKind.Mapping, root.ValueKind);
        Assert.AreEqual(1L, root.GetProperty("a").GetInt64());
        Assert.AreEqual("two", root.GetProperty("b").GetString());
        Assert.IsTrue(root.GetProperty("c").GetBoolean());
        Assert.AreEqual(3.5, root.GetProperty("d").GetDouble());
        Assert.AreEqual(YamlValueKind.Null, root.GetProperty("e").ValueKind);
        Assert.AreEqual(YamlValueKind.Null, root.GetProperty("f").ValueKind);
    }

    /// <summary>Verifies that a nested block mapping is parsed into nested mapping nodes.</summary>
    [TestMethod]
    public void Parse_WhenNestedMapping_ShouldNest()
    {
        using var doc = YamlDocument.Parse("server:\n  host: localhost\n  port: 8080\n");
        YamlElement server = doc.RootElement.GetProperty("server");

        Assert.AreEqual(YamlValueKind.Mapping, server.ValueKind);
        Assert.AreEqual("localhost", server.GetProperty("host").GetString());
        Assert.AreEqual(8080L, server.GetProperty("port").GetInt64());
    }

    /// <summary>Verifies that a block sequence is parsed into a sequence of scalars.</summary>
    [TestMethod]
    public void Parse_WhenBlockSequence_ShouldYieldElements()
    {
        using var doc = YamlDocument.Parse("- one\n- two\n- 3\n");
        YamlElement root = doc.RootElement;

        Assert.AreEqual(YamlValueKind.Sequence, root.ValueKind);
        Assert.AreEqual(3, root.GetSequenceLength());
        Assert.AreEqual("one", root[0].GetString());
        Assert.AreEqual("two", root[1].GetString());
        Assert.AreEqual(3L, root[2].GetInt64());
    }

    /// <summary>Verifies that a mapping whose value is a block sequence indented at the key column is parsed.</summary>
    [TestMethod]
    public void Parse_WhenMappingOfSequence_ShouldParse()
    {
        using var doc = YamlDocument.Parse("items:\n  - a\n  - b\nname: list\n");
        YamlElement root = doc.RootElement;

        YamlElement items = root.GetProperty("items");
        Assert.AreEqual(YamlValueKind.Sequence, items.ValueKind);
        Assert.AreEqual(2, items.GetSequenceLength());
        Assert.AreEqual("a", items[0].GetString());
        Assert.AreEqual("list", root.GetProperty("name").GetString());
    }

    /// <summary>Verifies that a sequence of mappings is parsed correctly.</summary>
    [TestMethod]
    public void Parse_WhenSequenceOfMappings_ShouldParse()
    {
        using var doc = YamlDocument.Parse("- name: a\n  age: 1\n- name: b\n  age: 2\n");
        YamlElement root = doc.RootElement;

        Assert.AreEqual(2, root.GetSequenceLength());
        Assert.AreEqual("a", root[0].GetProperty("name").GetString());
        Assert.AreEqual(2L, root[1].GetProperty("age").GetInt64());
    }

    /// <summary>Verifies that comments and blank lines are ignored.</summary>
    [TestMethod]
    public void Parse_WhenCommentsAndBlankLines_ShouldIgnore()
    {
        using var doc = YamlDocument.Parse("# header\n\na: 1   # inline\n\n# trailing\nb: 2\n");
        Assert.AreEqual(1L, doc.RootElement.GetProperty("a").GetInt64());
        Assert.AreEqual(2L, doc.RootElement.GetProperty("b").GetInt64());
    }

    /// <summary>Verifies that a leading document-start marker is accepted.</summary>
    [TestMethod]
    public void Parse_WhenDocumentStartMarker_ShouldParseBody()
    {
        using var doc = YamlDocument.Parse("---\nkey: value\n");
        Assert.AreEqual("value", doc.RootElement.GetProperty("key").GetString());
    }

    /// <summary>
    /// Verifies that a leading UTF-8 byte-order mark is consumed rather than treated as document content, so a file
    /// saved with a BOM parses identically to one without.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenLeadingByteOrderMark_ShouldParseBody()
    {
        using var doc = YamlDocument.Parse("\uFEFFkey: value\n");
        Assert.AreEqual(YamlValueKind.Mapping, doc.RootElement.ValueKind);
        Assert.AreEqual("value", doc.RootElement.GetProperty("key").GetString());
    }

    /// <summary>
    /// Verifies that a block sequence indented at the same column as its mapping key is parsed as that key's
    /// sequence value \u2014 the compact style of spec Example 8.22, ubiquitous in Kubernetes, Ansible, and CI manifests.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenBlockSequenceAtParentColumn_ShouldParseAsSequenceValue()
    {
        using var doc = YamlDocument.Parse("one:\n- 2\n- 3\nfour: 5\n");
        YamlElement root = doc.RootElement;

        Assert.AreEqual(YamlValueKind.Sequence, root.GetProperty("one").ValueKind);
        Assert.AreEqual(2, root.GetProperty("one").GetSequenceLength());
        Assert.AreEqual(2L, root.GetProperty("one")[0].GetInt64());
        Assert.AreEqual(3L, root.GetProperty("one")[1].GetInt64());
        Assert.AreEqual(5L, root.GetProperty("four").GetInt64());
    }

    /// <summary>
    /// Verifies that a compact sequence of mappings \u2014 each entry a block mapping whose keys sit at the sequence's
    /// own column, with the sequence at its parent key's column \u2014 parses, the most common real-world manifest shape.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenCompactSequenceOfMappings_ShouldParse()
    {
        using var doc = YamlDocument.Parse("items:\n- name: a\n  id: 1\n- name: b\n  id: 2\n");
        YamlElement items = doc.RootElement.GetProperty("items");

        Assert.AreEqual(YamlValueKind.Sequence, items.ValueKind);
        Assert.AreEqual(2, items.GetSequenceLength());
        Assert.AreEqual("a", items[0].GetProperty("name").GetString());
        Assert.AreEqual(2L, items[1].GetProperty("id").GetInt64());
    }

    /// <summary>
    /// Verifies that a same-column block sequence nests correctly inside another block mapping value, so the compact
    /// style composes to arbitrary depth.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenNestedCompactSequence_ShouldParse()
    {
        using var doc = YamlDocument.Parse("top:\n  a:\n  - 1\n  - 2\n");
        YamlElement a = doc.RootElement.GetProperty("top").GetProperty("a");

        Assert.AreEqual(YamlValueKind.Sequence, a.ValueKind);
        Assert.AreEqual(2, a.GetSequenceLength());
        Assert.AreEqual(1L, a[0].GetInt64());
        Assert.AreEqual(2L, a[1].GetInt64());
    }

    /// <summary>
    /// Verifies that a key with no value followed by an ordinary mapping entry still resolves to null, confirming
    /// the compact-sequence detection does not swallow unrelated following lines.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenEmptyValueFollowedByMappingEntry_ShouldResolveToNull()
    {
        using var doc = YamlDocument.Parse("a:\nb: 2\n");

        Assert.AreEqual(YamlValueKind.Null, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual(2L, doc.RootElement.GetProperty("b").GetInt64());
    }

    /// <summary>
    /// Verifies that an error's reported line number counts lone carriage-return line breaks, matching the lexer's
    /// line-break handling, so CR-only documents report accurate positions.
    /// </summary>
    [TestMethod]
    public void Parse_WhenErrorInCarriageReturnOnlyDocument_ShouldReportCorrectLine()
    {
        var ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var doc = YamlDocument.Parse("a: 1\rb: 2\rc: !unknown v\r");
        });

        Assert.AreEqual(3, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that the same error on an LF document reports the same line number as its CR-only equivalent.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenErrorInLineFeedDocument_ShouldReportCorrectLine()
    {
        var ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var doc = YamlDocument.Parse("a: 1\nb: 2\nc: !unknown v\n");
        });

        Assert.AreEqual(3, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that an explicit key with no <c>:</c> value line reads with a null value, whatever follows it: the end
    /// of the stream, another explicit key, an implicit key, or a less-indented line.
    /// </summary>
    /// <param name="yaml">A mapping with an explicit key that has no value line.</param>
    /// <param name="expected">The mapping expected, as JSON.</param>
    [TestMethod]
    [DataRow("?", """{"":null}""", DisplayName = "an empty explicit key alone")]
    [DataRow("?\n? a", """{"":null,"a":null}""", DisplayName = "an empty explicit key, then another")]
    [DataRow("? a\n? b\n", """{"a":null,"b":null}""", DisplayName = "two explicit keys")]
    [DataRow("? a\nb: 1\n", """{"a":null,"b":1}""", DisplayName = "an explicit key, then an implicit key")]
    [DataRow("c:\n  ? a\nd: 2\n", """{"c":{"a":null},"d":2}""", DisplayName = "nested explicit key, outer key")]
    public void Parse_WhenExplicitKeyHasNoValue_ShouldReadNullValue(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that a compact sequence or mapping may begin on the line of an explicit key's <c>:</c>, indented by
    /// its own column.
    /// </summary>
    /// <param name="yaml">A mapping whose explicit value is a compact collection.</param>
    /// <param name="expected">The mapping expected, as JSON.</param>
    [TestMethod]
    [DataRow("? a\n: -\tb", """{"a":["b"]}""", DisplayName = "a compact sequence, separated by a tab")]
    [DataRow("? a\n: - b\n  - c\n", """{"a":["b","c"]}""", DisplayName = "a compact sequence of two entries")]
    [DataRow("? a\n: -\tb\n  -  -\tc\n     - d\n", """{"a":["b",["c","d"]]}""", DisplayName = "nested compact")]
    [DataRow("? a\n: b: c\n  d: e\n", """{"a":{"b":"c","d":"e"}}""", DisplayName = "a compact mapping")]
    public void Parse_WhenExplicitValueIsCompactCollection_ShouldReadIt(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that an explicit key's <c>:</c> indented past the column of its <c>?</c> is rejected, since the value
    /// line must begin at the mapping's indentation.
    /// </summary>
    /// <param name="yaml">A mapping whose explicit value indicator is indented past its key.</param>
    [TestMethod]
    [DataRow("? \"foo\"\n : \"bar\"\n", DisplayName = "a quoted key, then a value indicator one column in")]
    [DataRow("? a\n  : b\n", DisplayName = "a plain key, then a value indicator two columns in")]
    public void Parse_WhenExplicitValueIndicatorIsIndentedPastKey_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that an anchor or tag before an explicit key's <c>?</c> on the same line is rejected, since a block
    /// mapping cannot begin on the line of its own node properties.
    /// </summary>
    /// <param name="yaml">A mapping whose explicit key follows node properties on its line.</param>
    [TestMethod]
    [DataRow("&a ? A", DisplayName = "an anchor, then a key with no value")]
    [DataRow("&a ? A\n: B\n", DisplayName = "an anchor, then a key and its value")]
    [DataRow("!!map ? A\n: B\n", DisplayName = "a tag, then a key and its value")]
    public void Parse_WhenPropertiesPrecedeExplicitKeyOnItsLine_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that a node property below an explicit key's <c>?</c> and not indented past it is not read as part of
    /// the key, so the line it begins is rejected.
    /// </summary>
    /// <param name="yaml">A mapping with a node property at the explicit key's column on the line after it.</param>
    [TestMethod]
    [DataRow("?\n!!null", DisplayName = "a tag below an empty key")]
    [DataRow("? &a\n!!str b\n", DisplayName = "a tag below a key's anchor")]
    public void Parse_WhenPropertyBelowExplicitKeyIsNotIndented_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that a tab between an explicit key's <c>:</c> and a compact sequence or mapping on the same line is
    /// rejected, since it would indent the collection.
    /// </summary>
    /// <param name="yaml">A mapping whose explicit value is a compact collection after a tab.</param>
    [TestMethod]
    [DataRow("? a\n:\t- b\n", DisplayName = "a compact sequence")]
    [DataRow("? a\n:\tb: c\n", DisplayName = "a compact mapping")]
    public void Parse_WhenTabPrecedesCompactExplicitValue_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that a tab between an explicit key's <c>:</c> and a scalar on the same line is separation, and the
    /// scalar is read as the value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTabPrecedesScalarExplicitValue_ShouldReadScalar()
    {
        using var document = YamlDocument.Parse("? a\n:\tb\n");

        Assert.AreEqual("""{"a":"b"}""", ToJson(document.RootElement));
    }
}
