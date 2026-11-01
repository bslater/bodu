// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.ToArray.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that an empty dictionary snapshots to an empty array.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenEmpty_ShouldReturnEmptyArray()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        Assert.IsEmpty(dictionary.ToArray());
    }

    /// <summary>
    /// Verifies that the snapshot contains exactly the stored entries.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenEntriesExist_ShouldContainExactlyStoredEntries()
    {
        var dictionary = new ConcurrentEvictingDictionary<int, string>(capacity: 32);
        for (int i = 0; i < 10; i++)
            dictionary.Add(i, $"v{i}");

        KeyValuePair<int, string>[] snapshot = dictionary.ToArray();

        Assert.HasCount(10, snapshot);
        foreach (KeyValuePair<int, string> pair in snapshot)
            Assert.AreEqual($"v{pair.Key}", pair.Value);
        Assert.AreEqual(10, snapshot.Select(p => p.Key).Distinct().Count());
    }

    /// <summary>
    /// Verifies that the snapshot is detached: mutations made after the call do not alter the returned array.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenMutatedAfterSnapshot_ShouldNotReflectMutations()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        KeyValuePair<string, int>[] snapshot = dictionary.ToArray();
        dictionary.Add("b", 2);
        Assert.IsTrue(dictionary.TryRemove("a", out _));

        Assert.HasCount(1, snapshot);
        Assert.AreEqual("a", snapshot[0].Key);
        Assert.AreEqual(1, snapshot[0].Value);
    }

    /// <summary>
    /// Verifies that snapshotting is a pure read for the capacity policy: it does not reposition entries under the
    /// LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCalledForLeastRecentlyUsed_ShouldNotRefreshRecency()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        _ = dictionary.ToArray();
        dictionary.Add("c", 3);

        Assert.IsFalse(dictionary.ContainsKey("a"), "The snapshot must not have counted as an access on any entry.");
    }

    /// <summary>
    /// Verifies that a snapshot omits expired-but-unpurged entries under every eviction policy, since each policy
    /// projects the live entries from its own tracking structure.
    /// </summary>
    /// <param name="policy">The eviction policy whose snapshot walk is exercised.</param>
    [TestMethod]
    [DataRow(EvictingDictionaryPolicy.FirstInFirstOut)]
    [DataRow(EvictingDictionaryPolicy.LeastRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.MostRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.LeastFrequentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.SecondChance)]
    [DataRow(EvictingDictionaryPolicy.RandomReplacement)]
    public void ToArray_WhenEntriesExpiredUnpurged_ShouldOmitExpiredEntriesForPolicy(EvictingDictionaryPolicy policy)
    {
        var time = new ManualTimeProvider();
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time);
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, policy, expiration);
        dictionary.Add("mortal", 1, TimeSpan.FromMinutes(1));
        dictionary.Add("live", 2);
        time.Advance(TimeSpan.FromMinutes(2));

        KeyValuePair<string, int>[] snapshot = dictionary.ToArray();

        Assert.AreEqual(2, dictionary.Count, "The expired entry is unpurged and must still be counted.");
        CollectionAssert.AreEqual(new[] { new KeyValuePair<string, int>("live", 2) }, snapshot);
    }
}
