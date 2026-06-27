// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlNodeTests.ToYamlString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Nodes;

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
}
