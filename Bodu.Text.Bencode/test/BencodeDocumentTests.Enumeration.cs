// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentTests.Enumeration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the enumerator semantics of <see cref="BencodeElement.ArrayEnumerator" /> and
/// <see cref="BencodeElement.ObjectEnumerator" />: independent nested walks, reset behaviour, stable exhaustion, and
/// subtree skipping between siblings.
/// </summary>
public partial class BencodeDocumentTests
{
    /// <summary>
    /// Verifies that enumerating the same array element twice yields the same sequence, because
    /// <see cref="BencodeElement.ArrayEnumerator.GetEnumerator" /> returns a freshly positioned copy.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenEnumeratedTwice_ShouldYieldSameSequence()
    {
        using BencodeDocument document = BencodeDocument.Parse(Bytes("li1ei2ei3ee"));
        BencodeElement.ArrayEnumerator enumerable = document.RootElement.EnumerateArray();

        List<long> first = [];
        foreach (BencodeElement element in enumerable)
            first.Add(element.GetInt64());

        List<long> second = [];
        foreach (BencodeElement element in enumerable)
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
        using BencodeDocument document = BencodeDocument.Parse(Bytes("li1ei2ee"));
        BencodeElement root = document.RootElement;

        List<string> pairs = [];
        foreach (BencodeElement outer in root.EnumerateArray())
        {
            foreach (BencodeElement inner in root.EnumerateArray())
                pairs.Add($"{outer.GetInt64()}:{inner.GetInt64()}");
        }

        CollectionAssert.AreEqual(new[] { "1:1", "1:2", "2:1", "2:2" }, pairs);
    }

    /// <summary>
    /// Verifies that a nested <see langword="foreach" /> over the same object element walks independently of the
    /// outer enumeration.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenNestedForeachOverSameObject_ShouldWalkIndependently()
    {
        using BencodeDocument document = BencodeDocument.Parse(Bytes("d1:ai1e1:bi2ee"));
        BencodeElement root = document.RootElement;

        List<string> pairs = [];
        foreach (BencodeProperty outer in root.EnumerateObject())
        {
            foreach (BencodeProperty inner in root.EnumerateObject())
                pairs.Add($"{outer.Name}:{inner.Name}");
        }

        CollectionAssert.AreEqual(new[] { "a:a", "a:b", "b:a", "b:b" }, pairs);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.ArrayEnumerator.MoveNext" /> keeps returning <see langword="false" />
    /// once the array is exhausted.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenMoveNextCalledPastEnd_ShouldKeepReturningFalse()
    {
        using BencodeDocument document = BencodeDocument.Parse(Bytes("li1ee"));
        BencodeElement.ArrayEnumerator enumerator = document.RootElement.EnumerateArray();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.ObjectEnumerator.MoveNext" /> keeps returning <see langword="false" />
    /// once the object is exhausted.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenMoveNextCalledPastEnd_ShouldKeepReturningFalse()
    {
        using BencodeDocument document = BencodeDocument.Parse(Bytes("d1:ai1ee"));
        BencodeElement.ObjectEnumerator enumerator = document.RootElement.EnumerateObject();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.ArrayEnumerator.Reset" /> repositions the enumerator before the first
    /// element.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenReset_ShouldRestartFromFirstElement()
    {
        using BencodeDocument document = BencodeDocument.Parse(Bytes("li1ei2ee"));
        BencodeElement.ArrayEnumerator enumerator = document.RootElement.EnumerateArray();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());
        enumerator.Reset();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1L, enumerator.Current.GetInt64());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.ObjectEnumerator.Reset" /> repositions the enumerator before the first
    /// pair.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenReset_ShouldRestartFromFirstPair()
    {
        using BencodeDocument document = BencodeDocument.Parse(Bytes("d1:ai1e1:bi2ee"));
        BencodeElement.ObjectEnumerator enumerator = document.RootElement.EnumerateObject();

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
        using BencodeDocument document = BencodeDocument.Parse(Bytes("le"));

        var count = 0;
        foreach (BencodeElement element in document.RootElement.EnumerateArray())
            count++;

        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Verifies that enumerating an empty object yields nothing.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenObjectEmpty_ShouldYieldNothing()
    {
        using BencodeDocument document = BencodeDocument.Parse(Bytes("de"));

        var count = 0;
        foreach (BencodeProperty property in document.RootElement.EnumerateObject())
            count++;

        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Verifies that the object enumerator skips each value's whole subtree, so pairs after container values are
    /// reached correctly.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenValuesAreContainers_ShouldSkipSubtreesBetweenPairs()
    {
        using BencodeDocument document = BencodeDocument.Parse(Bytes("d1:ali1ei2ee1:bd1:xi3ee1:ci4ee"));

        List<string> names = [];
        List<BencodeValueKind> kinds = [];
        foreach (BencodeProperty property in document.RootElement.EnumerateObject())
        {
            names.Add(property.Name);
            kinds.Add(property.Value.ValueKind);
        }

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, names);
        CollectionAssert.AreEqual(
            new[] { BencodeValueKind.Array, BencodeValueKind.Object, BencodeValueKind.Integer },
            kinds);
    }
}
