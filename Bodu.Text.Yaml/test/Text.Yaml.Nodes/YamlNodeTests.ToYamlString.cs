// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlNodeTests.ToYamlString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Nodes;

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies <see cref="YamlNode.ToYamlString" /> emission, round-tripping, and that mutations are reflected on
/// re-serialization.
/// </summary>
public partial class YamlNodeTests
{
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
    public void ToYamlString_WhenParsedAndReserialized_ShouldRoundTrip()
    {
        const string yaml = "name: test\nitems:\n  - 1\n  - 2\n";
        var node = YamlNode.Parse(yaml)!;
        var reparsed = YamlNode.Parse(node.ToYamlString())!;

        Assert.AreEqual("test", reparsed["name"]!.AsValue().GetValue<string>());
        Assert.AreEqual(2L, reparsed["items"]![1]!.AsValue().GetValue<long>());
    }

    /// <summary>Verifies that mutation through the indexer is reflected on re-serialization.</summary>
    [TestMethod]
    public void ToYamlString_WhenValueMutated_ShouldReflect()
    {
        var node = YamlNode.Parse("a: 1\n")!.AsObject();
        node["a"] = YamlValue.Create(99L);
        node["b"] = YamlValue.Create("new");

        Assert.AreEqual("a: 99\nb: new\n", node.ToYamlString());
    }
}
