// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncAutoResetEvent.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Threading;

/// <summary>
/// Provides an asynchronous auto-reset signaling primitive: each call to <see cref="Set" /> releases exactly one waiter
/// and then automatically returns to the unsignaled state.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AsyncAutoResetEvent" /> is the asynchronous analogue of <see cref="AutoResetEvent" />. When a waiter is
/// queued, <see cref="Set" /> releases the longest-waiting caller (strict FIFO) and consumes the signal. When no waiter
/// is queued, the signal is latched so that the next <see cref="WaitAsync()" /> completes immediately; the event holds
/// at most one pending signal.
/// </para>
/// <para>
/// Waiters are tracked in a FIFO queue of <see cref="TaskCompletionSource{TResult}" /> instances created with
/// <see cref="TaskCreationOptions.RunContinuationsAsynchronously" />, so the thread calling <see cref="Set" /> never
/// runs a waiter's continuation inline. The type owns no operating-system handle and does not implement
/// <see cref="IDisposable" />.
/// </para>
/// <para>
/// Cancellation follows the package-wide rule: a signal that is already latched is consumed even when the supplied
/// token is already canceled. A token only cancels a wait that cannot complete immediately.
/// </para>
/// </remarks>
[DebuggerDisplay("Signaled = {_signaled}, Waiters = {WaiterCount}")]
public sealed class AsyncAutoResetEvent
{
    private readonly object _gate = new();
    private readonly LinkedList<TaskCompletionSource<bool>> _waiters = new();
    private bool _signaled;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncAutoResetEvent" /> class in the unsignaled state.
    /// </summary>
    public AsyncAutoResetEvent()
        : this(false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncAutoResetEvent" /> class.
    /// </summary>
    /// <param name="initialState">
    /// <see langword="true" /> to create the event with a pending signal; otherwise, <see langword="false" />.
    /// </param>
    public AsyncAutoResetEvent(bool initialState)
    {
        _signaled = initialState;
    }

    /// <summary>
    /// Gets the number of callers currently queued waiting for a signal.
    /// </summary>
    /// <value>The number of queued waiters.</value>
    internal int WaiterCount
    {
        get
        {
            lock (_gate)
            {
                return _waiters.Count;
            }
        }
    }

    /// <summary>
    /// Asynchronously waits for the event to be signaled, consuming the signal.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> that completes when this caller receives the signal.</returns>
    /// <remarks>
    /// The returned <see cref="ValueTask" /> must be awaited exactly once.
    /// </remarks>
    public ValueTask WaitAsync() =>
        WaitAsync(CancellationToken.None);

    /// <summary>
    /// Asynchronously waits for the event to be signaled, consuming the signal and observing a cancellation request
    /// while waiting.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the pending wait.</param>
    /// <returns>A <see cref="ValueTask" /> that completes when this caller receives the signal.</returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was canceled before the signal was received.
    /// </exception>
    /// <remarks>
    /// The returned <see cref="ValueTask" /> must be awaited exactly once. A latched signal is consumed even when
    /// <paramref name="cancellationToken" /> is already canceled; the token only cancels a wait that must queue, and
    /// cancellation removes only the calling waiter from the queue.
    /// </remarks>
    public ValueTask WaitAsync(CancellationToken cancellationToken)
    {
        LinkedListNode<TaskCompletionSource<bool>> node;
        lock (_gate)
        {
            // Success wins: a latched signal is consumed before an already-canceled token is honored.
            if (_signaled)
            {
                _signaled = false;
                return ValueTask.CompletedTask;
            }

            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            node = _waiters.AddLast(new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
        }

        return AwaitWaiterAsync(node, cancellationToken);
    }

    /// <summary>
    /// Signals the event, releasing exactly one waiter or latching the signal when no waiter is queued.
    /// </summary>
    public void Set()
    {
        lock (_gate)
        {
            // Release the longest-waiting caller, skipping any whose task was already canceled.
            while (_waiters.First is { } first)
            {
                _waiters.RemoveFirst();
                if (first.Value.TrySetResult(true))
                    return;
            }

            // No waiter was available; latch the signal for the next caller.
            _signaled = true;
        }
    }

    /// <summary>
    /// Completes a contended wait, disposing the cancellation registration when the wait resolves.
    /// </summary>
    /// <param name="node">The queued waiter to observe.</param>
    /// <param name="cancellationToken">A token used to cancel the pending wait.</param>
    /// <returns>A <see cref="ValueTask" /> that completes when the signal is received.</returns>
    private async ValueTask AwaitWaiterAsync(LinkedListNode<TaskCompletionSource<bool>> node, CancellationToken cancellationToken)
    {
        using (cancellationToken.Register(static state =>
        {
            var (owner, waiter, token) = ((AsyncAutoResetEvent Owner, LinkedListNode<TaskCompletionSource<bool>> Node, CancellationToken Token))state!;
            owner.CancelWaiter(waiter, token);
        }, (this, node, cancellationToken)))
        {
            await node.Value.Task.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Removes a canceled waiter from the queue and transitions its task to the canceled state.
    /// </summary>
    /// <param name="node">The waiter to cancel.</param>
    /// <param name="cancellationToken">The token whose cancellation triggered the removal.</param>
    private void CancelWaiter(LinkedListNode<TaskCompletionSource<bool>> node, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (node.List is not null)
                _waiters.Remove(node);
        }

        node.Value.TrySetCanceled(cancellationToken);
    }
}
