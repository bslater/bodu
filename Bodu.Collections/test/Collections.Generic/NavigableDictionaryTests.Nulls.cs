// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.Nulls.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies the family-wide null policy: every key-taking member rejects a <see langword="null" /> key with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenKeyTakingMembersReceiveNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableDictionary<string, int>();
        sut.Add("a", 1);

        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.Add(null!, 1); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.TryAdd(null!, 1); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.Remove(null!); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.Remove(null!, out _); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.TryGetValue(null!, out _); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.ContainsKey(null!); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { _ = sut[null!]; });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut[null!] = 1; });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.IndexOfKey(null!); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.TryGetFloorEntry(null!, out _); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.TryGetCeilingEntry(null!, out _); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.TryGetHigherEntry(null!, out _); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.TryGetLowerEntry(null!, out _); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.TryGetFloorKey(null!, out _); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.TryGetCeilingKey(null!, out _); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.TryGetHigherKey(null!, out _); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.TryGetLowerKey(null!, out _); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.CountInRange(null!, "z"); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { _ = sut.Range(null!, "z"); });
    }

    /// <summary>
    /// Verifies that a rejected <see langword="null" /> key leaves the dictionary contents untouched.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenNullKeyRejected_ShouldLeaveDictionaryUnchanged()
    {
        var sut = new NavigableDictionary<string, int>();
        sut.Add("a", 1);
        sut.Add("b", 2);

        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.Add(null!, 3); });

        Assert.AreEqual(2, sut.Count);
        CollectionAssert.AreEqual(new[] { "a", "b" }, sut.Select(e => e.Key).ToList());
    }

    /// <summary>
    /// Verifies that <see langword="null" /> values are accepted and flow through add, lookup, enumeration,
    /// containment, and removal.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenValuesAreNull_ShouldStoreAndSurfaceNullThroughAllApis()
    {
        var sut = new NavigableDictionary<int, string?>();
        sut.Add(2, null);
        sut.Add(1, "one");
        sut[3] = null;

        Assert.AreEqual(3, sut.Count);
        Assert.IsNull(sut[2]);
        Assert.IsNull(sut[3]);
        Assert.IsTrue(sut.TryGetValue(2, out string? value));
        Assert.IsNull(value);
        Assert.IsTrue(sut.ContainsValue(null));
        CollectionAssert.AreEqual(new string?[] { "one", null, null }, sut.Select(e => e.Value).ToList());

        Assert.IsTrue(sut.Remove(2, out string? removed));
        Assert.IsNull(removed);
        Assert.AreEqual(2, sut.Count);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> value can be overwritten with a non-null value and vice versa through
    /// the indexer.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenValueOverwritten_ShouldToggleBetweenNullAndNonNull()
    {
        var sut = new NavigableDictionary<int, string?>();
        sut.Add(1, null);

        sut[1] = "present";
        Assert.AreEqual("present", sut[1]);

        sut[1] = null;
        Assert.IsNull(sut[1]);
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that the navigation queries surface entries whose values are <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenNavigatingToNullValuedEntries_ShouldReturnEntryWithNullValue()
    {
        var sut = new NavigableDictionary<int, string?>();
        sut.Add(10, null);
        sut.Add(20, "twenty");

        Assert.IsTrue(sut.TryGetFloorEntry(15, out KeyValuePair<int, string?> floor));
        Assert.AreEqual(10, floor.Key);
        Assert.IsNull(floor.Value);

        Assert.IsTrue(sut.TryGetMinEntry(out KeyValuePair<int, string?> min));
        Assert.IsNull(min.Value);
    }
}
