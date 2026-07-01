// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlNodeTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies that a parsed <see cref="YamlNode" /> tree round-trips through serialization and re-parsing.
/// </summary>
public partial class YamlNodeTests
{
    /// <summary>Verifies that a parsed tree round-trips through serialization.</summary>
    [TestMethod]
    public void RoundTrip_WhenParsedAndReserialized_ShouldMatch()
    {
        const string yaml = "name: test\nitems:\n  - 1\n  - 2\n";
        YamlNode node = YamlNode.Parse(yaml)!;
        YamlNode reparsed = YamlNode.Parse(node.ToYamlString())!;

        Assert.AreEqual("test", reparsed["name"]!.AsValue().GetValue<string>());
        Assert.AreEqual(2L, reparsed["items"]![1]!.AsValue().GetValue<long>());
    }
}
