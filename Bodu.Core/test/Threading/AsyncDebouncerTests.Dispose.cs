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
        int runs = 0;
        var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), _ =>
        {
            Interlocked.Increment(ref runs);
            return ValueTask.CompletedTask;
        }, timeProvider: time);

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
        bool canceledObserved = false;

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
        }, timeProvider: time);

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        await started.Task;

        sut.Dispose();
        await ended.Task;

        Assert.IsTrue(canceledObserved);
    }

    /// <summary>
    /// Verifies that disposing the debouncer while an in-flight callback completes concurrently does not surface
    /// <see cref="ObjectDisposedException" /> from <see cref="AsyncDebouncer.Dispose" /> — the completing callback
    /// disposes its per-run <see cref="CancellationTokenSource" /> while <see cref="AsyncDebouncer.Dispose" />
    /// cancels the same source it captured under the gate.
    /// </summary>
    [TestMethod]
    public async Task Dispose_WhenCallbackCompletesConcurrently_ShouldNotThrowObjectDisposedException()
    {
        for (int i = 0; i < 2000; i++)
        {
            var time = new FakeTimeProvider();
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), async _ =>
            {
                started.SetResult();
                await release.Task.ConfigureAwait(false);
            }, timeProvider: time);

            sut.Invoke();
            time.Advance(TimeSpan.FromMilliseconds(100));
            await started.Task;

            using var barrier = new Barrier(2);

            // Complete the callback (its finally disposes the per-run CTS) and dispose the debouncer (its cancel
            // loop touches the same CTS) at the same instant, maximizing the race window.
            var complete = Task.Run(() =>
            {
                barrier.SignalAndWait();
                release.SetResult();
            });
            var dispose = Task.Run(() =>
            {
                barrier.SignalAndWait();
                sut.Dispose();
            });

            await Task.WhenAll(complete, dispose);
        }
    }
}
