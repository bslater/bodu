// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Contains.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
        Interval<int> interval = Interval<int>.Closed(1, 5);

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
        Interval<int> interval = Interval<int>.Open(1, 5);

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
        Interval<int> interval = Interval<int>.ClosedOpen(1, 5);

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
        Interval<int> interval = Interval<int>.OpenClosed(1, 5);

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
    public void Contains_WhenEmpty_ShouldReturnFalseForAnyValue(int value)
    {
        Assert.IsFalse(Interval<int>.Empty.Contains(value));
    }

    /// <summary>
    /// Verifies that a degenerate single-point interval contains only its anchor.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDegenerate_ShouldContainOnlyTheAnchor()
    {
        Interval<int> interval = Interval<int>.Singleton(7);

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
        Interval<int> outer = Interval<int>.Closed(0, 10);

        Assert.IsTrue(outer.Contains(Interval<int>.Closed(2, 8)));
        Assert.IsTrue(outer.Contains(Interval<int>.Open(2, 8)));
        Assert.IsTrue(outer.Contains(Interval<int>.Closed(0, 10)));
        Assert.IsTrue(outer.Contains(Interval<int>.Singleton(5)));
        Assert.IsTrue(outer.Contains(Interval<int>.Empty));

        Assert.IsFalse(outer.Contains(Interval<int>.Closed(-1, 5)));
        Assert.IsFalse(outer.Contains(Interval<int>.Closed(5, 11)));
        Assert.IsFalse(Interval<int>.Open(0, 10).Contains(Interval<int>.Closed(0, 10)));
    }
}
