// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.TryRemove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that removing an existing key returns <see langword="true" /> and surfaces the removed value.
    /// </summary>
    [TestMethod]
    public void TryRemove_WhenKeyExists_ShouldReturnTrueAndValue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 42);

        Assert.IsTrue(dictionary.TryRemove("a", out int value));
        Assert.AreEqual(42, value);
        Assert.AreEqual(0, dictionary.Count);
    }

    /// <summary>
    /// Verifies that removing a missing key returns <see langword="false" /> and the default value.
    /// </summary>
    [TestMethod]
    public void TryRemove_WhenKeyMissing_ShouldReturnFalseAndDefault()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        Assert.IsFalse(dictionary.TryRemove("missing", out int value));
        Assert.AreEqual(0, value);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryRemove_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.TryRemove(null!, out _);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an explicit removal is not an eviction: it raises no
    /// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.ItemEvicted" /> event and does not increment
    /// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.EvictionCount" />.
    /// </summary>
    [TestMethod]
    public void TryRemove_WhenKeyExists_ShouldNotCountAsEviction()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        int evictedCallbacks = 0;
        dictionary.ItemEvicted += (_, _) => evictedCallbacks++;
        dictionary.Add("a", 1);

        Assert.IsTrue(dictionary.TryRemove("a", out _));

        Assert.AreEqual(0, evictedCallbacks);
        Assert.AreEqual(0, dictionary.EvictionCount);
    }

    /// <summary>
    /// Verifies that a removed key frees its slot: adding a new key afterward does not evict.
    /// </summary>
    [TestMethod]
    public void TryRemove_WhenFull_ShouldFreeSlotForNextAdd()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        Assert.IsTrue(dictionary.TryRemove("a", out _));
        dictionary.Add("c", 3);

        Assert.AreEqual(0, dictionary.EvictionCount);
        AssertContainsExactly(dictionary, ("b", 2), ("c", 3));
    }

    /// <summary>
    /// Verifies that removing a key leaves the remaining entries' eviction order intact.
    /// </summary>
    [TestMethod]
    public void TryRemove_WhenKeyRemovedForFirstInFirstOut_ShouldPreserveRemainingOrder()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 3, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);

        Assert.IsTrue(dictionary.TryRemove("a", out _));
        dictionary.Add("d", 4);
        dictionary.Add("e", 5);

        Assert.IsFalse(dictionary.ContainsKey("b"), "'b' became first-in after 'a' was removed and should have been evicted.");
        AssertContainsExactly(dictionary, ("c", 3), ("d", 4), ("e", 5));
    }
}
