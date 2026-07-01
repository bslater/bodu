// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlNodeTests.Mutate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies that mutating a <see cref="YamlNode" /> tree through the indexer is reflected on re-serialization.
/// </summary>
public partial class YamlNodeTests
{
    /// <summary>Verifies that mutation through the indexer is reflected on re-serialization.</summary>
    [TestMethod]
    public void Mutate_WhenEditingValue_ShouldReflect()
    {
        YamlObject node = YamlNode.Parse("a: 1\n")!.AsObject();
        node["a"] = YamlValue.Create(99L);
        node["b"] = YamlValue.Create("new");

        Assert.AreEqual("a: 99\nb: new\n", node.ToYamlString());
    }
}
