// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Policy.LRU.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that Add evicts the least recently used item when capacity is exceeded using
    /// LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    public void Add_WhenPolicyIsLRUAndCapacityExceeded_ShouldEvictLeastRecentlyUsed()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("one", 1);
        dictionary.Add("two", 2);
        dictionary["one"] = 11;   // access "one" so "two" becomes least recently used
        dictionary.Add("three", 3); // capacity exceeded — "two" should be evicted

        Assert.IsTrue(dictionary.ContainsKey("one"),
            "'one' should be retained because it was accessed most recently before capacity was exceeded.");
        Assert.IsFalse(dictionary.ContainsKey("two"),
            "'two' should have been evicted as it was the least recently used entry at the time capacity was exceeded.");
        Assert.IsTrue(dictionary.ContainsKey("three"),
            "'three' should be present as it was the item that triggered eviction and must be successfully added.");
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Clear" /> resets access order in an LeastRecentlyUsed dictionary.
    /// </summary>
    [TestMethod]
    public void Clear_WhenPolicyIsLRUAndCalled_ShouldResetAccessOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        _ = dictionary["A"];

        dictionary.Clear();
        dictionary.Add("C", 3);
        dictionary.Add("D", 4);

        var keys = dictionary.Keys.ToList();
        CollectionAssert.DoesNotContain(keys, "A");
        CollectionAssert.DoesNotContain(keys, "B");
    }

    /// <summary>
    /// Verifies that eviction does not throw when candidate was removed.
    /// </summary>
    [TestMethod]
    public void EvictionEvents_WhenPolicyIsLRUAndCandidateMissing_ShouldNotThrow()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Remove("A");

        dictionary.Add("C", 3);

        Assert.IsTrue(dictionary.ContainsKey("B"));
        Assert.IsTrue(dictionary.ContainsKey("C"));
    }

    /// <summary>
    /// Verifies that iteration respects insertion and access order under LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    public void IEnumerable_GetEnumerator_WhenPolicyIsLRU_ShouldRespectRecencyOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);
        dictionary.Touch("a");

        KeyValuePair<string, int>[] actual = dictionary.ToArray(); // Iteration order reflects recency
        KeyValuePair<string, int>[] expected =
        [
            new KeyValuePair<string, int>("b", 2),
            new KeyValuePair<string, int>("c", 3),
            new KeyValuePair<string, int>("a", 1),
        ];

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that eviction is skipped when candidate was manually removed before eviction.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenPolicyIsLRUAndCandidateRemovedBeforeEviction_ShouldSkipEviction()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Remove("A");

        dictionary.Add("C", 3);

        Assert.IsEmpty(evicted);
    }

    /// <summary>
    /// Verifies that the least recently used item is evicted when using LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenPolicyIsLRUAndEvictionOccurs_ShouldEvictLeastRecentlyUsed()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        CollectionAssert.Contains(evicted, "A");
    }

    /// <summary>
    /// Verifies that ItemEvicting is fired before ItemEvicted when eviction occurs using
    /// LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenPolicyIsLRUAndEvictionOccurs_ShouldFireAfterItemEvicting()
    {
        var sequence = new List<string>();
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);

        dictionary.ItemEvicting += (key, value) => sequence.Add($"Evicting:{key}:{value}");
        dictionary.ItemEvicted += (key, value) => sequence.Add($"Evicted:{key}:{value}");

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3); // Eviction occurs

        CollectionAssert.AreEqual(new[] { "Evicting:A:1", "Evicted:A:1" }, sequence);
    }

    /// <summary>
    /// Verifies that ItemEvicted is triggered with the correct key and value when an item is
    /// evicted using LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenPolicyIsLRUAndItemEvicted_ShouldBeCalledWithCorrectKeyValue()
    {
        var evicted = new List<string>();
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.ItemEvicted += (key, value) => evicted.Add($"{key}:{value}");

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3); // A should be evicted

        CollectionAssert.AreEqual(new[] { "A:1" }, evicted);
    }

    /// <summary>
    /// Verifies that multiple ItemEvicted events are triggered correctly under
    /// LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenPolicyIsLRUAndMultipleEvictions_ShouldTriggerMultipleEvents()
    {
        var evicted = new List<string>();
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.ItemEvicted += (key, value) => evicted.Add($"{key}:{value}");

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3); // A evicted
        dictionary.Add("D", 4); // B evicted

        CollectionAssert.AreEqual(new[] { "A:1", "B:2" }, evicted);
    }

    /// <summary>
    /// Verifies that ItemEvicting is triggered with the correct key and value before eviction
    /// using LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    public void ItemEvicting_WhenPolicyIsLRU_ShouldBeCalledWithCorrectKeyValue()
    {
        var evicted = new List<string>();
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.ItemEvicting += (key, value) => evicted.Add($"{key}:{value}");

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        CollectionAssert.AreEqual(new[] { "A:1" }, evicted);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.ItemEvicting" /> is triggered before ItemEvicted when using LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    public void ItemEvicting_WhenPolicyIsLRU_ShouldFireBeforeItemEvicted()
    {
        var sequence = new List<string>();
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);

        dictionary.ItemEvicting += (key, value) => sequence.Add($"Evicting:{key}:{value}");
        dictionary.ItemEvicted += (key, value) => sequence.Add($"Evicted:{key}:{value}");

        dictionary.Add("X", 1);
        dictionary.Add("Y", 2);
        dictionary.Add("Z", 3);

        CollectionAssert.AreEqual(new[] { "Evicting:X:1", "Evicted:X:1" }, sequence);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Keys" /> are returned in recency order under LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    public void Keys_WhenPolicyIsLRU_ShouldReturnKeysInRecencyOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);
        dictionary.Touch("a");

        string[] expected = new[] { "b", "c", "a" };
        CollectionAssert.AreEqual(expected, dictionary.Keys.ToList());
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> returns the least recently used key using LeastRecentlyUsed.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenPolicyIsLRU_ShouldReturnLeastRecentlyUsedKey()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);
        dictionary.Touch("a");

        Assert.AreEqual("b", dictionary.PeekEvictionCandidate());
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Touch" /> returns true when key exists in an LeastRecentlyUsed dictionary.
    /// </summary>
    [TestMethod]
    public void Touch_WhenPolicyIsLRUAndKeyExists_ShouldReturnTrue()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);

        bool actual = dictionary.Touch("a");

        Assert.IsTrue(actual);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Touch" /> updates recency in an LeastRecentlyUsed dictionary.
    /// </summary>
    [TestMethod]
    public void Touch_WhenPolicyIsLRUAndKeyTouched_ShouldUpdateRecency()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);

        dictionary.Touch("a");
        dictionary.Add("d", 4); // "b" should be evicted

        var expected = new Dictionary<string, int>
        {
            ["c"] = 3,
            ["a"] = 1,
            ["d"] = 4
        };

        CollectionAssert.AreEquivalent(expected, dictionary.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TouchOrThrow" /> updates recency when the key exists in an LeastRecentlyUsed dictionary.
    /// </summary>
    [TestMethod]
    public void TouchOrThrow_WhenPolicyIsLRUAndKeyExists_ShouldUpdateRecency()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("item", 42);

        dictionary.TouchOrThrow("item");

        Assert.IsTrue(dictionary.ContainsKey("item"));
    }

}
