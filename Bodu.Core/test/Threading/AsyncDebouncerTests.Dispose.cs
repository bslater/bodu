// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncDebouncerTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncDebouncerTests
{
    /// <summary>
    /// Verifies that disposing the debouncer twice does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var sut = new AsyncDebouncer(TimeSpan.FromSeconds(1), _ => ValueTask.CompletedTask);

        sut.Dispose();
        sut.Dispose();
    }

    /// <summary>
    /// Verifies that disposing the debouncer discards a pending invocation so its timer never runs the callback.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenPending_ShouldNotRunCallback()
    {
        var time = new FakeTimeProvider();
        var runs = 0;
        var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), _ =>
        {
            Interlocked.Increment(ref runs);
            return ValueTask.CompletedTask;
        }, time);

        sut.Invoke();
        sut.Dispose();
        time.Advance(TimeSpan.FromMilliseconds(200));

        Assert.AreEqual(0, runs);
    }

    /// <summary>
    /// Verifies that disposing while a callback is in flight signals cancellation to that callback.
    /// </summary>
    [TestMethod]
    public async Task Dispose_WhenCallbackInFlight_ShouldCancelTheCallback()
    {
        var time = new FakeTimeProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceledObserved = false;

        var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), async ct =>
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
        }, time);

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        await started.Task;

        sut.Dispose();
        await ended.Task;

        Assert.IsTrue(canceledObserved);
    }
}
