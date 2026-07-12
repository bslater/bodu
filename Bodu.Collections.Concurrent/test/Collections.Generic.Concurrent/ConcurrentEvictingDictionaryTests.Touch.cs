// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.Touch.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that touching an existing key returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Touch_WhenKeyExists_ShouldReturnTrue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        Assert.IsTrue(dictionary.Touch("a"));
    }

    /// <summary>
    /// Verifies that touching a missing key returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Touch_WhenKeyMissing_ShouldReturnFalse()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        Assert.IsFalse(dictionary.Touch("missing"));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Touch_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.Touch(null!);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a touch counts as an access for the LeastRecentlyUsed policy, protecting the touched key from
    /// eviction.
    /// </summary>
    [TestMethod]
    public void Touch_WhenKeyExistsForLeastRecentlyUsed_ShouldRefreshRecency()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        Assert.IsTrue(dictionary.Touch("a"));

        dictionary.Add("c", 3);

        Assert.IsFalse(dictionary.ContainsKey("b"), "Touching 'a' should have made 'b' the eviction candidate.");
        Assert.IsTrue(dictionary.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that a touch sets the reference flag under the SecondChance policy, sparing the touched key on the
    /// next eviction pass.
    /// </summary>
    [TestMethod]
    public void Touch_WhenKeyExistsForSecondChance_ShouldSpareTouchedKey()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        Assert.IsTrue(dictionary.Touch("a"));

        dictionary.Add("c", 3);

        Assert.IsFalse(dictionary.ContainsKey("b"), "'b' has no second chance and should have been evicted first.");
        Assert.IsTrue(dictionary.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that a successful touch increments
    /// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.TotalTouches" /> while a miss does not.
    /// </summary>
    [TestMethod]
    public void Touch_WhenCalled_ShouldIncrementTotalTouchesOnlyOnSuccess()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        Assert.IsTrue(dictionary.Touch("a"));
        Assert.IsFalse(dictionary.Touch("missing"));

        Assert.AreEqual(1, dictionary.TotalTouches);
    }
}
