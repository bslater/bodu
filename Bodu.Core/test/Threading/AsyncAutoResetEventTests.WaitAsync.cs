// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncAutoResetEventTests.WaitAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncAutoResetEventTests
{
    /// <summary>
    /// Verifies that an event constructed in the signaled state immediately satisfies the first waiter.
    /// </summary>
    [TestMethod]
    public void WaitAsync_WhenInitiallySignaled_ShouldCompleteSynchronously()
    {
        var sut = new AsyncAutoResetEvent(initialState: true);

        Assert.IsTrue(sut.WaitAsync().IsCompleted);
    }

    /// <summary>
    /// Verifies that consuming the latched signal returns the event to the unsignaled state.
    /// </summary>
    [TestMethod]
    public async Task WaitAsync_WhenSignalConsumed_ShouldReturnToUnsignaled()
    {
        var sut = new AsyncAutoResetEvent(initialState: true);

        await sut.WaitAsync();

        Assert.IsFalse(sut.WaitAsync().IsCompleted);
    }

    /// <summary>
    /// Verifies that a latched signal is consumed even when the token is already canceled, because success wins when
    /// the wait can complete immediately.
    /// </summary>
    [TestMethod]
    public void WaitAsync_WhenTokenAlreadyCanceledAndSignaled_ShouldConsumeSignal()
    {
        var sut = new AsyncAutoResetEvent(initialState: true);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        ValueTask wait = sut.WaitAsync(cts.Token);

        Assert.IsTrue(wait.IsCompletedSuccessfully);

        // The signal was consumed, so a subsequent wait does not complete immediately.
        Assert.IsFalse(sut.WaitAsync().IsCompleted);
    }

    /// <summary>
    /// Verifies that an already-canceled token cancels the wait when no signal is latched.
    /// </summary>
    [TestMethod]
    public async Task WaitAsync_WhenTokenAlreadyCanceledAndUnsignaled_ShouldCancel()
    {
        var sut = new AsyncAutoResetEvent(initialState: false);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        ValueTask wait = sut.WaitAsync(cts.Token);

        Assert.IsTrue(wait.IsCanceled);
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await wait);
    }

    /// <summary>
    /// Verifies that canceling a waiter throws without consuming a later signal.
    /// </summary>
    [TestMethod]
    public async Task WaitAsync_WhenCanceled_ShouldNotConsumeSignal()
    {
        var sut = new AsyncAutoResetEvent();
        using var cts = new CancellationTokenSource();

        ValueTask canceled = sut.WaitAsync(cts.Token);
        ValueTask live = sut.WaitAsync();

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceled);

        sut.Set();
        await live;
    }
}
