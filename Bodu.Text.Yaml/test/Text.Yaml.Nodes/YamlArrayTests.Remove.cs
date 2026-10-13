// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlArrayTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies <see cref="YamlArray.Remove(YamlNode)" /> detaches the node it removes.
/// </summary>
public partial class YamlArrayTests
{
    /// <summary>
    /// Verifies that removing an element detaches it, so it can be added to another container.
    /// </summary>
    [TestMethod]
    public void Remove_WhenElementRemoved_ShouldFreeItToBeAddedElsewhere()
    {
        YamlValue value = YamlValue.Create("a");
        var array = new YamlArray { value };

        Assert.IsTrue(array.Remove(value), "The result of removing the element.");

        var other = new YamlArray { value };
        Assert.AreSame(value, other[0], "The element once added elsewhere.");
    }

    /// <summary>
    /// Verifies that removing a node that belongs to another container reports failure and leaves the node attached
    /// to that container, so it still cannot be added elsewhere.
    /// </summary>
    [TestMethod]
    public void Remove_WhenNodeBelongsToAnotherContainer_ShouldLeaveItAttached()
    {
        YamlValue value = YamlValue.Create("a");
        var owner = new YamlArray { value };
        YamlArray array = Sequence("b");

        Assert.IsFalse(array.Remove(value), "The result of removing a node the sequence does not hold.");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array.Add(value);
        });
    }
}
