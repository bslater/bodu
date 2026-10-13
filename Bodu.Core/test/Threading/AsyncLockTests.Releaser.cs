// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLockTests.Releaser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncLockTests
{
    /// <summary>
    /// Verifies that disposing a default <see cref="AsyncLock.Releaser" /> (one with no owner) is a harmless no-op.
    /// </summary>
    [TestMethod]
    public void Releaser_WhenDefault_ShouldDisposeWithoutThrowing()
    {
        var releaser = default(AsyncLock.Releaser);

        releaser.Dispose();
    }

    /// <summary>
    /// Verifies that disposing the releaser returned by <see cref="AsyncLock.LockAsync()" /> releases the lock so it
    /// can be reacquired.
    /// </summary>
    [TestMethod]
    public async Task Releaser_WhenDisposed_ShouldReleaseLock()
    {
        var sut = new AsyncLock();

        AsyncLock.Releaser releaser = await sut.LockAsync();
        ValueTask<AsyncLock.Releaser> contended = sut.LockAsync();
        Assert.IsFalse(contended.IsCompleted);

        releaser.Dispose();

        using (await contended)
        {
        }
    }

    /// <summary>
    /// Verifies that disposing the releaser while a waiter is queued hands the lock over without allocating.
    /// </summary>
    [TestMethod]
    public async Task Releaser_WhenHandingTheLockToAQueuedWaiter_ShouldNotAllocate()
    {
        var sut = new AsyncLock();
        AsyncLock.Releaser holder = await sut.LockAsync();

        long bytes = ContendedWaitAllocation.BytesPerHandOff(
            () => sut.LockAsync().AsTask(),
            () => holder.Dispose(),
            granted => holder = granted);

        holder.Dispose();
        Assert.IsLessThanOrEqualTo(ContendedWaitAllocation.HandOffBytes, bytes, $"A hand-off allocated {bytes} bytes.");
    }
}
