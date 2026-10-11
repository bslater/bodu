// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.Flow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies flow-collection parsing: inline and nested sequences and mappings, multi-line flow, quoted flow keys,
/// and empty flow collections.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that flow collections are parsed.</summary>
    [TestMethod]
    public void Parse_WhenFlowCollections_ShouldParse()
    {
        using var doc = YamlDocument.Parse("nums: [1, 2, 3]\npoint: {x: 10, y: 20}\n");
        YamlElement root = doc.RootElement;

        YamlElement nums = root.GetProperty("nums");
        Assert.AreEqual(3, nums.GetSequenceLength());
        Assert.AreEqual(2L, nums[1].GetInt64());

        YamlElement point = root.GetProperty("point");
        Assert.AreEqual(10L, point.GetProperty("x").GetInt64());
        Assert.AreEqual(20L, point.GetProperty("y").GetInt64());
    }

    /// <summary>Verifies that nested flow collections parse.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenNestedFlowCollections_ShouldParse()
    {
        using var doc = YamlDocument.Parse("data: [{x: 1}, {x: 2}]\n");
        YamlElement data = doc.RootElement.GetProperty("data");
        Assert.AreEqual(2, data.GetSequenceLength());
        Assert.AreEqual(1L, data[0].GetProperty("x").GetInt64());
        Assert.AreEqual(2L, data[1].GetProperty("x").GetInt64());
    }

    /// <summary>Verifies that a flow collection spanning multiple lines parses.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenFlowCollectionSpansLines_ShouldParse()
    {
        using var doc = YamlDocument.Parse("items: [\n  1,\n  2,\n  3,\n]\n");
        Assert.AreEqual(3, doc.RootElement.GetProperty("items").GetSequenceLength());
        Assert.AreEqual(2L, doc.RootElement.GetProperty("items")[1].GetInt64());
    }

    /// <summary>Verifies that a quoted flow key containing colons parses.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenFlowKeyIsQuotedWithColons_ShouldParse()
    {
        using var doc = YamlDocument.Parse("config: {\"a:b:c\": value}\n");
        Assert.AreEqual("value", doc.RootElement.GetProperty("config").GetProperty("a:b:c").GetString());
    }

    /// <summary>Verifies that empty flow collections resolve to an empty sequence and mapping.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenEmptyFlowCollections_ShouldResolveToEmpty()
    {
        using var doc = YamlDocument.Parse("items: []\nconfig: {}\n");
        Assert.AreEqual(YamlValueKind.Sequence, doc.RootElement.GetProperty("items").ValueKind);
        Assert.AreEqual(0, doc.RootElement.GetProperty("items").GetSequenceLength());
        Assert.AreEqual(YamlValueKind.Mapping, doc.RootElement.GetProperty("config").ValueKind);
    }

    /// <summary>
    /// Verifies that a <c>?</c> followed by a separator inside a flow sequence begins a single-pair mapping entry, with
    /// an explicit key and an optional value.
    /// </summary>
    /// <param name="yaml">A flow sequence holding an explicit-key entry.</param>
    /// <param name="expected">The sequence expected, as JSON.</param>
    [TestMethod]
    [DataRow("[ ? ]", """[{"":null}]""", DisplayName = "an empty key")]
    [DataRow("[? 123]", """[{"123":null}]""", DisplayName = "a key with no value")]
    [DataRow("[ 123, ? ]", """[123,{"":null}]""", DisplayName = "an empty key after an entry")]
    [DataRow("[ 123, ? 456 ]", """[123,{"456":null}]""", DisplayName = "a key with no value after an entry")]
    [DataRow("[ ? a : b ]", """[{"a":"b"}]""", DisplayName = "a key and its value")]
    [DataRow("[ ? : b ]", """[{"":"b"}]""", DisplayName = "an empty key and a value")]
    [DataRow("[ ? a\n  : b ]", """[{"a":"b"}]""", DisplayName = "a value indicator on the next line")]
    [DataRow("[ ? a, b ]", """[{"a":null},"b"]""", DisplayName = "a key with no value, then an entry")]
    public void Parse_WhenFlowSequenceHoldsExplicitKey_ShouldReadSinglePairMapping(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that a <c>?</c> followed by a character other than a separator inside a flow sequence begins a plain
    /// scalar, not an explicit key.
    /// </summary>
    [TestMethod]
    public void Parse_WhenFlowSequenceEntryStartsWithQuestionMark_ShouldReadPlainScalar()
    {
        using var document = YamlDocument.Parse("[ ?a, ?b ]");

        Assert.AreEqual("""["?a","?b"]""", ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that an empty entry in a flow mapping, a comma with no entry before it, is rejected, as it is in a flow
    /// sequence.
    /// </summary>
    /// <param name="yaml">A flow mapping with an empty entry.</param>
    [TestMethod]
    [DataRow("{ , }", DisplayName = "a lone comma")]
    [DataRow("{,}", DisplayName = "a lone comma, unspaced")]
    [DataRow("{ , a: 1 }", DisplayName = "a leading comma")]
    [DataRow("{foo, bar,, baz}", DisplayName = "two consecutive commas")]
    [DataRow("{ a: 1,\n  ,\n  b: 2 }", DisplayName = "a comma alone on a line")]
    public void Parse_WhenFlowMappingHasEmptyEntry_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that a single trailing comma after a flow mapping's last entry is accepted.
    /// </summary>
    [TestMethod]
    public void Parse_WhenFlowMappingHasTrailingComma_ShouldParse()
    {
        using var document = YamlDocument.Parse("{ a: 1, b, }");

        Assert.AreEqual("""{"a":1,"b":null}""", ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that the closing bracket of a flow collection that is a block mapping's value is rejected when it sits
    /// left of the column of the mapping's keys.
    /// </summary>
    /// <param name="yaml">A block mapping whose flow value closes left of the mapping's keys.</param>
    [TestMethod]
    [DataRow(" foo: {\n  bar: boom\n}\n", DisplayName = "a flow mapping, closed at column 0 under a key at column 1")]
    [DataRow(" foo: [\n  boom\n]\n", DisplayName = "a flow sequence, closed at column 0 under a key at column 1")]
    [DataRow("a:\n  foo: {\n    bar: boom\n }\n", DisplayName = "a nested flow mapping, closed one column left")]
    public void Parse_WhenFlowClosingBracketIsLeftOfItsKey_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that the closing bracket of a flow collection that is a block mapping's value may sit in the column of
    /// the mapping's keys.
    /// </summary>
    /// <param name="yaml">A block mapping whose flow value closes in the column of its keys.</param>
    /// <param name="expected">The document expected, as JSON.</param>
    [TestMethod]
    [DataRow("foo: {\n  bar: boom\n}\n", """{"foo":{"bar":"boom"}}""", DisplayName = "a flow mapping at column 0")]
    [DataRow("a:\n  foo: [\n    1\n  ]\n", """{"a":{"foo":[1]}}""", DisplayName = "a nested flow sequence")]
    public void Parse_WhenFlowClosingBracketIsInItsKeyColumn_ShouldParse(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that a plain scalar in a flow collection may continue on following lines, which fold as in block
    /// context: a single line break into a space, and each blank line into a line feed.
    /// </summary>
    /// <param name="yaml">A flow collection holding a plain scalar that spans lines.</param>
    /// <param name="expected">The collection expected, as JSON.</param>
    [TestMethod]
    [DataRow("[ a\n b ]", """["a b"]""", DisplayName = "a sequence entry over two lines")]
    [DataRow("[ a\n\n b ]", """["a\nb"]""", DisplayName = "a sequence entry with a blank line")]
    [DataRow("[ a\n  b\n  c, d ]", """["a b c","d"]""", DisplayName = "a sequence entry over three lines")]
    [DataRow("{ a: b\n c }", """{"a":"b c"}""", DisplayName = "a mapping value over two lines")]
    [DataRow(
        "- { single line: value}\n- { multi\n  line: value}\n",
        """[{"single line":"value"},{"multi line":"value"}]""",
        DisplayName = "a mapping key over two lines")]
    [DataRow("{ matches\n% : 20 }", """{"matches %":20}""", DisplayName = "a mapping key continued by a percent sign")]
    public void Parse_WhenFlowPlainScalarSpansLines_ShouldFoldIt(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that a comment line ends a plain scalar in a flow collection, so the scalar does not continue past it.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCommentLineFollowsFlowPlainScalar_ShouldEndIt()
    {
        using var document = YamlDocument.Parse("[ a\n # c\n , b ]");

        Assert.AreEqual("""["a","b"]""", ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that text after a comment line that ends a plain scalar in a flow sequence is not joined to it, and
    /// without a comma between them is rejected.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTextFollowsCommentLineAfterFlowPlainScalar_ShouldThrowYamlFormatException()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse("[ a\n # c\n b ]");
        });
    }

    /// <summary>
    /// Verifies that the implicit key of a single-pair mapping entry in a flow sequence must stay on one line, so a
    /// plain key that spans lines is rejected.
    /// </summary>
    [TestMethod]
    public void Parse_WhenFlowSequencePairKeySpansLines_ShouldThrowYamlFormatException()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse("[ multi\n line: value ]");
        });
    }

    /// <summary>
    /// Verifies that a value indicator directly after a JSON-like key, a quoted scalar, begins the key's value with no
    /// space before it, in a flow sequence and in a flow mapping.
    /// </summary>
    /// <param name="yaml">A flow collection with a quoted key followed directly by its value indicator.</param>
    /// <param name="expected">The collection expected, as JSON.</param>
    [TestMethod]
    [DataRow("[ \"JSON like\":adjacent ]", """[{"JSON like":"adjacent"}]""", DisplayName = "a double-quoted key")]
    [DataRow("[ 'single':adjacent ]", """[{"single":"adjacent"}]""", DisplayName = "a single-quoted key in a sequence")]
    [DataRow("[ \"a\":, b ]", """[{"a":null},"b"]""", DisplayName = "a quoted key with an empty value in a sequence")]
    [DataRow("{ \"a\":b, 'c':1 }", """{"a":"b","c":1}""", DisplayName = "quoted keys in a mapping")]
    public void Parse_WhenValueIndicatorIsAdjacentToQuotedKey_ShouldReadValue(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that a value indicator directly after a flow collection used as a key in a flow sequence is read as the
    /// key's value indicator, so the entry is rejected as a complex key rather than as malformed flow.
    /// </summary>
    [TestMethod]
    public void Parse_WhenValueIndicatorIsAdjacentToCollectionKey_ShouldRejectComplexKey()
    {
        var ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse("[ [a]:b ]");
        });

        Assert.AreEqual(YamlResourceStrings.Format_Invalid_YamlComplexKeyUnsupported, ex.Message);
    }

    /// <summary>
    /// Verifies that after a plain key in a flow mapping, a <c>:</c> followed by a character that could continue a
    /// plain scalar is not a value indicator, so a <c>:</c> that a comment separates from the key is rejected.
    /// </summary>
    /// <param name="yaml">A flow mapping whose plain key ends at a comment, with <c>:1</c> on the next line.</param>
    [TestMethod]
    [DataRow("{a\n#c\n:1}", DisplayName = "a comment line between the key and the value indicator")]
    [DataRow("{ a #c\n:1 }", DisplayName = "a comment after the key")]
    public void Parse_WhenColonAfterPlainFlowKeyIsFollowedByPlainCharacter_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }
}
