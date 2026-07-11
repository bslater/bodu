// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.TryAdd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that adding a new key returns <see langword="true" /> and stores the entry.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyIsNew_ShouldReturnTrueAndStoreEntry()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        Assert.IsTrue(dictionary.TryAdd("a", 1));
        Assert.IsTrue(dictionary.TryGetValue("a", out int value));
        Assert.AreEqual(1, value);
    }

    /// <summary>
    /// Verifies that adding an existing key returns <see langword="false" /> and leaves the stored value unchanged.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyExists_ShouldReturnFalseAndKeepExistingValue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        Assert.IsFalse(dictionary.TryAdd("a", 2));
        Assert.IsTrue(dictionary.TryGetValue("a", out int value));
        Assert.AreEqual(1, value);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.TryAdd(null!, 1);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a rejected add does not disturb the existing entry's recency: the probe is not an access for the
    /// capacity policy.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyExistsForLeastRecentlyUsed_ShouldNotRefreshRecency()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        Assert.IsFalse(dictionary.TryAdd("a", 99));
        dictionary.Add("c", 3);

        Assert.IsFalse(dictionary.ContainsKey("a"), "The rejected TryAdd must not have counted as an access on 'a'.");
        AssertContainsExactly(dictionary, ("b", 2), ("c", 3));
    }

    /// <summary>
    /// Verifies that adding a new key to a full dictionary evicts per the policy, keeping the count at capacity.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenFull_ShouldEvictAndReturnTrue()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        Assert.IsTrue(dictionary.TryAdd("c", 3));
        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("a"));
    }
}
