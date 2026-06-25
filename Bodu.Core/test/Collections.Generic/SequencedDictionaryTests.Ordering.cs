// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.Ordering.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that, in insertion-order mode, enumeration order remains stable across value updates and reads.
    /// </summary>
    [TestMethod]
    public void Ordering_WhenInsertionOrderAndEntriesReadOrUpdated_ShouldRemainStable()
    {
        var dictionary = CreatePopulated();

        _ = dictionary["a"];
        dictionary["b"] = 20;
        _ = dictionary.TryGetValue("a", out _);

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that, in access-order mode, reading an entry through the indexer moves it to the end of the iteration order.
    /// </summary>
    [TestMethod]
    public void Ordering_WhenAccessOrderAndKeyRead_ShouldMoveEntryToEnd()
    {
        var dictionary = new SequencedDictionary<string, int>(accessOrder: true)
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3,
        };

        _ = dictionary["a"];

        CollectionAssert.AreEqual(new[] { "b", "c", "a" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that, in access-order mode, updating an existing entry's value moves it to the end of the iteration order.
    /// </summary>
    [TestMethod]
    public void Ordering_WhenAccessOrderAndValueUpdated_ShouldMoveEntryToEnd()
    {
        var dictionary = new SequencedDictionary<string, int>(accessOrder: true)
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3,
        };

        dictionary["b"] = 20;

        CollectionAssert.AreEqual(new[] { "a", "c", "b" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that, in access-order mode, reading the entry already at the tail leaves the order unchanged.
    /// </summary>
    [TestMethod]
    public void Ordering_WhenAccessOrderAndTailRead_ShouldLeaveOrderUnchanged()
    {
        var dictionary = new SequencedDictionary<string, int>(accessOrder: true)
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3,
        };

        _ = dictionary["c"];

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that an access-order dictionary models a least-recently-used cache: the least recently used key is the first entry.
    /// </summary>
    [TestMethod]
    public void Ordering_WhenAccessOrderUsedAsLruCache_ShouldExposeLeastRecentlyUsedAsFirst()
    {
        var dictionary = new SequencedDictionary<string, int>(accessOrder: true)
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3,
        };

        _ = dictionary["a"]; // touch a -> b becomes least-recently-used
        _ = dictionary["c"]; // touch c -> b is still least-recently-used

        Assert.AreEqual("b", dictionary.First.Key);
        Assert.AreEqual("c", dictionary.Last.Key);
    }

    /// <summary>
    /// Verifies that insertion order is maintained consistently across a sequence of add, remove, re-add, and clear
    /// operations, as observed through <see cref="SequencedDictionary{TKey, TValue}.Keys" />,
    /// <see cref="SequencedDictionary{TKey, TValue}.Values" />, and direct enumeration.
    /// </summary>
    /// <remarks>
    /// Adapted from the .NET runtime <c>OrderedDictionary</c> conformance test
    /// <c>Ordering_AddInsertRemoveClear_ExpectedOrderResults</c> (positional insert is not part of this type's surface).
    /// </remarks>
    [TestMethod]
    public void Ordering_WhenAddRemoveReaddClear_ShouldTrackInsertionOrderThroughout()
    {
        var dictionary = new SequencedDictionary<string, int>();

        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);
        dictionary.Add("d", 4);
        CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, dictionary.Keys.ToArray());

        // Removing a middle entry preserves the order of the rest.
        dictionary.Remove("b");
        CollectionAssert.AreEqual(new[] { "a", "c", "d" }, dictionary.Keys.ToArray());

        // Re-adding a removed key appends it at the tail.
        dictionary.Add("b", 20);
        CollectionAssert.AreEqual(new[] { "a", "c", "d", "b" }, dictionary.Keys.ToArray());
        CollectionAssert.AreEqual(new[] { 1, 3, 4, 20 }, dictionary.Values.ToArray());

        // Direct enumeration agrees with the views.
        var enumerated = new List<string>();
        foreach (KeyValuePair<string, int> kvp in dictionary)
            enumerated.Add(kvp.Key);
        CollectionAssert.AreEqual(new[] { "a", "c", "d", "b" }, enumerated);

        // Clear resets the sequence; subsequent adds start from empty.
        dictionary.Clear();
        dictionary.Add("x", 100);
        dictionary.Add("y", 200);
        CollectionAssert.AreEqual(new[] { "x", "y" }, dictionary.Keys.ToArray());
    }
}
