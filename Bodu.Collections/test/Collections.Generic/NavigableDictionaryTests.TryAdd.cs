// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.TryAdd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryAdd" /> returns <see langword="true" /> and
    /// stores the entry when the key is new.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyIsNew_ShouldReturnTrueAndStoreEntry()
    {
        var sut = new NavigableDictionary<int, string>();

        bool added = sut.TryAdd(1, "one");

        Assert.IsTrue(added);
        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual("one", sut[1]);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryAdd" /> returns <see langword="false" /> for a
    /// duplicate key and leaves the stored value unchanged.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyIsDuplicate_ShouldReturnFalseAndPreserveStoredValue()
    {
        var sut = new NavigableDictionary<int, string>();
        sut.Add(1, "original");

        bool added = sut.TryAdd(1, "replacement");

        Assert.IsFalse(added);
        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual("original", sut[1]);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryAdd" /> respects the comparer when deciding
    /// duplicates.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyIsComparerEqual_ShouldReturnFalse()
    {
        var sut = new NavigableDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        sut.Add("Alpha", 1);

        Assert.IsFalse(sut.TryAdd("ALPHA", 2));
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryAdd" /> inserts at the comparer position,
    /// keeping enumeration sorted.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeysAddedOutOfOrder_ShouldKeepKeySortedOrder()
    {
        var sut = new NavigableDictionary<int, int>();

        Assert.IsTrue(sut.TryAdd(30, 3000));
        Assert.IsTrue(sut.TryAdd(10, 1000));
        Assert.IsTrue(sut.TryAdd(20, 2000));

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, sut.Select(e => e.Key).ToList());
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryAdd" /> throws
    /// <see cref="ArgumentNullException" /> for a <see langword="null" /> key.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableDictionary<string, int>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.TryAdd(null!, 1);
        }, "key");
    }
}
