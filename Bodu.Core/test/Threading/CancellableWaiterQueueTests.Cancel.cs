// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CancellableWaiterQueueTests.Cancel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class CancellableWaiterQueueTests
{
    /// <summary>
    /// Verifies that cancelling a queued waiter unlinks it and cancels its task with the supplied token.
    /// </summary>
    [TestMethod]
    public void Cancel_WhenWaiterQueued_ShouldUnlinkAndCancelIt()
    {
        object gate = new();
        var sut = new CancellableWaiterQueue<int>(gate);
        using var cts = new CancellationTokenSource();
        LinkedListNode<TaskCompletionSource<int>> node;
        lock (gate)
        {
            node = sut.Enqueue();
        }

        sut.Cancel(node, cts.Token);

        Assert.AreEqual(0, sut.Count);
        Assert.IsNull(node.List);
        Assert.IsTrue(node.Value.Task.IsCanceled);
        var ex = Assert.ThrowsExactly<TaskCanceledException>(() =>
        {
            node.Value.Task.GetAwaiter().GetResult();
        });
        Assert.AreEqual(cts.Token, ex.CancellationToken);
    }

    /// <summary>
    /// Verifies that cancelling a waiter that has already been granted leaves the grant in place.
    /// </summary>
    [TestMethod]
    public void Cancel_WhenWaiterAlreadyGranted_ShouldKeepTheGrant()
    {
        object gate = new();
        var sut = new CancellableWaiterQueue<int>(gate);
        LinkedListNode<TaskCompletionSource<int>> node;
        lock (gate)
        {
            node = sut.Enqueue();
            Assert.IsTrue(sut.TryGrant(5));
        }

        sut.Cancel(node, new CancellationToken(canceled: true));

        Assert.IsTrue(node.Value.Task.IsCompletedSuccessfully);
        Assert.AreEqual(5, node.Value.Task.Result);
    }

    /// <summary>
    /// Verifies that the after-unlink callback runs with the owner's gate held, after the waiter has been unlinked.
    /// </summary>
    [TestMethod]
    public void Cancel_WhenCallbackSupplied_ShouldInvokeItUnderTheGateAfterUnlinking()
    {
        object gate = new();
        var sut = new CancellableWaiterQueue<int>(gate);
        LinkedListNode<TaskCompletionSource<int>> node;
        lock (gate)
        {
            node = sut.Enqueue();
        }

        bool heldGate = false;
        int countSeen = -1;
        sut.Cancel(node, CancellationToken.None, () =>
        {
            heldGate = Monitor.IsEntered(gate);
            countSeen = sut.Count;
        });

        Assert.IsTrue(heldGate);
        Assert.AreEqual(0, countSeen);
    }

    /// <summary>
    /// Verifies that the waiter's task is cancelled only after the owner's gate has been released, so its
    /// continuations can never observe the gate held by the cancelling thread.
    /// </summary>
    [TestMethod]
    public void Cancel_WhenCalled_ShouldCompleteTheTaskOutsideTheGate()
    {
        object gate = new();
        var sut = new CancellableWaiterQueue<int>(gate);
        LinkedListNode<TaskCompletionSource<int>> node;
        lock (gate)
        {
            node = sut.Enqueue();
        }

        bool completedInsideCallback = true;
        sut.Cancel(node, CancellationToken.None, () => completedInsideCallback = node.Value.Task.IsCompleted);

        Assert.IsFalse(completedInsideCallback);
        Assert.IsTrue(node.Value.Task.IsCanceled);
    }
}
