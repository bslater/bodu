// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Contains.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that a closed-closed interval contains both endpoints and the interior, but not values outside.
    /// </summary>
    [TestMethod]
    [DataRow(1, true)]
    [DataRow(3, true)]
    [DataRow(5, true)]
    [DataRow(0, false)]
    [DataRow(6, false)]
    public void Contains_WhenClosedClosed_ShouldIncludeBothEndpoints(int value, bool expected)
    {
        var interval = Interval<int>.Closed(1, 5);

        Assert.AreEqual(expected, interval.Contains(value));
    }

    /// <summary>
    /// Verifies that an open-open interval excludes both endpoints.
    /// </summary>
    [TestMethod]
    [DataRow(1, false)]
    [DataRow(3, true)]
    [DataRow(5, false)]
    [DataRow(0, false)]
    public void Contains_WhenOpenOpen_ShouldExcludeBothEndpoints(int value, bool expected)
    {
        var interval = Interval<int>.Open(1, 5);

        Assert.AreEqual(expected, interval.Contains(value));
    }

    /// <summary>
    /// Verifies that a closed-open interval includes the lower endpoint and excludes the upper.
    /// </summary>
    [TestMethod]
    [DataRow(1, true)]
    [DataRow(3, true)]
    [DataRow(5, false)]
    public void Contains_WhenClosedOpen_ShouldIncludeLowerExcludeUpper(int value, bool expected)
    {
        var interval = Interval<int>.ClosedOpen(1, 5);

        Assert.AreEqual(expected, interval.Contains(value));
    }

    /// <summary>
    /// Verifies that an open-closed interval excludes the lower endpoint and includes the upper.
    /// </summary>
    [TestMethod]
    [DataRow(1, false)]
    [DataRow(3, true)]
    [DataRow(5, true)]
    public void Contains_WhenOpenClosed_ShouldExcludeLowerIncludeUpper(int value, bool expected)
    {
        var interval = Interval<int>.OpenClosed(1, 5);

        Assert.AreEqual(expected, interval.Contains(value));
    }

    /// <summary>
    /// Verifies that an empty interval contains no value.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(int.MaxValue)]
    public void Contains_WhenEmpty_ShouldReturnFalseForAnyValue(int value) => Assert.IsFalse(Interval<int>.Empty.Contains(value));

    /// <summary>
    /// Verifies that a degenerate single-point interval contains only its anchor.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDegenerate_ShouldContainOnlyTheAnchor()
    {
        var interval = Interval<int>.Singleton(7);

        Assert.IsTrue(interval.Contains(7));
        Assert.IsFalse(interval.Contains(6));
        Assert.IsFalse(interval.Contains(8));
    }

    /// <summary>
    /// Verifies that an interval contains another interval iff every value of the inner interval is also in the
    /// outer. The empty interval is contained by every interval.
    /// </summary>
    [TestMethod]
    public void Contains_WhenGivenInterval_ShouldReturnTrueForSubsets()
    {
        var outer = Interval<int>.Closed(0, 10);

        Assert.IsTrue(outer.Contains(Interval<int>.Closed(2, 8)));
        Assert.IsTrue(outer.Contains(Interval<int>.Open(2, 8)));
        Assert.IsTrue(outer.Contains(Interval<int>.Closed(0, 10)));
        Assert.IsTrue(outer.Contains(Interval<int>.Singleton(5)));
        Assert.IsTrue(outer.Contains(Interval<int>.Empty));

        Assert.IsFalse(outer.Contains(Interval<int>.Closed(-1, 5)));
        Assert.IsFalse(outer.Contains(Interval<int>.Closed(5, 11)));
        Assert.IsFalse(Interval<int>.Open(0, 10).Contains(Interval<int>.Closed(0, 10)));
    }

    /// <summary>
    /// Verifies that an empty interval contains no non-empty interval, while still vacuously containing the empty
    /// interval.
    /// </summary>
    [TestMethod]
    public void Contains_WhenReceiverIsEmpty_ShouldReturnFalseForNonEmptyInterval()
    {
        Assert.IsFalse(Interval<int>.Empty.Contains(Interval<int>.Closed(1, 5)));
        Assert.IsTrue(Interval<int>.Empty.Contains(Interval<int>.Empty));
    }

    /// <summary>
    /// Verifies that an interval over <see cref="double" /> with an unbounded side contains neither NaN nor either
    /// infinity, since an unbounded side admits every finite value and no infinity.
    /// </summary>
    /// <param name="shape">The name of the unbounded factory under test.</param>
    [TestMethod]
    [DataRow("All")]
    [DataRow("AtLeast")]
    [DataRow("GreaterThan")]
    [DataRow("AtMost")]
    [DataRow("LessThan")]
    public void Contains_WhenSideIsUnbounded_ForDouble_ShouldExcludeNaNAndInfinities(string shape) =>
        AssertExcludesNonFiniteValues<double>(shape);

    /// <summary>
    /// Verifies that an interval over <see cref="float" /> with an unbounded side contains neither NaN nor either
    /// infinity, since an unbounded side admits every finite value and no infinity.
    /// </summary>
    /// <param name="shape">The name of the unbounded factory under test.</param>
    [TestMethod]
    [DataRow("All")]
    [DataRow("AtLeast")]
    [DataRow("GreaterThan")]
    [DataRow("AtMost")]
    [DataRow("LessThan")]
    public void Contains_WhenSideIsUnbounded_ForSingle_ShouldExcludeNaNAndInfinities(string shape) =>
        AssertExcludesNonFiniteValues<float>(shape);

    /// <summary>
    /// Verifies that an interval over <see cref="Half" /> with an unbounded side contains neither NaN nor either
    /// infinity, since an unbounded side admits every finite value and no infinity.
    /// </summary>
    /// <param name="shape">The name of the unbounded factory under test.</param>
    [TestMethod]
    [DataRow("All")]
    [DataRow("AtLeast")]
    [DataRow("GreaterThan")]
    [DataRow("AtMost")]
    [DataRow("LessThan")]
    public void Contains_WhenSideIsUnbounded_ForHalf_ShouldExcludeNaNAndInfinities(string shape) =>
        AssertExcludesNonFiniteValues<Half>(shape);

    /// <summary>
    /// Verifies that an interval with an unbounded side still contains the largest finite value on that side, for
    /// <see cref="double" />, <see cref="float" /> and <see cref="Half" />.
    /// </summary>
    /// <param name="shape">The name of the unbounded factory under test.</param>
    [TestMethod]
    [DataRow("All")]
    [DataRow("AtLeast")]
    [DataRow("GreaterThan")]
    [DataRow("AtMost")]
    [DataRow("LessThan")]
    public void Contains_WhenSideIsUnbounded_ShouldContainFiniteExtremeOnThatSide(string shape)
    {
        AssertContainsFiniteExtremes<double>(shape);
        AssertContainsFiniteExtremes<float>(shape);
        AssertContainsFiniteExtremes<Half>(shape);
    }

    /// <summary>
    /// Verifies that a bounded interval whose closed side carries an infinite endpoint contains that infinity, while
    /// an open side at the same infinity excludes it and no interval contains NaN.
    /// </summary>
    [TestMethod]
    public void Contains_WhenBoundedEndpointIsInfinite_ShouldFollowInclusivity()
    {
        Assert.IsTrue(Interval<double>.Closed(0.0, double.PositiveInfinity).Contains(double.PositiveInfinity));
        Assert.IsTrue(Interval<double>.Closed(double.NegativeInfinity, 0.0).Contains(double.NegativeInfinity));
        Assert.IsTrue(Interval<float>.Closed(float.NegativeInfinity, float.PositiveInfinity).Contains(float.PositiveInfinity));
        Assert.IsTrue(Interval<Half>.Closed(Half.NegativeInfinity, (Half)0).Contains(Half.NegativeInfinity));

        Assert.IsFalse(Interval<double>.ClosedOpen(0.0, double.PositiveInfinity).Contains(double.PositiveInfinity));
        Assert.IsFalse(Interval<double>.OpenClosed(double.NegativeInfinity, 0.0).Contains(double.NegativeInfinity));
        Assert.IsFalse(Interval<double>.Closed(double.NegativeInfinity, double.PositiveInfinity).Contains(double.NaN));
    }

    /// <summary>
    /// Asserts that the unbounded interval named by <paramref name="shape" /> contains neither NaN nor either infinity
    /// of <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    /// <param name="shape">The name of the unbounded factory.</param>
    private static void AssertExcludesNonFiniteValues<T>(string shape)
        where T : IFloatingPointIeee754<T>
    {
        Interval<T> interval = UnboundedFloatingPointShape<T>(shape);

        Assert.IsFalse(interval.Contains(T.NaN), $"{interval} contains NaN");
        Assert.IsFalse(interval.Contains(T.PositiveInfinity), $"{interval} contains +Infinity");
        Assert.IsFalse(interval.Contains(T.NegativeInfinity), $"{interval} contains -Infinity");
    }

    /// <summary>
    /// Asserts that the unbounded interval named by <paramref name="shape" /> contains the largest finite value of
    /// <typeparamref name="T" /> on each unbounded side.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    /// <param name="shape">The name of the unbounded factory.</param>
    private static void AssertContainsFiniteExtremes<T>(string shape)
        where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        Interval<T> interval = UnboundedFloatingPointShape<T>(shape);

        if (interval.LowerUnbounded)
            Assert.IsTrue(interval.Contains(T.MinValue), $"{interval} excludes {typeof(T).Name}.MinValue");

        if (interval.UpperUnbounded)
            Assert.IsTrue(interval.Contains(T.MaxValue), $"{interval} excludes {typeof(T).Name}.MaxValue");
    }

    /// <summary>
    /// Returns the unbounded interval over <typeparamref name="T" /> named by <paramref name="shape" />, anchored at
    /// zero where it has a bounded side.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    /// <param name="shape">The name of the unbounded factory.</param>
    /// <returns>The corresponding unbounded interval.</returns>
    private static Interval<T> UnboundedFloatingPointShape<T>(string shape)
        where T : IFloatingPointIeee754<T> =>
        shape switch
        {
            "All" => Interval<T>.All,
            "AtLeast" => Interval<T>.AtLeast(T.Zero),
            "GreaterThan" => Interval<T>.GreaterThan(T.Zero),
            "AtMost" => Interval<T>.AtMost(T.Zero),
            "LessThan" => Interval<T>.LessThan(T.Zero),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };

    /// <summary>
    /// Verifies that Contains(Interval) over <see cref="double" /> agrees with point membership for every pair of
    /// catalog intervals, so an unbounded side does not reach a bounded infinite endpoint.
    /// </summary>
    [TestMethod]
    public void Contains_WhenOperandsReachInfinity_ForDouble_ShouldAgreeWithPointMembership() =>
        AssertContainsIntervalAgreesWithMembership<double>();

    /// <summary>
    /// Verifies that Contains(Interval) over <see cref="float" /> agrees with point membership for every pair of
    /// catalog intervals, so an unbounded side does not reach a bounded infinite endpoint.
    /// </summary>
    [TestMethod]
    public void Contains_WhenOperandsReachInfinity_ForSingle_ShouldAgreeWithPointMembership() =>
        AssertContainsIntervalAgreesWithMembership<float>();

    /// <summary>
    /// Verifies that Contains(Interval) over <see cref="Half" /> agrees with point membership for every pair of
    /// catalog intervals, so an unbounded side does not reach a bounded infinite endpoint.
    /// </summary>
    [TestMethod]
    public void Contains_WhenOperandsReachInfinity_ForHalf_ShouldAgreeWithPointMembership() =>
        AssertContainsIntervalAgreesWithMembership<Half>();

    /// <summary>
    /// Asserts that <see cref="Interval{T}.Contains(Interval{T})" /> reports every catalog interval that is a subset
    /// of another, and only those.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    private static void AssertContainsIntervalAgreesWithMembership<T>()
        where T : IFloatingPointIeee754<T>
    {
        Interval<T>[] catalog = IntervalMembershipOracle.Catalog<T>();
        var failures = new List<string>();

        foreach (Interval<T> a in catalog)
        {
            int aMask = IntervalMembershipOracle.Mask(a);
            foreach (Interval<T> b in catalog)
            {
                bool expected = (IntervalMembershipOracle.Mask(b) & ~aMask) == 0;
                if (a.Contains(b) != expected)
                    failures.Add($"{a}.Contains({b}) returned {!expected}");
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("; ", failures.Take(8)));
    }
}
