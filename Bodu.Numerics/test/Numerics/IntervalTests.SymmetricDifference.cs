// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.SymmetricDifference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that the symmetric difference of two partially overlapping intervals is the two non-shared flanks.
    /// </summary>
    [TestMethod]
    public void SymmetricDifference_WhenPartiallyOverlapping_ShouldReturnBothFlanks()
    {
        IntervalPair<int> result = Interval<int>.Closed(0, 5).SymmetricDifference(Interval<int>.Closed(3, 8));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(Interval<int>.ClosedOpen(0, 3), result[0]);
        Assert.AreEqual(Interval<int>.OpenClosed(5, 8), result[1]);
    }

    /// <summary>
    /// Verifies that the symmetric difference of equal intervals is empty.
    /// </summary>
    [TestMethod]
    public void SymmetricDifference_WhenEqual_ShouldReturnEmpty()
    {
        Assert.AreEqual(0, Interval<int>.Closed(0, 5).SymmetricDifference(Interval<int>.Closed(0, 5)).Count);
    }

    /// <summary>
    /// Verifies that the symmetric difference with an interior interval matches the plain difference (the interior
    /// contributes nothing of its own).
    /// </summary>
    [TestMethod]
    public void SymmetricDifference_WhenOtherIsInterior_ShouldMatchDifference()
    {
        IntervalPair<int> result = Interval<int>.Closed(0, 10).SymmetricDifference(Interval<int>.Closed(3, 5));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(Interval<int>.ClosedOpen(0, 3), result[0]);
        Assert.AreEqual(Interval<int>.OpenClosed(5, 10), result[1]);
    }

    /// <summary>
    /// Verifies that the symmetric difference of two disjoint intervals is both intervals, ordered lowest-first.
    /// </summary>
    [TestMethod]
    public void SymmetricDifference_WhenDisjoint_ShouldReturnBothOrdered()
    {
        IntervalPair<int> result = Interval<int>.Closed(5, 7).SymmetricDifference(Interval<int>.Closed(0, 2));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(Interval<int>.Closed(0, 2), result[0]);
        Assert.AreEqual(Interval<int>.Closed(5, 7), result[1]);
    }

    /// <summary>
    /// Verifies that SymmetricDifference over <see cref="double" /> returns exactly the values one catalogue interval
    /// holds and the other does not, in ordered pieces that each hold a value.
    /// </summary>
    [TestMethod]
    public void SymmetricDifference_WhenOperandsReachInfinity_ForDouble_ShouldAgreeWithPointMembership() =>
        AssertSymmetricDifferenceAgreesWithMembership<double>();

    /// <summary>
    /// Verifies that SymmetricDifference over <see cref="float" /> returns exactly the values one catalogue interval
    /// holds and the other does not, in ordered pieces that each hold a value.
    /// </summary>
    [TestMethod]
    public void SymmetricDifference_WhenOperandsReachInfinity_ForSingle_ShouldAgreeWithPointMembership() =>
        AssertSymmetricDifferenceAgreesWithMembership<float>();

    /// <summary>
    /// Verifies that SymmetricDifference over <see cref="Half" /> returns exactly the values one catalogue interval
    /// holds and the other does not, in ordered pieces that each hold a value.
    /// </summary>
    [TestMethod]
    public void SymmetricDifference_WhenOperandsReachInfinity_ForHalf_ShouldAgreeWithPointMembership() =>
        AssertSymmetricDifferenceAgreesWithMembership<Half>();

    /// <summary>
    /// Asserts that <see cref="Interval{T}.SymmetricDifference(Interval{T})" /> returns exactly the values held by one
    /// catalogue operand and not the other, as disjoint ordered pieces that each hold a value.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    private static void AssertSymmetricDifferenceAgreesWithMembership<T>()
        where T : IFloatingPointIeee754<T>
    {
        Interval<T>[] catalog = IntervalMembershipOracle.Catalog<T>();
        var failures = new List<string>();

        foreach (Interval<T> a in catalog)
        {
            int aMask = IntervalMembershipOracle.Mask(a);
            foreach (Interval<T> b in catalog)
            {
                int expected = aMask ^ IntervalMembershipOracle.Mask(b);
                Interval<T>[] pieces = IntervalMembershipOracle.Pieces(a.SymmetricDifference(b));
                string? defect = IntervalMembershipOracle.FindPieceDefect(pieces, requireGap: false);
                if (IntervalMembershipOracle.Mask(pieces) != expected || defect is not null)
                    failures.Add($"{a}.SymmetricDifference({b}) returned {string.Join(" | ", pieces)} {defect}");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("; ", failures.Take(8)));
    }
}
