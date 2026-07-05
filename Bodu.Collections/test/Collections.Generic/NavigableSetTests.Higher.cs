// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.Higher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetHigher" /> skips an exact hit and returns the strictly greater
    /// element.
    /// </summary>
    [TestMethod]
    public void TryGetHigher_WhenValueIsPresent_ShouldReturnStrictlyGreaterElement()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsTrue(sut.TryGetHigher(20, out int higher));
        Assert.AreEqual(30, higher);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetHigher" /> returns the next-larger element for a value between
    /// two stored elements.
    /// </summary>
    [TestMethod]
    public void TryGetHigher_WhenValueBetweenElements_ShouldReturnNextLarger()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsTrue(sut.TryGetHigher(25, out int higher));
        Assert.AreEqual(30, higher);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetHigher" /> returns the minimum for a value below every stored
    /// element.
    /// </summary>
    [TestMethod]
    public void TryGetHigher_WhenValueBelowMinimum_ShouldReturnMinimum()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsTrue(sut.TryGetHigher(5, out int higher));
        Assert.AreEqual(10, higher);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetHigher" /> returns <see langword="false" /> when the value is
    /// the maximum or above.
    /// </summary>
    [TestMethod]
    public void TryGetHigher_WhenValueAtOrAboveMaximum_ShouldReturnFalse()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsFalse(sut.TryGetHigher(30, out _));
        Assert.IsFalse(sut.TryGetHigher(35, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetHigher" /> returns <see langword="false" /> on an empty set.
    /// </summary>
    [TestMethod]
    public void TryGetHigher_WhenSetIsEmpty_ShouldReturnFalse()
    {
        var sut = new NavigableSet<int>();

        Assert.IsFalse(sut.TryGetHigher(10, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetHigher" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> value.
    /// </summary>
    [TestMethod]
    public void TryGetHigher_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableSet<string>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.TryGetHigher(null!, out _);
        }, "value");
    }
}
