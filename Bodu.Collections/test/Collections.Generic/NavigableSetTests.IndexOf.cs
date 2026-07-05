// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.IndexOf.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.IndexOf" /> returns the zero-based rank of every stored element in
    /// sorted order.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenValueIsPresent_ShouldReturnZeroBasedRank()
    {
        var sut = CreateSet(50, 10, 40, 20, 30);

        Assert.AreEqual(0, sut.IndexOf(10));
        Assert.AreEqual(1, sut.IndexOf(20));
        Assert.AreEqual(2, sut.IndexOf(30));
        Assert.AreEqual(3, sut.IndexOf(40));
        Assert.AreEqual(4, sut.IndexOf(50));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.IndexOf" /> returns <c>-1</c> for absent values below, between, and
    /// above the stored elements.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenValueIsAbsent_ShouldReturnMinusOne()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.AreEqual(-1, sut.IndexOf(5));
        Assert.AreEqual(-1, sut.IndexOf(15));
        Assert.AreEqual(-1, sut.IndexOf(35));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.IndexOf" /> returns <c>-1</c> on an empty set.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenSetIsEmpty_ShouldReturnMinusOne()
    {
        var sut = new NavigableSet<int>();

        Assert.AreEqual(-1, sut.IndexOf(1));
    }

    /// <summary>
    /// Verifies that ranks shift down after a removal below the queried element.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenSmallerElementRemoved_ShouldShiftRankDown()
    {
        var sut = CreateSet(10, 20, 30);

        sut.Remove(10);

        Assert.AreEqual(0, sut.IndexOf(20));
        Assert.AreEqual(1, sut.IndexOf(30));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.IndexOf" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> value.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableSet<string>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.IndexOf(null!);
        }, "value");
    }
}
