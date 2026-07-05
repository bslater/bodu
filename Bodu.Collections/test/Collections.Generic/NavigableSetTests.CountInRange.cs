// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.CountInRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.CountInRange" /> spanning every element returns the full count.
    /// </summary>
    [TestMethod]
    public void CountInRange_WhenRangeSpansAllElements_ShouldReturnCount()
    {
        var sut = CreateSet(10, 20, 30, 40, 50);

        Assert.AreEqual(5, sut.CountInRange(10, 50));
        Assert.AreEqual(5, sut.CountInRange(0, 100));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.CountInRange" /> counts inclusively on both bounds.
    /// </summary>
    [TestMethod]
    public void CountInRange_WhenBoundsAreStoredElements_ShouldCountInclusively()
    {
        var sut = CreateSet(10, 20, 30, 40, 50);

        Assert.AreEqual(3, sut.CountInRange(20, 40));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.CountInRange" /> handles bounds that fall between stored elements.
    /// </summary>
    [TestMethod]
    public void CountInRange_WhenBoundsBetweenElements_ShouldCountEnclosedElements()
    {
        var sut = CreateSet(10, 20, 30, 40, 50);

        Assert.AreEqual(2, sut.CountInRange(15, 35));
        Assert.AreEqual(0, sut.CountInRange(21, 29));
    }

    /// <summary>
    /// Verifies that a degenerate single-value range counts one when present and zero when absent.
    /// </summary>
    [TestMethod]
    public void CountInRange_WhenBoundsAreEqual_ShouldCountExactHitOnly()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.AreEqual(1, sut.CountInRange(20, 20));
        Assert.AreEqual(0, sut.CountInRange(25, 25));
    }

    /// <summary>
    /// Verifies that ranges entirely outside the stored elements count zero.
    /// </summary>
    [TestMethod]
    public void CountInRange_WhenRangeOutsideElements_ShouldReturnZero()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.AreEqual(0, sut.CountInRange(1, 5));
        Assert.AreEqual(0, sut.CountInRange(35, 99));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.CountInRange" /> returns zero on an empty set.
    /// </summary>
    [TestMethod]
    public void CountInRange_WhenSetIsEmpty_ShouldReturnZero()
    {
        var sut = new NavigableSet<int>();

        Assert.AreEqual(0, sut.CountInRange(0, 100));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.CountInRange" /> throws <see cref="ArgumentException" /> when the
    /// lower bound orders after the upper bound.
    /// </summary>
    [TestMethod]
    public void CountInRange_WhenLowGreaterThanHigh_ShouldThrowArgumentException()
    {
        var sut = CreateSet(1, 2, 3);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.CountInRange(5, 1);
        });

        Assert.AreEqual("lowInclusive", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.CountInRange" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> bound.
    /// </summary>
    [TestMethod]
    public void CountInRange_WhenBoundIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableSet<string>();

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
