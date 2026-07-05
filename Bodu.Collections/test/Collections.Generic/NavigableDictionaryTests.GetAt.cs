// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.GetAt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.GetAt" /> returns the entry with the k-th smallest
    /// key, with its paired value, for every valid rank.
    /// </summary>
    [TestMethod]
    public void GetAt_WhenRankIsValid_ShouldReturnKthSmallestEntry()
    {
        var sut = CreateDictionary(50, 10, 40, 20, 30);

        Assert.AreEqual(new KeyValuePair<int, int>(10, 1000), sut.GetAt(0));
        Assert.AreEqual(new KeyValuePair<int, int>(20, 2000), sut.GetAt(1));
        Assert.AreEqual(new KeyValuePair<int, int>(30, 3000), sut.GetAt(2));
        Assert.AreEqual(new KeyValuePair<int, int>(40, 4000), sut.GetAt(3));
        Assert.AreEqual(new KeyValuePair<int, int>(50, 5000), sut.GetAt(4));
    }

    /// <summary>
    /// Verifies the rank round-trip contract: <c>GetAt(IndexOfKey(k)).Key == k</c> for every stored key.
    /// </summary>
    [TestMethod]
    public void GetAt_WhenComposedWithIndexOfKey_ShouldRoundTripEveryKey()
    {
        var sut = new NavigableDictionary<int, int>(
            new[] { 13, 7, 42, 1, 99, 56, 21, 34, 88, 3 }.Select(key => new KeyValuePair<int, int>(key, key * 100)));

        foreach (KeyValuePair<int, int> entry in sut)
            Assert.AreEqual(entry.Key, sut.GetAt(sut.IndexOfKey(entry.Key)).Key);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.GetAt" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for a negative rank.
    /// </summary>
    [TestMethod]
    public void GetAt_WhenRankIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var sut = CreateDictionary(1, 2, 3);

        ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            sut.GetAt(-1);
        }, "rank");
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.GetAt" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for a rank equal to or beyond
    /// <see cref="NavigableDictionary{TKey, TValue}.Count" />.
    /// </summary>
    [TestMethod]
    public void GetAt_WhenRankIsCountOrBeyond_ShouldThrowArgumentOutOfRangeException()
    {
        var sut = CreateDictionary(1, 2, 3);

        ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            sut.GetAt(3);
        }, "rank");
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.GetAt" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for any rank on an empty dictionary.
    /// </summary>
    [TestMethod]
    public void GetAt_WhenDictionaryIsEmpty_ShouldThrowArgumentOutOfRangeException()
    {
        var sut = new NavigableDictionary<int, int>();

        ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            sut.GetAt(0);
        }, "rank");
    }
}
