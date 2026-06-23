// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLockTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

/// <summary>
/// Contains unit tests for the <see cref="AsyncLock" /> type.
/// </summary>
[TestClass]
public sealed class AsyncLockTests
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
        var releaser = await sut.LockAsync();

        var contended = sut.LockAsync();
        Assert.IsFalse(contended.IsCompleted, "The second acquisition must not complete while the lock is held.");

        releaser.Dispose();

        using (await contended)
        {
        }
    }

    /// <summary>
    /// Verifies that only one holder is active at any time when many tasks contend for the lock.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public async Task LockAsync_WhenManyTasksContend_ShouldMaintainMutualExclusion()
    {
        var sut = new AsyncLock();
        var concurrent = 0;
        var maxObserved = 0;

        async Task Worker()
        {
            for (var i = 0; i < 200; i++)
            {
                using (await sut.LockAsync())
                {
                    var current = Interlocked.Increment(ref concurrent);
                    InterlockedMax(ref maxObserved, current);
                    await Task.Yield();
                    Interlocked.Decrement(ref concurrent);
                }
            }
        }

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(Worker)));

        Assert.AreEqual(1, maxObserved, "At most one task may hold the lock at a time.");
    }

    /// <summary>
    /// Verifies that requesting the lock with an already-canceled token throws <see cref="OperationCanceledException" />.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_WhenTokenAlreadyCanceled_ShouldThrowOperationCanceled()
    {
        var sut = new AsyncLock();
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

        using var held = await sut.LockAsync();
        var pending = sut.LockAsync(cts.Token);

        cts.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await pending);
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
    /// Verifies that disposing the lock twice does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var sut = new AsyncLock();

        sut.Dispose();
        sut.Dispose();
    }

    private static void InterlockedMax(ref int target, int value)
    {
        var current = Volatile.Read(ref target);
        while (value > current)
        {
            var prior = Interlocked.CompareExchange(ref target, value, current);
            if (prior == current)
                return;

            current = prior;
        }
    }
}
