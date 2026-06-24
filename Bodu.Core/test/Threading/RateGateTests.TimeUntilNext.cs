// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateGateTests.TimeUntilNext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class RateGateTests
{
    /// <summary>
    /// Verifies that <see cref="RateGate.TimeUntilNext" /> reports zero before any invocation has been admitted.
    /// </summary>
    [TestMethod]
    public void TimeUntilNext_WhenNeverInvoked_ShouldReportZero()
    {
        var time = new FakeTimeProvider();
        var sut = new RateGate(TimeSpan.FromSeconds(1), time);

        Assert.AreEqual(TimeSpan.Zero, sut.TimeUntilNext);
    }

    /// <summary>
    /// Verifies that <see cref="RateGate.TimeUntilNext" /> reports the remaining cool-down within the interval.
    /// </summary>
    [TestMethod]
    public void TimeUntilNext_WhenWithinInterval_ShouldReportRemaining()
    {
        var time = new FakeTimeProvider();
        var sut = new RateGate(TimeSpan.FromSeconds(1), time);

        Assert.IsTrue(sut.TryInvoke());
        time.Advance(TimeSpan.FromMilliseconds(400));

        Assert.AreEqual(TimeSpan.FromMilliseconds(600), sut.TimeUntilNext);
    }

    /// <summary>
    /// Verifies that <see cref="RateGate.TimeUntilNext" /> clamps to zero once the interval has fully elapsed.
    /// </summary>
    [TestMethod]
    public void TimeUntilNext_WhenIntervalElapsed_ShouldReportZero()
    {
        var time = new FakeTimeProvider();
        var sut = new RateGate(TimeSpan.FromSeconds(1), time);

        Assert.IsTrue(sut.TryInvoke());
        time.Advance(TimeSpan.FromSeconds(2));

        Assert.AreEqual(TimeSpan.Zero, sut.TimeUntilNext);
    }
}
