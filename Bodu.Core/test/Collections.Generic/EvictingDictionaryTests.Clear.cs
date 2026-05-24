// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Clear.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Clear" /> removes all key-value pairs and resets Count to zero.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldRemoveAllEntries()
    {
        var dictionary = new EvictingDictionary<string, int>(capacity: 3);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        dictionary.Clear();

        Assert.AreEqual(0, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("A"));
        Assert.IsFalse(dictionary.ContainsKey("B"));
    }

    /// <summary>
    /// Verifies that EvictionCount is reset to zero after Clear is called.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldResetEvictionCountToZero()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3); // triggers eviction

        Assert.IsGreaterThan(0, dictionary.EvictionCount);

        dictionary.Clear();

        Assert.AreEqual(0, dictionary.EvictionCount);
    }

    /// <summary>
    /// Verifies that Keys returns an empty collection after Clear is called.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldResetOrderedKeys()
    {
        var dictionary = new EvictingDictionary<string, int>(capacity: 3);
        dictionary.Add("X", 10);
        dictionary.Add("Y", 20);

        dictionary.Clear();

        Assert.IsEmpty(dictionary.Keys);
    }

    /// <summary>
    /// Verifies that TotalTouches is reset to zero after Clear is called.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldResetTotalTouchesToZero()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);
        dictionary.Touch("A");
        dictionary.Touch("A");

        Assert.IsGreaterThan(0, dictionary.TotalTouches);

        dictionary.Clear();

        Assert.AreEqual(0, dictionary.TotalTouches);
    }

    /// <summary>
    /// Verifies that items can be added again after Clear without corrupting eviction order.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalledAndItemsReinsertion_ShouldBehaveAsNewDictionary()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        _ = dictionary["A"]; // make A recently used

        dictionary.Clear();

        dictionary.Add("C", 3);
        dictionary.Add("D", 4);
        dictionary.Add("E", 5); // should evict C (oldest after clear)

        Assert.IsFalse(dictionary.ContainsKey("C"));
        Assert.IsTrue(dictionary.ContainsKey("D"));
        Assert.IsTrue(dictionary.ContainsKey("E"));
    }

}
