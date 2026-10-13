// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlObjectTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies the <see cref="YamlObject" /> indexer, including the rule that a node appears at most once in a tree.
/// </summary>
public sealed partial class YamlObjectTests
{
    /// <summary>
    /// Verifies that assigning a node that already belongs to another container throws
    /// <see cref="InvalidOperationException" /> and adds no entry.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetToNodeOfAnotherContainer_ShouldThrowInvalidOperationException()
    {
        YamlValue value = YamlValue.Create("Ada");
        var owner = new YamlObject { ["name"] = value };
        var sut = new YamlObject();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut["name"] = value;
        });

        Assert.AreEqual(0, sut.Count, "The mapping's count after the rejected assignment.");
        Assert.IsFalse(sut.ContainsKey("name"), "Whether the rejected key was added.");
    }

    /// <summary>
    /// Verifies that assigning the value of one key to another key of the same mapping throws
    /// <see cref="InvalidOperationException" /> rather than holding the node twice.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetToValueOfAnotherKey_ShouldThrowInvalidOperationException()
    {
        YamlValue value = YamlValue.Create("Ada");
        var sut = new YamlObject { ["name"] = value, ["alias"] = YamlValue.Create("Grace") };

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut["alias"] = value;
        });

        Assert.AreEqual("Grace", ((YamlValue)sut["alias"]!).GetValue<string>());
    }

    /// <summary>
    /// Verifies that assigning an entry's value over itself is accepted and leaves it attached, so it still cannot be
    /// added to another container.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetSameNodeOverItself_ShouldKeepItAttached()
    {
        YamlValue value = YamlValue.Create("Ada");
        var sut = new YamlObject { ["name"] = value };

        sut["name"] = value;

        Assert.AreSame(value, sut["name"], "The entry after assigning its value over itself.");
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            new YamlArray().Add(value);
        });
    }

    /// <summary>
    /// Verifies that replacing an entry's value detaches the replaced node, so it can be added to another container.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenEntryReplaced_ShouldFreeTheReplacedNode()
    {
        YamlValue replaced = YamlValue.Create("Ada");
        var sut = new YamlObject { ["name"] = replaced };

        sut["name"] = YamlValue.Create("Grace");

        var other = new YamlArray { replaced };
        Assert.AreSame(replaced, other[0]);
    }

    /// <summary>
    /// Verifies that the rule holds when the mapping is edited through the <see cref="YamlNode" /> indexer, the path a
    /// parsed tree is edited through.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetThroughYamlNodeToNodeOfAnotherContainer_ShouldThrowInvalidOperationException()
    {
        YamlNode root = YamlNode.Parse("server:\n  port: 8080\nbackup:\n  port: 8081\n")!;
        YamlNode? port = root["server"]!["port"];

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            root["backup"]!["port"] = port;
        });

        Assert.AreEqual(8081L, ((YamlValue)root["backup"]!["port"]!).GetValue<long>());
    }
}
