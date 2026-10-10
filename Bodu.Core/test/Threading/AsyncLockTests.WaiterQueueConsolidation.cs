// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLockTests.WaiterQueueConsolidation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncLockTests
{
    /// <summary>A grant completed before cancellation retains ownership of the mutex.</summary>
    [TestMethod]
    public async Task LockAsync_WhenGrantPrecedesCancellation_ShouldReturnReleaser()
    {
        var sut = new AsyncLock();
        using var cts = new CancellationTokenSource();
        AsyncLock.Releaser held = await sut.LockAsync();
        ValueTask<AsyncLock.Releaser> pending = sut.LockAsync(cts.Token);

        held.Dispose();
        cts.Cancel();

        (await pending).Dispose();
        Assert.AreEqual(0, sut.WaiterCount);
    }

    /// <summary>Cancellation of a middle queued waiter must not disturb FIFO handoff.</summary>
    [TestMethod]
    public async Task LockAsync_WhenMiddleWaiterCancels_ShouldGrantRemainingWaitersInOrder()
    {
        var sut = new AsyncLock();
        AsyncLock.Releaser held = await sut.LockAsync();
        using var cts = new CancellationTokenSource();
        ValueTask<AsyncLock.Releaser> first = sut.LockAsync();
        ValueTask<AsyncLock.Releaser> cancelled = sut.LockAsync(cts.Token);
        ValueTask<AsyncLock.Releaser> last = sut.LockAsync();

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await cancelled);
        held.Dispose();
        Assert.IsFalse(last.IsCompleted);
        (await first).Dispose();
        (await last).Dispose();
        Assert.AreEqual(0, sut.WaiterCount);
    }
}
