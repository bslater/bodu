// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Intersect.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that <see cref="Interval{T}.Overlaps" /> returns true for any pair of intervals that share a value.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenIntervalsShareAValue_ShouldReturnTrue()
    {
        Assert.IsTrue(Interval<int>.Closed(1, 5).Overlaps(Interval<int>.Closed(3, 7)));
        Assert.IsTrue(Interval<int>.Closed(1, 5).Overlaps(Interval<int>.Closed(5, 7)));
        Assert.IsTrue(Interval<int>.Closed(1, 10).Overlaps(Interval<int>.Closed(3, 7)));
    }

    /// <summary>
    /// Verifies that two intervals that touch at a value not included in either (or only one) do not overlap.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenIntervalsOnlyTouch_ShouldReflectInclusivity()
    {
        // [1, 5) and [5, 10] share no value because 5 is excluded from the first.
        Assert.IsFalse(Interval<int>.ClosedOpen(1, 5).Overlaps(Interval<int>.Closed(5, 10)));

        // (1, 5] and [5, 10] share the value 5.
        Assert.IsTrue(Interval<int>.OpenClosed(1, 5).Overlaps(Interval<int>.Closed(5, 10)));

        // (1, 5) and (5, 10) share nothing.
        Assert.IsFalse(Interval<int>.Open(1, 5).Overlaps(Interval<int>.Open(5, 10)));
    }

    /// <summary>
    /// Verifies that <see cref="Interval{T}.Overlaps" /> returns false when one operand is empty.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenEitherOperandIsEmpty_ShouldReturnFalse()
    {
        Assert.IsFalse(Interval<int>.Empty.Overlaps(Interval<int>.Closed(1, 5)));
        Assert.IsFalse(Interval<int>.Closed(1, 5).Overlaps(Interval<int>.Empty));
        Assert.IsFalse(Interval<int>.Empty.Overlaps(Interval<int>.Empty));
    }

    /// <summary>
    /// Verifies that the intersection of overlapping intervals takes the larger lower endpoint and the smaller upper.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenOverlapping_ShouldReturnTheNarrowestSubinterval()
    {
        Assert.AreEqual(Interval<int>.Closed(3, 5), Interval<int>.Closed(1, 5).Intersect(Interval<int>.Closed(3, 7)));
        Assert.AreEqual(Interval<int>.Closed(3, 5), Interval<int>.Closed(1, 10).Intersect(Interval<int>.Closed(3, 5)));
    }

    /// <summary>
    /// Verifies that inclusivity flags propagate correctly through intersection - the stricter (open) flag wins when
    /// endpoints tie.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenEndpointsTie_ShouldPickStricterInclusivity()
    {
        // [1, 5] ∩ (1, 5) -> (1, 5)
        Assert.AreEqual(Interval<int>.Open(1, 5), Interval<int>.Closed(1, 5).Intersect(Interval<int>.Open(1, 5)));

        // [1, 5] ∩ [1, 5) -> [1, 5)
        Assert.AreEqual(Interval<int>.ClosedOpen(1, 5), Interval<int>.Closed(1, 5).Intersect(Interval<int>.ClosedOpen(1, 5)));
    }

    /// <summary>
    /// Verifies that the intersection of disjoint intervals is empty.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenDisjoint_ShouldReturnEmpty()
    {
        Assert.IsTrue(Interval<int>.Closed(1, 3).Intersect(Interval<int>.Closed(5, 7)).IsEmpty);
        Assert.IsTrue(Interval<int>.ClosedOpen(1, 5).Intersect(Interval<int>.Closed(5, 7)).IsEmpty);
    }

    /// <summary>
    /// Verifies that intersection with the empty interval is always empty.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenEitherOperandIsEmpty_ShouldReturnEmpty()
    {
        Assert.IsTrue(Interval<int>.Empty.Intersect(Interval<int>.Closed(1, 5)).IsEmpty);
        Assert.IsTrue(Interval<int>.Closed(1, 5).Intersect(Interval<int>.Empty).IsEmpty);
    }

    /// <summary>
    /// Verifies that intersecting an interval whose lower bound starts after the other operand keeps this interval's
    /// lower endpoint and the other operand's upper endpoint.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenThisStartsAfterOther_ShouldClampToThisLowerAndOtherUpper()
    {
        Interval<int> result = Interval<int>.Closed(3, 8).Intersect(Interval<int>.Closed(1, 5));

        Assert.AreEqual(Interval<int>.Closed(3, 5), result);
    }

    /// <summary>
    /// Verifies that Overlaps over <see cref="double" /> agrees with point membership for every pair of catalogue
    /// intervals, so an unbounded side shares no value with a bounded infinite endpoint.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenOperandsReachInfinity_ForDouble_ShouldAgreeWithPointMembership() =>
        AssertOverlapsAgreesWithMembership<double>();

    /// <summary>
    /// Verifies that Overlaps over <see cref="float" /> agrees with point membership for every pair of catalogue
    /// intervals, so an unbounded side shares no value with a bounded infinite endpoint.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenOperandsReachInfinity_ForSingle_ShouldAgreeWithPointMembership() =>
        AssertOverlapsAgreesWithMembership<float>();

    /// <summary>
    /// Verifies that Overlaps over <see cref="Half" /> agrees with point membership for every pair of catalogue
    /// intervals, so an unbounded side shares no value with a bounded infinite endpoint.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenOperandsReachInfinity_ForHalf_ShouldAgreeWithPointMembership() =>
        AssertOverlapsAgreesWithMembership<Half>();

    /// <summary>
    /// Verifies that Intersect over <see cref="double" /> returns exactly the values both operands hold, for every pair
    /// of catalogue intervals, so an unbounded side does not reach a bounded infinite endpoint.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenOperandsReachInfinity_ForDouble_ShouldAgreeWithPointMembership() =>
        AssertIntersectAgreesWithMembership<double>();

    /// <summary>
    /// Verifies that Intersect over <see cref="float" /> returns exactly the values both operands hold, for every pair
    /// of catalogue intervals, so an unbounded side does not reach a bounded infinite endpoint.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenOperandsReachInfinity_ForSingle_ShouldAgreeWithPointMembership() =>
        AssertIntersectAgreesWithMembership<float>();

    /// <summary>
    /// Verifies that Intersect over <see cref="Half" /> returns exactly the values both operands hold, for every pair
    /// of catalogue intervals, so an unbounded side does not reach a bounded infinite endpoint.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenOperandsReachInfinity_ForHalf_ShouldAgreeWithPointMembership() =>
        AssertIntersectAgreesWithMembership<Half>();

    /// <summary>
    /// Asserts that <see cref="Interval{T}.Overlaps(Interval{T})" /> reports every pair of catalogue intervals that
    /// share a value, and only those.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    private static void AssertOverlapsAgreesWithMembership<T>()
        where T : IFloatingPointIeee754<T>
    {
        Interval<T>[] catalog = IntervalMembershipOracle.Catalog<T>();
        var failures = new List<string>();

        foreach (Interval<T> a in catalog)
        {
            int aMask = IntervalMembershipOracle.Mask(a);
            foreach (Interval<T> b in catalog)
            {
                bool expected = (aMask & IntervalMembershipOracle.Mask(b)) != 0;
                if (a.Overlaps(b) != expected)
                    failures.Add($"{a}.Overlaps({b}) returned {!expected}");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("; ", failures.Take(8)));
    }

    /// <summary>
    /// Asserts that <see cref="Interval{T}.Intersect(Interval{T})" /> holds exactly the values both catalogue operands
    /// hold, and is <see cref="Interval{T}.Empty" /> when they share none.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    private static void AssertIntersectAgreesWithMembership<T>()
        where T : IFloatingPointIeee754<T>
    {
        Interval<T>[] catalog = IntervalMembershipOracle.Catalog<T>();
        var failures = new List<string>();

        foreach (Interval<T> a in catalog)
        {
            int aMask = IntervalMembershipOracle.Mask(a);
            foreach (Interval<T> b in catalog)
            {
                int expected = aMask & IntervalMembershipOracle.Mask(b);
                Interval<T> result = a.Intersect(b);
                if (IntervalMembershipOracle.Mask(result) != expected || (expected == 0 && !result.IsEmpty))
                    failures.Add($"{a}.Intersect({b}) returned {result}");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("; ", failures.Take(8)));
    }
}
