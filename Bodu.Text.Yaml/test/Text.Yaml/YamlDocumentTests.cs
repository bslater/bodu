// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies parsing of YAML source into the read-only <see cref="YamlDocument" /> object model.
/// </summary>
[TestClass]
public partial class YamlDocumentTests
{
    /// <summary>Verifies that a simple block mapping resolves scalar value kinds correctly.</summary>
    [TestMethod]
    public void Parse_WhenBlockMapping_ShouldResolveScalarKinds()
    {
        using var doc = YamlDocument.Parse("a: 1\nb: two\nc: true\nd: 3.5\ne: null\nf: ~");
        var root = doc.RootElement;

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
        var server = doc.RootElement.GetProperty("server");

        Assert.AreEqual(YamlValueKind.Mapping, server.ValueKind);
        Assert.AreEqual("localhost", server.GetProperty("host").GetString());
        Assert.AreEqual(8080L, server.GetProperty("port").GetInt64());
    }

    /// <summary>Verifies that a block sequence is parsed into a sequence of scalars.</summary>
    [TestMethod]
    public void Parse_WhenBlockSequence_ShouldYieldElements()
    {
        using var doc = YamlDocument.Parse("- one\n- two\n- 3\n");
        var root = doc.RootElement;

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
        var root = doc.RootElement;

        var items = root.GetProperty("items");
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
        var root = doc.RootElement;

        Assert.AreEqual(2, root.GetSequenceLength());
        Assert.AreEqual("a", root[0].GetProperty("name").GetString());
        Assert.AreEqual(2L, root[1].GetProperty("age").GetInt64());
    }

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

    /// <summary>Verifies that double-quoted escapes are decoded.</summary>
    [TestMethod]
    public void Parse_WhenDoubleQuotedEscapes_ShouldDecode()
    {
        using var doc = YamlDocument.Parse("text: \"line1\\nline2\\ttab\\u0041\"\n");
        Assert.AreEqual("line1\nline2\ttabA", doc.RootElement.GetProperty("text").GetString());
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

    /// <summary>Verifies that a literal block scalar preserves line breaks with clip chomping.</summary>
    [TestMethod]
    public void Parse_WhenLiteralBlockScalar_ShouldPreserveLineBreaks()
    {
        using var doc = YamlDocument.Parse("text: |\n  line1\n  line2\n");
        Assert.AreEqual("line1\nline2\n", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that a folded block scalar folds line breaks into spaces with strip chomping.</summary>
    [TestMethod]
    public void Parse_WhenFoldedBlockScalarStripped_ShouldFold()
    {
        using var doc = YamlDocument.Parse("text: >-\n  line1\n  line2\n");
        Assert.AreEqual("line1 line2", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that the YAML 1.2 core schema does not treat <c>no</c> as a boolean (the Norway problem).</summary>
    [TestMethod]
    public void Parse_WhenNorwayUnderV12_ShouldStayString()
    {
        using var doc = YamlDocument.Parse("country: no\n");
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("country").ValueKind);
        Assert.AreEqual("no", doc.RootElement.GetProperty("country").GetString());
    }

    /// <summary>Verifies that the YAML 1.1 schema treats <c>no</c> as the boolean false.</summary>
    [TestMethod]
    public void Parse_WhenNorwayUnderV11_ShouldBeBoolean()
    {
        using var doc = YamlDocument.Parse("country: no\n", new YamlDocumentOptions { SpecVersion = YamlSpecVersion.V1_1 });
        Assert.AreEqual(YamlValueKind.Boolean, doc.RootElement.GetProperty("country").ValueKind);
        Assert.IsFalse(doc.RootElement.GetProperty("country").GetBoolean());
    }

    /// <summary>Verifies that comments and blank lines are ignored.</summary>
    [TestMethod]
    public void Parse_WhenCommentsAndBlankLines_ShouldIgnore()
    {
        using var doc = YamlDocument.Parse("# header\n\na: 1   # inline\n\n# trailing\nb: 2\n");
        Assert.AreEqual(1L, doc.RootElement.GetProperty("a").GetInt64());
        Assert.AreEqual(2L, doc.RootElement.GetProperty("b").GetInt64());
    }

    /// <summary>Verifies that a multi-line plain scalar folds continuation lines into spaces.</summary>
    [TestMethod]
    public void Parse_WhenMultiLinePlain_ShouldFold()
    {
        using var doc = YamlDocument.Parse("text: this is\n  a long value\n");
        Assert.AreEqual("this is a long value", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that a leading document-start marker is accepted.</summary>
    [TestMethod]
    public void Parse_WhenDocumentStartMarker_ShouldParseBody()
    {
        using var doc = YamlDocument.Parse("---\nkey: value\n");
        Assert.AreEqual("value", doc.RootElement.GetProperty("key").GetString());
    }

    /// <summary>Verifies that hexadecimal and negative integers resolve.</summary>
    [TestMethod]
    public void Parse_WhenHexAndNegativeIntegers_ShouldResolve()
    {
        using var doc = YamlDocument.Parse("hex: 0x1F\nneg: -42\n");
        Assert.AreEqual(31L, doc.RootElement.GetProperty("hex").GetInt64());
        Assert.AreEqual(-42L, doc.RootElement.GetProperty("neg").GetInt64());
    }

    /// <summary>Verifies that float special forms resolve.</summary>
    [TestMethod]
    public void Parse_WhenFloatSpecials_ShouldResolve()
    {
        using var doc = YamlDocument.Parse("inf: .inf\nninf: -.Inf\nnan: .nan\nexp: 1e3\n");
        Assert.IsTrue(double.IsPositiveInfinity(doc.RootElement.GetProperty("inf").GetDouble()));
        Assert.IsTrue(double.IsNegativeInfinity(doc.RootElement.GetProperty("ninf").GetDouble()));
        Assert.IsTrue(double.IsNaN(doc.RootElement.GetProperty("nan").GetDouble()));
        Assert.AreEqual(1000.0, doc.RootElement.GetProperty("exp").GetDouble());
    }

    /// <summary>Verifies that mapping enumeration yields all pairs in order.</summary>
    [TestMethod]
    public void EnumerateMapping_ShouldYieldPairsInOrder()
    {
        using var doc = YamlDocument.Parse("a: 1\nb: 2\nc: 3\n");
        var keys = new List<string>();
        foreach (var pair in doc.RootElement.EnumerateMapping())
            keys.Add(pair.Name);

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, keys);
    }
}
