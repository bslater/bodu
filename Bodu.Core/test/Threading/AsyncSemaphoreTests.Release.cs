// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncSemaphoreTests.Release.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncSemaphoreTests
{
    /// <summary>
    /// Verifies that queued waiters receive permits in strict first-in, first-out order.
    /// </summary>
    /// <remarks>
    /// Permits are handed out one at a time and each waiter is awaited before the next is released, because
    /// continuations are scheduled asynchronously and their execution order is otherwise non-deterministic.
    /// </remarks>
    [TestMethod]
    public async Task Release_WhenWaitersQueued_ShouldReleaseInFifoOrder()
    {
        var sut = new AsyncSemaphore(0);
        var order = new List<int>();

        async Task Waiter(int id)
        {
            await sut.WaitAsync();
            order.Add(id);
        }

        Task first = Waiter(1);
        await Task.Delay(20);
        Task second = Waiter(2);
        await Task.Delay(20);
        Task third = Waiter(3);
        await Task.Delay(20);

        sut.Release();
        await first;
        sut.Release();
        await second;
        sut.Release();
        await third;

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, order);
    }

    /// <summary>
    /// Verifies that releasing several permits at once satisfies multiple queued waiters.
    /// </summary>
    [TestMethod]
    public async Task Release_WhenReleasingMultiplePermits_ShouldSatisfyQueuedWaiters()
    {
        var sut = new AsyncSemaphore(0);

        Task first = sut.WaitAsync().AsTask();
        Task second = sut.WaitAsync().AsTask();

        sut.Release(2);

        await Task.WhenAll(first, second);
        Assert.AreEqual(0, sut.CurrentCount);
    }

    /// <summary>
    /// Verifies that a permit released after a waiter was canceled is handed to the next live waiter rather than the
    /// canceled one.
    /// </summary>
    [TestMethod]
    public async Task Release_WhenWaiterCanceled_ShouldSkipCanceledWaiter()
    {
        var sut = new AsyncSemaphore(0);
        using var cts = new CancellationTokenSource();

        ValueTask canceled = sut.WaitAsync(cts.Token);
        ValueTask live = sut.WaitAsync();

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceled);

        // A single release must satisfy the live waiter, proving the canceled waiter did not consume the permit.
        sut.Release();
        await live;
        Assert.AreEqual(0, sut.CurrentCount);
    }

    /// <summary>
    /// Verifies that releasing without queued waiters raises the available permit count.
    /// </summary>
    [TestMethod]
    public void Release_WhenNoWaiters_ShouldIncrementCount()
    {
        var sut = new AsyncSemaphore(0, 5);

        sut.Release(3);

        Assert.AreEqual(3, sut.CurrentCount);
    }

    /// <summary>
    /// Verifies that releasing beyond the configured maximum throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Release_WhenExceedingMaxCount_ShouldThrowInvalidOperationException()
    {
        var sut = new AsyncSemaphore(1, 1);

        Assert.ThrowsExactly<InvalidOperationException>(sut.Release);
    }

    /// <summary>
    /// Verifies that a release exceeding the configured maximum is atomic: it throws
    /// <see cref="InvalidOperationException" /> without granting any queued waiter and without changing
    /// <see cref="AsyncSemaphore.CurrentCount" />.
    /// </summary>
    [TestMethod]
    public async Task Release_WhenExceedingMaxCountWithWaiterQueued_ShouldThrowWithoutGrantingWaiter()
    {
        var sut = new AsyncSemaphore(0, 1);
        ValueTask waiter = sut.WaitAsync();

        // One waiter absorbs one permit; the remaining three cannot be stored under maxCount = 1.
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut.Release(4);
        });

        Assert.AreEqual(0, sut.CurrentCount);
        Assert.IsFalse(waiter.IsCompleted);

        // The waiter is still queued and is granted by a subsequent valid release.
        sut.Release();
        await waiter;
        Assert.AreEqual(0, sut.CurrentCount);
    }

    /// <summary>
    /// Verifies that a non-positive release count throws <see cref="ArgumentOutOfRangeException" /> naming <c>releaseCount</c>.
    /// </summary>
    [TestMethod]
    public void Release_WhenReleaseCountIsZero_ShouldThrowForReleaseCount()
    {
        var sut = new AsyncSemaphore(0);

        Assert.AreEqual(
            "releaseCount",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => sut.Release(0)).ParamName);
    }
}
