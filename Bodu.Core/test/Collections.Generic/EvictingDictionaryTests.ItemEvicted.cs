// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.ItemEvicted.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{
    /// <summary>
    /// Verifies that the ItemEvicted event is not triggered when capacity is not exceeded.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenCapacityNotExceeded_ShouldNotTriggerEvent()
    {
        var anyEventFired = false;
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.ItemEvicting += (_, _) => anyEventFired = true;
        dictionary.ItemEvicted += (_, _) => anyEventFired = true;

        dictionary.Add("one", 1);
        dictionary.Add("two", 2);

        Assert.IsFalse(anyEventFired);
    }

    /// <summary>
    /// Verifies that items are evicted in insertion order when untouched and capacity is exceeded.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenItemsAreUntouched_ShouldEvictInInsertionOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        var evictedKeys = new List<string>();

        dictionary.ItemEvicting += (key, _) => evictedKeys.Add(key);

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);
        dictionary.Add("D", 4);

        CollectionAssert.AreEqual(new[] { "A", "B" }, evictedKeys);
    }

    /// <summary>
    /// Verifies that the ItemEvicted event is not triggered when the order list is empty.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenOrderListIsEmpty_ShouldNotTriggerEvent()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Remove("A");
        dictionary.Remove("B");
        dictionary.Add("C", 3);

        Assert.AreEqual(0, evicted.Count);
    }

    /// <summary>
    /// Verifies that the ItemEvicted event is raised for the oldest item using FirstInFirstOut policy.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenPolicyIsFIFOAndEvictionOccurs_ShouldRaiseEventForOldestItem()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);

        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        Assert.AreEqual("A", evicted[0]);
    }

    /// <summary>
    /// Verifies that replacing an existing key at capacity does not trigger the ItemEvicted event.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenReplacingKeyAtCapacity_ShouldNotTriggerEvent()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        var evictedCount = 0;
        dictionary.ItemEvicted += (_, _) => evictedCount++;

        dictionary["A"] = 99;

        Assert.AreEqual(0, evictedCount);
    }

    /// <summary>
    /// Verifies that Remove and Clear operations do not trigger ItemEvicted or ItemEvicting events.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenUsingRemoveOrClear_ShouldNotTriggerEvent()
    {
        var eventFired = false;
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.ItemEvicted += (_, _) => eventFired = true;
        dictionary.ItemEvicting += (_, _) => eventFired = true;

        dictionary.Add("A", 1);
        dictionary.Remove("A");
        Assert.IsFalse(eventFired);

        dictionary.Add("B", 2);
        dictionary.Add("C", 3);
        dictionary.Clear();
        Assert.IsFalse(eventFired);
    }
}
