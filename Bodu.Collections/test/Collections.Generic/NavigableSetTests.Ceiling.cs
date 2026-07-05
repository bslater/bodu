// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.Ceiling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetCeiling" /> returns the element itself on an exact hit.
    /// </summary>
    [TestMethod]
    public void TryGetCeiling_WhenValueIsPresent_ShouldReturnValueItself()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsTrue(sut.TryGetCeiling(20, out int ceiling));
        Assert.AreEqual(20, ceiling);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetCeiling" /> returns the next-larger element for a value between
    /// two stored elements.
    /// </summary>
    [TestMethod]
    public void TryGetCeiling_WhenValueBetweenElements_ShouldReturnNextLarger()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsTrue(sut.TryGetCeiling(25, out int ceiling));
        Assert.AreEqual(30, ceiling);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetCeiling" /> returns the minimum for a value below every stored
    /// element.
    /// </summary>
    [TestMethod]
    public void TryGetCeiling_WhenValueBelowMinimum_ShouldReturnMinimum()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsTrue(sut.TryGetCeiling(5, out int ceiling));
        Assert.AreEqual(10, ceiling);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetCeiling" /> returns <see langword="false" /> for a value above
    /// the maximum.
    /// </summary>
    [TestMethod]
    public void TryGetCeiling_WhenValueAboveMaximum_ShouldReturnFalse()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsFalse(sut.TryGetCeiling(35, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetCeiling" /> returns <see langword="false" /> on an empty set.
    /// </summary>
    [TestMethod]
    public void TryGetCeiling_WhenSetIsEmpty_ShouldReturnFalse()
    {
        var sut = new NavigableSet<int>();

        Assert.IsFalse(sut.TryGetCeiling(10, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.TryGetCeiling" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> value.
    /// </summary>
    [TestMethod]
    public void TryGetCeiling_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableSet<string>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.TryGetCeiling(null!, out _);
        }, "value");
    }
}
