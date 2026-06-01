// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.EvictionCount.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.EvictionCount" /> increments each time an item is evicted due to exceeding capacity.
    /// </summary>
    [TestMethod]
    public void EvictionCount_WhenCapacityExceededMultipleTimes_ShouldIncrementWithEachEviction()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3); // eviction

        Assert.AreEqual(1, dictionary.EvictionCount);

        dictionary.Add("d", 4); // another eviction
        Assert.AreEqual(2, dictionary.EvictionCount);
    }

    /// <summary>
    /// Verifies that TotalTouches resets to zero after Clear is called.
    /// </summary>
    [TestMethod]
    public void EvictionCount_WhenCleared_ShouldResetToZero()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);

        Assert.IsGreaterThan(0, dictionary.EvictionCount);

        dictionary.Clear();

        Assert.AreEqual(0, dictionary.EvictionCount);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.EvictionCount" /> tracks the correct number of evictions over many additions.
    /// </summary>
    [TestMethod]
    public void EvictionCount_WhenManyEvictionsOccur_ShouldReflectCorrectEvictionTotal()
    {
        var dictionary = new EvictingDictionary<int, int>(1);
        for (var i = 0; i < 10; i++)
        {
            dictionary.Add(i, i);
        }

        Assert.AreEqual(9, dictionary.EvictionCount);
    }
    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.EvictionCount" /> is zero for a newly constructed dictionary before any evictions.
    /// </summary>
    [TestMethod]
    public void EvictionCount_WhenNoEvictions_ShouldBeZero()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        Assert.AreEqual(0, dictionary.EvictionCount);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.EvictionCount" /> does not increment when an existing key is overwritten.
    /// </summary>
    [TestMethod]
    public void EvictionCount_WhenReplacingExistingKey_ShouldNotIncrement()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("a", 99); // replace existing, no eviction

        Assert.AreEqual(0, dictionary.EvictionCount);
    }

}
