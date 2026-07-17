// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.Eviction.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that with a single segment the FirstInFirstOut policy evicts the earliest inserted key regardless of
    /// accesses.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenFirstInFirstOut_ShouldEvictEarliestInsertedKey()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 3, EvictingDictionaryPolicy.FirstInFirstOut);
        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);
        Assert.IsTrue(dictionary.TryGetValue("a", out _));

        dictionary.Add("d", 4);

        CollectionAssert.AreEqual(new[] { "a" }, evicted, "FIFO must ignore the access and evict the first inserted key.");
    }

    /// <summary>
    /// Verifies that with a single segment the LeastRecentlyUsed policy evicts the key that has gone longest without
    /// access.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenLeastRecentlyUsed_ShouldEvictLeastRecentlyAccessedKey()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 3, EvictingDictionaryPolicy.LeastRecentlyUsed);
        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);
        Assert.IsTrue(dictionary.TryGetValue("a", out _));

        dictionary.Add("d", 4);

        CollectionAssert.AreEqual(new[] { "b" }, evicted, "'b' is the least recently accessed key after 'a' was read.");
    }

    /// <summary>
    /// Verifies that with a single segment the MostRecentlyUsed policy evicts the most recently accessed key.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenMostRecentlyUsed_ShouldEvictMostRecentlyAccessedKey()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 3, EvictingDictionaryPolicy.MostRecentlyUsed);
        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);
        Assert.IsTrue(dictionary.TryGetValue("a", out _));

        dictionary.Add("d", 4);

        CollectionAssert.AreEqual(new[] { "a" }, evicted, "'a' became the most recently used key when it was read.");
    }

    /// <summary>
    /// Verifies that with a single segment the LeastFrequentlyUsed policy evicts the first key at the lowest access
    /// frequency.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenLeastFrequentlyUsed_ShouldEvictLowestFrequencyKey()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 3, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);
        Assert.IsTrue(dictionary.TryGetValue("a", out _));
        Assert.IsTrue(dictionary.TryGetValue("c", out _));

        dictionary.Add("d", 4);

        CollectionAssert.AreEqual(new[] { "b" }, evicted, "'b' is the only key still at frequency 1.");
    }

    /// <summary>
    /// Verifies that with a single segment the SecondChance policy spares a recently accessed key once, evicting the
    /// first unreferenced key instead.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenSecondChance_ShouldSpareReferencedKeyOnce()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 3, EvictingDictionaryPolicy.SecondChance);
        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);
        Assert.IsTrue(dictionary.TryGetValue("a", out _));

        dictionary.Add("d", 4);

        CollectionAssert.AreEqual(new[] { "b" }, evicted, "'a' holds a reference bit; 'b' is the first unreferenced key.");
        Assert.IsTrue(dictionary.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that the RandomReplacement policy evicts exactly one existing key when the dictionary is full.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenRandomReplacement_ShouldEvictExactlyOneExistingKey()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 3, EvictingDictionaryPolicy.RandomReplacement);
        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);

        dictionary.Add("d", 4);

        Assert.HasCount(1, evicted);
        Assert.Contains(evicted[0], new[] { "a", "b", "c" }, "The victim must be one of the previously stored keys.");
        Assert.AreEqual(3, dictionary.Count);
        Assert.IsTrue(dictionary.ContainsKey("d"));
    }

    /// <summary>
    /// Verifies that a sustained add sequence through a multi-segment dictionary keeps the global count bounded by the
    /// capacity for every policy.
    /// </summary>
    [TestMethod]
    [DataRow(EvictingDictionaryPolicy.FirstInFirstOut)]
    [DataRow(EvictingDictionaryPolicy.LeastRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.LeastFrequentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.MostRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.RandomReplacement)]
    [DataRow(EvictingDictionaryPolicy.SecondChance)]
    public void Eviction_WhenManyKeysAddedAcrossSegments_ShouldBoundCountByCapacity(EvictingDictionaryPolicy policy)
    {
        var dictionary = new ConcurrentEvictingDictionary<int, int>(capacity: 17, policy);

        for (int i = 0; i < 500; i++)
            dictionary.Add(i, i);

        Assert.IsTrue(dictionary.Count <= 17, $"Count {dictionary.Count} exceeded the capacity bound.");
        Assert.AreEqual(dictionary.Count, dictionary.ToArray().Length);
    }

    /// <summary>
    /// Verifies that frequency tracking under LeastFrequentlyUsed honors the dictionary's key comparer, so a key
    /// touched through an equivalent-but-differently-cased key retains its accumulated frequency and is not evicted
    /// ahead of colder entries.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenLFUWithCustomComparerAndEquivalentKeyTouched_ShouldEvictColderEntry()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(
            concurrencyLevel: 1, capacity: 2, EvictingDictionaryPolicy.LeastFrequentlyUsed, StringComparer.OrdinalIgnoreCase, null);
        dictionary.Add("one", 1);
        dictionary.Add("two", 2);

        Assert.IsTrue(dictionary.TryGetValue("ONE", out _)); // one frequency = 2 via an equivalent key

        dictionary.Add("three", 3); // two (frequency 1) should be evicted

        Assert.IsTrue(dictionary.ContainsKey("one"));
        Assert.IsFalse(dictionary.ContainsKey("two"));
        Assert.IsTrue(dictionary.ContainsKey("three"));
    }

    /// <summary>
    /// Verifies that repeated capacity-pressured adds under LeastFrequentlyUsed with a custom comparer keep producing
    /// eviction candidates after entries have been touched through equivalent keys, rather than permanently failing
    /// with <see cref="InvalidOperationException" /> from a desynchronized frequency bucket.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenLFUWithCustomComparerAndEvictionsRepeat_ShouldKeepEvicting()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(
            concurrencyLevel: 1, capacity: 2, EvictingDictionaryPolicy.LeastFrequentlyUsed, StringComparer.OrdinalIgnoreCase, null);
        dictionary.Add("one", 1);
        dictionary.Add("two", 2);
        Assert.IsTrue(dictionary.TryGetValue("ONE", out _));

        dictionary.Add("three", 3);
        dictionary.Add("four", 4);
        dictionary.Add("five", 5);

        Assert.AreEqual(2, dictionary.Count);
    }

    /// <summary>
    /// Verifies that removing an entry under LeastFrequentlyUsed through an equivalent key clears its frequency
    /// tracking, so later capacity-pressured adds do not encounter a stale bucket entry.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenLFUWithCustomComparerAndEquivalentKeyRemoved_ShouldClearFrequencyTracking()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(
            concurrencyLevel: 1, capacity: 2, EvictingDictionaryPolicy.LeastFrequentlyUsed, StringComparer.OrdinalIgnoreCase, null);
        dictionary.Add("one", 1);
        dictionary.Add("two", 2);

        Assert.IsTrue(dictionary.TryRemove("ONE", out _));

        dictionary.Add("three", 3);
        dictionary.Add("four", 4); // capacity pressure: must evict cleanly

        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("one"));
    }
}
