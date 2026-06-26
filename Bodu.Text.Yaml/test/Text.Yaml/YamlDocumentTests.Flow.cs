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
        var root = doc.RootElement;

        var nums = root.GetProperty("nums");
        Assert.AreEqual(3, nums.GetSequenceLength());
        Assert.AreEqual(2L, nums[1].GetInt64());

        var point = root.GetProperty("point");
        Assert.AreEqual(10L, point.GetProperty("x").GetInt64());
        Assert.AreEqual(20L, point.GetProperty("y").GetInt64());
    }

    /// <summary>Verifies that nested flow collections parse.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenNestedFlowCollections_ShouldParse()
    {
        using var doc = YamlDocument.Parse("data: [{x: 1}, {x: 2}]\n");
        var data = doc.RootElement.GetProperty("data");
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
}
