// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlArrayTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Verifies the <see cref="TomlArray" /> indexer, including an assignment that is rejected.
/// </summary>
public partial class TomlArrayTests
{
    /// <summary>
    /// Verifies that assigning to an index outside the array throws before the value is attached, leaving it without a
    /// parent.
    /// </summary>
    /// <param name="index">The out-of-range index: negative, or the count.</param>
    [TestMethod]
    [DataRow(-1, DisplayName = "negative")]
    [DataRow(1, DisplayName = "the count")]
    public void Indexer_WhenSetOutOfRange_ShouldLeaveValueWithoutParent(int index)
    {
        var array = new TomlArray(TomlValue.Create(1L));
        var value = TomlValue.Create(2L);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            array[index] = value;
        });

        Assert.IsNull(value.Parent);
    }

    /// <summary>
    /// Verifies that assigning an element to another index of the same array throws
    /// <see cref="InvalidOperationException" /> rather than holding the node twice, and leaves both slots unchanged.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetToElementAtAnotherIndex_ShouldThrowInvalidOperationException()
    {
        var first = TomlValue.Create(1L);
        var second = TomlValue.Create(2L);
        var array = new TomlArray(first, second);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array[1] = first;
        });

        Assert.AreSame(first, array[0], "The first slot after the rejected assignment.");
        Assert.AreSame(second, array[1], "The second slot after the rejected assignment.");
        Assert.AreSame(array, second.Parent, "The parent of the element that was not replaced.");
    }

    /// <summary>
    /// Verifies that assigning an ancestor to an element of one of its descendants throws
    /// <see cref="InvalidOperationException" /> rather than creating a cycle, and leaves the element in place.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetToAnAncestor_ShouldThrowInvalidOperationException()
    {
        var element = TomlValue.Create(1L);
        var child = new TomlArray();
        child.Add(element);
        var root = new TomlArray();
        root.Add(child);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            child[0] = root;
        });

        Assert.AreSame(element, child[0], "The slot after the rejected assignment.");
    }
}
