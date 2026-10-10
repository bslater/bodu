// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncAutoResetEventTests.WaiterQueueConsolidation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncAutoResetEventTests
{
    /// <summary>A signal handed to a queued waiter wins over a subsequent cancellation.</summary>
    [TestMethod]
    public async Task WaitAsync_WhenSetPrecedesCancellation_ShouldConsumeSignal()
    {
        var sut = new AsyncAutoResetEvent();
        using var cts = new CancellationTokenSource();
        ValueTask pending = sut.WaitAsync(cts.Token);

        sut.Set();
        cts.Cancel();

        await pending;
        Assert.AreEqual(0, sut.WaiterCount);
        ValueTask unsignaled = sut.WaitAsync();
        Assert.IsFalse(unsignaled.IsCompleted);
        sut.Set();
        await unsignaled;
    }

    /// <summary>Canceling the first waiter must preserve the queued successor's signal.</summary>
    [TestMethod]
    public async Task Set_WhenFrontWaiterCancels_ShouldReleaseNextWaiter()
    {
        var sut = new AsyncAutoResetEvent();
        using var cts = new CancellationTokenSource();
        ValueTask canceled = sut.WaitAsync(cts.Token);
        ValueTask live = sut.WaitAsync();

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceled);
        sut.Set();
        await live;
        Assert.AreEqual(0, sut.WaiterCount);
    }
}
