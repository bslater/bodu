// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlNodeTests.DeepEquals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Verifies the structural-equality contract of <see cref="TomlNode.DeepEquals(TomlNode?, TomlNode?)" />:
/// <see langword="null" /> handling, kind discrimination across the richer TOML scalar model, ordered array
/// comparison, and order-independent table comparison.
/// </summary>
public partial class TomlNodeTests
{
    /// <summary>
    /// Verifies that two <see langword="null" /> nodes compare equal.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenBothNull_ShouldReturnTrue()
    {
        Assert.IsTrue(TomlNode.DeepEquals(null, null));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> node never equals a non-null node.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenOneNull_ShouldReturnFalse()
    {
        var node = TomlValue.Create(1L);

        Assert.IsFalse(TomlNode.DeepEquals(node, null));
        Assert.IsFalse(TomlNode.DeepEquals(null, node));
    }

    /// <summary>
    /// Verifies that a node compares equal to itself by reference without walking its content.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenSameInstance_ShouldReturnTrue()
    {
        var array = new TomlArray(1, 2, 3);

        Assert.IsTrue(TomlNode.DeepEquals(array, array));
    }

    /// <summary>
    /// Verifies that nodes of different kinds never compare equal, even when their textual renderings match —
    /// including an integer against a float of the same numeric value.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenKindsDiffer_ShouldReturnFalse()
    {
        Assert.IsFalse(TomlNode.DeepEquals(TomlValue.Create(42L), TomlValue.Create("42")));
        Assert.IsFalse(TomlNode.DeepEquals(TomlValue.Create(1L), TomlValue.Create(1.0d)));
        Assert.IsFalse(TomlNode.DeepEquals(new TomlArray(), new TomlObject()));
    }

    /// <summary>
    /// Verifies that an offset date-time never equals a local date-time, even at the same instant, because the kinds
    /// differ.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenDateTimeKindsDiffer_ShouldReturnFalse()
    {
        var offset = TomlValue.Create(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero));
        var local = TomlValue.Create(new DateTime(1979, 5, 27, 7, 32, 0));

        Assert.IsFalse(TomlNode.DeepEquals(offset, local));
    }

    /// <summary>
    /// Verifies that scalars of each TOML kind compare equal when both kind and payload match.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenScalarsMatchKindAndPayload_ShouldReturnTrue()
    {
        Assert.IsTrue(TomlNode.DeepEquals(TomlValue.Create("x"), TomlValue.Create("x")));
        Assert.IsTrue(TomlNode.DeepEquals(TomlValue.Create(42L), TomlValue.Create(42L)));
        Assert.IsTrue(TomlNode.DeepEquals(TomlValue.Create(1.5d), TomlValue.Create(1.5d)));
        Assert.IsTrue(TomlNode.DeepEquals(TomlValue.Create(true), TomlValue.Create(true)));
        Assert.IsTrue(TomlNode.DeepEquals(
            TomlValue.Create(new DateOnly(1979, 5, 27)),
            TomlValue.Create(new DateOnly(1979, 5, 27))));
        Assert.IsTrue(TomlNode.DeepEquals(
            TomlValue.Create(new TimeOnly(7, 32, 0)),
            TomlValue.Create(new TimeOnly(7, 32, 0))));
    }

    /// <summary>
    /// Verifies that arrays differing in length or in element order never compare equal.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenArraysDifferInLengthOrOrder_ShouldReturnFalse()
    {
        Assert.IsFalse(TomlNode.DeepEquals(new TomlArray(1, 2), new TomlArray(1, 2, 3)));
        Assert.IsFalse(TomlNode.DeepEquals(new TomlArray(1, 2), new TomlArray(2, 1)));
    }

    /// <summary>
    /// Verifies that arrays whose corresponding entries are both <see langword="null" /> compare equal, while a
    /// <see langword="null" /> entry never equals a value.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenArraysContainNullEntries_ShouldCompareEntrywise()
    {
        var firstWithNull = new TomlArray();
        firstWithNull.Add(null);
        var secondWithNull = new TomlArray();
        secondWithNull.Add(null);

        Assert.IsTrue(TomlNode.DeepEquals(firstWithNull, secondWithNull));
        Assert.IsFalse(TomlNode.DeepEquals(firstWithNull, new TomlArray(1)));
    }

    /// <summary>
    /// Verifies that tables differing in entry count never compare equal.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenTablesDifferInCount_ShouldReturnFalse()
    {
        var first = new TomlObject();
        first["a"] = 1L;

        var second = new TomlObject();
        second["a"] = 1L;
        second["b"] = 2L;

        Assert.IsFalse(TomlNode.DeepEquals(first, second));
    }

    /// <summary>
    /// Verifies that nested trees with equal structure and values compare equal.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenNestedTreesEqual_ShouldReturnTrue()
    {
        static TomlObject Build()
        {
            var inner = new TomlObject();
            inner["x"] = new TomlArray(1, "two");

            var root = new TomlObject();
            root["a"] = inner;
            root["b"] = 3L;
            return root;
        }

        Assert.IsTrue(TomlNode.DeepEquals(Build(), Build()));
    }
}
