// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalSetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

/// <summary>
/// Verifies the <see cref="IntervalSet{T}" /> normalized disconnected-range type and its N-ary set algebra.
/// </summary>
[TestClass]
public sealed class IntervalSetTests
{
    /// <summary>
    /// Verifies that overlapping and adjacent inputs are coalesced into disjoint, non-adjacent pieces on construction.
    /// </summary>
    [TestMethod]
    public void Of_WhenInputsOverlapOrTouch_ShouldCoalesce()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(
            Interval<int>.Closed(1, 3),
            Interval<int>.Closed(2, 5),
            Interval<int>.Closed(8, 9));

        Assert.AreEqual(2, set.Count);
        Assert.AreEqual(Interval<int>.Closed(1, 5), set[0]);
        Assert.AreEqual(Interval<int>.Closed(8, 9), set[1]);
    }

    /// <summary>
    /// Verifies that a point excluded by both neighbours keeps the pieces separate.
    /// </summary>
    [TestMethod]
    public void Of_WhenPointExcludedByBoth_ShouldStaySeparate()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(
            Interval<int>.ClosedOpen(1, 2),
            Interval<int>.OpenClosed(2, 3));

        Assert.AreEqual(2, set.Count);
        Assert.IsFalse(set.Contains(2));
    }

    /// <summary>
    /// Verifies membership and enclosure across the disconnected pieces.
    /// </summary>
    [TestMethod]
    public void ContainsAndEncloses_ShouldReflectMembers()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(1, 5), Interval<int>.Closed(8, 9));

        Assert.IsTrue(set.Contains(3));
        Assert.IsFalse(set.Contains(6));
        Assert.IsTrue(set.Encloses(Interval<int>.Closed(2, 4)));
        Assert.IsFalse(set.Encloses(Interval<int>.Closed(4, 8)));   // spans the gap
    }

    /// <summary>
    /// Verifies the difference of a set and an interval splits the affected piece.
    /// </summary>
    [TestMethod]
    public void Except_WhenRemovingInterior_ShouldSplitThePiece()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(1, 10)).Except(Interval<int>.Closed(3, 5));

        Assert.AreEqual(2, set.Count);
        Assert.AreEqual(Interval<int>.ClosedOpen(1, 3), set[0]);
        Assert.AreEqual(Interval<int>.OpenClosed(5, 10), set[1]);
    }

    /// <summary>
    /// Verifies that the complement of a bounded set is the two surrounding half-lines.
    /// </summary>
    [TestMethod]
    public void Complement_WhenBoundedSet_ShouldReturnHalfLines()
    {
        IntervalSet<int> complement = IntervalSet<int>.Of(Interval<int>.Closed(1, 5)).Complement();

        Assert.AreEqual(2, complement.Count);
        Assert.AreEqual(Interval<int>.LessThan(1), complement[0]);
        Assert.AreEqual(Interval<int>.GreaterThan(5), complement[1]);
    }

    /// <summary>
    /// Verifies that the double complement returns the original set.
    /// </summary>
    [TestMethod]
    public void Complement_WhenAppliedTwice_ShouldReturnOriginal()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(1, 5), Interval<int>.Closed(10, 20));

        Assert.AreEqual(set, set.Complement().Complement());
    }

    /// <summary>
    /// Verifies union and intersection across two sets.
    /// </summary>
    [TestMethod]
    public void UnionAndIntersect_AcrossSets_ShouldNormalize()
    {
        IntervalSet<int> a = IntervalSet<int>.Of(Interval<int>.Closed(1, 5));
        IntervalSet<int> b = IntervalSet<int>.Of(Interval<int>.Closed(4, 8));

        Assert.AreEqual(IntervalSet<int>.Of(Interval<int>.Closed(1, 8)), a.Union(b));
        Assert.AreEqual(IntervalSet<int>.Of(Interval<int>.Closed(4, 5)), a.Intersect(b));
    }

    /// <summary>
    /// Verifies the empty set and its identities.
    /// </summary>
    [TestMethod]
    public void Empty_ShouldBehaveAsIdentity()
    {
        IntervalSet<int> empty = IntervalSet<int>.Empty;

        Assert.IsTrue(empty.IsEmpty);
        Assert.AreEqual(0, empty.Count);
        Assert.AreEqual("∅", empty.ToString());
        Assert.AreEqual(IntervalSet<int>.Of(Interval<int>.Closed(1, 5)), empty.Union(Interval<int>.Closed(1, 5)));
    }
}
