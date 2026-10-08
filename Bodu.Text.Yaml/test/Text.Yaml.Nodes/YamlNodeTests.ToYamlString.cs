// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlNodeTests.ToYamlString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies that <see cref="YamlNode.ToYamlString" /> emits the expected YAML text for a node tree built in code.
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

    /// <summary>
    /// Verifies that a mapping entry holding <see langword="null" /> is written as <c>a: null</c> and parses back to an
    /// entry whose value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToYamlString_WhenObjectHoldsNullValue_ShouldWriteNull()
    {
        var obj = new YamlObject { ["a"] = null };

        string yaml = obj.ToYamlString();

        Assert.AreEqual("a: null\n", yaml);
        YamlObject parsed = YamlNode.Parse(yaml)!.AsObject();
        Assert.IsTrue(parsed.TryGetValue("a", out YamlNode? value));
        Assert.IsNull(value);
    }
}
