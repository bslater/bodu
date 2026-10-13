// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlArrayTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies the <see cref="YamlArray" /> indexer, including the rule that a node appears at most once in a tree.
/// </summary>
public partial class YamlArrayTests
{
    /// <summary>
    /// Verifies that assigning a node that already belongs to another container throws
    /// <see cref="InvalidOperationException" /> and leaves the element in place.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetToNodeOfAnotherContainer_ShouldThrowInvalidOperationException()
    {
        YamlValue value = YamlValue.Create("shared");
        var owner = new YamlObject { ["key"] = value };
        YamlArray array = Sequence("a");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array[0] = value;
        });

        CollectionAssert.AreEqual(new[] { "a" }, Values(array));
    }

    /// <summary>
    /// Verifies that assigning an element to another index of the same sequence throws
    /// <see cref="InvalidOperationException" /> rather than holding the node twice.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetToElementAtAnotherIndex_ShouldThrowInvalidOperationException()
    {
        YamlArray array = Sequence("a", "b");
        YamlNode? first = array[0];

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array[1] = first;
        });

        CollectionAssert.AreEqual(new[] { "a", "b" }, Values(array));
    }

    /// <summary>
    /// Verifies that assigning an element over itself is accepted and leaves it attached, so it still cannot be added
    /// to another container.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetSameNodeOverItself_ShouldKeepItAttached()
    {
        YamlValue value = YamlValue.Create("a");
        var array = new YamlArray { value };

        array[0] = value;

        Assert.AreSame(value, array[0], "The element after assigning it over itself.");
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            new YamlArray().Add(value);
        });
    }

    /// <summary>
    /// Verifies that replacing an element detaches the replaced node, so it can be added to another container.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenElementReplaced_ShouldFreeTheReplacedNode()
    {
        YamlValue replaced = YamlValue.Create("a");
        var array = new YamlArray { replaced };

        array[0] = YamlValue.Create("b");

        var other = new YamlArray { replaced };
        Assert.AreSame(replaced, other[0]);
    }

    /// <summary>
    /// Verifies that assigning to an index outside the sequence throws before the value is attached, leaving it free
    /// to be added elsewhere.
    /// </summary>
    /// <param name="index">The out-of-range index: negative, or the count.</param>
    [TestMethod]
    [DataRow(-1, DisplayName = "negative")]
    [DataRow(1, DisplayName = "the count")]
    public void Indexer_WhenSetOutOfRange_ShouldLeaveValueFreeToAddElsewhere(int index)
    {
        YamlArray array = Sequence("a");
        YamlValue value = YamlValue.Create("b");

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            array[index] = value;
        });

        var other = new YamlArray { value };
        Assert.AreSame(value, other[0]);
    }
}
