// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CancellableWaiterQueueTests.TryGrant.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class CancellableWaiterQueueTests
{
    /// <summary>
    /// Verifies that granting completes the oldest waiter first and unlinks it.
    /// </summary>
    [TestMethod]
    public void TryGrant_WhenWaitersQueued_ShouldGrantTheOldestFirst()
    {
        object gate = new();
        var sut = new CancellableWaiterQueue<int>(gate);

        lock (gate)
        {
            LinkedListNode<TaskCompletionSource<int>> first = sut.Enqueue();
            LinkedListNode<TaskCompletionSource<int>> second = sut.Enqueue();

            Assert.IsTrue(sut.TryGrant(7));

            Assert.AreEqual(7, first.Value.Task.Result);
            Assert.IsNull(first.List);
            Assert.IsFalse(second.Value.Task.IsCompleted);
            Assert.AreEqual(1, sut.Count);
        }
    }

    /// <summary>
    /// Verifies that granting with no waiter queued reports that nothing accepted the grant.
    /// </summary>
    [TestMethod]
    public void TryGrant_WhenQueueIsEmpty_ShouldReturnFalse()
    {
        object gate = new();
        var sut = new CancellableWaiterQueue<int>(gate);

        lock (gate)
        {
            Assert.IsFalse(sut.TryGrant(7));
        }
    }

    /// <summary>
    /// Verifies that a grant never runs the waiter's continuation on the granting thread while the grant is in
    /// progress, even when the continuation asks to run synchronously, so a grant made under the owner's gate never
    /// runs caller code under it.
    /// </summary>
    [TestMethod]
    public async Task TryGrant_WhenGranted_ShouldNotRunTheContinuationInline()
    {
        object gate = new();
        var sut = new CancellableWaiterQueue<int>(gate);
        LinkedListNode<TaskCompletionSource<int>> node;
        lock (gate)
        {
            node = sut.Enqueue();
        }

        int grantingThread = Environment.CurrentManagedThreadId;
        bool granting = false;
        bool ranInline = false;
        Task continuation = node.Value.Task.ContinueWith(
            _ => ranInline = Volatile.Read(ref granting) && Environment.CurrentManagedThreadId == grantingThread,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        lock (gate)
        {
            Volatile.Write(ref granting, true);
            Assert.IsTrue(sut.TryGrant(1));
            Volatile.Write(ref granting, false);
        }

        await continuation.WaitAsync(Timeout);

        Assert.IsFalse(ranInline);
    }

    /// <summary>
    /// Verifies that a grant racing a cancellation of the same waiter settles it exactly one way: either the grant is
    /// accepted and the waiter completes with it, or the grant is refused and the waiter is canceled.
    /// </summary>
    [TestMethod]
    public async Task TryGrant_WhenRacingCancel_ShouldSettleEachWaiterOnce()
    {
        object gate = new();
        var sut = new CancellableWaiterQueue<int>(gate);
        using var cts = new CancellationTokenSource();

        for (int i = 0; i < 500; i++)
        {
            LinkedListNode<TaskCompletionSource<int>> node;
            lock (gate)
            {
                node = sut.Enqueue();
            }

            using var start = new Barrier(2);
            Task<bool> grant = Task.Run(() =>
            {
                start.SignalAndWait();
                lock (gate)
                {
                    return sut.TryGrant(i);
                }
            });
            Task cancel = Task.Run(() =>
            {
                start.SignalAndWait();
                sut.Cancel(node, cts.Token);
            });

            await Task.WhenAll(grant, cancel).WaitAsync(Timeout);

            Task<int> waiter = node.Value.Task;
            if (grant.Result)
            {
                Assert.IsTrue(waiter.IsCompletedSuccessfully, $"iteration {i}");
                Assert.AreEqual(i, waiter.Result);
            }
            else
            {
                Assert.IsTrue(waiter.IsCanceled, $"iteration {i}");
            }

            Assert.AreEqual(0, sut.Count);
        }
    }
}
