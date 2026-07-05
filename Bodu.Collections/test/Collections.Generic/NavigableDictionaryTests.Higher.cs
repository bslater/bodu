// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.Higher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetHigherEntry" /> skips an exact key hit and
    /// returns the strictly greater entry.
    /// </summary>
    [TestMethod]
    public void TryGetHigherEntry_WhenKeyIsPresent_ShouldReturnStrictlyGreaterEntry()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetHigherEntry(20, out KeyValuePair<int, int> entry));
        Assert.AreEqual(30, entry.Key);
        Assert.AreEqual(3000, entry.Value);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetHigherEntry" /> returns the next-larger-key
    /// entry for a key between two stored keys.
    /// </summary>
    [TestMethod]
    public void TryGetHigherEntry_WhenKeyBetweenStoredKeys_ShouldReturnNextLargerEntry()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetHigherEntry(15, out KeyValuePair<int, int> entry));
        Assert.AreEqual(20, entry.Key);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetHigherEntry" /> returns the minimum entry
    /// for a key below every stored key.
    /// </summary>
    [TestMethod]
    public void TryGetHigherEntry_WhenKeyBelowMinimum_ShouldReturnMinimumEntry()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetHigherEntry(5, out KeyValuePair<int, int> entry));
        Assert.AreEqual(10, entry.Key);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetHigherEntry" /> returns
    /// <see langword="false" /> for the maximum key and beyond.
    /// </summary>
    [TestMethod]
    public void TryGetHigherEntry_WhenKeyIsMaximumOrBeyond_ShouldReturnFalse()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsFalse(sut.TryGetHigherEntry(30, out _));
        Assert.IsFalse(sut.TryGetHigherEntry(35, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetHigherEntry" /> returns
    /// <see langword="false" /> on an empty dictionary.
    /// </summary>
    [TestMethod]
    public void TryGetHigherEntry_WhenDictionaryIsEmpty_ShouldReturnFalse()
    {
        var sut = new NavigableDictionary<int, int>();

        Assert.IsFalse(sut.TryGetHigherEntry(10, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetHigherKey" /> mirrors the entry variant,
    /// returning only the next-higher key.
    /// </summary>
    [TestMethod]
    public void TryGetHigherKey_WhenKeyIsPresent_ShouldReturnStrictlyGreaterKey()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetHigherKey(20, out int higherKey));
        Assert.AreEqual(30, higherKey);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetHigherKey" /> returns
    /// <see langword="false" /> when no key orders strictly above the reference key.
    /// </summary>
    [TestMethod]
    public void TryGetHigherKey_WhenKeyIsMaximum_ShouldReturnFalse()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsFalse(sut.TryGetHigherKey(30, out _));
    }

    /// <summary>
    /// Verifies that the higher queries throw <see cref="ArgumentNullException" /> for a <see langword="null" />
    /// reference key.
    /// </summary>
    [TestMethod]
    public void TryGetHigherEntry_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableDictionary<string, int>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.TryGetHigherEntry(null!, out _);
        }, "key");

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.TryGetHigherKey(null!, out _);
        }, "key");
    }
}
