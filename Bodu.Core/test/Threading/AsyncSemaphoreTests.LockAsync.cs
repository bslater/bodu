// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncSemaphoreTests.LockAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncSemaphoreTests
{
    /// <summary>
    /// Verifies that taking and returning a permit updates the available count.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public async Task LockAsync_WhenPermitAvailable_ShouldTakeAndReturnPermit()
    {
        var sut = new AsyncSemaphore(1);

        using (await sut.LockAsync())
        {
            Assert.AreEqual(0, sut.CurrentCount);
        }

        Assert.AreEqual(1, sut.CurrentCount);
    }

    /// <summary>
    /// Verifies that a contended <see cref="AsyncSemaphore.LockAsync()" /> resolves through the asynchronous
    /// continuation once a permit is released, then returns the permit when disposed.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_WhenContended_ShouldResolveAfterReleaseAndReturnPermit()
    {
        var sut = new AsyncSemaphore(0);

        ValueTask<AsyncSemaphore.Releaser> pending = sut.LockAsync();
        Assert.IsFalse(pending.IsCompleted);

        sut.Release();

        using (await pending)
        {
            Assert.AreEqual(0, sut.CurrentCount);
        }

        Assert.AreEqual(1, sut.CurrentCount);
    }

    /// <summary>
    /// Verifies that canceling a contended <see cref="AsyncSemaphore.LockAsync(System.Threading.CancellationToken)" />
    /// throws and does not consume a later release.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_WhenCanceledWhileWaiting_ShouldCancelOnlyThatWaiter()
    {
        var sut = new AsyncSemaphore(0);
        using var cts = new CancellationTokenSource();

        ValueTask<AsyncSemaphore.Releaser> canceled = sut.LockAsync(cts.Token);
        ValueTask<AsyncSemaphore.Releaser> live = sut.LockAsync();

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceled);

        sut.Release();
        using (await live)
        {
        }
    }
}
