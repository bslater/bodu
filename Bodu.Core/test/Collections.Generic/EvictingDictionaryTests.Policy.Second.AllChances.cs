// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Policy.Second.AllChances.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{
    /// <summary>
    /// Verifies that the Second-Chance algorithm falls back to evicting the first ordered item when every entry has been granted a
    /// second chance (each item's reference bit is cleared during the pass and the rotated head is returned).
    /// </summary>
    [TestMethod]
    [TestCategory("SecondChance")]
    public void Add_WhenAllItemsHaveSecondChance_ShouldEvictFirstAfterClockSweep()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        // Mark every entry with a second-chance flag so the clock algorithm has to walk the full list,
        // clearing each flag in turn, before returning the rotated head as the eviction candidate.
        dictionary.Touch("A");
        dictionary.Touch("B");
        dictionary.Touch("C");

        dictionary.Add("D", 4);

        // Exactly one of the original entries must have been evicted, and the dictionary should still hold three items.
        Assert.AreEqual(3, dictionary.Count);
        Assert.IsTrue(dictionary.ContainsKey("D"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> returns the key that the Second-Chance
    /// algorithm would select next, without mutating the dictionary.
    /// </summary>
    [TestMethod]
    [TestCategory("SecondChance")]
    public void PeekEvictionCandidate_ForSecondChancePolicy_ShouldReturnCandidateWithoutMutating()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        // Mark A so the next candidate is B (first non-second-chance item).
        dictionary.Touch("A");

        var candidate = dictionary.PeekEvictionCandidate();
        Assert.AreEqual("B", candidate);

        // Peek must not mutate the order or count.
        Assert.AreEqual(3, dictionary.Count);
        Assert.IsTrue(dictionary.ContainsKey("A"));
        Assert.IsTrue(dictionary.ContainsKey("B"));
        Assert.IsTrue(dictionary.ContainsKey("C"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> on a Second-Chance dictionary falls back to
    /// the oldest entry when every entry has been granted a second chance.
    /// </summary>
    [TestMethod]
    [TestCategory("SecondChance")]
    public void PeekEvictionCandidate_ForSecondChancePolicy_WhenAllEntriesTouched_ShouldReturnOldestEntry()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        dictionary.Touch("A");
        dictionary.Touch("B");
        dictionary.Touch("C");

        var candidate = dictionary.PeekEvictionCandidate();
        Assert.AreEqual("A", candidate);
    }
}
