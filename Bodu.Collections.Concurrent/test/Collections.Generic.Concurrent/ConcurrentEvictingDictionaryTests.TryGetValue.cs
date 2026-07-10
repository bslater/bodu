// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that a lookup of an existing key returns <see langword="true" /> and the stored value.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyExists_ShouldReturnTrueAndValue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();
        dictionary.Add("a", 42);

        Assert.IsTrue(dictionary.TryGetValue("a", out int value));
        Assert.AreEqual(42, value);
    }

    /// <summary>
    /// Verifies that a lookup of a missing key returns <see langword="false" /> and the default value.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyMissing_ShouldReturnFalseAndDefault()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();

        Assert.IsFalse(dictionary.TryGetValue("missing", out int value));
        Assert.AreEqual(0, value);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.TryGetValue(null!, out _);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a successful lookup counts as an access for the LeastRecentlyUsed policy, changing the eviction
    /// candidate.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenHitForLeastRecentlyUsed_ShouldRefreshRecency()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        Assert.IsTrue(dictionary.TryGetValue("a", out _));

        dictionary.Add("c", 3);

        Assert.IsFalse(dictionary.ContainsKey("b"), "Reading 'a' should have made 'b' the eviction candidate.");
    }

    /// <summary>
    /// Verifies that a successful lookup increments the frequency for the LeastFrequentlyUsed policy, protecting the
    /// read key from eviction.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenHitForLeastFrequentlyUsed_ShouldIncrementFrequency()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        Assert.IsTrue(dictionary.TryGetValue("a", out _));

        dictionary.Add("c", 3);

        Assert.IsFalse(dictionary.ContainsKey("b"), "The unread key 'b' should have been the least frequently used victim.");
        Assert.IsTrue(dictionary.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that a miss does not increment <see cref="ConcurrentEvictingDictionary{TKey, TValue}.TotalTouches" />.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyMissing_ShouldNotIncrementTotalTouches()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();

        Assert.IsFalse(dictionary.TryGetValue("missing", out _));
        Assert.AreEqual(0, dictionary.TotalTouches);
    }
}
