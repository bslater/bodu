// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlObjectTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Verifies the <see cref="TomlObject" /> indexer, including the rule that a node appears at most once in a tree.
/// </summary>
public partial class TomlObjectTests
{
    /// <summary>
    /// Verifies that assigning the value of one key to another existing key of the same object throws
    /// <see cref="InvalidOperationException" /> rather than holding the node twice, and leaves both entries unchanged.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetToValueOfAnotherKey_ShouldThrowInvalidOperationException()
    {
        var first = TomlValue.Create(1L);
        var second = TomlValue.Create(2L);
        var obj = new TomlObject { { "a", first }, { "b", second } };

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            obj["b"] = first;
        });

        Assert.AreSame(second, obj["b"], "The entry after the rejected assignment.");
        Assert.AreSame(obj, second.Parent, "The parent of the value that was not replaced.");
    }

    /// <summary>
    /// Verifies that assigning the value of an existing key to a new key of the same object throws
    /// <see cref="InvalidOperationException" /> and adds no entry.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetUnderANewKeyToAValueTheObjectHolds_ShouldThrowInvalidOperationException()
    {
        var value = TomlValue.Create(1L);
        var obj = new TomlObject { { "a", value } };

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            obj["b"] = value;
        });

        Assert.AreEqual(1, obj.Count, "The object's count after the rejected assignment.");
        Assert.IsFalse(obj.ContainsKey("b"), "Whether the rejected key was added.");
    }

    /// <summary>
    /// Verifies that assigning an object to one of its own keys throws <see cref="InvalidOperationException" /> rather
    /// than creating a cycle, and adds no entry.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetToTheObjectItself_ShouldThrowInvalidOperationException()
    {
        var obj = new TomlObject();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            obj["self"] = obj;
        });

        Assert.IsFalse(obj.ContainsKey("self"), "Whether the rejected key was added.");
    }

    /// <summary>
    /// Verifies that assigning an ancestor to a key of one of its descendants throws
    /// <see cref="InvalidOperationException" /> rather than creating a cycle, and adds no entry.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetToAnAncestor_ShouldThrowInvalidOperationException()
    {
        var root = new TomlObject();
        var child = new TomlObject();
        root["child"] = child;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            child["root"] = root;
        });

        Assert.IsFalse(child.ContainsKey("root"), "Whether the rejected key was added.");
    }
}
