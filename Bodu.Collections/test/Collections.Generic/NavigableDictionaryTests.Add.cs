// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that adding a new key stores the entry and increments the count.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNew_ShouldStoreEntryAndIncrementCount()
    {
        var sut = new NavigableDictionary<int, string>();

        sut.Add(42, "answer");

        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual("answer", sut[42]);
    }

    /// <summary>
    /// Verifies that adding unordered keys keeps enumeration in ascending comparer order.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeysAddedOutOfOrder_ShouldEnumerateInKeySortedOrder()
    {
        var sut = CreateDictionary(3, 1, 4, 1 + 4, 9, 2, 6);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 9 }, sut.Select(e => e.Key).ToList());
    }

    /// <summary>
    /// Verifies that adding a comparer-equal duplicate key throws <see cref="ArgumentException" /> and leaves the
    /// stored entry unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsDuplicate_ShouldThrowArgumentExceptionAndPreserveEntry()
    {
        var sut = new NavigableDictionary<int, string>();
        sut.Add(1, "original");

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Add(1, "replacement");
        });

        Assert.AreEqual("key", ex.ParamName);
        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual("original", sut[1]);
    }

    /// <summary>
    /// Verifies that keys the comparer orders equal are rejected as duplicates even when they differ under the
    /// default comparison.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsComparerEqual_ShouldThrowArgumentException()
    {
        var sut = new NavigableDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        sut.Add("Alpha", 1);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Add("ALPHA", 2);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a large ascending insertion sequence stays sorted and rank-consistent, exercising the insertion
    /// fixup paths.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeysAddedAscending_ShouldStaySortedAndRankConsistent()
    {
        var sut = new NavigableDictionary<int, int>();
        for (int key = 0; key < 64; key++)
            sut.Add(key, key * 100);

        CollectionAssert.AreEqual(Enumerable.Range(0, 64).ToArray(), sut.Select(e => e.Key).ToList());
        for (int rank = 0; rank < 64; rank++)
            Assert.AreEqual(rank, sut.GetAt(rank).Key);
    }

    /// <summary>
    /// Verifies that a large descending insertion sequence stays sorted and rank-consistent, exercising the mirrored
    /// insertion fixup paths.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeysAddedDescending_ShouldStaySortedAndRankConsistent()
    {
        var sut = new NavigableDictionary<int, int>();
        for (int key = 63; key >= 0; key--)
            sut.Add(key, key * 100);

        CollectionAssert.AreEqual(Enumerable.Range(0, 64).ToArray(), sut.Select(e => e.Key).ToList());
        for (int rank = 0; rank < 64; rank++)
            Assert.AreEqual(rank, sut.GetAt(rank).Key);
    }

    /// <summary>
    /// Verifies that adding a <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableDictionary<string, int>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.Add(null!, 1);
        }, "key");
    }
}
