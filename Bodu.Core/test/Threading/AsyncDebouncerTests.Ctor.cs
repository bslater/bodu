// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncDebouncerTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncDebouncerTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> callback throws <see cref="ArgumentNullException" /> naming <c>callback</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCallbackIsNull_ShouldThrowForCallback()
    {
        Assert.AreEqual(
            "callback",
            Assert.ThrowsExactly<ArgumentNullException>(() => new AsyncDebouncer(TimeSpan.FromSeconds(1), null!)).ParamName);
    }

    /// <summary>
    /// Verifies that a negative delay throws <see cref="ArgumentOutOfRangeException" /> naming <c>delay</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDelayIsNegative_ShouldThrowForDelay()
    {
        Assert.AreEqual(
            "delay",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new AsyncDebouncer(TimeSpan.FromTicks(-1), _ => ValueTask.CompletedTask)).ParamName);
    }

    /// <summary>
    /// Verifies that a zero delay is permitted and the callback runs once the timer is fired.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDelayIsZero_ShouldRunCallbackOnTimerElapsed()
    {
        var time = new FakeTimeProvider();
        var runs = 0;
        using var sut = new AsyncDebouncer(TimeSpan.Zero, _ =>
        {
            Interlocked.Increment(ref runs);
            return ValueTask.CompletedTask;
        }, time);

        sut.Invoke();
        time.Advance(TimeSpan.Zero);

        Assert.AreEqual(1, runs);
    }
}
