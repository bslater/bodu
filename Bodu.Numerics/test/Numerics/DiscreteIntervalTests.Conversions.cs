// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteIntervalTests.Conversions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class DiscreteIntervalTests
{
    /// <summary>
    /// Verifies that converting a continuous half-line whose open bound sits at the domain extreme yields the empty
    /// discrete interval: no integer lies strictly beyond <c>T.MaxValue</c> or strictly below <c>T.MinValue</c>, and
    /// the inward snap must not wrap around the integer domain.
    /// </summary>
    [TestMethod]
    public void FromInterval_WhenOpenBoundAtDomainExtreme_ShouldReturnEmpty()
    {
        Assert.IsTrue(DiscreteInterval<int>.FromInterval(Interval<int>.GreaterThan(int.MaxValue)).IsEmpty);
        Assert.IsTrue(DiscreteInterval<int>.FromInterval(Interval<int>.LessThan(int.MinValue)).IsEmpty);
    }

    /// <summary>
    /// Verifies that a bounded continuous interval with open bounds snaps inward to the nearest contained integers.
    /// </summary>
    [TestMethod]
    public void FromInterval_WhenOpenBoundsAreBounded_ShouldSnapInward()
    {
        Assert.AreEqual(DiscreteInterval<int>.Closed(2, 4), DiscreteInterval<int>.FromInterval(Interval<int>.Open(1, 5)));
        Assert.AreEqual(DiscreteInterval<int>.Closed(1, 4), DiscreteInterval<int>.FromInterval(Interval<int>.ClosedOpen(1, 5)));
    }
}
