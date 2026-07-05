// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.IndexOfKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.IndexOfKey" /> returns the zero-based rank of
    /// every present key in ascending order.
    /// </summary>
    [TestMethod]
    public void IndexOfKey_WhenKeyIsPresent_ShouldReturnZeroBasedRank()
    {
        var sut = CreateDictionary(50, 10, 40, 20, 30);

        Assert.AreEqual(0, sut.IndexOfKey(10));
        Assert.AreEqual(1, sut.IndexOfKey(20));
        Assert.AreEqual(2, sut.IndexOfKey(30));
        Assert.AreEqual(3, sut.IndexOfKey(40));
        Assert.AreEqual(4, sut.IndexOfKey(50));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.IndexOfKey" /> returns <c>-1</c> for an absent
    /// key.
    /// </summary>
    [TestMethod]
    public void IndexOfKey_WhenKeyIsAbsent_ShouldReturnMinusOne()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.AreEqual(-1, sut.IndexOfKey(25));
        Assert.AreEqual(-1, sut.IndexOfKey(5));
        Assert.AreEqual(-1, sut.IndexOfKey(35));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.IndexOfKey" /> returns <c>-1</c> on an empty
    /// dictionary.
    /// </summary>
    [TestMethod]
    public void IndexOfKey_WhenDictionaryIsEmpty_ShouldReturnMinusOne()
    {
        var sut = new NavigableDictionary<int, int>();

        Assert.AreEqual(-1, sut.IndexOfKey(1));
    }

    /// <summary>
    /// Verifies that ranks shift down after removing a lower-ranked key.
    /// </summary>
    [TestMethod]
    public void IndexOfKey_WhenLowerRankedKeyRemoved_ShouldShiftRanksDown()
    {
        var sut = CreateDictionary(10, 20, 30);

        sut.Remove(10);

        Assert.AreEqual(0, sut.IndexOfKey(20));
        Assert.AreEqual(1, sut.IndexOfKey(30));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.IndexOfKey" /> throws
    /// <see cref="ArgumentNullException" /> for a <see langword="null" /> key.
    /// </summary>
    [TestMethod]
    public void IndexOfKey_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableDictionary<string, int>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.IndexOfKey(null!);
        }, "key");
    }
}
