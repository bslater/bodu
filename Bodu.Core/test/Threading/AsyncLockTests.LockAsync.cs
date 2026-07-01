// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLockTests.LockAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncLockTests
{
    /// <summary>
    /// Verifies that acquiring an uncontended lock completes and that the lock can be reacquired after release.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public async Task LockAsync_WhenUncontended_ShouldAcquireAndAllowReacquire()
    {
        var sut = new AsyncLock();

        using (await sut.LockAsync())
        {
        }

        using (await sut.LockAsync())
        {
        }
    }

    /// <summary>
    /// Verifies that a second acquisition does not complete while the lock is held, then completes once released.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_WhenHeld_ShouldBlockUntilReleased()
    {
        var sut = new AsyncLock();
        AsyncLock.Releaser releaser = await sut.LockAsync();

        ValueTask<AsyncLock.Releaser> contended = sut.LockAsync();
        Assert.IsFalse(contended.IsCompleted, "The second acquisition must not complete while the lock is held.");

        releaser.Dispose();

        using (await contended)
        {
        }
    }

    /// <summary>
    /// Verifies that a contended acquisition resolves through the asynchronous continuation once the holder releases.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_WhenContended_ShouldResolveAfterRelease()
    {
        var sut = new AsyncLock();
        AsyncLock.Releaser first = await sut.LockAsync();

        ValueTask<AsyncLock.Releaser> second = sut.LockAsync();
        Assert.IsFalse(second.IsCompleted);

        first.Dispose();

        using (await second)
        {
        }

        // A further acquisition proves the contended path returned the lock cleanly.
        using (await sut.LockAsync())
        {
        }
    }

    /// <summary>
    /// Verifies that a free lock is acquired even when the token is already canceled, because success wins when the
    /// acquisition can complete immediately.
    /// </summary>
    [TestMethod]
    public void LockAsync_WhenTokenAlreadyCanceledAndFree_ShouldAcquire()
    {
        var sut = new AsyncLock();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        ValueTask<AsyncLock.Releaser> acquire = sut.LockAsync(cts.Token);

        Assert.IsTrue(acquire.IsCompletedSuccessfully);
        acquire.Result.Dispose();
    }

    /// <summary>
    /// Verifies that requesting a held lock with an already-canceled token throws <see cref="TaskCanceledException" />.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_WhenTokenAlreadyCanceledAndHeld_ShouldThrowTaskCanceled()
    {
        var sut = new AsyncLock();
        using AsyncLock.Releaser held = await sut.LockAsync();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            using (await sut.LockAsync(cts.Token))
            {
            }
        });
    }

    /// <summary>
    /// Verifies that a pending acquisition is canceled when its token is canceled while the lock is held elsewhere.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_WhenTokenCanceledWhileWaiting_ShouldCancelPendingAcquisition()
    {
        var sut = new AsyncLock();
        using var cts = new CancellationTokenSource();

        using AsyncLock.Releaser held = await sut.LockAsync();
        ValueTask<AsyncLock.Releaser> pending = sut.LockAsync(cts.Token);

        cts.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await pending);
    }

    /// <summary>
    /// Verifies that canceling a pending acquisition does not consume the lock, so the next caller still acquires it.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_WhenPendingCanceled_ShouldNotConsumeLock()
    {
        var sut = new AsyncLock();
        using var cts = new CancellationTokenSource();

        AsyncLock.Releaser held = await sut.LockAsync();
        ValueTask<AsyncLock.Releaser> canceled = sut.LockAsync(cts.Token);

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceled);

        held.Dispose();

        // The lock was never taken by the canceled waiter, so this acquisition completes.
        using (await sut.LockAsync())
        {
        }
    }

    /// <summary>
    /// Verifies that acquiring a disposed lock throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void LockAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var sut = new AsyncLock();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = sut.LockAsync();
        });
    }

    /// <summary>
    /// Verifies that only one holder is active at any time when many tasks contend for the lock.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public async Task LockAsync_WhenManyTasksContend_ShouldMaintainMutualExclusion()
    {
        var sut = new AsyncLock();
        int concurrent = 0;
        int maxObserved = 0;

        async Task Worker()
        {
            for (int i = 0; i < 200; i++)
            {
                using (await sut.LockAsync())
                {
                    int current = Interlocked.Increment(ref concurrent);
                    InterlockedMax(ref maxObserved, current);
                    await Task.Yield();
                    Interlocked.Decrement(ref concurrent);
                }
            }
        }

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(Worker)));

        Assert.AreEqual(1, maxObserved, "At most one task may hold the lock at a time.");
    }
}
