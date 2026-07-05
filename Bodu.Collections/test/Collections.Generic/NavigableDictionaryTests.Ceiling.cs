// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.Ceiling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetCeilingEntry" /> returns the entry itself on
    /// an exact key hit.
    /// </summary>
    [TestMethod]
    public void TryGetCeilingEntry_WhenKeyIsPresent_ShouldReturnEntryItself()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetCeilingEntry(20, out KeyValuePair<int, int> entry));
        Assert.AreEqual(20, entry.Key);
        Assert.AreEqual(2000, entry.Value);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetCeilingEntry" /> returns the next-larger-key
    /// entry for a key between two stored keys.
    /// </summary>
    [TestMethod]
    public void TryGetCeilingEntry_WhenKeyBetweenStoredKeys_ShouldReturnNextLargerEntry()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetCeilingEntry(25, out KeyValuePair<int, int> entry));
        Assert.AreEqual(30, entry.Key);
        Assert.AreEqual(3000, entry.Value);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetCeilingEntry" /> returns the minimum entry
    /// for a key below every stored key.
    /// </summary>
    [TestMethod]
    public void TryGetCeilingEntry_WhenKeyBelowMinimum_ShouldReturnMinimumEntry()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetCeilingEntry(5, out KeyValuePair<int, int> entry));
        Assert.AreEqual(10, entry.Key);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetCeilingEntry" /> returns
    /// <see langword="false" /> for a key above the maximum.
    /// </summary>
    [TestMethod]
    public void TryGetCeilingEntry_WhenKeyAboveMaximum_ShouldReturnFalse()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsFalse(sut.TryGetCeilingEntry(35, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetCeilingEntry" /> returns
    /// <see langword="false" /> on an empty dictionary.
    /// </summary>
    [TestMethod]
    public void TryGetCeilingEntry_WhenDictionaryIsEmpty_ShouldReturnFalse()
    {
        var sut = new NavigableDictionary<int, int>();

        Assert.IsFalse(sut.TryGetCeilingEntry(10, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetCeilingKey" /> mirrors the entry variant,
    /// returning only the ceiling key.
    /// </summary>
    [TestMethod]
    public void TryGetCeilingKey_WhenKeyBetweenStoredKeys_ShouldReturnNextLargerKey()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetCeilingKey(25, out int ceilingKey));
        Assert.AreEqual(30, ceilingKey);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetCeilingKey" /> returns
    /// <see langword="false" /> when no key orders at or above the reference key.
    /// </summary>
    [TestMethod]
    public void TryGetCeilingKey_WhenKeyAboveMaximum_ShouldReturnFalse()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsFalse(sut.TryGetCeilingKey(35, out _));
    }

    /// <summary>
    /// Verifies that the ceiling queries throw <see cref="ArgumentNullException" /> for a <see langword="null" />
    /// reference key.
    /// </summary>
    [TestMethod]
    public void TryGetCeilingEntry_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableDictionary<string, int>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.TryGetCeilingEntry(null!, out _);
        }, "key");

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.TryGetCeilingKey(null!, out _);
        }, "key");
    }
}
