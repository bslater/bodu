// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncAutoResetEventTests.Set.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncAutoResetEventTests
{
    /// <summary>
    /// Verifies that a single <see cref="AsyncAutoResetEvent.Set" /> releases exactly one waiter.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public async Task Set_WhenTwoWaiters_ShouldReleaseExactlyOne()
    {
        var sut = new AsyncAutoResetEvent();
        var first = sut.WaitAsync().AsTask();
        var second = sut.WaitAsync().AsTask();

        sut.Set();

        var completed = await Task.WhenAny(first, second);
        await completed;

        Assert.AreEqual(1, (first.IsCompleted ? 1 : 0) + (second.IsCompleted ? 1 : 0));
    }

    /// <summary>
    /// Verifies that a signal raised with no waiters is latched for the next waiter.
    /// </summary>
    [TestMethod]
    public async Task Set_WhenNoWaiters_ShouldLatchSignalForNextWaiter()
    {
        var sut = new AsyncAutoResetEvent();

        sut.Set();

        await sut.WaitAsync();
        Assert.IsFalse(sut.WaitAsync().IsCompleted, "The latched signal is consumed by a single waiter.");
    }

    /// <summary>
    /// Verifies that the event holds at most one pending signal, so two consecutive sets release only one waiter.
    /// </summary>
    [TestMethod]
    public async Task Set_WhenCalledTwiceWithNoWaiters_ShouldLatchSingleSignal()
    {
        var sut = new AsyncAutoResetEvent();

        sut.Set();
        sut.Set();

        await sut.WaitAsync();
        Assert.IsFalse(sut.WaitAsync().IsCompleted, "Only a single signal may be latched.");
    }

    /// <summary>
    /// Verifies that waiters are released in strict first-in, first-out order.
    /// </summary>
    [TestMethod]
    public async Task Set_WhenWaitersQueued_ShouldReleaseInFifoOrder()
    {
        var sut = new AsyncAutoResetEvent();
        var order = new List<int>();

        async Task Waiter(int id)
        {
            await sut.WaitAsync();
            lock (order)
            {
                order.Add(id);
            }
        }

        var first = Waiter(1);
        await Task.Delay(20);
        var second = Waiter(2);
        await Task.Delay(20);

        sut.Set();
        await first;
        sut.Set();
        await second;

        CollectionAssert.AreEqual(new[] { 1, 2 }, order);
    }

    /// <summary>
    /// Verifies that <see cref="AsyncAutoResetEvent.Set" /> skips a canceled waiter and releases the next live one.
    /// </summary>
    [TestMethod]
    public async Task Set_WhenFirstWaiterCanceled_ShouldReleaseNextWaiter()
    {
        var sut = new AsyncAutoResetEvent();
        using var cts = new CancellationTokenSource();

        var canceled = sut.WaitAsync(cts.Token);
        await Task.Delay(20);
        var live = sut.WaitAsync();

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceled);

        // A single Set must satisfy the live waiter, proving the canceled waiter was skipped.
        sut.Set();
        await live;
    }
}
