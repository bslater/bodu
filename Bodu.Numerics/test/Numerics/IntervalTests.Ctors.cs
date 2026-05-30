// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Ctors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that the primary constructor preserves the supplied endpoints and inclusivity flags.
    /// </summary>
    [TestMethod]
    [DataRow(1, 5, true, true)]
    [DataRow(1, 5, false, false)]
    [DataRow(1, 5, true, false)]
    [DataRow(1, 5, false, true)]
    [DataRow(-3, 3, true, true)]
    public void Constructor_WhenGivenEndpoints_ShouldPreserveBoundsAndInclusivity(int lower, int upper, bool lowerInclusive, bool upperInclusive)
    {
        Interval<int> interval = new Interval<int>(lower, upper, lowerInclusive, upperInclusive);

        Assert.AreEqual(lower, interval.Lower);
        Assert.AreEqual(upper, interval.Upper);
        Assert.AreEqual(lowerInclusive, interval.LowerInclusive);
        Assert.AreEqual(upperInclusive, interval.UpperInclusive);
    }

    /// <summary>
    /// Verifies that <see cref="Interval{T}.Closed" /> produces a closed-closed interval containing both endpoints.
    /// </summary>
    [TestMethod]
    public void Closed_WhenGivenBounds_ShouldProduceClosedClosedInterval()
    {
        Interval<int> interval = Interval<int>.Closed(1, 5);

        Assert.AreEqual(1, interval.Lower);
        Assert.AreEqual(5, interval.Upper);
        Assert.IsTrue(interval.LowerInclusive);
        Assert.IsTrue(interval.UpperInclusive);
    }

    /// <summary>
    /// Verifies that <see cref="Interval{T}.Open" /> produces an open-open interval excluding both endpoints.
    /// </summary>
    [TestMethod]
    public void Open_WhenGivenBounds_ShouldProduceOpenOpenInterval()
    {
        Interval<int> interval = Interval<int>.Open(1, 5);

        Assert.IsFalse(interval.LowerInclusive);
        Assert.IsFalse(interval.UpperInclusive);
    }

    /// <summary>
    /// Verifies that <see cref="Interval{T}.ClosedOpen" /> includes the lower endpoint and excludes the upper.
    /// </summary>
    [TestMethod]
    public void ClosedOpen_WhenGivenBounds_ShouldIncludeLowerAndExcludeUpper()
    {
        Interval<int> interval = Interval<int>.ClosedOpen(1, 5);

        Assert.IsTrue(interval.LowerInclusive);
        Assert.IsFalse(interval.UpperInclusive);
    }

    /// <summary>
    /// Verifies that <see cref="Interval{T}.OpenClosed" /> excludes the lower endpoint and includes the upper.
    /// </summary>
    [TestMethod]
    public void OpenClosed_WhenGivenBounds_ShouldExcludeLowerAndIncludeUpper()
    {
        Interval<int> interval = Interval<int>.OpenClosed(1, 5);

        Assert.IsFalse(interval.LowerInclusive);
        Assert.IsTrue(interval.UpperInclusive);
    }

    /// <summary>
    /// Verifies that <see cref="Interval{T}.Singleton" /> produces a degenerate single-point interval.
    /// </summary>
    [TestMethod]
    public void Singleton_WhenGivenValue_ShouldProduceDegenerateInterval()
    {
        Interval<int> interval = Interval<int>.Singleton(7);

        Assert.AreEqual(7, interval.Lower);
        Assert.AreEqual(7, interval.Upper);
        Assert.IsTrue(interval.IsDegenerate);
        Assert.IsFalse(interval.IsEmpty);
    }

    /// <summary>
    /// Verifies that the non-generic <c>Interval.Closed</c> helper infers the endpoint type from its arguments.
    /// </summary>
    [TestMethod]
    public void NonGenericInterval_WhenGivenIntBounds_ShouldInferIntervalOfInt()
    {
        Interval<int> a = Interval.Closed(1, 5);
        Interval<double> b = Interval.Open(1.0, 5.0);
        Interval<decimal> c = Interval.ClosedOpen(0m, 100m);

        Assert.AreEqual(1, a.Lower);
        Assert.AreEqual(1.0, b.Lower);
        Assert.AreEqual(0m, c.Lower);
    }
}
