// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Threading;

/// <summary>
/// Provides an asynchronous, non-reentrant mutual-exclusion primitive whose acquisition can be awaited without blocking
/// a thread.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AsyncLock" /> is the asynchronous analogue of the C# <c>lock</c> statement. Because a held lock may need
/// to span an <c>await</c>, the lock is acquired with <see cref="LockAsync()" /> and released by disposing the
/// <see cref="Releaser" /> it returns, typically with a <c>using</c> statement.
/// </para>
/// <para>
/// The lock owns an explicit FIFO wait queue rather than delegating to <see cref="SemaphoreSlim" />. When the lock is
/// free, acquisition completes synchronously and allocates nothing; only a contended acquisition allocates the awaited
/// task. Waiters are granted ownership in <b>strict first-in, first-out order</b>. Each waiter completion source uses
/// <see cref="TaskCreationOptions.RunContinuationsAsynchronously" />, so the releasing thread never runs a waiter's
/// continuation inline while holding the internal gate.
/// </para>
/// <para>
/// The lock is <b>not reentrant</b>: a caller that already holds the lock and attempts to acquire it again on the same
/// logical flow will deadlock. Following the package-wide rule, a free lock is acquired even when the supplied token is
/// already canceled; the token only cancels an acquisition that must queue.
/// </para>
/// <para>
/// <see cref="Dispose" /> faults every still-waiting acquisition with <see cref="ObjectDisposedException" /> and
/// rejects subsequent <see cref="LockAsync()" /> calls. Disposing is intended for shutdown; dispose only when no
/// further acquisitions are expected. Releasing a holder's <see cref="Releaser" /> after disposal is a harmless no-op.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// private readonly AsyncLock _mutex = new();
///
/// public async Task UpdateAsync()
/// {
///     using (await _mutex.LockAsync())
///     {
///         // Exclusive section; safe to await here.
///         await SomeOperationAsync();
///     }
/// }
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Held = {_held}, Waiters = {WaiterCount}")]
public sealed partial class AsyncLock
    : IDisposable
{
    /// <summary>The synchronization object guarding the waiter queue and lock state.</summary>
    private readonly object _gate = new();

    /// <summary>The queue of pending acquirers, granted the lock in FIFO order as it is released.</summary>
    private readonly CancellableWaiterQueue<Releaser> _waiters;

    /// <summary>Indicates whether the lock is currently held.</summary>
    private bool _held;

    /// <summary>Indicates whether the lock has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncLock" /> class in the released state.
    /// </summary>
    public AsyncLock()
    {
        _waiters = new CancellableWaiterQueue<Releaser>(_gate);
    }

    /// <summary>
    /// Gets the number of callers currently queued waiting to acquire the lock.
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
    /// Asynchronously acquires the lock.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask{TResult}" /> that completes once the lock is held, yielding a <see cref="Releaser" />
    /// whose disposal releases the lock.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    /// <remarks>
    /// The returned <see cref="ValueTask{TResult}" /> must be awaited exactly once.
    /// </remarks>
    public ValueTask<Releaser> LockAsync() =>
        LockAsync(CancellationToken.None);

    /// <summary>
    /// Asynchronously acquires the lock, observing a cancellation request while waiting.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the pending acquisition.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}" /> that completes once the lock is held, yielding a <see cref="Releaser" />
    /// whose disposal releases the lock.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was canceled before the lock was acquired.
    /// </exception>
    /// <remarks>
    /// The returned <see cref="ValueTask{TResult}" /> must be awaited exactly once. If the lock is free the result is
    /// produced synchronously and no allocation occurs, even when <paramref name="cancellationToken" /> is already
    /// canceled; otherwise the caller waits until ownership is transferred or the token is canceled.
    /// </remarks>
    public ValueTask<Releaser> LockAsync(CancellationToken cancellationToken)
    {
        LinkedListNode<TaskCompletionSource<Releaser>> node;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Success wins: a free lock is acquired before an already-canceled token is honored.
            if (!_held)
            {
                _held = true;
                return new ValueTask<Releaser>(new Releaser(this));
            }

            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<Releaser>(cancellationToken);

            node = _waiters.Enqueue();
        }

        return AwaitAcquireAsync(node, cancellationToken);
    }

    /// <summary>
    /// Releases the lock, transferring ownership to the next queued waiter if one exists. Invoked by
    /// <see cref="Releaser.Dispose" />.
    /// </summary>
    internal void Release()
    {
        lock (_gate)
        {
            // Transfer ownership to the longest-waiting caller, skipping any whose task was already canceled.
            if (_waiters.Count > 0 && _waiters.TryGrant(() => new Releaser(this)))
                return;

            _held = false;
        }
    }

    /// <summary>
    /// Releases the resources used by the lock. Any callers still waiting observe an
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    public void Dispose()
    {
        List<TaskCompletionSource<Releaser>> toFault;
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;

            toFault = _waiters.Drain();
        }

        foreach (TaskCompletionSource<Releaser> tcs in toFault)
            tcs.TrySetException(new ObjectDisposedException(nameof(AsyncLock), ResourceStrings.Op_Invalid_AsyncPrimitiveDisposedWaiters));
    }

    /// <summary>
    /// Completes acquisition after a contended wait and produces the releaser.
    /// </summary>
    /// <param name="node">The queued waiter to observe.</param>
    /// <param name="cancellationToken">A token used to cancel the pending acquisition.</param>
    /// <returns>A <see cref="Releaser" /> whose disposal releases the lock.</returns>
    /// <remarks>
    /// Cancellation is observed only while the acquisition is still pending: once ownership has been transferred to
    /// this waiter, the grant wins and the lock is returned even if the token is signaled in the same instant. This
    /// "success wins" policy matches <see cref="System.Threading.SemaphoreSlim.WaitAsync(CancellationToken)" /> and is
    /// shared by <see cref="AsyncSemaphore" /> and <see cref="AsyncReaderWriterLock" />.
    /// </remarks>
    private ValueTask<Releaser> AwaitAcquireAsync(LinkedListNode<TaskCompletionSource<Releaser>> node, CancellationToken cancellationToken) =>
        _waiters.AwaitAsync(node, cancellationToken);
}
