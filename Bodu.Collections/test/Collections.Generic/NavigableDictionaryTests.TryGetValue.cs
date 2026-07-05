// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetValue" /> returns <see langword="true" />
    /// with the stored value for a present key.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsPresent_ShouldReturnTrueAndStoredValue()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsTrue(sut.TryGetValue(20, out int value));
        Assert.AreEqual(2000, value);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetValue" /> returns <see langword="false" />
    /// with the default value for an absent key.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsAbsent_ShouldReturnFalseAndDefaultValue()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.IsFalse(sut.TryGetValue(99, out int value));
        Assert.AreEqual(0, value);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetValue" /> resolves keys through the active
    /// comparer.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsComparerEqual_ShouldReturnStoredValue()
    {
        var sut = new NavigableDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        sut.Add("Alpha", 1);

        Assert.IsTrue(sut.TryGetValue("ALPHA", out int value));
        Assert.AreEqual(1, value);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetValue" /> returns <see langword="false" />
    /// on an empty dictionary.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenDictionaryIsEmpty_ShouldReturnFalse()
    {
        var sut = new NavigableDictionary<int, int>();

        Assert.IsFalse(sut.TryGetValue(1, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.TryGetValue" /> throws
    /// <see cref="ArgumentNullException" /> for a <see langword="null" /> key.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableDictionary<string, int>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.TryGetValue(null!, out _);
        }, "key");
    }
}
