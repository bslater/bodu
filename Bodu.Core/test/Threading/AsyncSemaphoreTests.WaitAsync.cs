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

        ValueTask wait = sut.WaitAsync();

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

        ValueTask pending = sut.WaitAsync();
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

        ValueTask wait = sut.WaitAsync(cts.Token);

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

        ValueTask wait = sut.WaitAsync(cts.Token);

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

        ValueTask canceled = sut.WaitAsync(cts.Token);
        ValueTask live = sut.WaitAsync();

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceled);

        sut.Release();
        await live;
    }

    /// <summary>
    /// Verifies that a wait granted a permit before its token is canceled completes, and that the permit is not
    /// returned to the semaphore.
    /// </summary>
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

    /// <summary>
    /// Verifies that a wait that must queue, with a token that cannot be canceled, allocates no more than its queued
    /// waiter: no cancellation registration and no asynchronous state machine.
    /// </summary>
    [TestMethod]
    public async Task WaitAsync_WhenQueuedWithoutCancellationToken_ShouldAllocateOnlyTheWaiter()
    {
        var sut = new AsyncSemaphore(0);

        (long bytes, Task[] pending) = ContendedWaitAllocation.BytesPerQueuedCall(() => sut.WaitAsync().AsTask());

        sut.Release(pending.Length);

        await Task.WhenAll(pending).WaitAsync(TimeSpan.FromSeconds(30));
        Assert.IsLessThanOrEqualTo(ContendedWaitAllocation.QueuedWaiterBytes, bytes, $"A queued wait allocated {bytes} bytes.");
    }

    /// <summary>
    /// Verifies that a wait that must queue with a cancelable token allocates its waiter, its registration and the
    /// queue's one asynchronous state machine, without a second state machine wrapped around it.
    /// </summary>
    [TestMethod]
    public async Task WaitAsync_WhenQueuedWithCancellationToken_ShouldNotAllocateAWrapperStateMachine()
    {
        var sut = new AsyncSemaphore(0);
        using var cts = new CancellationTokenSource();

        (long bytes, Task[] pending) = ContendedWaitAllocation.BytesPerQueuedCall(() => sut.WaitAsync(cts.Token).AsTask());

        await cts.CancelAsync();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await Task.WhenAll(pending).WaitAsync(TimeSpan.FromSeconds(30)));
        Assert.IsLessThanOrEqualTo(ContendedWaitAllocation.CancellableQueuedWaiterBytes, bytes, $"A queued wait allocated {bytes} bytes.");
    }
}
