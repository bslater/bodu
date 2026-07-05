// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.Floor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetFloorEntry" /> returns the entry itself on
    /// an exact key hit.
    /// </summary>
    [TestMethod]
    public void TryGetFloorEntry_WhenKeyIsPresent_ShouldReturnEntryItself()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetFloorEntry(20, out KeyValuePair<int, int> entry));
        Assert.AreEqual(20, entry.Key);
        Assert.AreEqual(2000, entry.Value);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetFloorEntry" /> returns the next-smaller-key
    /// entry for a key between two stored keys.
    /// </summary>
    [TestMethod]
    public void TryGetFloorEntry_WhenKeyBetweenStoredKeys_ShouldReturnNextSmallerEntry()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetFloorEntry(25, out KeyValuePair<int, int> entry));
        Assert.AreEqual(20, entry.Key);
        Assert.AreEqual(2000, entry.Value);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetFloorEntry" /> returns
    /// <see langword="false" /> for a key below the minimum.
    /// </summary>
    [TestMethod]
    public void TryGetFloorEntry_WhenKeyBelowMinimum_ShouldReturnFalse()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsFalse(sut.TryGetFloorEntry(5, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetFloorEntry" /> returns the maximum entry for
    /// a key above every stored key.
    /// </summary>
    [TestMethod]
    public void TryGetFloorEntry_WhenKeyAboveMaximum_ShouldReturnMaximumEntry()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetFloorEntry(35, out KeyValuePair<int, int> entry));
        Assert.AreEqual(30, entry.Key);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetFloorEntry" /> returns
    /// <see langword="false" /> on an empty dictionary.
    /// </summary>
    [TestMethod]
    public void TryGetFloorEntry_WhenDictionaryIsEmpty_ShouldReturnFalse()
    {
        var sut = new NavigableDictionary<int, int>();

        Assert.IsFalse(sut.TryGetFloorEntry(10, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetFloorKey" /> mirrors the entry variant,
    /// returning only the floor key.
    /// </summary>
    [TestMethod]
    public void TryGetFloorKey_WhenKeyBetweenStoredKeys_ShouldReturnNextSmallerKey()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetFloorKey(25, out int floorKey));
        Assert.AreEqual(20, floorKey);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetFloorKey" /> returns
    /// <see langword="false" /> when no key orders at or below the reference key.
    /// </summary>
    [TestMethod]
    public void TryGetFloorKey_WhenKeyBelowMinimum_ShouldReturnFalse()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsFalse(sut.TryGetFloorKey(5, out _));
    }

    /// <summary>
    /// Verifies that the floor queries throw <see cref="ArgumentNullException" /> for a <see langword="null" />
    /// reference key.
    /// </summary>
    [TestMethod]
    public void TryGetFloorEntry_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableDictionary<string, int>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.TryGetFloorEntry(null!, out _);
        }, "key");

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.TryGetFloorKey(null!, out _);
        }, "key");
    }
}
