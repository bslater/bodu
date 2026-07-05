// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.Views.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Ascending" /> yields every element in ascending comparer order.
    /// </summary>
    [TestMethod]
    public void Ascending_WhenSetHasElements_ShouldYieldElementsAscending()
    {
        var sut = CreateSet(30, 10, 20);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, sut.Ascending().ToList());
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Descending" /> yields every element in descending comparer order.
    /// </summary>
    [TestMethod]
    public void Descending_WhenSetHasElements_ShouldYieldElementsDescending()
    {
        var sut = CreateSet(30, 10, 20);

        CollectionAssert.AreEqual(new[] { 30, 20, 10 }, sut.Descending().ToList());
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Ascending" /> and <see cref="NavigableSet{T}.Descending" /> yield
    /// nothing on an empty set.
    /// </summary>
    [TestMethod]
    public void Views_WhenSetIsEmpty_ShouldYieldNothing()
    {
        var sut = new NavigableSet<int>();

        Assert.AreEqual(0, sut.Ascending().Count());
        Assert.AreEqual(0, sut.Descending().Count());
        Assert.AreEqual(0, sut.Range(0, 100).Count());
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Range" /> yields only the elements within the inclusive bounds, in
    /// ascending order.
    /// </summary>
    [TestMethod]
    public void Range_WhenBoundsEncloseElements_ShouldYieldInRangeElementsAscending()
    {
        var sut = CreateSet(50, 10, 40, 20, 30);

        CollectionAssert.AreEqual(new[] { 20, 30, 40 }, sut.Range(20, 40).ToList());
        CollectionAssert.AreEqual(new[] { 20, 30, 40 }, sut.Range(15, 45).ToList());
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Range" /> yields nothing when no stored element falls between the
    /// bounds.
    /// </summary>
    [TestMethod]
    public void Range_WhenNoElementBetweenBounds_ShouldYieldNothing()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.AreEqual(0, sut.Range(21, 29).Count());
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Range" /> handles bounds entirely below or above the stored
    /// elements.
    /// </summary>
    [TestMethod]
    public void Range_WhenBoundsOutsideElements_ShouldYieldNothing()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.AreEqual(0, sut.Range(0, 5).Count());
        Assert.AreEqual(0, sut.Range(35, 99).Count());
    }

    /// <summary>
    /// Verifies that a degenerate single-value range yields the exact hit only.
    /// </summary>
    [TestMethod]
    public void Range_WhenBoundsAreEqual_ShouldYieldExactHitOnly()
    {
        var sut = CreateSet(10, 20, 30);

        CollectionAssert.AreEqual(new[] { 20 }, sut.Range(20, 20).ToList());
        Assert.AreEqual(0, sut.Range(25, 25).Count());
    }

    /// <summary>
    /// Verifies that the views are live: a view obtained before a mutation reflects that mutation when iterated
    /// afresh.
    /// </summary>
    [TestMethod]
    public void Views_WhenIteratedAfterMutation_ShouldReflectCurrentState()
    {
        var sut = CreateSet(10, 30);
        IEnumerable<int> ascending = sut.Ascending();
        IEnumerable<int> descending = sut.Descending();
        IEnumerable<int> range = sut.Range(0, 100);

        sut.Add(20);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, ascending.ToList());
        CollectionAssert.AreEqual(new[] { 30, 20, 10 }, descending.ToList());
        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, range.ToList());
    }

    /// <summary>
    /// Verifies that mutating the set mid-iteration causes each view to throw
    /// <see cref="InvalidOperationException" /> on the next advance.
    /// </summary>
    [TestMethod]
    public void Views_WhenMutatedMidIteration_ShouldThrowInvalidOperationException()
    {
        foreach (string view in new[] { "ascending", "descending", "range" })
        {
            var sut = CreateSet(1, 2, 3, 4, 5);
            IEnumerable<int> sequence = view switch
            {
                "ascending" => sut.Ascending(),
                "descending" => sut.Descending(),
                _ => sut.Range(0, 100),
            };

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                foreach (int item in sequence)
                {
                    if (item == 3 || item == 2)
                        sut.Add(99);
                }
            }, $"The {view} view should fail fast.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Range" /> throws <see cref="ArgumentException" /> eagerly — before
    /// iteration — when the lower bound orders after the upper bound.
    /// </summary>
    [TestMethod]
    public void Range_WhenLowGreaterThanHigh_ShouldThrowArgumentExceptionEagerly()
    {
        var sut = CreateSet(1, 2, 3);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = sut.Range(5, 1);
        });

        Assert.AreEqual("lowInclusive", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Range" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> bound.
    /// </summary>
    [TestMethod]
    public void Range_WhenBoundIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableSet<string>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = sut.Range(null!, "z");
        }, "lowInclusive");

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = sut.Range("a", null!);
        }, "highInclusive");
    }
}
