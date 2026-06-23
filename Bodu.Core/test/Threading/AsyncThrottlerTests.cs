// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncThrottlerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

/// <summary>
/// Contains unit tests for the <see cref="AsyncThrottler" /> type.
/// </summary>
[TestClass]
public sealed class AsyncThrottlerTests
{
    /// <summary>
    /// Verifies that the first invocation is admitted and a second within the interval is denied.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void TryInvoke_WhenWithinInterval_ShouldAdmitFirstAndDenySecond()
    {
        var time = new FakeTimeProvider();
        var sut = new AsyncThrottler(TimeSpan.FromSeconds(1), time);

        Assert.IsTrue(sut.TryInvoke());
        Assert.IsFalse(sut.TryInvoke());
    }

    /// <summary>
    /// Verifies that an invocation is admitted again once the interval has elapsed.
    /// </summary>
    [TestMethod]
    public void TryInvoke_WhenIntervalElapsed_ShouldAdmitAgain()
    {
        var time = new FakeTimeProvider();
        var sut = new AsyncThrottler(TimeSpan.FromSeconds(1), time);

        Assert.IsTrue(sut.TryInvoke());

        time.Advance(TimeSpan.FromSeconds(1));

        Assert.IsTrue(sut.TryInvoke());
    }

    /// <summary>
    /// Verifies that <see cref="AsyncThrottler.TimeUntilNext" /> reports the remaining cool-down.
    /// </summary>
    [TestMethod]
    public void TimeUntilNext_WhenWithinInterval_ShouldReportRemaining()
    {
        var time = new FakeTimeProvider();
        var sut = new AsyncThrottler(TimeSpan.FromSeconds(1), time);

        Assert.AreEqual(TimeSpan.Zero, sut.TimeUntilNext);

        Assert.IsTrue(sut.TryInvoke());
        time.Advance(TimeSpan.FromMilliseconds(400));

        Assert.AreEqual(TimeSpan.FromMilliseconds(600), sut.TimeUntilNext);
    }

    /// <summary>
    /// Verifies that a non-positive interval throws <see cref="ArgumentOutOfRangeException" /> naming <c>interval</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIntervalIsZero_ShouldThrowForInterval()
    {
        Assert.AreEqual(
            "interval",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AsyncThrottler(TimeSpan.Zero)).ParamName);
    }
}
