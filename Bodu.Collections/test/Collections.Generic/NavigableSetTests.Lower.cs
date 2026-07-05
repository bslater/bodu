// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.Lower.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetLower" /> skips an exact hit and returns the strictly smaller
    /// element.
    /// </summary>
    [TestMethod]
    public void TryGetLower_WhenValueIsPresent_ShouldReturnStrictlySmallerElement()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsTrue(sut.TryGetLower(20, out int lower));
        Assert.AreEqual(10, lower);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetLower" /> returns the next-smaller element for a value between
    /// two stored elements.
    /// </summary>
    [TestMethod]
    public void TryGetLower_WhenValueBetweenElements_ShouldReturnNextSmaller()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsTrue(sut.TryGetLower(25, out int lower));
        Assert.AreEqual(20, lower);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetLower" /> returns <see langword="false" /> when the value is
    /// the minimum or below.
    /// </summary>
    [TestMethod]
    public void TryGetLower_WhenValueAtOrBelowMinimum_ShouldReturnFalse()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsFalse(sut.TryGetLower(10, out _));
        Assert.IsFalse(sut.TryGetLower(5, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetLower" /> returns the maximum for a value above every stored
    /// element.
    /// </summary>
    [TestMethod]
    public void TryGetLower_WhenValueAboveMaximum_ShouldReturnMaximum()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsTrue(sut.TryGetLower(35, out int lower));
        Assert.AreEqual(30, lower);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetLower" /> returns <see langword="false" /> on an empty set.
    /// </summary>
    [TestMethod]
    public void TryGetLower_WhenSetIsEmpty_ShouldReturnFalse()
    {
        var sut = new NavigableSet<int>();

        Assert.IsFalse(sut.TryGetLower(10, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetLower" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> value.
    /// </summary>
    [TestMethod]
    public void TryGetLower_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableSet<string>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.TryGetLower(null!, out _);
        }, "value");
    }
}
