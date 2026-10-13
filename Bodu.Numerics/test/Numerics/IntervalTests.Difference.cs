// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Difference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

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

    /// <summary>
    /// Verifies that Difference over <see cref="double" /> leaves exactly the values of the first catalogue interval
    /// that the second lacks, in ordered pieces that each hold a value, so an unbounded side removes no bounded
    /// infinite endpoint.
    /// </summary>
    [TestMethod]
    public void Difference_WhenOperandsReachInfinity_ForDouble_ShouldAgreeWithPointMembership() =>
        AssertDifferenceAgreesWithMembership<double>();

    /// <summary>
    /// Verifies that Difference over <see cref="float" /> leaves exactly the values of the first catalogue interval
    /// that the second lacks, in ordered pieces that each hold a value, so an unbounded side removes no bounded
    /// infinite endpoint.
    /// </summary>
    [TestMethod]
    public void Difference_WhenOperandsReachInfinity_ForSingle_ShouldAgreeWithPointMembership() =>
        AssertDifferenceAgreesWithMembership<float>();

    /// <summary>
    /// Verifies that Difference over <see cref="Half" /> leaves exactly the values of the first catalogue interval that
    /// the second lacks, in ordered pieces that each hold a value, so an unbounded side removes no bounded infinite
    /// endpoint.
    /// </summary>
    [TestMethod]
    public void Difference_WhenOperandsReachInfinity_ForHalf_ShouldAgreeWithPointMembership() =>
        AssertDifferenceAgreesWithMembership<Half>();

    /// <summary>
    /// Asserts that <see cref="Interval{T}.Difference(Interval{T})" /> leaves exactly the values of each catalogue
    /// interval that the other lacks, as disjoint ordered pieces that each hold a value.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    private static void AssertDifferenceAgreesWithMembership<T>()
        where T : IFloatingPointIeee754<T>
    {
        Interval<T>[] catalog = IntervalMembershipOracle.Catalog<T>();
        var failures = new List<string>();

        foreach (Interval<T> a in catalog)
        {
            int aMask = IntervalMembershipOracle.Mask(a);
            foreach (Interval<T> b in catalog)
            {
                int expected = aMask & ~IntervalMembershipOracle.Mask(b);
                Interval<T>[] pieces = IntervalMembershipOracle.Pieces(a.Difference(b));
                string? defect = IntervalMembershipOracle.FindPieceDefect(pieces, requireGap: false);
                if (IntervalMembershipOracle.Mask(pieces) != expected || defect is not null)
                    failures.Add($"{a}.Difference({b}) returned {string.Join(" | ", pieces)} {defect}");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("; ", failures.Take(8)));
    }
}
