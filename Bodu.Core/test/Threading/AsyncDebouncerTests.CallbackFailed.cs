// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncDebouncerTests.CallbackFailed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncDebouncerTests
{
    /// <summary>
    /// Verifies that an exception thrown by the callback is surfaced through the <see cref="AsyncDebouncer.CallbackFailed" />
    /// event rather than being silently lost.
    /// </summary>
    [TestMethod]
    public async Task CallbackFailed_WhenCallbackThrows_ShouldRaiseEventWithException()
    {
        var time = new FakeTimeProvider();
        Exception? captured = null;
        using var sut = new AsyncDebouncer(
            TimeSpan.FromMilliseconds(100),
            _ => throw new InvalidOperationException("boom"),
            AsyncDebouncerExecutionPolicy.QueueOneTrailingRun,
            time);
        sut.CallbackFailed += (_, ex) => captured = ex;

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        await sut.DrainAsync();

        Assert.IsInstanceOfType<InvalidOperationException>(captured);
        Assert.AreEqual("boom", captured!.Message);
    }

    /// <summary>
    /// Verifies that a callback canceled by <see cref="AsyncDebouncer.Cancel" /> does not raise
    /// <see cref="AsyncDebouncer.CallbackFailed" />.
    /// </summary>
    [TestMethod]
    public async Task CallbackFailed_WhenCallbackCanceled_ShouldNotRaiseEvent()
    {
        var time = new FakeTimeProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool raised = false;
        using var sut = new AsyncDebouncer(
            TimeSpan.FromMilliseconds(100),
            async ct =>
            {
                started.SetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                }
                finally
                {
                    ended.TrySetResult();
                }
            },
            AsyncDebouncerExecutionPolicy.QueueOneTrailingRun,
            time);
        sut.CallbackFailed += (_, _) => raised = true;

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));
        await started.Task;

        sut.Cancel();
        await ended.Task;
        await sut.DrainAsync();

        Assert.IsFalse(raised, "Cancellation must not be reported as a callback failure.");
    }
}
