// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.Lower.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetLowerEntry" /> skips an exact key hit and
    /// returns the strictly smaller entry.
    /// </summary>
    [TestMethod]
    public void TryGetLowerEntry_WhenKeyIsPresent_ShouldReturnStrictlySmallerEntry()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetLowerEntry(20, out KeyValuePair<int, int> entry));
        Assert.AreEqual(10, entry.Key);
        Assert.AreEqual(1000, entry.Value);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetLowerEntry" /> returns the next-smaller-key
    /// entry for a key between two stored keys.
    /// </summary>
    [TestMethod]
    public void TryGetLowerEntry_WhenKeyBetweenStoredKeys_ShouldReturnNextSmallerEntry()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetLowerEntry(25, out KeyValuePair<int, int> entry));
        Assert.AreEqual(20, entry.Key);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetLowerEntry" /> returns the maximum entry for
    /// a key above every stored key.
    /// </summary>
    [TestMethod]
    public void TryGetLowerEntry_WhenKeyAboveMaximum_ShouldReturnMaximumEntry()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetLowerEntry(35, out KeyValuePair<int, int> entry));
        Assert.AreEqual(30, entry.Key);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetLowerEntry" /> returns
    /// <see langword="false" /> for the minimum key and below.
    /// </summary>
    [TestMethod]
    public void TryGetLowerEntry_WhenKeyIsMinimumOrBelow_ShouldReturnFalse()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsFalse(sut.TryGetLowerEntry(10, out _));
        Assert.IsFalse(sut.TryGetLowerEntry(5, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetLowerEntry" /> returns
    /// <see langword="false" /> on an empty dictionary.
    /// </summary>
    [TestMethod]
    public void TryGetLowerEntry_WhenDictionaryIsEmpty_ShouldReturnFalse()
    {
        var sut = new NavigableDictionary<int, int>();

        Assert.IsFalse(sut.TryGetLowerEntry(10, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetLowerKey" /> mirrors the entry variant,
    /// returning only the next-lower key.
    /// </summary>
    [TestMethod]
    public void TryGetLowerKey_WhenKeyIsPresent_ShouldReturnStrictlySmallerKey()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetLowerKey(20, out int lowerKey));
        Assert.AreEqual(10, lowerKey);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetLowerKey" /> returns
    /// <see langword="false" /> when no key orders strictly below the reference key.
    /// </summary>
    [TestMethod]
    public void TryGetLowerKey_WhenKeyIsMinimum_ShouldReturnFalse()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsFalse(sut.TryGetLowerKey(10, out _));
    }

    /// <summary>
    /// Verifies that the lower queries throw <see cref="ArgumentNullException" /> for a <see langword="null" />
    /// reference key.
    /// </summary>
    [TestMethod]
    public void TryGetLowerEntry_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableDictionary<string, int>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.TryGetLowerEntry(null!, out _);
        }, "key");

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.TryGetLowerKey(null!, out _);
        }, "key");
    }
}
