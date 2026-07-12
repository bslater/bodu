// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.ContainsKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that an existing key is reported as present.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyExists_ShouldReturnTrue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        Assert.IsTrue(dictionary.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that a missing key is reported as absent.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyMissing_ShouldReturnFalse()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        Assert.IsFalse(dictionary.ContainsKey("missing"));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.ContainsKey(null!);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a containment probe is a pure read for the capacity policy: it does not reposition the key under
    /// the LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenHitForLeastRecentlyUsed_ShouldNotRefreshRecency()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        Assert.IsTrue(dictionary.ContainsKey("a"));

        dictionary.Add("c", 3);

        Assert.IsFalse(dictionary.ContainsKey("a"), "The containment probe must not have counted as an access on 'a'.");
    }

    /// <summary>
    /// Verifies that a containment probe does not increment
    /// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.TotalTouches" />.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenHit_ShouldNotIncrementTotalTouches()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        Assert.IsTrue(dictionary.ContainsKey("a"));
        Assert.AreEqual(0, dictionary.TotalTouches);
    }
}
