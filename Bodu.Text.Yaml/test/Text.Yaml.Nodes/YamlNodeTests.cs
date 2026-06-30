// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlNodeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies the mutable <see cref="YamlNode" /> document object model.
/// </summary>
[TestClass]
public partial class YamlNodeTests
{
    /// <summary>Verifies that a mapping parses into a <see cref="YamlObject" /> with typed values.</summary>
    [TestMethod]
    public void Parse_WhenMapping_ShouldBuildObject()
    {
        YamlNode node = YamlNode.Parse("a: 1\nb: hello\nc: true\n")!;
        YamlObject obj = node.AsObject();

        Assert.AreEqual(1L, obj["a"]!.AsValue().GetValue<long>());
        Assert.AreEqual("hello", obj["b"]!.AsValue().GetValue<string>());
        Assert.IsTrue(obj["c"]!.AsValue().GetValue<bool>());
    }

    /// <summary>Verifies that a sequence parses into a <see cref="YamlArray" />.</summary>
    [TestMethod]
    public void Parse_WhenSequence_ShouldBuildArray()
    {
        YamlArray array = YamlNode.Parse("- 1\n- 2\n- 3\n")!.AsArray();

        Assert.AreEqual(3, array.Count);
        Assert.AreEqual(2L, array[1]!.AsValue().GetValue<long>());
    }

    /// <summary>Verifies that a nested structure is navigable.</summary>
    [TestMethod]
    public void Parse_WhenNested_ShouldNavigate()
    {
        YamlNode node = YamlNode.Parse("server:\n  hosts:\n    - a\n    - b\n  port: 80\n")!;
        YamlNode server = node["server"]!;

        Assert.AreEqual("b", server["hosts"]![1]!.AsValue().GetValue<string>());
        Assert.AreEqual(80L, server["port"]!.AsValue().GetValue<long>());
    }

    /// <summary>Verifies that the empty-document case yields a null node.</summary>
    [TestMethod]
    public void Parse_WhenEmpty_ShouldReturnNull()
    {
        Assert.IsNull(YamlNode.Parse(string.Empty));
    }
}
