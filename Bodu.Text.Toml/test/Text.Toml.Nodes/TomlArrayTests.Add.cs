// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlArrayTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Verifies <see cref="TomlArray.Add(TomlNode?)" />, including the rule that a node appears at most once in a tree.
/// </summary>
public partial class TomlArrayTests
{
    /// <summary>
    /// Verifies that adding a node a second time to the array that already holds it throws
    /// <see cref="InvalidOperationException" /> and leaves a single occurrence attached to the array.
    /// </summary>
    [TestMethod]
    public void Add_WhenNodeIsAlreadyAnElement_ShouldThrowInvalidOperationException()
    {
        var value = TomlValue.Create(1L);
        var array = new TomlArray();
        array.Add(value);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array.Add(value);
        });

        Assert.AreEqual(1, array.Count, "The array's count after the rejected add.");
        Assert.AreSame(array, value.Parent, "The parent of the element.");
    }

    /// <summary>
    /// Verifies that adding an array to itself throws <see cref="InvalidOperationException" /> with the resource
    /// message, rather than creating a cycle, and leaves the array empty.
    /// </summary>
    [TestMethod]
    public void Add_WhenNodeIsTheArrayItself_ShouldThrowInvalidOperationException()
    {
        var array = new TomlArray();

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array.Add(array);
        });

        Assert.AreEqual(
            TomlResourceStrings.ResourceManager.GetString("Op_Invalid_NodeCycle", TomlResourceStrings.Culture),
            ex.Message);
        Assert.AreEqual(0, array.Count, "The array's count after the rejected add.");
    }

    /// <summary>
    /// Verifies that adding an array to one of its own descendants throws <see cref="InvalidOperationException" />
    /// rather than creating a cycle, and leaves the descendant unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenNodeIsAnAncestor_ShouldThrowInvalidOperationException()
    {
        var root = new TomlArray();
        var child = new TomlArray();
        root.Add(child);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            child.Add(root);
        });

        Assert.AreEqual(0, child.Count, "The descendant's count after the rejected add.");
    }

    /// <summary>
    /// Verifies that adding the root of a tree to a node two levels below it throws
    /// <see cref="InvalidOperationException" /> rather than creating a cycle.
    /// </summary>
    [TestMethod]
    public void Add_WhenNodeIsADistantAncestor_ShouldThrowInvalidOperationException()
    {
        var root = new TomlArray();
        var middle = new TomlArray();
        var leaf = new TomlArray();
        root.Add(middle);
        middle.Add(leaf);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            leaf.Add(root);
        });

        Assert.AreEqual(0, leaf.Count, "The descendant's count after the rejected add.");
    }
}
