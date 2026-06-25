// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LinkedDictionaryTests.Ordering.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LinkedDictionaryTests
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
        var dictionary = new LinkedDictionary<string, int>(accessOrder: true)
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
        var dictionary = new LinkedDictionary<string, int>(accessOrder: true)
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
        var dictionary = new LinkedDictionary<string, int>(accessOrder: true)
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
        var dictionary = new LinkedDictionary<string, int>(accessOrder: true)
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
}
