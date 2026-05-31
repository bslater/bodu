// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Indexer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that accessing a key via indexer updates recency and avoids its eviction.
    /// </summary>
    [TestMethod]
    public void Indexer_Get_WhenAccessed_ShouldPreventEvictionOfAccessedKey()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        _ = dictionary["A"]; // Touch A

        dictionary.Add("C", 3); // Should evict B

        Assert.IsTrue(dictionary.ContainsKey("A"));
        Assert.IsTrue(dictionary.ContainsKey("C"));
        Assert.IsFalse(dictionary.ContainsKey("B"));
    }

    /// <summary>
    /// Verifies that accessing a key via indexer updates its recency.
    /// </summary>
    [TestMethod]
    public void Indexer_Get_WhenAccessingKey_ShouldUpdateRecency()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        _ = dictionary["A"];
        dictionary.Add("C", 3);

        Assert.IsTrue(dictionary.ContainsKey("A"));
        Assert.IsTrue(dictionary.ContainsKey("C"));
        Assert.IsFalse(dictionary.ContainsKey("B"));
    }

    /// <summary>
    /// Verifies that accessing a missing key via indexer throws a KeyNotFoundException.
    /// </summary>
    [TestMethod]
    public void Indexer_Get_WhenKeyIsMissing_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(1);
        Assert.ThrowsExactly<KeyNotFoundException>(() => _ = dictionary["missing"]);
    }

    /// <summary>
    /// Verifies that assigning the same value to an existing key does not trigger an eviction.
    /// </summary>
    [TestMethod]
    public void Indexer_Set_WhenAssigningSameValue_ShouldNotCauseEviction()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        var evictionCount = 0;
        dictionary.ItemEvicting += (_, _) => evictionCount++;

        dictionary["A"] = 1; // No-op

        Assert.AreEqual(0, evictionCount);
        Assert.AreEqual(2, dictionary.Count);
    }

    /// <summary>
    /// Verifies that assigning a new key at capacity triggers eviction of the least recently used key.
    /// </summary>
    [TestMethod]
    public void Indexer_Set_WhenAtCapacity_ShouldEvictLeastRecentlyUsed()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        string evictedKey = null!;
        dictionary.ItemEvicting += (key, _) => evictedKey = key;

        dictionary["C"] = 3;

        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey(evictedKey));
        Assert.IsTrue(dictionary.ContainsKey("C"));
    }

    /// <summary>
    /// Verifies that assigning a new key via indexer raises the ItemEvicting event when eviction occurs.
    /// </summary>
    [TestMethod]
    public void Indexer_Set_WhenCausingEviction_ShouldRaiseItemEvictingEvent()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        string evictedKey = null!;
        dictionary.ItemEvicting += (key, _) => evictedKey = key;

        dictionary["C"] = 3;

        Assert.AreEqual("A", evictedKey);
    }

    /// <summary>
    /// Verifies that assigning an existing key using the indexer replaces the value without altering the count.
    /// </summary>
    [TestMethod]
    public void Indexer_Set_WhenKeyExists_ShouldReplaceValueWithoutChangingCount()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("X", 1);

        dictionary["X"] = 99;

        Assert.AreEqual(1, dictionary.Count);
        Assert.AreEqual(99, dictionary["X"]);
    }

    /// <summary>
    /// Verifies that assigning a new key using the indexer adds a new entry.
    /// </summary>
    [TestMethod]
    public void Indexer_Set_WhenKeyIsNew_ShouldAddEntry()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary["X"] = 42;

        Assert.AreEqual(1, dictionary.Count);
        Assert.AreEqual(42, dictionary["X"]);
    }

    /// <summary>
    /// Verifies that reassigning a previously evicted key treats it as a new entry and may trigger eviction.
    /// </summary>
    [TestMethod]
    public void Indexer_Set_WhenReaddingEvictedKey_ShouldTreatAsNewAndEvictIfNeeded()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3); // Evicts A

        dictionary["A"] = 100; // Should evict B

        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("B"));
        Assert.IsTrue(dictionary.ContainsKey("A"));
        Assert.AreEqual(100, dictionary["A"]);
    }

}
