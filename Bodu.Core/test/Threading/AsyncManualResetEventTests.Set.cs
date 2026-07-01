// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncManualResetEventTests.Set.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncManualResetEventTests
{
    /// <summary>
    /// Verifies that setting the event releases every outstanding waiter.
    /// </summary>
    [TestMethod]
    public async Task Set_WhenMultipleWaiters_ShouldReleaseAll()
    {
        var sut = new AsyncManualResetEvent();
        Task[] waiters = Enumerable.Range(0, 5).Select(_ => sut.WaitAsync().AsTask()).ToArray();

        sut.Set();

        await Task.WhenAll(waiters);
    }

    /// <summary>
    /// Verifies that setting an already-set event is an idempotent no-op that leaves the event signaled.
    /// </summary>
    [TestMethod]
    public void Set_WhenAlreadySet_ShouldRemainSet()
    {
        var sut = new AsyncManualResetEvent(initialState: true);

        sut.Set();

        Assert.IsTrue(sut.IsSet);
        Assert.IsTrue(sut.WaitAsync().IsCompleted);
    }
}
