// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Difference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that removing an interior interval leaves the two flanking pieces with flipped cut inclusivity.
    /// </summary>
    [TestMethod]
    public void Difference_WhenOtherIsInterior_ShouldReturnTwoFlankingPieces()
    {
        IntervalPair<int> result = Interval<int>.Closed(0, 10).Difference(Interval<int>.Closed(3, 5));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(Interval<int>.ClosedOpen(0, 3), result[0]);
        Assert.AreEqual(Interval<int>.OpenClosed(5, 10), result[1]);
    }

    /// <summary>
    /// Verifies that removing an open interior interval keeps the touched endpoints, producing a degenerate left piece.
    /// </summary>
    [TestMethod]
    public void Difference_WhenOtherIsOpenInterior_ShouldKeepTouchedEndpoints()
    {
        IntervalPair<int> result = Interval<int>.Closed(1, 10).Difference(Interval<int>.Open(1, 5));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(Interval<int>.Singleton(1), result[0]);
        Assert.AreEqual(Interval<int>.Closed(5, 10), result[1]);
    }

    /// <summary>
    /// Verifies that trimming one side yields a single remainder.
    /// </summary>
    [TestMethod]
    public void Difference_WhenOtherTrimsOneSide_ShouldReturnOnePiece()
    {
        IntervalPair<int> result = Interval<int>.Closed(0, 10).Difference(Interval<int>.Closed(8, 20));

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(Interval<int>.ClosedOpen(0, 8), result[0]);
    }

    /// <summary>
    /// Verifies that subtracting a superset (or an equal set) yields no pieces.
    /// </summary>
    [TestMethod]
    public void Difference_WhenOtherCoversThis_ShouldReturnEmpty()
    {
        Assert.AreEqual(0, Interval<int>.Closed(0, 10).Difference(Interval<int>.Closed(0, 10)).Count);
        Assert.AreEqual(0, Interval<int>.Closed(2, 8).Difference(Interval<int>.Closed(0, 10)).Count);
    }

    /// <summary>
    /// Verifies that subtracting a disjoint interval leaves this interval unchanged.
    /// </summary>
    [TestMethod]
    public void Difference_WhenDisjoint_ShouldReturnThisUnchanged()
    {
        IntervalPair<int> result = Interval<int>.Closed(0, 5).Difference(Interval<int>.Closed(10, 20));

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(Interval<int>.Closed(0, 5), result[0]);
    }

    /// <summary>
    /// Verifies that subtracting a finite interval from the whole line yields the two surrounding half-lines.
    /// </summary>
    [TestMethod]
    public void Difference_WhenSubtractingFromAll_ShouldReturnTwoHalfLines()
    {
        IntervalPair<int> result = Interval<int>.All.Difference(Interval<int>.Closed(3, 5));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(Interval<int>.LessThan(3), result[0]);
        Assert.AreEqual(Interval<int>.GreaterThan(5), result[1]);
    }

    /// <summary>
    /// Verifies that the difference of an empty interval is empty.
    /// </summary>
    [TestMethod]
    public void Difference_WhenThisIsEmpty_ShouldReturnEmpty()
    {
        Assert.AreEqual(0, Interval<int>.Empty.Difference(Interval<int>.Closed(0, 10)).Count);
    }
}
