// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlNodeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Nodes;

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies the mutable <see cref="YamlNode" /> document object model.
/// </summary>
[TestClass]
public sealed class YamlNodeTests
{
    /// <summary>Verifies that a mapping parses into a <see cref="YamlObject" /> with typed values.</summary>
    [TestMethod]
    public void Parse_WhenMapping_ShouldBuildObject()
    {
        var node = YamlNode.Parse("a: 1\nb: hello\nc: true\n")!;
        var obj = node.AsObject();

        Assert.AreEqual(1L, obj["a"]!.AsValue().GetValue<long>());
        Assert.AreEqual("hello", obj["b"]!.AsValue().GetValue<string>());
        Assert.IsTrue(obj["c"]!.AsValue().GetValue<bool>());
    }

    /// <summary>Verifies that a sequence parses into a <see cref="YamlArray" />.</summary>
    [TestMethod]
    public void Parse_WhenSequence_ShouldBuildArray()
    {
        var array = YamlNode.Parse("- 1\n- 2\n- 3\n")!.AsArray();

        Assert.AreEqual(3, array.Count);
        Assert.AreEqual(2L, array[1]!.AsValue().GetValue<long>());
    }

    /// <summary>Verifies that a nested structure is navigable.</summary>
    [TestMethod]
    public void Parse_WhenNested_ShouldNavigate()
    {
        var node = YamlNode.Parse("server:\n  hosts:\n    - a\n    - b\n  port: 80\n")!;
        var server = node["server"]!;

        Assert.AreEqual("b", server["hosts"]![1]!.AsValue().GetValue<string>());
        Assert.AreEqual(80L, server["port"]!.AsValue().GetValue<long>());
    }

    /// <summary>Verifies that a node tree built in code serializes to YAML.</summary>
    [TestMethod]
    public void ToYamlString_WhenBuiltInCode_ShouldEmit()
    {
        var obj = new YamlObject
        {
            ["name"] = YamlValue.Create("test"),
            ["count"] = YamlValue.Create(3L),
        };
        var tags = new YamlArray { YamlValue.Create("alpha"), YamlValue.Create("beta") };
        obj["tags"] = tags;

        Assert.AreEqual("name: test\ncount: 3\ntags:\n  - alpha\n  - beta\n", obj.ToYamlString());
    }

    /// <summary>Verifies that a parsed tree round-trips through serialization.</summary>
    [TestMethod]
    public void RoundTrip_WhenParsedAndReserialized_ShouldMatch()
    {
        const string yaml = "name: test\nitems:\n  - 1\n  - 2\n";
        var node = YamlNode.Parse(yaml)!;
        var reparsed = YamlNode.Parse(node.ToYamlString())!;

        Assert.AreEqual("test", reparsed["name"]!.AsValue().GetValue<string>());
        Assert.AreEqual(2L, reparsed["items"]![1]!.AsValue().GetValue<long>());
    }

    /// <summary>Verifies that mutation through the indexer is reflected on re-serialization.</summary>
    [TestMethod]
    public void Mutate_WhenEditingValue_ShouldReflect()
    {
        var node = YamlNode.Parse("a: 1\n")!.AsObject();
        node["a"] = YamlValue.Create(99L);
        node["b"] = YamlValue.Create("new");

        Assert.AreEqual("a: 99\nb: new\n", node.ToYamlString());
    }

    /// <summary>Verifies that the empty-document case yields a null node.</summary>
    [TestMethod]
    public void Parse_WhenEmpty_ShouldReturnNull()
    {
        Assert.IsNull(YamlNode.Parse(string.Empty));
    }
}
