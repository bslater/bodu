// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CancellableWaiterQueueTests.Drain.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class CancellableWaiterQueueTests
{
    /// <summary>
    /// Verifies that draining returns every queued waiter in queue order, still pending, and empties the queue.
    /// </summary>
    [TestMethod]
    public void Drain_WhenWaitersQueued_ShouldReturnThemInOrderAndEmptyTheQueue()
    {
        object gate = new();
        var sut = new CancellableWaiterQueue<int>(gate);

        lock (gate)
        {
            LinkedListNode<TaskCompletionSource<int>> first = sut.Enqueue();
            LinkedListNode<TaskCompletionSource<int>> second = sut.Enqueue();

            List<TaskCompletionSource<int>> drained = sut.Drain();

            CollectionAssert.AreEqual(new[] { first.Value, second.Value }, drained);
            Assert.AreEqual(0, sut.Count);
            Assert.IsFalse(first.Value.Task.IsCompleted);
            Assert.IsNull(first.List);
        }
    }

    /// <summary>
    /// Verifies that a drain racing a cancellation of the same waiter settles it exactly one way: faulted by the
    /// drain's owner or canceled.
    /// </summary>
    [TestMethod]
    public async Task Drain_WhenRacingCancel_ShouldSettleEachWaiterOnce()
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
            Task drain = Task.Run(() =>
            {
                start.SignalAndWait();
                List<TaskCompletionSource<int>> drained;
                lock (gate)
                {
                    drained = sut.Drain();
                }

                foreach (TaskCompletionSource<int> waiter in drained)
                    waiter.TrySetException(new ObjectDisposedException("owner"));
            });
            Task cancel = Task.Run(() =>
            {
                start.SignalAndWait();
                sut.Cancel(node, cts.Token);
            });

            await Task.WhenAll(drain, cancel).WaitAsync(Timeout);

            Task<int> task = node.Value.Task;
            Assert.IsTrue(task.IsFaulted || task.IsCanceled, $"iteration {i}");
            Assert.AreEqual(0, sut.Count);
        }
    }
}
