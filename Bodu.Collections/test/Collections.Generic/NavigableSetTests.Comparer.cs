// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.Comparer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that a reverse comparer inverts the enumeration order and the <see cref="NavigableSet{T}.Min" /> /
    /// <see cref="NavigableSet{T}.Max" /> extremes.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenReverseComparer_ShouldInvertOrderAndExtremes()
    {
        var sut = new NavigableSet<int>(ReverseComparer<int>.Instance);
        sut.Add(1);
        sut.Add(2);
        sut.Add(3);

        CollectionAssert.AreEqual(new[] { 3, 2, 1 }, sut.ToList());
        Assert.AreEqual(3, sut.Min);
        Assert.AreEqual(1, sut.Max);
    }

    /// <summary>
    /// Verifies that navigation queries follow the reversed ordering: the floor of a value is the nearest element on
    /// the reversed axis.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenReverseComparer_ShouldNavigateOnReversedAxis()
    {
        var sut = new NavigableSet<int>(new[] { 10, 20, 30 }, ReverseComparer<int>.Instance);

        // Under the reversed axis, "floor of 25" is the nearest element that orders <= 25, which is 30.
        Assert.IsTrue(sut.TryGetFloor(25, out int floor));
        Assert.AreEqual(30, floor);

        Assert.IsTrue(sut.TryGetCeiling(25, out int ceiling));
        Assert.AreEqual(20, ceiling);
    }

    /// <summary>
    /// Verifies that ranks follow the comparer order rather than the natural order.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenReverseComparer_ShouldRankOnReversedAxis()
    {
        var sut = new NavigableSet<int>(new[] { 10, 20, 30 }, ReverseComparer<int>.Instance);

        Assert.AreEqual(0, sut.IndexOf(30));
        Assert.AreEqual(2, sut.IndexOf(10));
        Assert.AreEqual(30, sut.GetAt(0));
    }

    /// <summary>
    /// Verifies that a case-insensitive comparer treats differently cased spellings as the same element across add,
    /// containment, and removal.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCaseInsensitive_ShouldTreatSpellingsAsOneElement()
    {
        var sut = new NavigableSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(sut.Add("alpha"));
        Assert.IsFalse(sut.Add("ALPHA"));
        Assert.IsTrue(sut.Contains("Alpha"));
        Assert.IsTrue(sut.Remove("aLpHa"));
        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Comparer" /> exposes the construction-time comparer.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenSuppliedAtConstruction_ShouldBeExposed()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;

        var sut = new NavigableSet<string>(comparer);

        Assert.AreSame(comparer, sut.Comparer);
    }
}
