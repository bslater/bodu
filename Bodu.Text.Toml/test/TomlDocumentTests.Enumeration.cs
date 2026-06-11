// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentTests.Enumeration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Document;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the enumerator semantics of <see cref="TomlElement.ArrayEnumerator" /> and
/// <see cref="TomlElement.ObjectEnumerator" />: independent nested walks, reset behaviour, stable exhaustion, and
/// subtree skipping between siblings.
/// </summary>
public partial class TomlDocumentTests
{
    /// <summary>
    /// Verifies that enumerating the same array element twice yields the same sequence, because
    /// <see cref="TomlElement.ArrayEnumerator.GetEnumerator" /> returns a freshly positioned copy.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenEnumeratedTwice_ShouldYieldSameSequence()
    {
        using TomlDocument document = TomlDocument.Parse("a = [1, 2, 3]\n");
        TomlElement.ArrayEnumerator enumerable = document.RootElement.GetProperty("a").EnumerateArray();

        List<long> first = [];
        foreach (TomlElement element in enumerable)
            first.Add(element.GetInt64());

        List<long> second = [];
        foreach (TomlElement element in enumerable)
            second.Add(element.GetInt64());

        CollectionAssert.AreEqual(new[] { 1L, 2L, 3L }, first);
        CollectionAssert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that a nested <see langword="foreach" /> over the same array element walks independently of the outer
    /// enumeration.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenNestedForeachOverSameArray_ShouldWalkIndependently()
    {
        using TomlDocument document = TomlDocument.Parse("a = [1, 2]\n");
        TomlElement array = document.RootElement.GetProperty("a");

        List<string> pairs = [];
        foreach (TomlElement outer in array.EnumerateArray())
        {
            foreach (TomlElement inner in array.EnumerateArray())
                pairs.Add($"{outer.GetInt64()}:{inner.GetInt64()}");
        }

        CollectionAssert.AreEqual(new[] { "1:1", "1:2", "2:1", "2:2" }, pairs);
    }

    /// <summary>
    /// Verifies that a nested <see langword="foreach" /> over the same table element walks independently of the outer
    /// enumeration.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenNestedForeachOverSameTable_ShouldWalkIndependently()
    {
        using TomlDocument document = TomlDocument.Parse("a = 1\nb = 2\n");
        TomlElement root = document.RootElement;

        List<string> pairs = [];
        foreach (TomlProperty outer in root.EnumerateObject())
        {
            foreach (TomlProperty inner in root.EnumerateObject())
                pairs.Add($"{outer.Name}:{inner.Name}");
        }

        CollectionAssert.AreEqual(new[] { "a:a", "a:b", "b:a", "b:b" }, pairs);
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.ArrayEnumerator.MoveNext" /> keeps returning <see langword="false" /> once
    /// the array is exhausted.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenMoveNextCalledPastEnd_ShouldKeepReturningFalse()
    {
        using TomlDocument document = TomlDocument.Parse("a = [1]\n");
        TomlElement.ArrayEnumerator enumerator = document.RootElement.GetProperty("a").EnumerateArray();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.ObjectEnumerator.MoveNext" /> keeps returning <see langword="false" />
    /// once the table is exhausted.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenMoveNextCalledPastEnd_ShouldKeepReturningFalse()
    {
        using TomlDocument document = TomlDocument.Parse("a = 1\n");
        TomlElement.ObjectEnumerator enumerator = document.RootElement.EnumerateObject();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.ArrayEnumerator.Reset" /> repositions the enumerator before the first
    /// element.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenReset_ShouldRestartFromFirstElement()
    {
        using TomlDocument document = TomlDocument.Parse("a = [1, 2]\n");
        TomlElement.ArrayEnumerator enumerator = document.RootElement.GetProperty("a").EnumerateArray();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());
        enumerator.Reset();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1L, enumerator.Current.GetInt64());
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.ObjectEnumerator.Reset" /> repositions the enumerator before the first
    /// pair.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenReset_ShouldRestartFromFirstPair()
    {
        using TomlDocument document = TomlDocument.Parse("a = 1\nb = 2\n");
        TomlElement.ObjectEnumerator enumerator = document.RootElement.EnumerateObject();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());
        enumerator.Reset();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("a", enumerator.Current.Name);
    }

    /// <summary>
    /// Verifies that enumerating an empty array yields nothing.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenArrayEmpty_ShouldYieldNothing()
    {
        using TomlDocument document = TomlDocument.Parse("a = []\n");

        var count = 0;
        foreach (TomlElement element in document.RootElement.GetProperty("a").EnumerateArray())
            count++;

        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Verifies that enumerating an empty table yields nothing.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenTableEmpty_ShouldYieldNothing()
    {
        using TomlDocument document = TomlDocument.Parse("a = {}\n");

        var count = 0;
        foreach (TomlProperty property in document.RootElement.GetProperty("a").EnumerateObject())
            count++;

        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Verifies that the object enumerator skips each value's whole subtree, so pairs after container values are
    /// reached correctly and in document order.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenValuesAreContainers_ShouldSkipSubtreesBetweenPairs()
    {
        using TomlDocument document = TomlDocument.Parse("a = [1, 2]\nb = {x = 3}\nc = 4\n");

        List<string> names = [];
        List<TomlValueKind> kinds = [];
        foreach (TomlProperty property in document.RootElement.EnumerateObject())
        {
            names.Add(property.Name);
            kinds.Add(property.Value.ValueKind);
        }

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, names);
        CollectionAssert.AreEqual(
            new[] { TomlValueKind.Array, TomlValueKind.Table, TomlValueKind.Integer },
            kinds);
    }

    /// <summary>
    /// Verifies that the array enumerator yields elements of an array of tables in document order, skipping each
    /// table's pairs between elements.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenArrayOfTables_ShouldYieldTablesInDocumentOrder()
    {
        using TomlDocument document = TomlDocument.Parse(
            "[[p]]\nname = \"first\"\nsize = 1\n\n[[p]]\nname = \"second\"\n");

        List<string> names = [];
        foreach (TomlElement element in document.RootElement.GetProperty("p").EnumerateArray())
            names.Add(element.GetProperty("name").GetString());

        CollectionAssert.AreEqual(new[] { "first", "second" }, names);
    }
}
