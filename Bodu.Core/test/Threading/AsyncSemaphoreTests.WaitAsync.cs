// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncSemaphoreTests.WaitAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncSemaphoreTests
{
    /// <summary>
    /// Verifies that a wait completes synchronously and consumes a permit when one is available.
    /// </summary>
    [TestMethod]
    public void WaitAsync_WhenPermitAvailable_ShouldCompleteSynchronously()
    {
        var sut = new AsyncSemaphore(1);

        var wait = sut.WaitAsync();

        Assert.IsTrue(wait.IsCompletedSuccessfully);
        Assert.AreEqual(0, sut.CurrentCount);
    }

    /// <summary>
    /// Verifies that a wait beyond the available permits does not complete until a permit is returned.
    /// </summary>
    [TestMethod]
    public async Task WaitAsync_WhenNoPermitAvailable_ShouldBlockUntilReleased()
    {
        var sut = new AsyncSemaphore(1);
        await sut.WaitAsync();

        var pending = sut.WaitAsync();
        Assert.IsFalse(pending.IsCompleted);

        sut.Release();
        await pending;
    }

    /// <summary>
    /// Verifies that an available permit is taken even when the token is already canceled, because success wins when
    /// the wait can complete immediately.
    /// </summary>
    [TestMethod]
    public void WaitAsync_WhenTokenAlreadyCanceledAndPermitAvailable_ShouldTakePermit()
    {
        var sut = new AsyncSemaphore(1);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var wait = sut.WaitAsync(cts.Token);

        Assert.IsTrue(wait.IsCompletedSuccessfully);
        Assert.AreEqual(0, sut.CurrentCount);
    }

    /// <summary>
    /// Verifies that an already-canceled token cancels the wait when no permit is available, leaving the permit count
    /// unchanged.
    /// </summary>
    [TestMethod]
    public async Task WaitAsync_WhenTokenAlreadyCanceledAndNoPermit_ShouldCancelSynchronously()
    {
        var sut = new AsyncSemaphore(0);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var wait = sut.WaitAsync(cts.Token);

        Assert.IsTrue(wait.IsCanceled);
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await wait);
        Assert.AreEqual(0, sut.CurrentCount);
    }

    /// <summary>
    /// Verifies that canceling a queued waiter removes it without consuming a later release.
    /// </summary>
    [TestMethod]
    public async Task WaitAsync_WhenCanceledWhileWaiting_ShouldCancelOnlyThatWaiter()
    {
        var sut = new AsyncSemaphore(0);
        using var cts = new CancellationTokenSource();

        var canceled = sut.WaitAsync(cts.Token);
        var live = sut.WaitAsync();

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceled);

        sut.Release();
        await live;
    }
}
