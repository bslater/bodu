// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncCountdownEventTests.WaitAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncCountdownEventTests
{
    /// <summary>
    /// Verifies that a wait on an unsignaled event does not complete until the count reaches zero.
    /// </summary>
    [TestMethod]
    public async Task WaitAsync_WhenSignaledToZero_ShouldComplete()
    {
        var sut = new AsyncCountdownEvent(1);

        var pending = sut.WaitAsync();
        Assert.IsFalse(pending.IsCompleted);

        sut.Signal();
        await pending;
    }

    /// <summary>
    /// Verifies that a wait with an already-canceled token cancels because the event is not yet signaled.
    /// </summary>
    [TestMethod]
    public async Task WaitAsync_WhenTokenAlreadyCanceled_ShouldCancel()
    {
        var sut = new AsyncCountdownEvent(1);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var wait = sut.WaitAsync(cts.Token);

        Assert.IsTrue(wait.IsCanceled);
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await wait);
    }

    /// <summary>
    /// Verifies that canceling the token while waiting cancels the wait without affecting the count.
    /// </summary>
    [TestMethod]
    public async Task WaitAsync_WhenCanceledWhileWaiting_ShouldCancelOnlyThatWaiter()
    {
        var sut = new AsyncCountdownEvent(1);
        using var cts = new CancellationTokenSource();

        var canceled = sut.WaitAsync(cts.Token);
        var live = sut.WaitAsync();

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceled);

        sut.Signal();
        await live;
    }

    /// <summary>
    /// Verifies that an already-signaled event completes a token-bearing wait synchronously, ignoring the token.
    /// </summary>
    [TestMethod]
    public void WaitAsync_WhenAlreadySignaled_ShouldCompleteSynchronously()
    {
        var sut = new AsyncCountdownEvent(0);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.IsTrue(sut.WaitAsync(cts.Token).IsCompletedSuccessfully);
    }
}
