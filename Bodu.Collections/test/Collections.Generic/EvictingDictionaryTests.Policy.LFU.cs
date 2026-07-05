// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Policy.LFU.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Add" /> evicts the least frequently used item when capacity is exceeded.
    /// </summary>
    [TestMethod]
    public void Add_WhenPolicyIsLFUAndCapacityExceeded_ShouldEvictLeastFrequentlyUsed()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        dictionary.Add("one", 1);
        dictionary.Add("two", 2);
        _ = dictionary["one"]; // one frequency = 2

        dictionary.Add("three", 3); // two (frequency 1) should be evicted

        Assert.IsTrue(dictionary.ContainsKey("one"));
        Assert.IsFalse(dictionary.ContainsKey("two"));
        Assert.IsTrue(dictionary.ContainsKey("three"));
    }

    /// <summary>
    /// Verifies that replacing an existing key under LeastFrequentlyUsed preserves its accumulated frequency,
    /// so a well-used key is not knocked back to a cold state by a subsequent value update.
    /// </summary>
    [TestMethod]
    public void Add_WhenReplacingExistingKeyUnderLFU_ShouldPreserveFrequency()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        // Accumulate frequency on "A".
        for (int i = 0; i < 5; i++)
            _ = dictionary["A"];

        // Replace "A"'s value. Under the old behaviour this reset its frequency, making it a cold candidate.
        dictionary.Add("A", 999);

        // Adding a new key should evict "B" (low frequency), not "A" (which retained its high frequency).
        dictionary.Add("C", 3);

        Assert.IsTrue(dictionary.ContainsKey("A"));
        Assert.IsFalse(dictionary.ContainsKey("B"));
        Assert.IsTrue(dictionary.ContainsKey("C"));
        Assert.AreEqual(999, dictionary["A"]);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Clear" /> resets access frequency and allows fresh insertion.
    /// </summary>
    [TestMethod]
    public void Clear_WhenPolicyIsLFU_ShouldAllowFreshInsertAfterReset()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Touch("A");

        dictionary.Clear();
        dictionary.Add("C", 3);
        dictionary.Add("D", 4);

        var keys = dictionary.Keys.ToList();
        CollectionAssert.DoesNotContain(keys, "A");
        CollectionAssert.DoesNotContain(keys, "B");
    }
    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Clear" /> resets LeastFrequentlyUsed frequency tracking when called.
    /// </summary>
    [TestMethod]
    public void Clear_WhenPolicyIsLFUAndCalled_ShouldResetFrequencyTracking()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastFrequentlyUsed);
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
    /// Verifies that eviction event is not fired when no item is eligible for LeastFrequentlyUsed eviction.
    /// </summary>
    [TestMethod]
    public void EvictionEvents_WhenPolicyIsLFUAndNoCandidateFound_ShouldNotFireEvent()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        bool eventFired = false;
        dictionary.ItemEvicted += (_, _) => eventFired = true;

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Remove("A");
        dictionary.Remove("B");

        dictionary.Add("C", 3);

        Assert.IsFalse(eventFired);
    }

    /// <summary>
    /// Verifies that LeastFrequentlyUsed evicts the least frequently used item.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenPolicyIsLFUAndEvictionOccurs_ShouldEvictLeastFrequentlyUsed()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        _ = dictionary["B"]; // B frequency = 2

        dictionary.Add("C", 3); // A (frequency 1) should be evicted

        CollectionAssert.Contains(evicted, "A");
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> returns the least frequently used key.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenPolicyIsLFU_ShouldReturnLeastFrequentlyUsedKey()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        dictionary.Add("x", 1);
        dictionary.Add("y", 2);
        dictionary.Add("z", 3);
        dictionary.Touch("x"); // freq = 2
        dictionary.Touch("y"); // freq = 2

        Assert.AreEqual("z", dictionary.PeekEvictionCandidate());
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Touch" /> returns true when key exists in LeastFrequentlyUsed.
    /// </summary>
    [TestMethod]
    public void Touch_WhenPolicyIsLFUAndKeyExists_ShouldReturnTrue()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        dictionary.Add("x", 42);

        Assert.IsTrue(dictionary.Touch("x"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Touch" /> updates frequency in LeastFrequentlyUsed policy and prevents eviction of the touched key.
    /// </summary>
    [TestMethod]
    public void Touch_WhenPolicyIsLFUAndKeyTouched_ShouldUpdateFrequencyAndPreventEviction()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);

        dictionary.Touch("a"); // freq = 2
        dictionary.Touch("a"); // freq = 3
        dictionary.Touch("b"); // freq = 2

        dictionary.Add("d", 4); // c (freq = 1) should be evicted

        CollectionAssert.AreEquivalent(new[] { "a", "b", "d" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TouchOrThrow" /> updates frequency when the key exists in LeastFrequentlyUsed.
    /// </summary>
    [TestMethod]
    public void TouchOrThrow_WhenPolicyIsLFUAndKeyExists_ShouldUpdateFrequency()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        dictionary.Add("x", 10);

        dictionary.TouchOrThrow("x");

        Assert.IsTrue(dictionary.ContainsKey("x"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TryGetValue" /> updates frequency for LeastFrequentlyUsed, protecting the accessed key from eviction.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenPolicyIsLFUAndKeyAccessed_ShouldUpdateFrequency()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        dictionary.Add("x", 10);
        dictionary.Add("y", 20);

        _ = dictionary.TryGetValue("x", out _); // x frequency = 2; y remains at 1

        dictionary.Add("z", 30); // should evict y (lower frequency)

        Assert.IsTrue(dictionary.ContainsKey("x"));
        Assert.IsFalse(dictionary.ContainsKey("y"));
        Assert.IsTrue(dictionary.ContainsKey("z"));
    }

}
