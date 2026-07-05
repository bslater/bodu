// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.CountInRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.CountInRange" /> counts entries whose keys fall
    /// within the inclusive bounds.
    /// </summary>
    [TestMethod]
    public void CountInRange_WhenBoundsEncloseKeys_ShouldCountInclusiveHits()
    {
        var sut = CreateDictionary(10, 20, 30, 40, 50);

        Assert.AreEqual(3, sut.CountInRange(20, 40));
        Assert.AreEqual(3, sut.CountInRange(15, 45));
        Assert.AreEqual(5, sut.CountInRange(10, 50));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.CountInRange" /> counts a degenerate single-key
    /// range as one on an exact hit and zero on a miss.
    /// </summary>
    [TestMethod]
    public void CountInRange_WhenBoundsAreEqual_ShouldCountExactHitOnly()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.AreEqual(1, sut.CountInRange(20, 20));
        Assert.AreEqual(0, sut.CountInRange(25, 25));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.CountInRange" /> returns zero when no stored key
    /// falls between the bounds.
    /// </summary>
    [TestMethod]
    public void CountInRange_WhenNoKeyBetweenBounds_ShouldReturnZero()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.AreEqual(0, sut.CountInRange(21, 29));
        Assert.AreEqual(0, sut.CountInRange(0, 5));
        Assert.AreEqual(0, sut.CountInRange(35, 99));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.CountInRange" /> returns zero on an empty
    /// dictionary.
    /// </summary>
    [TestMethod]
    public void CountInRange_WhenDictionaryIsEmpty_ShouldReturnZero()
    {
        var sut = new NavigableDictionary<int, int>();

        Assert.AreEqual(0, sut.CountInRange(0, 100));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.CountInRange" /> throws
    /// <see cref="ArgumentException" /> when the lower bound orders after the upper bound.
    /// </summary>
    [TestMethod]
    public void CountInRange_WhenLowerBoundExceedsUpperBound_ShouldThrowArgumentException()
    {
        var sut = CreateDictionary(10, 20, 30);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.CountInRange(30, 10);
        });

        Assert.AreEqual("lowInclusive", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableDictionary{TKey, TValue}.CountInRange" /> throws
    /// <see cref="ArgumentNullException" /> for a <see langword="null" /> bound.
    /// </summary>
    [TestMethod]
    public void CountInRange_WhenBoundIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableDictionary<string, int>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.CountInRange(null!, "z");
        }, "lowInclusive");

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.CountInRange("a", null!);
        }, "highInclusive");
    }
}
