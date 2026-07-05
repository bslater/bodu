// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.MinMax.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Min" /> returns the smallest element under the comparer.
    /// </summary>
    [TestMethod]
    public void Min_WhenSetHasElements_ShouldReturnSmallestElement()
    {
        var sut = CreateSet(30, 10, 20);

        Assert.AreEqual(10, sut.Min);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Max" /> returns the largest element under the comparer.
    /// </summary>
    [TestMethod]
    public void Max_WhenSetHasElements_ShouldReturnLargestElement()
    {
        var sut = CreateSet(30, 10, 20);

        Assert.AreEqual(30, sut.Max);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Min" /> throws <see cref="InvalidOperationException" /> on an empty
    /// set.
    /// </summary>
    [TestMethod]
    public void Min_WhenSetIsEmpty_ShouldThrowInvalidOperationException()
    {
        var sut = new NavigableSet<int>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = sut.Min;
        });
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Max" /> throws <see cref="InvalidOperationException" /> on an empty
    /// set.
    /// </summary>
    [TestMethod]
    public void Max_WhenSetIsEmpty_ShouldThrowInvalidOperationException()
    {
        var sut = new NavigableSet<int>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = sut.Max;
        });
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetMin" /> returns <see langword="true" /> with the smallest
    /// element for a non-empty set.
    /// </summary>
    [TestMethod]
    public void TryGetMin_WhenSetHasElements_ShouldReturnTrueAndSmallest()
    {
        var sut = CreateSet(5, 3, 9);

        Assert.IsTrue(sut.TryGetMin(out int min));
        Assert.AreEqual(3, min);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetMax" /> returns <see langword="true" /> with the largest
    /// element for a non-empty set.
    /// </summary>
    [TestMethod]
    public void TryGetMax_WhenSetHasElements_ShouldReturnTrueAndLargest()
    {
        var sut = CreateSet(5, 3, 9);

        Assert.IsTrue(sut.TryGetMax(out int max));
        Assert.AreEqual(9, max);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetMin" /> and <see cref="NavigableSet{T}.TryGetMax" /> return
    /// <see langword="false" /> on an empty set.
    /// </summary>
    [TestMethod]
    public void TryGetMinMax_WhenSetIsEmpty_ShouldReturnFalse()
    {
        var sut = new NavigableSet<int>();

        Assert.IsFalse(sut.TryGetMin(out _));
        Assert.IsFalse(sut.TryGetMax(out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Min" /> and <see cref="NavigableSet{T}.Max" /> track removals of the
    /// current extremes.
    /// </summary>
    [TestMethod]
    public void MinMax_WhenExtremesRemoved_ShouldTrackRemainingElements()
    {
        var sut = CreateSet(1, 2, 3, 4, 5);

        sut.Remove(1);
        sut.Remove(5);

        Assert.AreEqual(2, sut.Min);
        Assert.AreEqual(4, sut.Max);
    }
}
