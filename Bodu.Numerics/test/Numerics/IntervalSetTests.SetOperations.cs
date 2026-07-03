// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalSetTests.SetOperations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class IntervalSetTests
{
    /// <summary>
    /// Verifies that the union with a single interval coalesces overlapping and adjacent pieces, and that the empty set
    /// is the union identity.
    /// </summary>
    [TestMethod]
    public void Union_WithInterval_ShouldCoalesceAndActAsIdentity()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(1, 3), Interval<int>.Closed(8, 9));

        Assert.AreEqual(
            IntervalSet<int>.Of(Interval<int>.Closed(1, 5), Interval<int>.Closed(8, 9)),
            set.Union(Interval<int>.Closed(2, 5)));

        Assert.AreEqual(
            IntervalSet<int>.Of(Interval<int>.Closed(1, 5)),
            IntervalSet<int>.Empty.Union(Interval<int>.Closed(1, 5)));
    }

    /// <summary>
    /// Verifies that the union of two sets normalizes the combined pieces.
    /// </summary>
    [TestMethod]
    public void Union_WithSet_ShouldNormalizeCombinedPieces()
    {
        IntervalSet<int> a = IntervalSet<int>.Of(Interval<int>.Closed(1, 5));
        IntervalSet<int> b = IntervalSet<int>.Of(Interval<int>.Closed(4, 8));

        Assert.AreEqual(IntervalSet<int>.Of(Interval<int>.Closed(1, 8)), a.Union(b));
    }

    /// <summary>
    /// Verifies that the intersection keeps only the shared members, over both an interval and a set.
    /// </summary>
    [TestMethod]
    public void Intersect_WithIntervalAndSet_ShouldReturnSharedMembers()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(1, 5), Interval<int>.Closed(8, 12));

        Assert.AreEqual(
            IntervalSet<int>.Of(Interval<int>.Closed(4, 5), Interval<int>.Closed(8, 10)),
            set.Intersect(Interval<int>.Closed(4, 10)));

        Assert.AreEqual(
            IntervalSet<int>.Of(Interval<int>.Closed(4, 5)),
            set.Intersect(IntervalSet<int>.Of(Interval<int>.Closed(4, 5))));

        Assert.IsTrue(set.Intersect(IntervalSet<int>.Empty).IsEmpty);
    }

    /// <summary>
    /// Verifies that removing an interior interval splits the affected piece.
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
    /// Verifies that subtracting a whole set removes every one of its members.
    /// </summary>
    [TestMethod]
    public void Except_WithSet_ShouldRemoveEveryMember()
    {
        IntervalSet<int> set = IntervalSet<int>.Of(Interval<int>.Closed(1, 20));
        IntervalSet<int> remove = IntervalSet<int>.Of(Interval<int>.Closed(3, 5), Interval<int>.Closed(10, 12));

        Assert.AreEqual(
            IntervalSet<int>.Of(
                Interval<int>.ClosedOpen(1, 3),
                Interval<int>.Open(5, 10),
                Interval<int>.OpenClosed(12, 20)),
            set.Except(remove));
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
    /// Verifies that the complement of the empty set is the whole line.
    /// </summary>
    [TestMethod]
    public void Complement_WhenEmptySet_ShouldReturnWholeLine()
    {
        Assert.AreEqual(IntervalSet<int>.Of(Interval<int>.All), IntervalSet<int>.Empty.Complement());
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
}
