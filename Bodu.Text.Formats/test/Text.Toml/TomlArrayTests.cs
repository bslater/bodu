// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlArrayTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Behavioural tests for <see cref="TomlArray" /> — ordering, indexing, enumeration, mixed types, and null handling.
/// </summary>
[TestClass]
public sealed class TomlArrayTests
{
    /// <summary>
    /// Verifies that elements are appended in order and exposed by index and count.
    /// </summary>
    [TestMethod]
    public void Add_WhenElementsAppended_ShouldPreserveOrder()
    {
        var array = new TomlArray();
        array.Add(new TomlInteger(1));
        array.Add(new TomlInteger(2));
        array.Add(new TomlInteger(3));

        Assert.AreEqual(3, array.Count);
        Assert.AreEqual(2L, ((TomlInteger)array[1]).Value);
    }

    /// <summary>
    /// Verifies that a TOML array may hold elements of differing types, as the specification permits.
    /// </summary>
    [TestMethod]
    public void Add_WhenMixedTypes_ShouldBeAllowed()
    {
        var array = new TomlArray { new TomlInteger(1), new TomlString("two"), new TomlBoolean(true) };

        Assert.AreEqual(TomlValueKind.Integer, array[0].Kind);
        Assert.AreEqual(TomlValueKind.String, array[1].Kind);
        Assert.AreEqual(TomlValueKind.Boolean, array[2].Kind);
    }

    /// <summary>
    /// Verifies that the array enumerates its elements in order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenIterated_ShouldYieldInOrder()
    {
        var array = new TomlArray { new TomlInteger(10), new TomlInteger(20) };

        var values = array.Select(static v => ((TomlInteger)v).Value).ToArray();

        CollectionAssert.AreEqual(new[] { 10L, 20L }, values);
    }

    /// <summary>
    /// Verifies that appending a null element throws.
    /// </summary>
    [TestMethod]
    public void Add_WhenNullElement_ShouldThrowExactly()
    {
        var array = new TomlArray();

        Assert.ThrowsExactly<ArgumentNullException>(() => array.Add(null!));
    }

    /// <summary>
    /// Verifies that constructing an array from a sequence containing null throws.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSequenceContainsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new TomlArray([new TomlInteger(1), null!]);
        });
    }
}
