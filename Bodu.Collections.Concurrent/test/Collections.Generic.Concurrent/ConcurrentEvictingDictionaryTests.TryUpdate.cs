// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.TryUpdate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.TryUpdate" /> replaces the value and returns
    /// <see langword="true" /> when the current value equals the comparison value.
    /// </summary>
    [TestMethod]
    public void TryUpdate_WhenComparisonMatches_ShouldReplaceValueAndReturnTrue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        bool updated = dictionary.TryUpdate("a", newValue: 2, comparisonValue: 1);

        Assert.IsTrue(updated);
        Assert.AreEqual(2, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.TryUpdate" /> leaves the value unchanged and
    /// returns <see langword="false" /> when the current value differs from the comparison value.
    /// </summary>
    [TestMethod]
    public void TryUpdate_WhenComparisonDoesNotMatch_ShouldReturnFalseAndKeepValue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        bool updated = dictionary.TryUpdate("a", newValue: 2, comparisonValue: 99);

        Assert.IsFalse(updated);
        Assert.AreEqual(1, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.TryUpdate" /> returns <see langword="false" />
    /// when the key is not present.
    /// </summary>
    [TestMethod]
    public void TryUpdate_WhenKeyMissing_ShouldReturnFalse()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        bool updated = dictionary.TryUpdate("z", newValue: 2, comparisonValue: 1);

        Assert.IsFalse(updated);
        Assert.IsFalse(dictionary.ContainsKey("z"));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.TryUpdate" /> throws
    /// <see cref="ArgumentNullException" /> for a <see langword="null" /> key.
    /// </summary>
    [TestMethod]
    public void TryUpdate_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.TryUpdate(null!, newValue: 2, comparisonValue: 1);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a reference-type value comparison uses <see cref="EqualityComparer{TValue}.Default" /> rather than
    /// reference identity.
    /// </summary>
    [TestMethod]
    public void TryUpdate_WhenValuesAreEqualByDefaultComparer_ShouldUpdate()
    {
        var dictionary = new ConcurrentEvictingDictionary<int, string>(capacity: 128);
        dictionary.Add(1, "hello");

        // A freshly built string is equal by value but not by reference.
        bool updated = dictionary.TryUpdate(1, newValue: "world", comparisonValue: new string("hello".ToCharArray()));

        Assert.IsTrue(updated);
        Assert.AreEqual("world", dictionary[1]);
    }

    /// <summary>
    /// Verifies that a successful <see cref="ConcurrentEvictingDictionary{TKey, TValue}.TryUpdate" /> counts as a policy
    /// access, refreshing recency for the least-recently-used policy.
    /// </summary>
    [TestMethod]
    public void TryUpdate_WhenSuccessfulForLeastRecentlyUsed_ShouldRefreshRecency()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        _ = dictionary.TryUpdate("a", newValue: 11, comparisonValue: 1);
        dictionary.Add("c", 3);

        Assert.IsFalse(dictionary.ContainsKey("b"), "The TryUpdate on 'a' should have made 'b' the eviction candidate.");
        Assert.IsTrue(dictionary.ContainsKey("a"));
    }
}
