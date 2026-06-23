// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncDebouncerTests.Invoke.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncDebouncerTests
{
    /// <summary>
    /// Verifies that a single trigger runs the callback once the quiet period elapses.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Invoke_WhenQuietPeriodElapses_ShouldRunCallbackOnce()
    {
        var time = new FakeTimeProvider();
        var runs = 0;
        using var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), _ =>
        {
            Interlocked.Increment(ref runs);
            return ValueTask.CompletedTask;
        }, timeProvider: time);

        sut.Invoke();
        Assert.AreEqual(0, runs);

        time.Advance(TimeSpan.FromMilliseconds(100));

        Assert.AreEqual(1, runs);
    }

    /// <summary>
    /// Verifies that a burst of triggers within the quiet period results in a single callback invocation.
    /// </summary>
    [TestMethod]
    public void Invoke_WhenTriggeredRepeatedly_ShouldCoalesceIntoOneRun()
    {
        var time = new FakeTimeProvider();
        var runs = 0;
        using var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), _ =>
        {
            Interlocked.Increment(ref runs);
            return ValueTask.CompletedTask;
        }, timeProvider: time);

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(50));
        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(50));
        sut.Invoke();

        Assert.AreEqual(0, runs);

        time.Advance(TimeSpan.FromMilliseconds(100));

        Assert.AreEqual(1, runs);
    }

    /// <summary>
    /// Verifies that triggering again after a completed run schedules and runs the callback a second time.
    /// </summary>
    [TestMethod]
    public void Invoke_WhenTriggeredAfterRun_ShouldRunCallbackAgain()
    {
        var time = new FakeTimeProvider();
        var runs = 0;
        using var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), _ =>
        {
            Interlocked.Increment(ref runs);
            return ValueTask.CompletedTask;
        }, timeProvider: time);

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        Assert.AreEqual(1, runs);

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        Assert.AreEqual(2, runs);
    }

    /// <summary>
    /// Verifies that triggering a disposed debouncer throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Invoke_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var sut = new AsyncDebouncer(TimeSpan.FromSeconds(1), _ => ValueTask.CompletedTask);
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => sut.Invoke());
    }
}
