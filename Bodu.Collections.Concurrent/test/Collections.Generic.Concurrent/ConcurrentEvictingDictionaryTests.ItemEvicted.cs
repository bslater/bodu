// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.ItemEvicted.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that a capacity eviction raises the event with the evicted key and value.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenCapacityEvictionOccurs_ShouldRaiseWithEvictedEntry()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.FirstInFirstOut);
        var evicted = new List<(string Key, int Value)>();
        dictionary.ItemEvicted += (key, value) => evicted.Add((key, value));
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        dictionary.Add("c", 3);

        Assert.HasCount(1, evicted);
        Assert.AreEqual(("a", 1), evicted[0]);
    }

    /// <summary>
    /// Verifies that the event is raised after the eviction has been committed: the evicted key is already absent when
    /// the handler runs.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenHandlerRuns_ShouldObserveEntryAlreadyRemoved()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.FirstInFirstOut);
        bool? containedDuringHandler = null;
        dictionary.ItemEvicted += (key, _) => containedDuringHandler = dictionary.ContainsKey(key);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        dictionary.Add("c", 3);

        Assert.IsFalse(containedDuringHandler, "The evicted entry must already be gone when the handler observes the dictionary.");
    }

    /// <summary>
    /// Verifies that the handler runs outside the segment lock: a handler can call back into the dictionary without
    /// deadlocking or corrupting state.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenHandlerMutatesDictionary_ShouldNotDeadlockOrCorrupt()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 3, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.ItemEvicted += (key, value) =>
        {
            // Only react to the first, known victim so the callback cannot cascade indefinitely.
            if (key == "a")
                _ = dictionary.TryAdd("evicted:a", value);
        };
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);

        dictionary.Add("d", 4);

        Assert.IsTrue(dictionary.Count <= dictionary.Capacity);
        Assert.IsTrue(dictionary.ContainsKey("evicted:a"), "The re-entrant add from the handler must have been applied.");
    }

    /// <summary>
    /// Verifies that a throwing handler is suppressed and does not prevent later subscribers from being notified.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenHandlerThrows_ShouldSuppressAndNotifyRemainingSubscribers()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.FirstInFirstOut);
        int secondHandlerCalls = 0;
        dictionary.ItemEvicted += (_, _) => throw new InvalidOperationException("handler failure");
        dictionary.ItemEvicted += (_, _) => secondHandlerCalls++;
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        dictionary.Add("c", 3);

        Assert.AreEqual(1, secondHandlerCalls, "The second subscriber must be notified despite the first throwing.");
        Assert.AreEqual(2, dictionary.Count);
    }

    /// <summary>
    /// Verifies that an expiry-driven lazy removal raises the event, matching the non-concurrent contract.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenEntryExpiresLazily_ShouldRaiseWithExpiredEntry()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(1), EvictingDictionaryExpirationKind.Absolute, time);
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 4, expiration: expiration);
        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);
        dictionary.Add("a", 1);

        time.Advance(TimeSpan.FromMinutes(2));
        Assert.IsFalse(dictionary.TryGetValue("a", out _));

        Assert.HasCount(1, evicted);
        Assert.AreEqual("a", evicted[0]);
    }

    /// <summary>
    /// Verifies that a dictionary with no subscribers still evicts correctly and tracks
    /// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.EvictionCount" />.
    /// </summary>
    [TestMethod]
    public void ItemEvicted_WhenNoSubscribers_ShouldStillEvictAndCount()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        dictionary.Add("c", 3);

        Assert.AreEqual(2, dictionary.Count);
        Assert.AreEqual(1, dictionary.EvictionCount);
    }
}
