// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncDebouncerTests.CurrentExecution.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncDebouncerTests
{
    /// <summary>
    /// Verifies that <see cref="AsyncDebouncer.CurrentExecution" /> is <see langword="null" /> before any callback has
    /// started.
    /// </summary>
    [TestMethod]
    public void CurrentExecution_WhenNeverInvoked_ShouldBeNull()
    {
        var time = new FakeTimeProvider();
        using var sut = new AsyncDebouncer(TimeSpan.FromMilliseconds(100), _ => ValueTask.CompletedTask, timeProvider: time);

        Assert.IsNull(sut.CurrentExecution);
    }

    /// <summary>
    /// Verifies that <see cref="AsyncDebouncer.CurrentExecution" /> exposes the in-flight callback task and reverts to
    /// <see langword="null" /> once the callback completes.
    /// </summary>
    [TestMethod]
    public async Task CurrentExecution_WhenCallbackInFlight_ShouldExposeTaskThenClear()
    {
        var time = new FakeTimeProvider();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var sut = new AsyncDebouncer(
            TimeSpan.FromMilliseconds(100),
            async _ => await gate.Task,
            AsyncDebouncerExecutionPolicy.QueueOneTrailingRun,
            time);

        sut.Invoke();
        time.Advance(TimeSpan.FromMilliseconds(100));

        Assert.IsNotNull(sut.CurrentExecution);

        gate.SetResult();
        await sut.DrainAsync();

        Assert.IsNull(sut.CurrentExecution);
    }
}
