// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateGateTests.TryInvoke.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class RateGateTests
{
    /// <summary>
    /// Verifies that the first invocation is admitted and a second within the interval is denied.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void TryInvoke_WhenWithinInterval_ShouldAdmitFirstAndDenySecond()
    {
        var time = new FakeTimeProvider();
        var sut = new RateGate(TimeSpan.FromSeconds(1), time);

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
        var sut = new RateGate(TimeSpan.FromSeconds(1), time);

        Assert.IsTrue(sut.TryInvoke());

        time.Advance(TimeSpan.FromSeconds(1));

        Assert.IsTrue(sut.TryInvoke());
    }

    /// <summary>
    /// Verifies that a second invocation is admitted only once the cool-down window has fully elapsed.
    /// </summary>
    /// <param name="advanceMilliseconds">The simulated time advanced after the first admitted invocation.</param>
    /// <param name="expectedAdmitted">The expected result of the second invocation.</param>
    [TestMethod]
    [DataRow(0, false)]
    [DataRow(500, false)]
    [DataRow(999, false)]
    [DataRow(1000, true)]
    [DataRow(1500, true)]
    public void TryInvoke_WhenTimeAdvances_ShouldAdmitOnlyAfterInterval(int advanceMilliseconds, bool expectedAdmitted)
    {
        var time = new FakeTimeProvider();
        var sut = new RateGate(TimeSpan.FromSeconds(1), time);

        Assert.IsTrue(sut.TryInvoke());
        time.Advance(TimeSpan.FromMilliseconds(advanceMilliseconds));

        Assert.AreEqual(expectedAdmitted, sut.TryInvoke());
    }

    /// <summary>
    /// Verifies that the throttler functions with the default <see cref="TimeProvider.System" /> when no provider is
    /// supplied.
    /// </summary>
    [TestMethod]
    public void TryInvoke_WhenNoTimeProviderSupplied_ShouldAdmitFirstInvocation()
    {
        var sut = new RateGate(TimeSpan.FromSeconds(30));

        Assert.IsTrue(sut.TryInvoke());
        Assert.IsFalse(sut.TryInvoke());
    }
}
