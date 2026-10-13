// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CancellableWaiterQueue{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

/// <summary>
/// Shared FIFO queue and cancellation lifetime for contended asynchronous synchronization primitives. The owning
/// primitive retains responsibility for its own permit, ownership, signal and fairness policies.
/// </summary>
/// <remarks>
/// Enqueue, Count, TryGrant and Drain must be called with the owner's gate held. Cancellation acquires that same gate
/// before unlinking, and completes the cancellation outside it. Grant runs under the gate and uses asynchronous
/// continuations so a successful grant always wins over a concurrent cancellation. The caller owns any policy action
/// supplied to cancellation (such as waking readers after removal of the last queued writer); an owner that passes one
/// on every wait should cache the delegate rather than create it per call. A wait whose token cannot be canceled is not
/// registered for cancellation at all, and completes with the waiter's own task.
/// </remarks>
/// <typeparam name="TResult">The result supplied to a queued waiter when it is granted access.</typeparam>
internal sealed class CancellableWaiterQueue<TResult>
{
    /// <summary>The owning primitive's synchronization object, used when canceling queued waiters.</summary>
    private readonly object _gate;

    /// <summary>The pending waiters, held in first-in, first-out order.</summary>
    private readonly LinkedList<TaskCompletionSource<TResult>> _queue = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CancellableWaiterQueue{TResult}" /> class using the owner's gate.
    /// </summary>
    /// <param name="gate">The synchronization object shared with the owning primitive.</param>
    internal CancellableWaiterQueue(object gate) =>
        _gate = gate;

    /// <summary>
    /// Gets the number of waiters currently in the queue.
    /// </summary>
    /// <value>The number of pending, linked waiters.</value>
    internal int Count => _queue.Count;

    /// <summary>
    /// Appends a new asynchronous waiter to the queue.
    /// </summary>
    /// <returns>The queue node representing the new waiter.</returns>
    /// <remarks>
    /// The owning primitive must hold its gate while calling this method.
    /// </remarks>
    internal LinkedListNode<TaskCompletionSource<TResult>> Enqueue() =>
        _queue.AddLast(new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously));

    /// <summary>
    /// Attempts to complete the first pending waiter with a supplied value.
    /// </summary>
    /// <param name="value">The result to grant to the next pending waiter.</param>
    /// <returns><see langword="true" /> if a waiter accepted the result; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// The owning primitive must hold its gate while calling this method. Every waiter still linked accepts the grant,
    /// because cancellation and <see cref="Drain" /> unlink a waiter before completing it, so a value built for a
    /// non-empty queue is never discarded.
    /// </remarks>
    internal bool TryGrant(TResult value)
    {
        while (_queue.First is { } first)
        {
            _queue.RemoveFirst();
            if (first.Value.TrySetResult(value))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Removes all queued waiters and returns their completion sources.
    /// </summary>
    /// <returns>The removed waiter completion sources, in queue order.</returns>
    /// <remarks>
    /// The owning primitive must hold its gate while calling this method.
    /// </remarks>
    internal List<TaskCompletionSource<TResult>> Drain()
    {
        var pending = new List<TaskCompletionSource<TResult>>(_queue);
        _queue.Clear();
        return pending;
    }

    /// <summary>
    /// Removes a waiter from the queue, if still linked, and attempts to cancel its task.
    /// </summary>
    /// <param name="node">The queue node representing the waiter.</param>
    /// <param name="cancellationToken">The token associated with the cancellation.</param>
    /// <param name="afterUnlinkUnderLock">An optional callback invoked while the owner's gate is held.</param>
    /// <remarks>
    /// The task is canceled after the gate is released. A waiter that has already received its result remains
    /// completed; cancellation does not replace a successful grant.
    /// </remarks>
    internal void Cancel(
        LinkedListNode<TaskCompletionSource<TResult>> node,
        CancellationToken cancellationToken,
        Action? afterUnlinkUnderLock = null)
    {
        lock (_gate)
        {
            if (node.List is not null)
                _queue.Remove(node);

            afterUnlinkUnderLock?.Invoke();
        }

        node.Value.TrySetCanceled(cancellationToken);
    }

    /// <summary>
    /// Asynchronously waits for a queued waiter to be granted or canceled.
    /// </summary>
    /// <param name="node">The queue node representing the waiter.</param>
    /// <param name="cancellationToken">The token used to cancel the pending wait.</param>
    /// <param name="afterUnlinkUnderLock">
    /// An optional callback invoked with the owner's gate held during cancellation.
    /// </param>
    /// <returns>A task-like value whose result is the grant supplied to the waiter.</returns>
    /// <remarks>
    /// A token that cannot be canceled is not registered: the result is the waiter's own task, with no state machine
    /// and no registration allocated. Otherwise the registration is disposed after the wait completes. A successful
    /// grant is not replaced by a subsequent cancellation.
    /// </remarks>
    internal ValueTask<TResult> AwaitAsync(
        LinkedListNode<TaskCompletionSource<TResult>> node,
        CancellationToken cancellationToken,
        Action? afterUnlinkUnderLock = null) =>
        cancellationToken.CanBeCanceled
            ? AwaitCancellableAsync(node, afterUnlinkUnderLock, cancellationToken)
            : new ValueTask<TResult>(node.Value.Task);

    /// <summary>
    /// Asynchronously waits for a queued waiter to be granted or cancelled through a token that can be canceled.
    /// </summary>
    /// <param name="node">The queue node representing the waiter.</param>
    /// <param name="afterUnlinkUnderLock">
    /// An optional callback invoked with the owner's gate held during cancellation.
    /// </param>
    /// <param name="cancellationToken">The token used to cancel the pending wait.</param>
    /// <returns>A task-like value whose result is the grant supplied to the waiter.</returns>
    private async ValueTask<TResult> AwaitCancellableAsync(
        LinkedListNode<TaskCompletionSource<TResult>> node,
        Action? afterUnlinkUnderLock,
        CancellationToken cancellationToken)
    {
        using (cancellationToken.Register(
            static state =>
            {
                var (queue, waiter, token, afterUnlink) =
                    ((CancellableWaiterQueue<TResult> Queue,
                      LinkedListNode<TaskCompletionSource<TResult>> Node,
                      CancellationToken Token,
                      Action? AfterUnlink))state!;
                queue.Cancel(waiter, token, afterUnlink);
            },
            (this, node, cancellationToken, afterUnlinkUnderLock)))
        {
            // The task belongs to this synchronisation primitive; no foreign-task deadlock is possible.
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
            return await node.Value.Task.ConfigureAwait(false);
#pragma warning restore VSTHRD003
        }
    }
}
