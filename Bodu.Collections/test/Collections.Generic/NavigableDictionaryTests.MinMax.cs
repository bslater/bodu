// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.MinMax.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.MinEntry" /> returns the entry with the smallest
    /// key under the comparer.
    /// </summary>
    [TestMethod]
    public void MinEntry_WhenDictionaryHasEntries_ShouldReturnSmallestKeyEntry()
    {
        var sut = CreateDictionary(30, 10, 20);

        Assert.AreEqual(10, sut.MinEntry.Key);
        Assert.AreEqual(1000, sut.MinEntry.Value);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.MaxEntry" /> returns the entry with the largest
    /// key under the comparer.
    /// </summary>
    [TestMethod]
    public void MaxEntry_WhenDictionaryHasEntries_ShouldReturnLargestKeyEntry()
    {
        var sut = CreateDictionary(30, 10, 20);

        Assert.AreEqual(30, sut.MaxEntry.Key);
        Assert.AreEqual(3000, sut.MaxEntry.Value);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.MinEntry" /> throws
    /// <see cref="InvalidOperationException" /> on an empty dictionary.
    /// </summary>
    [TestMethod]
    public void MinEntry_WhenDictionaryIsEmpty_ShouldThrowInvalidOperationException()
    {
        var sut = new NavigableDictionary<int, int>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = sut.MinEntry;
        });
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.MaxEntry" /> throws
    /// <see cref="InvalidOperationException" /> on an empty dictionary.
    /// </summary>
    [TestMethod]
    public void MaxEntry_WhenDictionaryIsEmpty_ShouldThrowInvalidOperationException()
    {
        var sut = new NavigableDictionary<int, int>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = sut.MaxEntry;
        });
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetMinEntry" /> returns <see langword="true" />
    /// with the smallest-key entry for a non-empty dictionary.
    /// </summary>
    [TestMethod]
    public void TryGetMinEntry_WhenDictionaryHasEntries_ShouldReturnTrueAndSmallest()
    {
        var sut = CreateDictionary(5, 3, 9);

        Assert.IsTrue(sut.TryGetMinEntry(out KeyValuePair<int, int> entry));
        Assert.AreEqual(3, entry.Key);
        Assert.AreEqual(300, entry.Value);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetMaxEntry" /> returns <see langword="true" />
    /// with the largest-key entry for a non-empty dictionary.
    /// </summary>
    [TestMethod]
    public void TryGetMaxEntry_WhenDictionaryHasEntries_ShouldReturnTrueAndLargest()
    {
        var sut = CreateDictionary(5, 3, 9);

        Assert.IsTrue(sut.TryGetMaxEntry(out KeyValuePair<int, int> entry));
        Assert.AreEqual(9, entry.Key);
        Assert.AreEqual(900, entry.Value);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetMinEntry" /> and
    /// <see cref="NavigableDictionary{TKey, TValue}.TryGetMaxEntry" /> return <see langword="false" /> on an empty
    /// dictionary.
    /// </summary>
    [TestMethod]
    public void TryGetMinMaxEntry_WhenDictionaryIsEmpty_ShouldReturnFalse()
    {
        var sut = new NavigableDictionary<int, int>();

        Assert.IsFalse(sut.TryGetMinEntry(out _));
        Assert.IsFalse(sut.TryGetMaxEntry(out _));
    }

    /// <summary>
    /// Verifies that the extremes track mutations: removing the current minimum or maximum promotes the next entry.
    /// </summary>
    [TestMethod]
    public void MinMaxEntry_WhenExtremesRemoved_ShouldPromoteNextEntries()
    {
        var sut = CreateDictionary(1, 2, 3, 4);

        sut.Remove(1);
        sut.Remove(4);

        Assert.AreEqual(2, sut.MinEntry.Key);
        Assert.AreEqual(3, sut.MaxEntry.Key);
    }
}
