// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedObjectTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited.Nodes;

/// <summary>
/// Verifies the dictionary surface of <see cref="DelimitedObject" />, the mutable record node.
/// </summary>
[TestClass]
public sealed class DelimitedObjectTests
{
    /// <summary>
    /// Verifies that a new object reports the object value kind and is empty.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultConstructed_ShouldBeEmptyObject()
    {
        var sut = new DelimitedObject();

        Assert.AreEqual(DelimitedValueKind.Object, sut.ValueKind);
        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.Keys.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedObject.Count" /> and <see cref="DelimitedObject.Keys" /> track the fields
    /// added, and that <see cref="DelimitedObject.Keys" /> preserves insertion order.
    /// </summary>
    [TestMethod]
    public void Keys_WhenFieldsAdded_ShouldPreserveInsertionOrder()
    {
        var sut = new DelimitedObject
        {
            ["name"] = new DelimitedValue("Ada"),
            ["age"] = new DelimitedValue("36"),
        };

        Assert.AreEqual(2, sut.Count);
        CollectionAssert.AreEqual(new[] { "name", "age" }, sut.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedObject.ContainsKey(string)" /> distinguishes present from absent fields.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenFieldPresent_ShouldReturnTrue()
    {
        var sut = new DelimitedObject { ["name"] = new DelimitedValue("Ada") };

        Assert.IsTrue(sut.ContainsKey("name"));
        Assert.IsFalse(sut.ContainsKey("missing"));
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedObject.TryGetValue(string, out DelimitedNode)" /> returns the stored node
    /// for a present field.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenFieldPresent_ShouldReturnTrueAndNode()
    {
        var expected = new DelimitedValue("Ada");
        var sut = new DelimitedObject { ["name"] = expected };

        Assert.IsTrue(sut.TryGetValue("name", out DelimitedNode? actual));
        Assert.AreSame(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedObject.TryGetValue(string, out DelimitedNode)" /> reports failure and
    /// yields <see langword="null" /> for an absent field.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenFieldAbsent_ShouldReturnFalse()
    {
        var sut = new DelimitedObject();

        Assert.IsFalse(sut.TryGetValue("missing", out DelimitedNode? actual));
        Assert.IsNull(actual);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedObject.Remove(string)" /> removes the field from both the lookup and the
    /// ordered key list, so a later write does not emit a stale column.
    /// </summary>
    [TestMethod]
    public void Remove_WhenFieldPresent_ShouldRemoveFromLookupAndOrder()
    {
        var sut = new DelimitedObject
        {
            ["name"] = new DelimitedValue("Ada"),
            ["age"] = new DelimitedValue("36"),
        };

        Assert.IsTrue(sut.Remove("name"));

        Assert.AreEqual(1, sut.Count);
        Assert.IsFalse(sut.ContainsKey("name"));
        CollectionAssert.AreEqual(new[] { "age" }, sut.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedObject.Remove(string)" /> reports failure for an absent field and leaves
    /// the object unchanged.
    /// </summary>
    [TestMethod]
    public void Remove_WhenFieldAbsent_ShouldReturnFalse()
    {
        var sut = new DelimitedObject { ["name"] = new DelimitedValue("Ada") };

        Assert.IsFalse(sut.Remove("missing"));
        Assert.AreEqual(1, sut.Count);
    }
}
