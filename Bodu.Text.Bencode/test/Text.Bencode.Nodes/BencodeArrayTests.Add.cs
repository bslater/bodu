// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeArrayTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies <see cref="BencodeArray.Add(BencodeNode?)" />, including the rule that a node appears at most once in a tree.
/// </summary>
public partial class BencodeArrayTests
{
    /// <summary>
    /// Verifies that adding a node a second time to the array that already holds it throws
    /// <see cref="InvalidOperationException" /> and leaves a single occurrence attached to the array.
    /// </summary>
    [TestMethod]
    public void Add_WhenNodeIsAlreadyAnElement_ShouldThrowInvalidOperationException()
    {
        var value = BencodeValue.Create(1L);
        var array = new BencodeArray();
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
        var array = new BencodeArray();

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array.Add(array);
        });

        Assert.AreEqual(
            BencodeResourceStrings.ResourceManager.GetString("Op_Invalid_NodeCycle", BencodeResourceStrings.Culture),
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
        var root = new BencodeArray();
        var child = new BencodeArray();
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
        var root = new BencodeArray();
        var middle = new BencodeArray();
        var leaf = new BencodeArray();
        root.Add(middle);
        middle.Add(leaf);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            leaf.Add(root);
        });

        Assert.AreEqual(0, leaf.Count, "The descendant's count after the rejected add.");
    }
}
