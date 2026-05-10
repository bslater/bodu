// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Add.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Scale and stress tests that verify correctness of <see cref="EvictingDictionary{TKey, TValue}" />
/// under high operation counts and repeated lifecycle transitions.
/// </summary>
public partial class EvictingDictionaryTests
{
    // --------------------------------------------------------
    // Eviction count accuracy at scale
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that Count never exceeds Capacity when many items are added under FirstInFirstOut policy.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Add_WhenPolicyIsFIFOAndManyItemsAdded_ShouldNeverExceedCapacity()
    {
        const int capacity = 50;
        const int iterations = 1_000;
        var dictionary = new EvictingDictionary<int, int>(capacity, EvictingDictionaryPolicy.FirstInFirstOut);

        for (var i = 0; i < iterations; i++)
        {
            dictionary.Add(i, i);
            Assert.IsTrue(dictionary.Count <= capacity,
                $"Count {dictionary.Count} exceeded capacity {capacity} after adding item {i}.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.EvictionCount" /> equals the number of items added beyond capacity under LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void EvictionCount_WhenPolicyIsLRUAndManyItemsAdded_ShouldMatchExpectedEvictionTotal()
    {
        const int capacity = 10;
        const int itemCount = 200;
        var dictionary = new EvictingDictionary<int, int>(capacity, EvictingDictionaryPolicy.LeastRecentlyUsed);

        for (var i = 0; i < itemCount; i++)
            dictionary.Add(i, i);

        long expectedEvictions = itemCount - capacity;
        Assert.AreEqual(expectedEvictions, dictionary.EvictionCount);
        Assert.AreEqual(capacity, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.EvictionCount" /> equals the number of items added beyond capacity under LeastFrequentlyUsed policy.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void EvictionCount_WhenPolicyIsLFUAndManyItemsAdded_ShouldMatchExpectedEvictionTotal()
    {
        const int capacity = 10;
        const int itemCount = 500;
        var dictionary = new EvictingDictionary<int, int>(capacity, EvictingDictionaryPolicy.LeastFrequentlyUsed);

        for (var i = 0; i < itemCount; i++)
            dictionary.Add(i, i);

        long expectedEvictions = itemCount - capacity;
        Assert.AreEqual(expectedEvictions, dictionary.EvictionCount);
        Assert.AreEqual(capacity, dictionary.Count);
    }

    /// <summary>
    /// Verifies that Count never exceeds Capacity when many items are added under MostRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Add_WhenPolicyIsMRUAndManyItemsAdded_ShouldNeverExceedCapacity()
    {
        const int capacity = 25;
        const int iterations = 1_000;
        var dictionary = new EvictingDictionary<int, int>(capacity, EvictingDictionaryPolicy.MostRecentlyUsed);

        for (var i = 0; i < iterations; i++)
        {
            dictionary.Add(i, i);
            Assert.IsTrue(dictionary.Count <= capacity,
                $"Count {dictionary.Count} exceeded capacity {capacity} at iteration {i}.");
        }
    }

    /// <summary>
    /// Verifies that Count never exceeds Capacity when many items are added under SecondChance policy.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Add_WhenPolicyIsSecondChanceAndManyItemsAdded_ShouldNeverExceedCapacity()
    {
        const int capacity = 20;
        const int iterations = 500;
        var dictionary = new EvictingDictionary<int, int>(capacity, EvictingDictionaryPolicy.SecondChance);

        for (var i = 0; i < iterations; i++)
        {
            dictionary.Add(i, i);

            // Periodically touch several items to exercise second-chance cycling.
            if (i % 10 == 0 && dictionary.Count > 2)
            {
                dictionary.Touch(i - 1);
                dictionary.Touch(i);
            }

            Assert.IsTrue(dictionary.Count <= capacity,
                $"Count {dictionary.Count} exceeded capacity {capacity} at iteration {i}.");
        }
    }

    // --------------------------------------------------------
    // LRU correctness under access patterns
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a frequently touched item is never evicted when items are continuously added under
    /// LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Touch_WhenPolicyIsLRUAndItemIsTouchedRepeatedly_ShouldNeverBeEvicted()
    {
        const int capacity = 5;
        const int iterations = 200;
        var dictionary = new EvictingDictionary<int, int>(capacity, EvictingDictionaryPolicy.LeastRecentlyUsed);

        dictionary.Add(0, 0); // pinned item

        for (var i = 1; i < iterations; i++)
        {
            dictionary.Touch(0); // refresh 0 before each new insertion
            dictionary.Add(i, i);

            Assert.IsTrue(dictionary.ContainsKey(0),
                $"Pinned key 0 was evicted at iteration {i}.");
        }
    }

    // --------------------------------------------------------
    // LFU correctness under frequency accumulation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that an item accessed many times is never evicted when other items have lower frequency
    /// under LeastFrequentlyUsed policy.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Touch_WhenPolicyIsLFUAndItemTouchedManyTimes_ShouldNeverBeEvicted()
    {
        const int capacity = 5;
        const int iterations = 100;
        var dictionary = new EvictingDictionary<int, int>(capacity, EvictingDictionaryPolicy.LeastFrequentlyUsed);

        // Seed pinned item with high frequency.
        dictionary.Add(0, 0);
        for (var t = 0; t < 50; t++)
            dictionary.Touch(0);

        for (var i = 1; i < iterations; i++)
        {
            dictionary.Add(i, i);

            Assert.IsTrue(dictionary.ContainsKey(0),
                $"High-frequency key 0 was evicted at iteration {i}.");
        }
    }

    // --------------------------------------------------------
    // Clear-and-refill cycle stability
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that repeated Clear-and-refill cycles produce a stable, uncorrupted dictionary state
    /// under LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Clear_WhenPolicyIsLRUAndCalledRepeatedly_ShouldNotCorruptState()
    {
        const int capacity = 10;
        const int cycles = 20;
        const int itemsPerCycle = 15;
        var dictionary = new EvictingDictionary<int, int>(capacity, EvictingDictionaryPolicy.LeastRecentlyUsed);

        for (var cycle = 0; cycle < cycles; cycle++)
        {
            dictionary.Clear();

            Assert.AreEqual(0, dictionary.Count, $"Count should be 0 after Clear in cycle {cycle}.");
            Assert.AreEqual(0, dictionary.TotalTouches, $"TotalTouches should be 0 after Clear in cycle {cycle}.");
            Assert.AreEqual(0, dictionary.EvictionCount, $"EvictionCount should be 0 after Clear in cycle {cycle}.");

            for (var i = 0; i < itemsPerCycle; i++)
                dictionary.Add(i, i);

            Assert.AreEqual(capacity, dictionary.Count,
                $"Count should equal capacity after filling in cycle {cycle}.");
        }
    }

    /// <summary>
    /// Verifies that repeated Clear-and-refill cycles produce a stable, uncorrupted dictionary state
    /// under LeastFrequentlyUsed policy.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Clear_WhenPolicyIsLFUAndCalledRepeatedly_ShouldNotCorruptState()
    {
        const int capacity = 8;
        const int cycles = 15;
        const int itemsPerCycle = 20;
        var dictionary = new EvictingDictionary<int, int>(capacity, EvictingDictionaryPolicy.LeastFrequentlyUsed);

        for (var cycle = 0; cycle < cycles; cycle++)
        {
            dictionary.Clear();

            for (var i = 0; i < itemsPerCycle; i++)
            {
                dictionary.Add(i, i);

                // Touch some items to build up frequency.
                if (i % 3 == 0)
                    dictionary.Touch(i);
            }

            Assert.AreEqual(capacity, dictionary.Count,
                $"Count should equal capacity after filling in cycle {cycle}.");
        }
    }

    // --------------------------------------------------------
    // TotalTouches accumulation accuracy
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that TotalTouches accurately reflects the cumulative number of Touch and TryGetValue
    /// calls that succeeded over many operations.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void TotalTouches_WhenManyAccessesOccur_ShouldAccumulateAccurately()
    {
        const int capacity = 10;
        var dictionary = new EvictingDictionary<int, int>(capacity, EvictingDictionaryPolicy.LeastRecentlyUsed);

        for (var i = 0; i < capacity; i++)
            dictionary.Add(i, i);

        // 50 Touch calls on existing keys, 25 TryGetValue hits.
        var expectedTouches = 0;
        for (var t = 0; t < 50; t++)
        {
            if (dictionary.Touch(t % capacity))
                expectedTouches++;
        }

        for (var g = 0; g < 25; g++)
        {
            if (dictionary.TryGetValue(g % capacity, out _))
                expectedTouches++;
        }

        Assert.AreEqual(expectedTouches, dictionary.TotalTouches);
    }

    // --------------------------------------------------------
    // RandomReplacement at scale
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that Count never exceeds Capacity under RandomReplacement policy when many items are added.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Add_WhenPolicyIsRandomReplacementAndManyItemsAdded_ShouldNeverExceedCapacity()
    {
        const int capacity = 30;
        const int iterations = 1_000;
        var dictionary = new EvictingDictionary<int, int>(capacity, EvictingDictionaryPolicy.RandomReplacement);

        for (var i = 0; i < iterations; i++)
        {
            dictionary.Add(i, i);
            Assert.IsTrue(dictionary.Count <= capacity,
                $"Count {dictionary.Count} exceeded capacity {capacity} at iteration {i}.");
        }

        Assert.AreEqual(capacity, dictionary.Count);
    }
}
