// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Policy.FIFO.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Add" /> evicts the oldest item when capacity is exceeded using FirstInFirstOut policy.
    /// </summary>
    [TestMethod]
    public void Add_WhenPolicyIsFIFOAndCapacityExceeded_ShouldEvictOldest()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("one", 1);
        dictionary.Add("two", 2);
        dictionary.Add("three", 3);

        Assert.IsFalse(dictionary.ContainsKey("one"));
        Assert.IsTrue(dictionary.ContainsKey("two"));
        Assert.IsTrue(dictionary.ContainsKey("three"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.ItemEvicted" /> is raised with the correct key and value when an item is evicted using FirstInFirstOut.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenPolicyIsFIFOAndItemEvicted_ShouldBeCalledWithCorrectKeyValue()
    {
        var evictedItems = new List<string>();
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.ItemEvicted += (key, value) => evictedItems.Add($"{key}:{value}");

        dictionary.Add("X", 10);
        dictionary.Add("Y", 20);
        dictionary.Add("Z", 30);

        CollectionAssert.AreEqual(new[] { "X:10" }, evictedItems);
    }

    /// <summary>
    /// Verifies that multiple ItemEvicted events are raised when multiple evictions occur.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenPolicyIsFIFOAndMultipleEvictions_ShouldTriggerMultipleEvents()
    {
        var evicted = new List<string>();
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.ItemEvicted += (key, value) => evicted.Add($"{key}:{value}");

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);
        dictionary.Add("D", 4);

        CollectionAssert.AreEqual(new[] { "A:1", "B:2" }, evicted);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.ItemEvicting" /> is raised before ItemEvicted when eviction occurs using FirstInFirstOut policy.
    /// </summary>
    [TestMethod]
    public void ItemEvicting_WhenPolicyIsFIFOAndEvictionOccurs_ShouldFireBeforeItemEvicted()
    {
        var sequence = new List<string>();
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);

        dictionary.ItemEvicting += (key, value) => sequence.Add($"Evicting:{key}:{value}");
        dictionary.ItemEvicted += (key, value) => sequence.Add($"Evicted:{key}:{value}");

        dictionary.Add("one", 1);
        dictionary.Add("two", 2);
        dictionary.Add("three", 3);

        CollectionAssert.AreEqual(new[] { "Evicting:one:1", "Evicted:one:1" }, sequence);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.ItemEvicting" /> is raised with the correct key and value before eviction occurs.
    /// </summary>
    [TestMethod]
    public void ItemEvicting_WhenPolicyIsFIFOAndEvictionOccurs_ShouldBeCalledWithCorrectKeyValue()
    {
        var evictedItems = new List<string>();
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.ItemEvicting += (key, value) => evictedItems.Add($"{key}:{value}");

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        CollectionAssert.AreEqual(new[] { "A:1" }, evictedItems);
    }

    /// <summary>
    /// Verifies that entries are returned in insertion order when using FirstInFirstOut eviction policy.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenPolicyIsFIFO_ShouldReturnItemsInInsertionOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(5, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);

        KeyValuePair<string, int>[] expected = new[]
        {
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>("b", 2),
            new KeyValuePair<string, int>("c", 3)
        };

        CollectionAssert.AreEqual(expected, new List<KeyValuePair<string, int>>(dictionary));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Keys" /> are returned in insertion order when using FirstInFirstOut eviction policy.
    /// </summary>
    [TestMethod]
    public void Keys_WhenPolicyIsFIFO_ShouldReturnKeysInInsertionOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.FirstInFirstOut)
        {
            ["one"] = 1,
            ["two"] = 2,
            ["three"] = 3
        };

        CollectionAssert.AreEqual(new[] { "one", "two", "three" }, dictionary.Keys.ToList());
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Values" /> are returned in insertion order when using FirstInFirstOut eviction policy.
    /// </summary>
    [TestMethod]
    public void Values_WhenPolicyIsFIFO_ShouldReturnValuesInInsertionOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.FirstInFirstOut)
        {
            ["a"] = 10,
            ["b"] = 20,
            ["c"] = 30
        };

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, dictionary.Values.ToList());
    }

    /// <summary>
    /// Verifies that the oldest item is evicted when using FirstInFirstOut policy.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenPolicyIsFIFO_ShouldEvictOldestItem()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        CollectionAssert.Contains(evicted, "A");
    }

    /// <summary>
    /// Verifies that the FirstInFirstOut insertion order is reset when Clear is called.
    /// </summary>
    [TestMethod]
    public void Clear_WhenPolicyIsFIFOAndCalled_ShouldResetInsertionOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        dictionary.Clear();
        dictionary.Add("C", 3);
        dictionary.Add("D", 4);

        var keys = dictionary.Keys.ToList();
        CollectionAssert.DoesNotContain(keys, "A");
        CollectionAssert.DoesNotContain(keys, "B");
    }

    /// <summary>
    /// Verifies that the oldest inserted key is returned when using FirstInFirstOut policy.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenPolicyIsFIFO_ShouldReturnOldestKey()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("first", 1);
        dictionary.Add("second", 2);
        dictionary.Add("third", 3);

        Assert.AreEqual("first", dictionary.PeekEvictionCandidate());
    }

    /// <summary>
    /// Verifies that no exception is thrown when the FirstInFirstOut eviction candidate has already been removed.
    /// </summary>
    [TestMethod]
    public void EvictionEvents_WhenPolicyIsFIFOAndCandidateMissing_ShouldNotThrow()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Remove("A");

        dictionary.Add("C", 3); // candidate "A" was already removed

        Assert.IsTrue(dictionary.ContainsKey("B"));
        Assert.IsTrue(dictionary.ContainsKey("C"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Touch" /> does not affect eviction order when using FirstInFirstOut policy.
    /// </summary>
    [TestMethod]
    public void Touch_WhenPolicyIsFIFOAndKeyTouched_ShouldHaveNoEffectOnEvictionOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);

        dictionary.Touch("a"); // no effect in FIFO

        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);

        dictionary.Add("d", 4); // should still evict "a" (oldest insertion)

        CollectionAssert.Contains(evicted, "a");
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Touch" /> increments TotalTouches when using FirstInFirstOut policy.
    /// </summary>
    [TestMethod]
    public void Touch_WhenPolicyIsFIFO_ShouldIncrementTotalTouches()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("a", 1);
        var before = dictionary.TotalTouches;

        dictionary.Touch("a");

        Assert.AreEqual(before + 1, dictionary.TotalTouches);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TouchOrThrow" /> does not alter eviction order when using FirstInFirstOut policy.
    /// </summary>
    [TestMethod]
    public void TouchOrThrow_WhenPolicyIsFIFOAndKeyExists_ShouldHaveNoEffect()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("item", 99);

        dictionary.TouchOrThrow("item");

        Assert.IsTrue(dictionary.ContainsKey("item"));
    }
}
