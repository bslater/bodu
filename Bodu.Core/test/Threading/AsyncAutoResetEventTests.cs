// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncAutoResetEventTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

/// <summary>
/// Contains unit tests for the <see cref="AsyncAutoResetEvent" /> type.
/// </summary>
[TestClass]
public sealed class AsyncAutoResetEventTests
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
    /// Verifies that an event constructed in the signaled state immediately satisfies the first waiter.
    /// </summary>
    [TestMethod]
    public void WaitAsync_WhenInitiallySignaled_ShouldCompleteSynchronously()
    {
        var sut = new AsyncAutoResetEvent(initialState: true);

        Assert.IsTrue(sut.WaitAsync().IsCompleted);
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
    /// Verifies that canceling a waiter throws without consuming a later signal.
    /// </summary>
    [TestMethod]
    public async Task WaitAsync_WhenCanceled_ShouldNotConsumeSignal()
    {
        var sut = new AsyncAutoResetEvent();
        using var cts = new CancellationTokenSource();

        var canceled = sut.WaitAsync(cts.Token);
        var live = sut.WaitAsync();

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceled);

        sut.Set();
        await live;
    }
}
