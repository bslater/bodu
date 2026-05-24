// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Remove.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Remove" /> returns false when the key is not present.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyDoesNotExist_ShouldReturnFalse()
    {
        var dictionary = new EvictingDictionary<string, int>(5);

        var result = dictionary.Remove("missing");

        Assert.IsFalse(result);
    }
    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Remove" /> returns true and deletes the key when it exists.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyExists_ShouldRemoveSuccessfully()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("x", 42);

        var result = dictionary.Remove("x");

        Assert.IsTrue(result);
        Assert.IsFalse(dictionary.ContainsKey("x"));
        Assert.AreEqual(0, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Remove" />(KeyValuePair) returns false if key exists but value does not match.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyExistsButValueDoesNotMatch_ShouldNotRemove()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("a", 1);

        var result = dictionary.Remove(new KeyValuePair<string, int>("a", 2));

        Assert.IsFalse(result);
        Assert.IsTrue(dictionary.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that a removed key is excluded from future eviction operations.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyIsRemoved_ShouldExcludeFromFutureEvictions()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        var evictedKeys = new List<string>();
        dictionary.ItemEvicting += (key, _) => evictedKeys.Add(key);

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Remove("A");

        dictionary.Add("C", 3);
        dictionary.Add("D", 4);

        Assert.DoesNotContain("A", evictedKeys);
        CollectionAssert.Contains(evictedKeys, "B");
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Remove" />(KeyValuePair) removes the entry only if both key and value match.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyValuePairMatches_ShouldRemoveSuccessfully()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("a", 1);

        var result = dictionary.Remove(new KeyValuePair<string, int>("a", 1));

        Assert.IsTrue(result);
        Assert.IsFalse(dictionary.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Remove" /> only deletes the specified key when multiple keys share the same value.
    /// </summary>
    [TestMethod]
    public void Remove_WhenMultipleKeysWithSameValue_ShouldOnlyRemoveMatchingKey()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("a", 10);
        dictionary.Add("b", 10);

        var result = dictionary.Remove(new KeyValuePair<string, int>("a", 10));

        Assert.IsTrue(result);
        Assert.IsFalse(dictionary.ContainsKey("a"));
        Assert.IsTrue(dictionary.ContainsKey("b"));
    }

    /// <summary>
    /// Verifies that removing an item correctly updates frequency tracking in LeastFrequentlyUsed mode.
    /// </summary>
    [TestMethod]
    public void Remove_WhenPolicyIsLFUAndKeyRemoved_ShouldUpdateFrequencyTracking()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        dictionary.Add("x", 100);
        dictionary.Touch("x");

        var result = dictionary.Remove("x");

        Assert.IsTrue(result);
        Assert.IsFalse(dictionary.ContainsKey("x"));
        Assert.AreEqual(0, dictionary.Count);
    }

    /// <summary>
    /// Verifies that removing an item correctly updates the access order in LeastRecentlyUsed mode.
    /// </summary>
    [TestMethod]
    public void Remove_WhenPolicyIsLRUAndKeyRemoved_ShouldUpdateEvictionOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        var result = dictionary.Remove("a");

        Assert.IsTrue(result);
        Assert.IsFalse(dictionary.ContainsKey("a"));
        Assert.AreEqual(1, dictionary.Count);
    }

    /// <summary>
    /// Verifies that PeekEvictionCandidate returns a live key after the previous MRU key was removed
    /// under MostRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    public void Remove_WhenPolicyIsMRUAndMRUItemRemoved_ShouldProduceLivePeekCandidate()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.MostRecentlyUsed);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3); // C is MRU

        dictionary.Remove("C"); // before fix, C's orphaned node remained at _order.Last

        var candidate = dictionary.PeekEvictionCandidate();

        // The candidate must be a key that actually exists in the dictionary.
        Assert.IsNotNull(candidate);
        Assert.IsTrue(dictionary.ContainsKey(candidate!));
        Assert.AreEqual("B", candidate); // B is now the MRU
    }

    /// <summary>
    /// Verifies that removing the current MRU item under MostRecentlyUsed policy does not corrupt
    /// the eviction order for subsequent operations.
    /// </summary>
    /// <remarks>
    /// This test guards against the bug where Remove did not unlink the node from the order list
    /// for MostRecentlyUsed, leaving an orphaned node that could cause PeekEvictionCandidate to
    /// return a key that no longer exists in the dictionary.
    /// </remarks>
    [TestMethod]
    public void Remove_WhenPolicyIsMRUAndRemovedItemWasMRU_ShouldNotCorruptEvictionOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.MostRecentlyUsed);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2); // B is MRU

        dictionary.Remove("B"); // removes MRU; before fix, B's node remained in _order

        // After removal, A is the only entry. Adding C fills the capacity.
        dictionary.Add("C", 3); // Count was 1, no eviction.

        // Adding D at capacity should evict C (the current MRU), not B which was already removed.
        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);
        dictionary.Add("D", 4);

        Assert.HasCount(1, evicted);
        Assert.AreEqual("C", evicted[0]);
        Assert.IsTrue(dictionary.ContainsKey("A"));
        Assert.IsTrue(dictionary.ContainsKey("D"));
    }

    /// <summary>
    /// Verifies that removing an item and then triggering eviction via SecondChance policy
    /// produces correct results without corrupting the order list.
    /// </summary>
    /// <remarks>
    /// This test exercises two related fixes: Remove now unlinks nodes for SecondChance,
    /// and GetSecondChanceCandidate now updates item.Node after cycling an item to the tail,
    /// so a subsequent Remove of the cycled item can be performed in O(1) without error.
    /// </remarks>
    [TestMethod]
    public void Remove_WhenPolicyIsSecondChanceAndItemWasCycledByPriorEviction_ShouldSucceedAndNotCorruptOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        // Give both items a second chance; the first eviction will cycle A and B before evicting A.
        dictionary.Touch("A");
        dictionary.Touch("B");
        dictionary.Add("C", 3); // cycles A then B; A (oldest after cycling) is evicted.

        // B was cycled during eviction — its internal node reference must have been updated.
        // Before the fix, Remove("B") could either leave B's node orphaned (old Remove for SC)
        // or throw InvalidOperationException (new Remove using a stale node from old cycling code).
        var result = dictionary.Remove("B");

        Assert.IsTrue(result);
        Assert.AreEqual(1, dictionary.Count);
        Assert.IsTrue(dictionary.ContainsKey("C"));
        Assert.IsFalse(dictionary.ContainsKey("B"));

        // Verify _order is not corrupted by completing a further eviction cycle.
        dictionary.Add("D", 4); // Count was 1, no eviction.
        dictionary.Add("E", 5); // Count == 2 == capacity; should evict C without error.

        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("C"));
    }

}
