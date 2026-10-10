// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncSemaphoreTests.WaiterQueueConsolidation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncSemaphoreTests
{
    /// <summary>A permit committed to a waiter must not be revoked by subsequent cancellation.</summary>
    [TestMethod]
    public async Task WaitAsync_WhenReleasePrecedesCancellation_ShouldCompleteSuccessfully()
    {
        var sut = new AsyncSemaphore(0);
        using var cts = new CancellationTokenSource();
        ValueTask pending = sut.WaitAsync(cts.Token);

        sut.Release();
        cts.Cancel();

        await pending;
        Assert.AreEqual(0, sut.WaiterCount);
        Assert.AreEqual(0, sut.CurrentCount);
    }

    /// <summary>Queued cancellations must not consume permits or compromise a multi-permit release.</summary>
    [TestMethod]
    public async Task Release_WhenCanceledWaiterBetweenLiveWaiters_ShouldGrantTwoPermits()
    {
        var sut = new AsyncSemaphore(0, 2);
        using var cts = new CancellationTokenSource();
        ValueTask first = sut.WaitAsync();
        ValueTask cancelled = sut.WaitAsync(cts.Token);
        ValueTask last = sut.WaitAsync();

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await cancelled);
        sut.Release(2);
        await first;
        await last;
        Assert.AreEqual(0, sut.CurrentCount);
        Assert.AreEqual(0, sut.WaiterCount);
    }
}
