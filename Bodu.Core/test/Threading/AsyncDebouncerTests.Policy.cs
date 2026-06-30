// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncDebouncerTests.Policy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncDebouncerTests
{
    /// <summary>
    /// Verifies that under <see cref="AsyncDebouncerExecutionPolicy.DropWhileRunning" /> a run that becomes due while a
    /// callback is in flight is discarded rather than overlapping.
    /// </summary>
    [TestMethod]
    public async Task Invoke_WhenDropWhileRunningAndCallbackInFlight_ShouldNotStartSecond()
    {
        var time = new FakeTimeProvider();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int started = 0;
        using var sut = new AsyncDebouncer(
            TimeSpan.FromMilliseconds(100),
            async _ =>
            {
                Interlocked.Increment(ref started);
                await gate.Task;
            },
            AsyncDebouncerExecutionPolicy.DropWhileRunning,
            time);

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        Task? run1 = sut.CurrentExecution;
        Assert.AreEqual(1, started);

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        Assert.AreEqual(1, started, "A due run must be dropped while a callback is in flight.");

        gate.SetResult();
        await run1!;

        Assert.AreEqual(1, started);
    }

    /// <summary>
    /// Verifies that under <see cref="AsyncDebouncerExecutionPolicy.QueueOneTrailingRun" /> a run that becomes due
    /// while a callback is in flight runs exactly once after the in-flight callback completes.
    /// </summary>
    [TestMethod]
    public async Task Invoke_WhenQueueOneTrailingRunAndCallbackInFlight_ShouldRunExactlyOnceMore()
    {
        var time = new FakeTimeProvider();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int started = 0;
        using var sut = new AsyncDebouncer(
            TimeSpan.FromMilliseconds(100),
            async _ =>
            {
                Interlocked.Increment(ref started);
                await gate.Task;
            },
            AsyncDebouncerExecutionPolicy.QueueOneTrailingRun,
            time);

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        Assert.AreEqual(1, started);

        // Two further triggers while running coalesce into a single trailing run.
        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        Assert.AreEqual(1, started);

        gate.SetResult();
        await sut.DrainAsync();

        Assert.AreEqual(2, started, "Exactly one trailing run should follow the in-flight callback.");
    }

    /// <summary>
    /// Verifies that under <see cref="AsyncDebouncerExecutionPolicy.CancelAndRestart" /> a run that becomes due while a
    /// callback is in flight cancels that callback and starts a replacement.
    /// </summary>
    [TestMethod]
    public async Task Invoke_WhenCancelAndRestartAndCallbackInFlight_ShouldCancelAndStartReplacement()
    {
        var time = new FakeTimeProvider();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool firstCanceled = false;
        int starts = 0;
        using var sut = new AsyncDebouncer(
            TimeSpan.FromMilliseconds(100),
            async ct =>
            {
                if (Interlocked.Increment(ref starts) == 1)
                {
                    firstStarted.SetResult();
                    try
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        firstCanceled = true;
                        throw;
                    }
                }
            },
            AsyncDebouncerExecutionPolicy.CancelAndRestart,
            time);

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        await firstStarted.Task;
        Assert.AreEqual(1, starts);

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        await sut.DrainAsync();

        Assert.AreEqual(2, starts);
        Assert.IsTrue(firstCanceled, "The in-flight callback should have been canceled.");
    }

    /// <summary>
    /// Verifies that under <see cref="AsyncDebouncerExecutionPolicy.AllowOverlap" /> a run that becomes due while a
    /// callback is in flight starts concurrently.
    /// </summary>
    [TestMethod]
    public async Task Invoke_WhenAllowOverlapAndCallbackInFlight_ShouldStartConcurrently()
    {
        var time = new FakeTimeProvider();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int started = 0;
        using var sut = new AsyncDebouncer(
            TimeSpan.FromMilliseconds(100),
            async _ =>
            {
                Interlocked.Increment(ref started);
                await gate.Task;
            },
            AsyncDebouncerExecutionPolicy.AllowOverlap,
            time);

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        Assert.AreEqual(1, started);

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        Assert.AreEqual(2, started, "Overlap policy should start a concurrent callback.");

        gate.SetResult();
        await sut.DrainAsync();
    }
}
