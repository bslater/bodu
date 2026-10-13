// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlArrayTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies <see cref="YamlArray.Add(YamlNode)" />, including the rule that a node appears at most once in a tree.
/// </summary>
public partial class YamlArrayTests
{
    /// <summary>
    /// Verifies that adding a node that already belongs to another sequence throws
    /// <see cref="InvalidOperationException" /> with the resource message, and leaves both sequences unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenNodeBelongsToAnotherSequence_ShouldThrowInvalidOperationException()
    {
        YamlValue value = YamlValue.Create(1L);
        var first = new YamlArray { value };
        var second = new YamlArray();

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            second.Add(value);
        });

        Assert.AreEqual(
            YamlResourceStrings.ResourceManager.GetString("Op_Invalid_YamlNodeAlreadyHasParent", YamlResourceStrings.Culture),
            ex.Message);
        Assert.AreEqual(0, second.Count, "The count of the sequence that rejected the node.");
        Assert.AreSame(value, first[0], "The node in the sequence that holds it.");
    }

    /// <summary>
    /// Verifies that adding a node that is already the value of a mapping entry throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenNodeBelongsToAMapping_ShouldThrowInvalidOperationException()
    {
        YamlValue value = YamlValue.Create("shared");
        var mapping = new YamlObject { ["key"] = value };
        var array = new YamlArray();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array.Add(value);
        });

        Assert.AreEqual(0, array.Count);
    }

    /// <summary>
    /// Verifies that adding a node a second time to the sequence that already holds it throws
    /// <see cref="InvalidOperationException" />, leaving a single occurrence.
    /// </summary>
    [TestMethod]
    public void Add_WhenNodeIsAlreadyAnElement_ShouldThrowInvalidOperationException()
    {
        YamlValue value = YamlValue.Create(1L);
        var array = new YamlArray { value };

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array.Add(value);
        });

        Assert.AreEqual(1, array.Count);
    }

    /// <summary>
    /// Verifies that adding a container node that is already nested in another tree throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenNodeIsAContainerNestedInAnotherTree_ShouldThrowInvalidOperationException()
    {
        var child = new YamlObject { ["name"] = YamlValue.Create("Ada") };
        var owner = new YamlArray { child };
        var array = new YamlArray();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array.Add(child);
        });

        Assert.AreEqual(0, array.Count);
    }

    /// <summary>
    /// Verifies that a node taken from a parsed tree cannot be added to another tree while it is still attached, since
    /// parsing attaches every node to its container.
    /// </summary>
    [TestMethod]
    public void Add_WhenNodeWasParsedIntoAnotherTree_ShouldThrowInvalidOperationException()
    {
        YamlNode root = YamlNode.Parse("items:\n  - 1\n  - 2\n")!;
        YamlNode element = root["items"]![0]!;
        var array = new YamlArray();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array.Add(element);
        });

        Assert.AreEqual(0, array.Count);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> element, which is not a node, can be added any number of times.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsNull_ShouldAcceptItRepeatedly()
    {
        var array = new YamlArray();

        array.Add(null);
        array.Add(null);

        Assert.AreEqual(2, array.Count);
    }
}
