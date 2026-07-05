// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultisetTests.TypeCoverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultisetTests
{

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Contains"/> returns <see langword="true"/> for a value-type
    /// element that is equal by value but is a distinct instance.
    /// </summary>
    [TestMethod]
    public void Contains_WithValueTypeStruct_ShouldMatchByValue()
    {
        var mvd = new Multiset<Point>();
        mvd.Add(new Point(5, 10));

        Assert.IsTrue(mvd.Contains(new Point(5, 10)));
        Assert.IsFalse(mvd.Contains(new Point(5, 99)));
    }

    // --------------------------------------------------------
    // Custom equality comparer
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a <see cref="Multiset{T}"/> constructed with a custom equality comparer uses that
    /// comparer to determine element equality, merging elements that the comparer considers equivalent.
    /// </summary>
    [TestMethod]
    public void Multiset_WithCustomEqualityComparer_ShouldUseComparerForElementEquality()
    {
        var mvd = new Multiset<Point>(new XOnlyComparer());
        mvd.Add(new Point(1, 10));
        mvd.Add(new Point(1, 99));
        mvd.Add(new Point(2, 50));

        Assert.AreEqual(3, mvd.Count);
        Assert.AreEqual(2, mvd.DistinctCount);
        Assert.AreEqual(2, mvd.CountOf(new Point(1, 0)));
        Assert.AreEqual(1, mvd.CountOf(new Point(2, 0)));
    }

    // --------------------------------------------------------
    // Reference-type class elements (reference identity)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that two distinct <see cref="Widget"/> instances with the same field values are treated as
    /// separate elements because <see cref="Widget"/> uses reference identity.
    /// </summary>
    [TestMethod]
    public void Multiset_WithReferenceTypeElements_ShouldTrackByReferenceByDefault()
    {
        var w1 = new Widget(1);
        var w2 = new Widget(1);

        var mvd = new Multiset<Widget>();
        mvd.Add(w1);
        mvd.Add(w2);

        Assert.AreEqual(2, mvd.Count);
        Assert.AreEqual(2, mvd.DistinctCount);
        Assert.AreEqual(1, mvd.CountOf(w1));
        Assert.AreEqual(1, mvd.CountOf(w2));
    }

    // --------------------------------------------------------
    // Value-type struct elements
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that two distinct <see cref="Point"/> instances with the same field values are treated as
    /// the same element, confirming value-type equality semantics.
    /// </summary>
    [TestMethod]
    public void Multiset_WithValueTypeStruct_ShouldTrackByValue()
    {
        var mvd = new Multiset<Point>();
        mvd.Add(new Point(1, 2));
        mvd.Add(new Point(1, 2));
        mvd.Add(new Point(3, 4));

        Assert.AreEqual(3, mvd.Count);
        Assert.AreEqual(2, mvd.DistinctCount);
        Assert.AreEqual(2, mvd.CountOf(new Point(1, 2)));
        Assert.AreEqual(1, mvd.CountOf(new Point(3, 4)));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Remove"/> honours the custom equality comparer when locating
    /// the element to remove.
    /// </summary>
    [TestMethod]
    public void Remove_WithCustomEqualityComparer_ShouldLocateElementViaComparer()
    {
        var mvd = new Multiset<Point>(new XOnlyComparer());
        mvd.Add(new Point(7, 100));
        mvd.Add(new Point(7, 200));

        bool removed = mvd.Remove(new Point(7, 999));

        Assert.IsTrue(removed);
        Assert.AreEqual(1, mvd.CountOf(new Point(7, 0)));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Remove"/> with a reference-type element removes only the
    /// specific object instance, leaving a distinct instance with the same field values untouched.
    /// </summary>
    [TestMethod]
    public void Remove_WithReferenceTypeElement_ShouldRemoveSpecificInstance()
    {
        var w1 = new Widget(99);
        var w2 = new Widget(99);

        var mvd = new Multiset<Widget>();
        mvd.Add(w1);
        mvd.Add(w2);

        mvd.Remove(w1);

        Assert.AreEqual(1, mvd.Count);
        Assert.IsFalse(mvd.Contains(w1));
        Assert.IsTrue(mvd.Contains(w2));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Remove"/> removes a value-type element by value equality,
    /// not by reference identity.
    /// </summary>
    [TestMethod]
    public void Remove_WithValueTypeStruct_ShouldRemoveByValue()
    {
        var mvd = new Multiset<Point>();
        mvd.Add(new Point(1, 2), 3);

        bool removed = mvd.Remove(new Point(1, 2));

        Assert.IsTrue(removed);
        Assert.AreEqual(2, mvd.CountOf(new Point(1, 2)));
    }

    /// <summary>
    /// Verifies that set operations on a <see cref="Multiset{T}"/> of value-type elements produce the
    /// correct element counts based on value equality.
    /// </summary>
    [TestMethod]
    public void SetOperations_WithValueTypeStruct_ShouldApplyValueEquality()
    {
        var a = new Multiset<Point>();
        a.Add(new Point(1, 2), 3);
        a.Add(new Point(3, 4), 1);

        var b = new Multiset<Point>();
        b.Add(new Point(1, 2), 1);
        b.Add(new Point(5, 6), 2);

        Multiset<Point> union = a.Union(b);
        Multiset<Point> intersect = a.Intersect(b);
        Multiset<Point> except = a.Except(b);
        Multiset<Point> sum = a.Sum(b);

        Assert.AreEqual(3, union.CountOf(new Point(1, 2)));
        Assert.AreEqual(1, intersect.CountOf(new Point(1, 2)));
        Assert.AreEqual(2, except.CountOf(new Point(1, 2)));
        Assert.AreEqual(4, sum.CountOf(new Point(1, 2)));
    }
    // --------------------------------------------------------
    // Private helper types
    // --------------------------------------------------------

    /// <summary>Value-type element with structural (value-based) equality via record struct.</summary>
    private readonly record struct Point(int X, int Y);

    /// <summary>Reference-type element with no overridden equality — uses reference identity.</summary>
    private sealed class Widget
    {

        public Widget(int id) { Id = id; }

        public int Id { get; }

    }

    /// <summary>Equality comparer that considers two <see cref="Point"/> values equal when their X coordinates match.</summary>
    private sealed class XOnlyComparer
        : IEqualityComparer<Point>
    {

        public bool Equals(Point x, Point y) => x.X == y.X;

        public int GetHashCode(Point obj) => obj.X.GetHashCode();

    }

}
