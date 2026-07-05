// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.Floor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetFloor" /> returns the element itself on an exact hit.
    /// </summary>
    [TestMethod]
    public void TryGetFloor_WhenValueIsPresent_ShouldReturnValueItself()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsTrue(sut.TryGetFloor(20, out int floor));
        Assert.AreEqual(20, floor);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetFloor" /> returns the next-smaller element for a value between
    /// two stored elements.
    /// </summary>
    [TestMethod]
    public void TryGetFloor_WhenValueBetweenElements_ShouldReturnNextSmaller()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsTrue(sut.TryGetFloor(25, out int floor));
        Assert.AreEqual(20, floor);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetFloor" /> returns <see langword="false" /> for a value below
    /// the minimum.
    /// </summary>
    [TestMethod]
    public void TryGetFloor_WhenValueBelowMinimum_ShouldReturnFalse()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsFalse(sut.TryGetFloor(5, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetFloor" /> returns the maximum for a value above every stored
    /// element.
    /// </summary>
    [TestMethod]
    public void TryGetFloor_WhenValueAboveMaximum_ShouldReturnMaximum()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsTrue(sut.TryGetFloor(35, out int floor));
        Assert.AreEqual(30, floor);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetFloor" /> returns <see langword="false" /> on an empty set.
    /// </summary>
    [TestMethod]
    public void TryGetFloor_WhenSetIsEmpty_ShouldReturnFalse()
    {
        var sut = new NavigableSet<int>();

        Assert.IsFalse(sut.TryGetFloor(10, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetFloor" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> value.
    /// </summary>
    [TestMethod]
    public void TryGetFloor_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableSet<string>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.TryGetFloor(null!, out _);
        }, "value");
    }
}
