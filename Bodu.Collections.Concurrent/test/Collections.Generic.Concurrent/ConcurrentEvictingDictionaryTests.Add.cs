// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that adding a new key stores the entry.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNew_ShouldStoreEntry()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        dictionary.Add("a", 1);

        Assert.AreEqual(1, dictionary.Count);
        Assert.IsTrue(dictionary.TryGetValue("a", out int value));
        Assert.AreEqual(1, value);
    }

    /// <summary>
    /// Verifies that adding an existing key replaces the value in place without changing the count and without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyExists_ShouldReplaceValueWithoutChangingCount()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        dictionary.Add("a", 2);

        Assert.AreEqual(1, dictionary.Count);
        Assert.IsTrue(dictionary.TryGetValue("a", out int value));
        Assert.AreEqual(2, value);
    }

    /// <summary>
    /// Verifies that adding a <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            dictionary.Add(null!, 1);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that adding a new key to a full dictionary evicts exactly one entry so the count never exceeds the
    /// capacity.
    /// </summary>
    [TestMethod]
    public void Add_WhenFull_ShouldEvictOneEntry()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        dictionary.Add("c", 3);

        Assert.AreEqual(2, dictionary.Count);
        Assert.AreEqual(1, dictionary.EvictionCount);
    }

    /// <summary>
    /// Verifies that under the LeastRecentlyUsed policy the least recently accessed entry is evicted when a new key is
    /// added to a full dictionary.
    /// </summary>
    [TestMethod]
    public void Add_WhenFullForLeastRecentlyUsed_ShouldEvictLeastRecentlyAccessedKey()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        Assert.IsTrue(dictionary.TryGetValue("a", out _));

        dictionary.Add("c", 3);

        Assert.IsFalse(dictionary.ContainsKey("b"), "The least recently used key should have been evicted.");
        AssertContainsExactly(dictionary, ("a", 1), ("c", 3));
    }

    /// <summary>
    /// Verifies that replacing an existing key's value on a full dictionary does not trigger an eviction.
    /// </summary>
    [TestMethod]
    public void Add_WhenFullAndKeyExists_ShouldReplaceWithoutEvicting()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        dictionary.Add("a", 10);

        Assert.AreEqual(2, dictionary.Count);
        Assert.AreEqual(0, dictionary.EvictionCount);
        AssertContainsExactly(dictionary, ("a", 10), ("b", 2));
    }

    /// <summary>
    /// Verifies that in-place replacement preserves the entry's eviction metadata: for FirstInFirstOut the replaced
    /// entry keeps its original queue position.
    /// </summary>
    [TestMethod]
    public void Add_WhenReplacingForFirstInFirstOut_ShouldPreserveInsertionOrder()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        dictionary.Add("a", 10);
        dictionary.Add("c", 3);

        Assert.IsFalse(dictionary.ContainsKey("a"), "Replacing must not reset FIFO order; 'a' is still first in.");
        AssertContainsExactly(dictionary, ("b", 2), ("c", 3));
    }
}
