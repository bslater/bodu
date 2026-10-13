// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Union.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that the union of two overlapping intervals takes the smaller lower endpoint and the larger upper.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenOverlapping_ShouldReturnSpanningInterval()
    {
        bool ok = Interval<int>.Closed(1, 5).TryUnion(Interval<int>.Closed(3, 7), out Interval<int> result);

        Assert.IsTrue(ok);
        Assert.AreEqual(Interval<int>.Closed(1, 7), result);
    }

    /// <summary>
    /// Verifies that adjacent intervals - where one's upper endpoint equals the other's lower endpoint and at least
    /// one is inclusive - union to a contiguous interval.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenAdjacent_ShouldReturnContiguousInterval()
    {
        // [1, 5) ∪ [5, 10] -> [1, 10]
        bool ok = Interval<int>.ClosedOpen(1, 5).TryUnion(Interval<int>.Closed(5, 10), out Interval<int> result);

        Assert.IsTrue(ok);
        Assert.AreEqual(Interval<int>.Closed(1, 10), result);
    }

    /// <summary>
    /// Verifies that two intervals with a gap at the touching point - both open at the shared endpoint - cannot be
    /// unioned to a single contiguous interval.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenBothOpenAtSharedEndpoint_ShouldReturnFalse()
    {
        // [1, 5) ∪ (5, 10] - neither contains 5, so the result is not contiguous.
        bool ok = Interval<int>.ClosedOpen(1, 5).TryUnion(Interval<int>.OpenClosed(5, 10), out Interval<int> result);

        Assert.IsFalse(ok);
        Assert.IsTrue(result.IsEmpty);
    }

    /// <summary>
    /// Verifies that disjoint intervals separated by a gap cannot be unioned to a single contiguous interval.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenDisjoint_ShouldReturnFalse()
    {
        bool ok = Interval<int>.Closed(1, 3).TryUnion(Interval<int>.Closed(5, 7), out Interval<int> result);

        Assert.IsFalse(ok);
        Assert.IsTrue(result.IsEmpty);
    }

    /// <summary>
    /// Verifies that the union of any interval with the empty interval returns the original interval.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenEitherOperandIsEmpty_ShouldReturnTheOther()
    {
        var nonEmpty = Interval<int>.Closed(1, 5);

        Assert.IsTrue(Interval<int>.Empty.TryUnion(nonEmpty, out Interval<int> a));
        Assert.AreEqual(nonEmpty, a);

        Assert.IsTrue(nonEmpty.TryUnion(Interval<int>.Empty, out Interval<int> b));
        Assert.AreEqual(nonEmpty, b);
    }

    /// <summary>
    /// Verifies that when the two operands' endpoints tie, union picks the looser (inclusive) inclusivity.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenEndpointsTie_ShouldPickLooserInclusivity()
    {
        // [1, 5] ∪ (1, 5) -> [1, 5]
        Assert.IsTrue(Interval<int>.Closed(1, 5).TryUnion(Interval<int>.Open(1, 5), out Interval<int> result));
        Assert.AreEqual(Interval<int>.Closed(1, 5), result);
    }

    /// <summary>
    /// Verifies that the union of an interval fully contained within another adopts the wider operand's endpoints on
    /// both sides.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenThisIsContainedInOther_ShouldAdoptOtherBounds()
    {
        Assert.IsTrue(Interval<int>.Closed(3, 7).TryUnion(Interval<int>.Closed(1, 9), out Interval<int> result));
        Assert.AreEqual(Interval<int>.Closed(1, 9), result);
    }

    /// <summary>
    /// Verifies that the union of an interval that extends past the other operand on the upper side keeps this
    /// interval's upper endpoint and the other operand's lower endpoint.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenThisExtendsPastOtherUpper_ShouldKeepThisUpperAndOtherLower()
    {
        Assert.IsTrue(Interval<int>.Closed(3, 9).TryUnion(Interval<int>.Closed(1, 5), out Interval<int> result));
        Assert.AreEqual(Interval<int>.Closed(1, 9), result);
    }

    /// <summary>
    /// Verifies that TryUnion over <see cref="double" /> succeeds exactly when the values of two catalogue intervals
    /// form one run, and then returns those values, so an unbounded side meets a bounded infinite endpoint rather than
    /// reaching past it.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenOperandsReachInfinity_ForDouble_ShouldAgreeWithPointMembership() =>
        AssertTryUnionAgreesWithMembership<double>();

    /// <summary>
    /// Verifies that TryUnion over <see cref="float" /> succeeds exactly when the values of two catalogue intervals
    /// form one run, and then returns those values, so an unbounded side meets a bounded infinite endpoint rather than
    /// reaching past it.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenOperandsReachInfinity_ForSingle_ShouldAgreeWithPointMembership() =>
        AssertTryUnionAgreesWithMembership<float>();

    /// <summary>
    /// Verifies that TryUnion over <see cref="Half" /> succeeds exactly when the values of two catalogue intervals form
    /// one run, and then returns those values, so an unbounded side meets a bounded infinite endpoint rather than
    /// reaching past it.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenOperandsReachInfinity_ForHalf_ShouldAgreeWithPointMembership() =>
        AssertTryUnionAgreesWithMembership<Half>();

    /// <summary>
    /// Asserts that <see cref="Interval{T}.TryUnion(Interval{T}, out Interval{T})" /> succeeds for every pair of
    /// catalogue intervals whose values form one run, and only those, with a result that holds exactly those values.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    private static void AssertTryUnionAgreesWithMembership<T>()
        where T : IFloatingPointIeee754<T>
    {
        Interval<T>[] catalog = IntervalMembershipOracle.Catalog<T>();
        var failures = new List<string>();

        foreach (Interval<T> a in catalog)
        {
            int aMask = IntervalMembershipOracle.Mask(a);
            foreach (Interval<T> b in catalog)
            {
                int expected = aMask | IntervalMembershipOracle.Mask(b);
                bool united = a.TryUnion(b, out Interval<T> result);
                bool expectedUnited = IntervalMembershipOracle.IsContiguous(expected);
                int actual = IntervalMembershipOracle.Mask(result);
                if (united != expectedUnited || (united && actual != expected) || (!united && !result.IsEmpty))
                    failures.Add($"{a}.TryUnion({b}) returned {united} with {result}");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("; ", failures.Take(8)));
    }
}
