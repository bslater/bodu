// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CancellableWaiterQueueTests.Enqueue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class CancellableWaiterQueueTests
{
    /// <summary>
    /// Verifies that enqueueing appends a pending waiter and counts it.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenCalled_ShouldAppendAPendingWaiter()
    {
        object gate = new();
        var sut = new CancellableWaiterQueue<int>(gate);

        LinkedListNode<TaskCompletionSource<int>> first;
        LinkedListNode<TaskCompletionSource<int>> second;
        lock (gate)
        {
            first = sut.Enqueue();
            second = sut.Enqueue();
        }

        Assert.AreEqual(2, sut.Count);
        Assert.IsFalse(first.Value.Task.IsCompleted);
        Assert.IsFalse(second.Value.Task.IsCompleted);
    }

    /// <summary>
    /// Verifies that every queued waiter's completion source runs its continuations asynchronously.
    /// </summary>
    [TestMethod]
    public void Enqueue_WhenCalled_ShouldRequestAsynchronousContinuations()
    {
        object gate = new();
        var sut = new CancellableWaiterQueue<int>(gate);

        LinkedListNode<TaskCompletionSource<int>> node;
        lock (gate)
        {
            node = sut.Enqueue();
        }

        Assert.IsTrue(node.Value.Task.CreationOptions.HasFlag(TaskCreationOptions.RunContinuationsAsynchronously));
    }
}
