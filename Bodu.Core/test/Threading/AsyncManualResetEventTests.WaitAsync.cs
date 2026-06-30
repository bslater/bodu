// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncManualResetEventTests.WaitAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncManualResetEventTests
{
    /// <summary>
    /// Verifies that setting the event releases a pending waiter.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public async Task WaitAsync_WhenSet_ShouldComplete()
    {
        var sut = new AsyncManualResetEvent();

        ValueTask pending = sut.WaitAsync();
        Assert.IsFalse(pending.IsCompleted);

        sut.Set();
        await pending;

        Assert.IsTrue(sut.IsSet);
    }

    /// <summary>
    /// Verifies that a wait completes synchronously when the event is already set.
    /// </summary>
    [TestMethod]
    public void WaitAsync_WhenAlreadySet_ShouldCompleteSynchronously()
    {
        var sut = new AsyncManualResetEvent(initialState: true);

        Assert.IsTrue(sut.WaitAsync().IsCompleted);
    }

    /// <summary>
    /// Verifies that an already-set event completes a token-bearing wait synchronously, ignoring the canceled token.
    /// </summary>
    [TestMethod]
    public void WaitAsync_WhenAlreadySetAndTokenCanceled_ShouldCompleteSynchronously()
    {
        var sut = new AsyncManualResetEvent(initialState: true);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.IsTrue(sut.WaitAsync(cts.Token).IsCompletedSuccessfully);
    }

    /// <summary>
    /// Verifies that an unsignaled event cancels a wait when the token is already canceled.
    /// </summary>
    [TestMethod]
    public async Task WaitAsync_WhenUnsignaledAndTokenCanceled_ShouldCancel()
    {
        var sut = new AsyncManualResetEvent();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        ValueTask wait = sut.WaitAsync(cts.Token);

        Assert.IsTrue(wait.IsCanceled);
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await wait);
    }

    /// <summary>
    /// Verifies that canceling a waiter throws without disturbing other waiters.
    /// </summary>
    [TestMethod]
    public async Task WaitAsync_WhenCanceled_ShouldCancelOnlyThatWaiter()
    {
        var sut = new AsyncManualResetEvent();
        using var cts = new CancellationTokenSource();

        ValueTask canceled = sut.WaitAsync(cts.Token);
        ValueTask live = sut.WaitAsync();

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceled);

        sut.Set();
        await live;
    }
}
