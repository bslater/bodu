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
}
