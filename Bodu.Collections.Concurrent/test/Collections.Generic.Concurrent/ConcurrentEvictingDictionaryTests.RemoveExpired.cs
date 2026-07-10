// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.RemoveExpired.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that purging removes every expired entry and returns the number removed.
    /// </summary>
    [TestMethod]
    public void RemoveExpired_WhenEntriesExpired_ShouldRemoveThemAndReturnCount()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(null, EvictingDictionaryExpirationKind.Absolute, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 8, expiration);
        dictionary.Add("mortal1", 1, TimeSpan.FromMinutes(1));
        dictionary.Add("mortal2", 2, TimeSpan.FromMinutes(1));
        dictionary.Add("immortal", 3);

        time.Advance(TimeSpan.FromMinutes(2));
        int removed = dictionary.RemoveExpired();

        Assert.AreEqual(2, removed);
        Assert.AreEqual(1, dictionary.Count);
        Assert.IsTrue(dictionary.ContainsKey("immortal"));
    }

    /// <summary>
    /// Verifies that purging returns zero when nothing has expired.
    /// </summary>
    [TestMethod]
    public void RemoveExpired_WhenNothingExpired_ShouldReturnZero()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 4, expiration);
        dictionary.Add("a", 1);

        Assert.AreEqual(0, dictionary.RemoveExpired());
        Assert.AreEqual(1, dictionary.Count);
    }

    /// <summary>
    /// Verifies that purging returns zero when no expiration configuration was supplied at construction.
    /// </summary>
    [TestMethod]
    public void RemoveExpired_WhenExpirationNotConfigured_ShouldReturnZero()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();
        dictionary.Add("a", 1);

        Assert.AreEqual(0, dictionary.RemoveExpired());
        Assert.AreEqual(1, dictionary.Count);
    }

    /// <summary>
    /// Verifies that each purge removal counts as an eviction: it raises
    /// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.ItemEvicted" /> and increments
    /// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.EvictionCount" />.
    /// </summary>
    [TestMethod]
    public void RemoveExpired_WhenEntriesExpired_ShouldRaiseItemEvictedAndCount()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(1), EvictingDictionaryExpirationKind.Absolute, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 8, expiration);
        var evicted = new List<string>();
        dictionary.ItemEvicted += (key, _) => evicted.Add(key);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        time.Advance(TimeSpan.FromMinutes(2));
        int removed = dictionary.RemoveExpired();

        Assert.AreEqual(2, removed);
        Assert.HasCount(2, evicted);
        Assert.AreEqual(2, dictionary.EvictionCount);
    }

    /// <summary>
    /// Verifies that purging reconciles <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Count" /> with the live
    /// set after entries expired unpurged.
    /// </summary>
    [TestMethod]
    public void RemoveExpired_WhenExpiredEntriesLinger_ShouldReconcileCount()
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(1), EvictingDictionaryExpirationKind.Absolute, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 8, expiration);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        time.Advance(TimeSpan.FromMinutes(2));
        Assert.AreEqual(2, dictionary.Count);

        _ = dictionary.RemoveExpired();

        Assert.AreEqual(0, dictionary.Count);
        Assert.IsTrue(dictionary.IsEmpty);
    }
}
