// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CancellableWaiterQueueTests.AwaitAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class CancellableWaiterQueueTests
{
    /// <summary>
    /// Verifies that awaiting a queued waiter yields the value it is granted.
    /// </summary>
    [TestMethod]
    public async Task AwaitAsync_WhenGranted_ShouldReturnTheGrant()
    {
        object gate = new();
        var sut = new CancellableWaiterQueue<int>(gate);
        using var cts = new CancellationTokenSource();
        LinkedListNode<TaskCompletionSource<int>> node;
        lock (gate)
        {
            node = sut.Enqueue();
        }

        Task<int> wait = sut.AwaitAsync(node, cts.Token).AsTask();
        lock (gate)
        {
            Assert.IsTrue(sut.TryGrant(42));
        }

        Assert.AreEqual(42, await wait.WaitAsync(Timeout));
    }

    /// <summary>
    /// Verifies that awaiting without a cancellable token still yields the value the waiter is granted.
    /// </summary>
    [TestMethod]
    public async Task AwaitAsync_WhenGrantedWithoutCancellationToken_ShouldReturnTheGrant()
    {
        object gate = new();
        var sut = new CancellableWaiterQueue<int>(gate);
        LinkedListNode<TaskCompletionSource<int>> node;
        lock (gate)
        {
            node = sut.Enqueue();
        }

        Task<int> wait = sut.AwaitAsync(node, CancellationToken.None).AsTask();
        lock (gate)
        {
            Assert.IsTrue(sut.TryGrant(42));
        }

        Assert.AreEqual(42, await wait.WaitAsync(Timeout));
    }

    /// <summary>
    /// Verifies that cancelling the token of a pending wait unlinks the waiter, runs the after-unlink callback under
    /// the gate, and surfaces <see cref="TaskCanceledException" /> carrying the token.
    /// </summary>
    [TestMethod]
    public async Task AwaitAsync_WhenTokenCanceled_ShouldUnlinkRunTheCallbackAndThrow()
    {
        object gate = new();
        var sut = new CancellableWaiterQueue<int>(gate);
        using var cts = new CancellationTokenSource();
        LinkedListNode<TaskCompletionSource<int>> node;
        lock (gate)
        {
            node = sut.Enqueue();
        }

        bool callbackHeldGate = false;
        Task<int> wait = sut.AwaitAsync(node, cts.Token, () => callbackHeldGate = Monitor.IsEntered(gate)).AsTask();

        cts.Cancel();

        var ex = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            _ = await wait.WaitAsync(Timeout);
        });
        Assert.AreEqual(cts.Token, ex.CancellationToken);
        Assert.IsTrue(callbackHeldGate);
        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that a token cancelled after the waiter has been granted does not replace the grant.
    /// </summary>
    [TestMethod]
    public async Task AwaitAsync_WhenTokenCanceledAfterGrant_ShouldReturnTheGrant()
    {
        object gate = new();
        var sut = new CancellableWaiterQueue<int>(gate);
        using var cts = new CancellationTokenSource();
        LinkedListNode<TaskCompletionSource<int>> node;
        lock (gate)
        {
            node = sut.Enqueue();
        }

        Task<int> wait = sut.AwaitAsync(node, cts.Token).AsTask();
        lock (gate)
        {
            Assert.IsTrue(sut.TryGrant(9));
        }

        cts.Cancel();

        Assert.AreEqual(9, await wait.WaitAsync(Timeout));
    }
}
