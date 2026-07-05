// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.Comparer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that a reverse comparer inverts the enumeration order and the
    /// <see cref="NavigableDictionary{TKey, TValue}.MinEntry" /> /
    /// <see cref="NavigableDictionary{TKey, TValue}.MaxEntry" /> extremes.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenReverseComparer_ShouldInvertOrderAndExtremes()
    {
        var sut = new NavigableDictionary<int, string>(ReverseComparer<int>.Instance);
        sut.Add(1, "one");
        sut.Add(2, "two");
        sut.Add(3, "three");

        CollectionAssert.AreEqual(new[] { 3, 2, 1 }, sut.Select(e => e.Key).ToList());
        Assert.AreEqual(3, sut.MinEntry.Key);
        Assert.AreEqual(1, sut.MaxEntry.Key);
    }

    /// <summary>
    /// Verifies that navigation queries follow the reversed ordering: the floor of a key is the nearest key on the
    /// reversed axis.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenReverseComparer_ShouldNavigateOnReversedAxis()
    {
        var sut = new NavigableDictionary<int, int>(
            new[] { 10, 20, 30 }.Select(key => new KeyValuePair<int, int>(key, key * 100)),
            ReverseComparer<int>.Instance);

        // Under the reversed axis, "floor of 25" is the nearest key that orders <= 25, which is 30.
        Assert.IsTrue(sut.TryGetFloorEntry(25, out KeyValuePair<int, int> floor));
        Assert.AreEqual(30, floor.Key);

        Assert.IsTrue(sut.TryGetCeilingEntry(25, out KeyValuePair<int, int> ceiling));
        Assert.AreEqual(20, ceiling.Key);
    }

    /// <summary>
    /// Verifies that ranks follow the comparer order rather than the natural order.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenReverseComparer_ShouldRankOnReversedAxis()
    {
        var sut = new NavigableDictionary<int, int>(
            new[] { 10, 20, 30 }.Select(key => new KeyValuePair<int, int>(key, key * 100)),
            ReverseComparer<int>.Instance);

        Assert.AreEqual(0, sut.IndexOfKey(30));
        Assert.AreEqual(2, sut.IndexOfKey(10));
        Assert.AreEqual(30, sut.GetAt(0).Key);
    }

    /// <summary>
    /// Verifies that a case-insensitive comparer treats differently cased spellings as the same key across add,
    /// lookup, and removal.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCaseInsensitive_ShouldTreatCasedSpellingsAsSameKey()
    {
        var sut = new NavigableDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        sut.Add("Alpha", 1);

        Assert.IsTrue(sut.ContainsKey("ALPHA"));
        Assert.AreEqual(1, sut["alpha"]);
        Assert.IsFalse(sut.TryAdd("aLpHa", 2));
        Assert.IsTrue(sut.Remove("ALPHA"));
        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that the indexer upsert resolves an existing comparer-equal key and overwrites its value while
    /// preserving the originally stored key spelling.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCaseInsensitiveUpsert_ShouldOverwriteValueAndPreserveStoredKey()
    {
        var sut = new NavigableDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        sut.Add("Alpha", 1);

        sut["ALPHA"] = 2;

        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual(2, sut["alpha"]);
        Assert.AreEqual("Alpha", sut.MinEntry.Key);
    }
}
