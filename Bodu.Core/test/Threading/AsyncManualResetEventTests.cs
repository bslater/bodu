// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncManualResetEventTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

/// <summary>
/// Contains unit tests for the <see cref="AsyncManualResetEvent" /> type.
/// </summary>
[TestClass]
public sealed class AsyncManualResetEventTests
{
    /// <summary>
    /// Verifies that setting the event releases a pending waiter.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public async Task WaitAsync_WhenSet_ShouldComplete()
    {
        var sut = new AsyncManualResetEvent();

        var pending = sut.WaitAsync();
        Assert.IsFalse(pending.IsCompleted);

        sut.Set();
        await pending;

        Assert.IsTrue(sut.IsSet);
    }

    /// <summary>
    /// Verifies that a wait completes synchronously when the event is already set.
    /// </summary>
    [TestMethod]
    public void WaitAsync_WhenAlreadySet_ShouldCompleteSynchronously()
    {
        var sut = new AsyncManualResetEvent(initialState: true);

        Assert.IsTrue(sut.WaitAsync().IsCompleted);
    }

    /// <summary>
    /// Verifies that setting the event releases every outstanding waiter.
    /// </summary>
    [TestMethod]
    public async Task Set_WhenMultipleWaiters_ShouldReleaseAll()
    {
        var sut = new AsyncManualResetEvent();
        var waiters = Enumerable.Range(0, 5).Select(_ => sut.WaitAsync().AsTask()).ToArray();

        sut.Set();

        await Task.WhenAll(waiters);
    }

    /// <summary>
    /// Verifies that resetting a set event causes subsequent waits to block again.
    /// </summary>
    [TestMethod]
    public void Reset_WhenSet_ShouldReArmTheGate()
    {
        var sut = new AsyncManualResetEvent(initialState: true);

        sut.Reset();

        Assert.IsFalse(sut.IsSet);
        Assert.IsFalse(sut.WaitAsync().IsCompleted);
    }

    /// <summary>
    /// Verifies that canceling a waiter throws without disturbing other waiters.
    /// </summary>
    [TestMethod]
    public async Task WaitAsync_WhenCanceled_ShouldCancelOnlyThatWaiter()
    {
        var sut = new AsyncManualResetEvent();
        using var cts = new CancellationTokenSource();

        var canceled = sut.WaitAsync(cts.Token);
        var live = sut.WaitAsync();

        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceled);

        sut.Set();
        await live;
    }
}
