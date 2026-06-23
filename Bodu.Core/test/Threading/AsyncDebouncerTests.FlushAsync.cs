// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncDebouncerTests.FlushAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncDebouncerTests
{
    /// <summary>
    /// Verifies that <see cref="AsyncDebouncer.FlushAsync" /> runs a pending invocation immediately.
    /// </summary>
    [TestMethod]
    public async Task FlushAsync_WhenPending_ShouldRunCallbackImmediately()
    {
        var time = new FakeTimeProvider();
        var runs = 0;
        using var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), _ =>
        {
            Interlocked.Increment(ref runs);
            return ValueTask.CompletedTask;
        }, time);

        sut.Invoke();
        await sut.FlushAsync();

        Assert.AreEqual(1, runs);
    }

    /// <summary>
    /// Verifies that flushing without a pending invocation completes synchronously without running the callback.
    /// </summary>
    [TestMethod]
    public void FlushAsync_WhenNothingPending_ShouldCompleteSynchronously()
    {
        var time = new FakeTimeProvider();
        var runs = 0;
        using var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), _ =>
        {
            Interlocked.Increment(ref runs);
            return ValueTask.CompletedTask;
        }, time);

        var flush = sut.FlushAsync();

        Assert.IsTrue(flush.IsCompleted);
        Assert.AreEqual(0, runs);
    }

    /// <summary>
    /// Verifies that flushing cancels the pending timer so the callback does not also run when time advances.
    /// </summary>
    [TestMethod]
    public async Task FlushAsync_WhenPending_ShouldNotRunCallbackAgainOnTimerElapsed()
    {
        var time = new FakeTimeProvider();
        var runs = 0;
        using var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), _ =>
        {
            Interlocked.Increment(ref runs);
            return ValueTask.CompletedTask;
        }, time);

        sut.Invoke();
        await sut.FlushAsync();
        time.Advance(TimeSpan.FromMilliseconds(200));

        Assert.AreEqual(1, runs);
    }

    /// <summary>
    /// Verifies that flushing a disposed debouncer throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void FlushAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var sut = new AsyncDebouncer(TimeSpan.FromSeconds(1), _ => ValueTask.CompletedTask);
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = sut.FlushAsync();
        });
    }
}
