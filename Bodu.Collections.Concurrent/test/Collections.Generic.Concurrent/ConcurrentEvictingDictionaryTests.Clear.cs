// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that clearing removes every entry.
    /// </summary>
    [TestMethod]
    public void Clear_WhenEntriesExist_ShouldRemoveEverything()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        dictionary.Clear();

        Assert.AreEqual(0, dictionary.Count);
        Assert.IsTrue(dictionary.IsEmpty);
        Assert.IsFalse(dictionary.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that clearing resets the <see cref="ConcurrentEvictingDictionary{TKey, TValue}.TotalTouches" /> and
    /// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.EvictionCount" /> counters.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCountersAccumulated_ShouldResetTelemetry()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        Assert.IsTrue(dictionary.TryGetValue("a", out _));
        dictionary.Add("c", 3);
        Assert.AreNotEqual(0, dictionary.TotalTouches);
        Assert.AreNotEqual(0, dictionary.EvictionCount);

        dictionary.Clear();

        Assert.AreEqual(0, dictionary.TotalTouches);
        Assert.AreEqual(0, dictionary.EvictionCount);
    }

    /// <summary>
    /// Verifies that clearing does not raise <see cref="ConcurrentEvictingDictionary{TKey, TValue}.ItemEvicted" />.
    /// </summary>
    [TestMethod]
    public void Clear_WhenEntriesExist_ShouldNotRaiseItemEvicted()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        int evictedCallbacks = 0;
        dictionary.ItemEvicted += (_, _) => evictedCallbacks++;
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        dictionary.Clear();

        Assert.AreEqual(0, evictedCallbacks);
    }

    /// <summary>
    /// Verifies that the dictionary is fully usable after a clear: entries can be added again up to capacity.
    /// </summary>
    [TestMethod]
    public void Clear_WhenReusedAfterClear_ShouldBehaveAsEmptyDictionary()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Clear();

        dictionary.Add("x", 10);
        dictionary.Add("y", 20);
        dictionary.Add("z", 30);

        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("x"), "FIFO order must restart after Clear; 'x' was first in.");
        AssertContainsExactly(dictionary, ("y", 20), ("z", 30));
    }
}
