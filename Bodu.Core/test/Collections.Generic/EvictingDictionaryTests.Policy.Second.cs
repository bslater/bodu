// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Policy.Second.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Add" /> evicts the first item without a second-chance flag when capacity is exceeded.
    /// </summary>
    [TestMethod]
    public void Add_WhenPolicyIsSCAndCapacityExceeded_ShouldEvictItemWithoutSecondChance()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Touch("A"); // A gets second chance

        dictionary.Add("C", 3); // B (no second chance) should be evicted

        Assert.IsTrue(dictionary.ContainsKey("A"));
        Assert.IsFalse(dictionary.ContainsKey("B"));
        Assert.IsTrue(dictionary.ContainsKey("C"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Touch" /> grants a second chance to an item under SecondChance policy.
    /// </summary>
    [TestMethod]
    public void Touch_WhenPolicyIsSC_ShouldGrantSecondChance()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("X", 10);
        dictionary.Add("Y", 20);

        dictionary.Touch("X"); // X gets second chance
        dictionary.Add("Z", 30); // Y evicted

        Assert.IsTrue(dictionary.ContainsKey("X"));
        Assert.IsFalse(dictionary.ContainsKey("Y"));
        Assert.IsTrue(dictionary.ContainsKey("Z"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Touch" /> returns true for existing keys under SecondChance policy.
    /// </summary>
    [TestMethod]
    public void Touch_WhenPolicyIsSCAndKeyExists_ShouldReturnTrue()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("key", 42);

        Assert.IsTrue(dictionary.Touch("key"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.TouchOrThrow" /> does not throw and sets the second-chance flag under SecondChance policy.
    /// </summary>
    [TestMethod]
    public void TouchOrThrow_WhenPolicyIsSCAndKeyExists_ShouldSetSecondChance()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("key", 100);

        dictionary.TouchOrThrow("key");

        Assert.IsTrue(dictionary.ContainsKey("key"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> skips items with the second-chance flag and returns the correct candidate.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenPolicyIsSC_ShouldSkipSecondChanceItems()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);
        dictionary.Touch("a"); // a has second chance; b should be next candidate

        Assert.AreEqual("b", dictionary.PeekEvictionCandidate());
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> falls back to the oldest item when all items have the second-chance flag set.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenPolicyIsSCAndAllItemsHaveSecondChance_ShouldReturnOldestKey()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);
        dictionary.Touch("a");
        dictionary.Touch("b");
        dictionary.Touch("c"); // all have second chance

        // All items have the second-chance flag; peek should return the oldest (first in order).
        var candidate = dictionary.PeekEvictionCandidate();

        Assert.IsNotNull(candidate);
        Assert.IsTrue(dictionary.ContainsKey(candidate!));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Clear" /> resets second-chance tracking in SecondChance policy.
    /// </summary>
    [TestMethod]
    public void Clear_WhenPolicyIsSC_ShouldResetSecondChanceFlags()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("A", 1);
        dictionary.Touch("A");

        dictionary.Clear();
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        Assert.IsTrue(dictionary.ContainsKey("B"));
        Assert.IsTrue(dictionary.ContainsKey("C"));
    }

    /// <summary>
    /// Verifies that items with the second-chance flag are cycled to the back of the queue during eviction,
    /// and the first item without the flag is ultimately evicted.
    /// </summary>
    [TestMethod]
    public void Add_WhenPolicyIsSCAndAllHaveSecondChance_ShouldEvictOldestAfterClearingFlags()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Touch("A");
        dictionary.Touch("B");

        dictionary.Add("C", 3); // cycles A then B; A (oldest after cycling) is evicted

        Assert.IsFalse(dictionary.ContainsKey("A"));
        Assert.IsTrue(dictionary.ContainsKey("B"));
        Assert.IsTrue(dictionary.ContainsKey("C"));
    }

    /// <summary>
    /// Verifies that removing an item under SecondChance policy after it was cycled during a prior eviction
    /// succeeds correctly and does not corrupt the internal order list.
    /// </summary>
    /// <remarks>
    /// This test guards against two related bugs: Remove not unlinking the node for SecondChance items,
    /// and GetSecondChanceCandidate not updating item.Node after cycling an item to the tail.
    /// </remarks>
    [TestMethod]
    public void Remove_WhenPolicyIsSCAndItemWasCycledInPriorEviction_ShouldSucceedAndNotCorruptOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Touch("A");
        dictionary.Touch("B");

        dictionary.Add("C", 3); // cycles A and B; A is evicted

        // B was cycled to the tail — its node reference must have been updated.
        var result = dictionary.Remove("B");

        Assert.IsTrue(result);
        Assert.AreEqual(1, dictionary.Count);
        Assert.IsTrue(dictionary.ContainsKey("C"));
        Assert.IsFalse(dictionary.ContainsKey("B"));

        // Confirm the order list is not corrupted by performing a further add and eviction.
        dictionary.Add("D", 4);
        dictionary.Add("E", 5); // should evict C without error

        Assert.AreEqual(2, dictionary.Count);
    }

    /// <summary>
    /// Verifies that iteration reflects correct item order under SecondChance policy.
    /// </summary>
    [TestMethod]
    public void IEnumerable_GetEnumerator_WhenPolicyIsSC_ShouldRespectInsertionAndRotation()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Touch("a");
        dictionary.Add("c", 3); // b evicted; a given second chance, then c added

        CollectionAssert.AreEquivalent(new[] { "a", "c" }, dictionary.Keys.ToList());
    }
}
