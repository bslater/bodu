// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncSemaphoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

/// <summary>
/// Contains unit tests for the <see cref="AsyncSemaphore" /> type.
/// </summary>
[TestClass]
public sealed class AsyncSemaphoreTests
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

        var first = Waiter(1);
        await Task.Delay(20);
        var second = Waiter(2);
        await Task.Delay(20);
        var third = Waiter(3);
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
    /// Verifies that releasing beyond the configured maximum throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Release_WhenExceedingMaxCount_ShouldThrowInvalidOperationException()
    {
        var sut = new AsyncSemaphore(1, 1);

        Assert.ThrowsExactly<InvalidOperationException>(() => sut.Release());
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

    /// <summary>
    /// Verifies that a negative initial count throws <see cref="ArgumentOutOfRangeException" /> naming <c>initialCount</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInitialCountIsNegative_ShouldThrowForInitialCount()
    {
        Assert.AreEqual(
            "initialCount",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AsyncSemaphore(-1)).ParamName);
    }

    /// <summary>
    /// Verifies that an initial count greater than the maximum throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInitialCountExceedsMax_ShouldThrowForInitialCount()
    {
        Assert.AreEqual(
            "initialCount",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AsyncSemaphore(5, 2)).ParamName);
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
