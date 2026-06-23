// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncDebouncerTests.Cancel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncDebouncerTests
{
    /// <summary>
    /// Verifies that <see cref="AsyncDebouncer.Cancel" /> discards a pending invocation before its timer elapses.
    /// </summary>
    [TestMethod]
    public void Cancel_WhenPending_ShouldNotRunCallback()
    {
        var time = new FakeTimeProvider();
        var runs = 0;
        using var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), _ =>
        {
            Interlocked.Increment(ref runs);
            return ValueTask.CompletedTask;
        }, timeProvider: time);

        sut.Invoke();
        sut.Cancel();
        time.Advance(TimeSpan.FromMilliseconds(200));

        Assert.AreEqual(0, runs);
    }

    /// <summary>
    /// Verifies that canceling when nothing is pending and no callback is in flight is a harmless no-op.
    /// </summary>
    [TestMethod]
    public void Cancel_WhenNothingPending_ShouldNotThrow()
    {
        var time = new FakeTimeProvider();
        using var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), _ => ValueTask.CompletedTask, timeProvider: time);

        sut.Cancel();
    }

    /// <summary>
    /// Verifies that canceling while a callback is in flight signals cancellation to that callback.
    /// </summary>
    [TestMethod]
    public async Task Cancel_WhenCallbackInFlight_ShouldCancelTheCallback()
    {
        var time = new FakeTimeProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceledObserved = false;

        using var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), async ct =>
        {
            started.SetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            }
            catch (OperationCanceledException)
            {
                canceledObserved = true;
                throw;
            }
            finally
            {
                ended.TrySetResult();
            }
        }, timeProvider: time);

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        await started.Task;

        sut.Cancel();
        await ended.Task;

        Assert.IsTrue(canceledObserved);
    }

    /// <summary>
    /// Verifies that canceling a disposed debouncer throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Cancel_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var sut = new AsyncDebouncer(TimeSpan.FromSeconds(1), _ => ValueTask.CompletedTask);
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => sut.Cancel());
    }
}
