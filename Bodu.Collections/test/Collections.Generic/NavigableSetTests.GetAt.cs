// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.GetAt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.GetAt" /> returns the k-th smallest element for every valid rank.
    /// </summary>
    [TestMethod]
    public void GetAt_WhenRankIsValid_ShouldReturnKthSmallestElement()
    {
        var sut = CreateSet(50, 10, 40, 20, 30);

        Assert.AreEqual(10, sut.GetAt(0));
        Assert.AreEqual(20, sut.GetAt(1));
        Assert.AreEqual(30, sut.GetAt(2));
        Assert.AreEqual(40, sut.GetAt(3));
        Assert.AreEqual(50, sut.GetAt(4));
    }

    /// <summary>
    /// Verifies the rank round-trip contract: <c>GetAt(IndexOf(x)) == x</c> for every stored element.
    /// </summary>
    [TestMethod]
    public void GetAt_WhenComposedWithIndexOf_ShouldRoundTripEveryElement()
    {
        var sut = new NavigableSet<int>(new[] { 13, 7, 42, 1, 99, 56, 21, 34, 88, 3 });

        foreach (int item in sut)
            Assert.AreEqual(item, sut.GetAt(sut.IndexOf(item)));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.GetAt" /> throws <see cref="ArgumentOutOfRangeException" /> for a
    /// negative rank.
    /// </summary>
    [TestMethod]
    public void GetAt_WhenRankIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var sut = CreateSet(1, 2, 3);

        ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            sut.GetAt(-1);
        }, "rank");
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.GetAt" /> throws <see cref="ArgumentOutOfRangeException" /> for a
    /// rank equal to or beyond <see cref="NavigableSet{T}.Count" />.
    /// </summary>
    [TestMethod]
    public void GetAt_WhenRankIsCountOrBeyond_ShouldThrowArgumentOutOfRangeException()
    {
        var sut = CreateSet(1, 2, 3);

        ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            sut.GetAt(3);
        }, "rank");
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.GetAt" /> throws <see cref="ArgumentOutOfRangeException" /> for any
    /// rank on an empty set.
    /// </summary>
    [TestMethod]
    public void GetAt_WhenSetIsEmpty_ShouldThrowArgumentOutOfRangeException()
    {
        var sut = new NavigableSet<int>();

        ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            sut.GetAt(0);
        }, "rank");
    }
}
