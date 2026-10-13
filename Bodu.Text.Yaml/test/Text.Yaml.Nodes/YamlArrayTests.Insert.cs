// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlArrayTests.Insert.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies <see cref="YamlArray.Insert(int, YamlNode)" />, including the rule that a node appears at most once in a
/// tree.
/// </summary>
public partial class YamlArrayTests
{
    /// <summary>
    /// Verifies that inserting a node that already belongs to another container throws
    /// <see cref="InvalidOperationException" /> and leaves the sequence unchanged.
    /// </summary>
    [TestMethod]
    public void Insert_WhenNodeBelongsToAnotherContainer_ShouldThrowInvalidOperationException()
    {
        YamlValue value = YamlValue.Create(1L);
        var owner = new YamlArray { value };
        YamlArray array = Sequence("a");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array.Insert(0, value);
        });

        CollectionAssert.AreEqual(new[] { "a" }, Values(array));
    }

    /// <summary>
    /// Verifies that inserting a node into the sequence that already holds it throws
    /// <see cref="InvalidOperationException" />, leaving a single occurrence.
    /// </summary>
    [TestMethod]
    public void Insert_WhenNodeIsAlreadyAnElement_ShouldThrowInvalidOperationException()
    {
        YamlValue value = YamlValue.Create("a");
        var array = new YamlArray { value };

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array.Insert(0, value);
        });

        Assert.AreEqual(1, array.Count);
    }

    /// <summary>
    /// Verifies that an insert rejected for an index outside the sequence leaves the item unattached, free to be added
    /// elsewhere, and the sequence unchanged.
    /// </summary>
    /// <param name="index">The out-of-range index: negative, or one past the end.</param>
    [TestMethod]
    [DataRow(-1, DisplayName = "negative")]
    [DataRow(2, DisplayName = "one past the end")]
    public void Insert_WhenIndexIsOutOfRange_ShouldLeaveItemFreeToAddElsewhere(int index)
    {
        YamlArray array = Sequence("a");
        YamlValue item = YamlValue.Create("b");

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            array.Insert(index, item);
        });

        Assert.AreEqual(1, array.Count, "The sequence's count after the rejected insert.");
        var other = new YamlArray { item };
        Assert.AreSame(item, other[0], "The item once added elsewhere.");
    }

    /// <summary>
    /// Verifies that inserting an array into itself throws <see cref="InvalidOperationException" /> rather than
    /// creating a cycle, and leaves the array unchanged.
    /// </summary>
    [TestMethod]
    public void Insert_WhenNodeIsTheArrayItself_ShouldThrowInvalidOperationException()
    {
        var array = new YamlArray();
        array.Add(YamlValue.Create(1L));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array.Insert(0, array);
        });

        Assert.AreEqual(1, array.Count, "The array's count after the rejected insert.");
    }
}
