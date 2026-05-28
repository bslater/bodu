// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Properties.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that an inverted-bounds interval (lower &gt; upper) is reported as empty regardless of endpoint
    /// inclusivity.
    /// </summary>
    [TestMethod]
    [DataRow(5, 1, true, true)]
    [DataRow(5, 1, false, false)]
    [DataRow(5, 1, true, false)]
    public void IsEmpty_WhenLowerExceedsUpper_ShouldReturnTrue(int lower, int upper, bool lowerInclusive, bool upperInclusive)
    {
        Interval<int> interval = new Interval<int>(lower, upper, lowerInclusive, upperInclusive);

        Assert.IsTrue(interval.IsEmpty);
    }

    /// <summary>
    /// Verifies that equal-bounds intervals are empty when either endpoint is open, and non-empty (degenerate) only
    /// when both endpoints are closed.
    /// </summary>
    [TestMethod]
    [DataRow(5, false, false, true)]
    [DataRow(5, true, false, true)]
    [DataRow(5, false, true, true)]
    [DataRow(5, true, true, false)]
    public void IsEmpty_WhenLowerEqualsUpper_ShouldReflectInclusivity(int value, bool lowerInclusive, bool upperInclusive, bool expectedIsEmpty)
    {
        Interval<int> interval = new Interval<int>(value, value, lowerInclusive, upperInclusive);

        Assert.AreEqual(expectedIsEmpty, interval.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="Interval{T}.IsDegenerate" /> is true only for closed-closed intervals with equal
    /// endpoints.
    /// </summary>
    [TestMethod]
    public void IsDegenerate_WhenSinglePointClosedClosed_ShouldReturnTrue()
    {
        Assert.IsTrue(Interval<int>.Singleton(7).IsDegenerate);
        Assert.IsFalse(Interval<int>.Closed(1, 2).IsDegenerate);
        Assert.IsFalse(new Interval<int>(5, 5, lowerInclusive: false, upperInclusive: false).IsDegenerate);
    }

    /// <summary>
    /// Verifies that the algebraic length is the difference of the endpoints regardless of inclusivity, and zero
    /// for empty intervals.
    /// </summary>
    [TestMethod]
    public void Length_WhenNonEmpty_ShouldReturnUpperMinusLower()
    {
        Assert.AreEqual(4, Interval<int>.Closed(1, 5).Length);
        Assert.AreEqual(4, Interval<int>.Open(1, 5).Length);
        Assert.AreEqual(4, Interval<int>.ClosedOpen(1, 5).Length);
        Assert.AreEqual(4, Interval<int>.OpenClosed(1, 5).Length);
    }

    /// <summary>
    /// Verifies that the algebraic length of an empty interval is zero.
    /// </summary>
    [TestMethod]
    public void Length_WhenEmpty_ShouldReturnZero()
    {
        Assert.AreEqual(0, Interval<int>.Empty.Length);
        Assert.AreEqual(0, new Interval<int>(5, 1, true, true).Length);
    }

    /// <summary>
    /// Verifies that <see cref="Interval{T}.Empty" /> reports <see cref="Interval{T}.IsEmpty" /> as true.
    /// </summary>
    [TestMethod]
    public void Empty_ShouldReportIsEmptyTrue()
    {
        Assert.IsTrue(Interval<int>.Empty.IsEmpty);
        Assert.IsTrue(Interval<double>.Empty.IsEmpty);
    }

    /// <summary>
    /// Verifies that the default-constructed value is interpreted as empty. The default <c>(0, 0, false, false)</c>
    /// representation has equal endpoints with both endpoints open, which is the canonical empty case.
    /// </summary>
    [TestMethod]
    public void Default_WhenConstructed_ShouldBeEmpty()
    {
        Interval<int> interval = default;

        Assert.IsTrue(interval.IsEmpty);
    }
}
