// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncReaderWriterLock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Threading;

/// <summary>
/// Provides an asynchronous, writer-preferring reader/writer lock whose acquisitions can be awaited without blocking
/// a thread.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AsyncReaderWriterLock" /> permits any number of concurrent readers or a single exclusive writer. Read
/// access is acquired with <see cref="ReaderAsync()" /> and write access with <see cref="WriterAsync()" />; both
/// return a <see cref="Releaser" /> whose disposal releases the corresponding access, typically scoped with a
/// <c>using</c> statement.
/// </para>
/// <para>
/// The lock is <b>writer-preferring</b>: while a writer is active or queued, newly arriving readers wait, which
/// prevents a steady stream of readers from starving a pending writer. Queued writers are granted in strict FIFO
/// order; when a writer releases and no writer is queued, all waiting readers are admitted together.
/// </para>
/// <para>
/// The lock is <b>not reentrant</b>. All waiter completion sources use
/// <see cref="TaskCreationOptions.RunContinuationsAsynchronously" />, so releasing threads never run waiter
/// continuations inline while holding the internal gate. Disposing the lock faults any still-waiting acquisition with
/// <see cref="ObjectDisposedException" />.
/// </para>
/// </remarks>
[DebuggerDisplay("Readers = {_readersActive}, WriterActive = {_writerActive}")]
public sealed partial class AsyncReaderWriterLock : IDisposable
{
    private readonly object _gate = new();
    private readonly List<TaskCompletionSource<Releaser>> _waitingReaders = new();
    private readonly LinkedList<TaskCompletionSource<Releaser>> _waitingWriters = new();
    private int _readersActive;
    private bool _writerActive;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncReaderWriterLock" /> class in the released state.
    /// </summary>
    public AsyncReaderWriterLock()
    {
    }

    /// <summary>
    /// Asynchronously acquires shared (read) access.
    /// </summary>
    /// <returns>A <see cref="ValueTask{TResult}" /> yielding a <see cref="Releaser" /> whose disposal releases read access.</returns>
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    public ValueTask<Releaser> ReaderAsync() =>
        ReaderAsync(CancellationToken.None);

    /// <summary>
    /// Asynchronously acquires shared (read) access, observing a cancellation request while waiting.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the pending acquisition.</param>
    /// <returns>A <see cref="ValueTask{TResult}" /> yielding a <see cref="Releaser" /> whose disposal releases read access.</returns>
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was canceled before access was acquired.</exception>
    /// <remarks>The returned <see cref="ValueTask{TResult}" /> must be awaited exactly once.</remarks>
    public ValueTask<Releaser> ReaderAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<Releaser>(cancellationToken);

        TaskCompletionSource<Releaser> tcs;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_writerActive && _waitingWriters.Count == 0)
            {
                _readersActive++;
                return new ValueTask<Releaser>(new Releaser(this, isWriter: false));
            }

            tcs = new TaskCompletionSource<Releaser>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waitingReaders.Add(tcs);
        }

        return AwaitReaderAsync(tcs, cancellationToken);
    }

    /// <summary>
    /// Asynchronously acquires exclusive (write) access.
    /// </summary>
    /// <returns>A <see cref="ValueTask{TResult}" /> yielding a <see cref="Releaser" /> whose disposal releases write access.</returns>
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    public ValueTask<Releaser> WriterAsync() =>
        WriterAsync(CancellationToken.None);

    /// <summary>
    /// Asynchronously acquires exclusive (write) access, observing a cancellation request while waiting.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the pending acquisition.</param>
    /// <returns>A <see cref="ValueTask{TResult}" /> yielding a <see cref="Releaser" /> whose disposal releases write access.</returns>
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was canceled before access was acquired.</exception>
    /// <remarks>The returned <see cref="ValueTask{TResult}" /> must be awaited exactly once.</remarks>
    public ValueTask<Releaser> WriterAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<Releaser>(cancellationToken);

        LinkedListNode<TaskCompletionSource<Releaser>> node;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_writerActive && _readersActive == 0 && _waitingWriters.Count == 0)
            {
                _writerActive = true;
                return new ValueTask<Releaser>(new Releaser(this, isWriter: true));
            }

            node = _waitingWriters.AddLast(new TaskCompletionSource<Releaser>(TaskCreationOptions.RunContinuationsAsynchronously));
        }

        return AwaitWriterAsync(node, cancellationToken);
    }

    /// <summary>
    /// Releases the resources used by the lock. Any callers still waiting observe an <see cref="ObjectDisposedException" />.
    /// </summary>
    public void Dispose()
    {
        List<TaskCompletionSource<Releaser>> toFault;
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;

            toFault = new List<TaskCompletionSource<Releaser>>(_waitingReaders);
            toFault.AddRange(_waitingWriters);
            _waitingReaders.Clear();
            _waitingWriters.Clear();
        }

        foreach (var tcs in toFault)
            tcs.TrySetException(new ObjectDisposedException(nameof(AsyncReaderWriterLock), ResourceStrings.Op_Invalid_AsyncLockDisposedWaiters));
    }

    /// <summary>
    /// Releases shared (read) access held by one reader. Invoked by <see cref="Releaser.Dispose" />.
    /// </summary>
    internal void ReleaseReader()
    {
        lock (_gate)
        {
            _readersActive--;
            if (_readersActive == 0)
                GrantNextWriter();
        }
    }

    /// <summary>
    /// Releases exclusive (write) access. Invoked by <see cref="Releaser.Dispose" />.
    /// </summary>
    internal void ReleaseWriter()
    {
        lock (_gate)
        {
            _writerActive = false;

            // Writer-preference: hand off to the next queued writer if any, otherwise admit all waiting readers.
            if (!GrantNextWriter())
                GrantAllReaders();
        }
    }

    /// <summary>
    /// Grants the next queued writer, if one exists, skipping any whose task was already canceled. Must be called
    /// while holding <see cref="_gate" />.
    /// </summary>
    /// <returns><see langword="true" /> if a writer was granted; otherwise, <see langword="false" />.</returns>
    private bool GrantNextWriter()
    {
        while (_waitingWriters.First is { } first)
        {
            _waitingWriters.RemoveFirst();
            if (first.Value.TrySetResult(new Releaser(this, isWriter: true)))
            {
                _writerActive = true;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Admits every waiting reader. Must be called while holding <see cref="_gate" />.
    /// </summary>
    private void GrantAllReaders()
    {
        foreach (var tcs in _waitingReaders)
        {
            if (tcs.TrySetResult(new Releaser(this, isWriter: false)))
                _readersActive++;
        }

        _waitingReaders.Clear();
    }

    /// <summary>
    /// Completes a contended reader acquisition, disposing the cancellation registration when it resolves.
    /// </summary>
    /// <param name="tcs">The queued reader to observe.</param>
    /// <param name="cancellationToken">A token used to cancel the pending acquisition.</param>
    /// <returns>A <see cref="ValueTask{TResult}" /> yielding the read releaser.</returns>
    private async ValueTask<Releaser> AwaitReaderAsync(TaskCompletionSource<Releaser> tcs, CancellationToken cancellationToken)
    {
        using (cancellationToken.Register(static state =>
        {
            var (owner, waiter) = ((AsyncReaderWriterLock Owner, TaskCompletionSource<Releaser> Waiter))state!;
            owner.CancelReader(waiter);
        }, (this, tcs)))
        {
            return await tcs.Task.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Completes a contended writer acquisition, disposing the cancellation registration when it resolves.
    /// </summary>
    /// <param name="node">The queued writer to observe.</param>
    /// <param name="cancellationToken">A token used to cancel the pending acquisition.</param>
    /// <returns>A <see cref="ValueTask{TResult}" /> yielding the write releaser.</returns>
    private async ValueTask<Releaser> AwaitWriterAsync(LinkedListNode<TaskCompletionSource<Releaser>> node, CancellationToken cancellationToken)
    {
        using (cancellationToken.Register(static state =>
        {
            var (owner, waiter) = ((AsyncReaderWriterLock Owner, LinkedListNode<TaskCompletionSource<Releaser>> Node))state!;
            owner.CancelWriter(waiter);
        }, (this, node)))
        {
            return await node.Value.Task.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Removes a canceled reader from the wait list and transitions its task to the canceled state.
    /// </summary>
    /// <param name="tcs">The reader to cancel.</param>
    private void CancelReader(TaskCompletionSource<Releaser> tcs)
    {
        lock (_gate)
        {
            _waitingReaders.Remove(tcs);
        }

        tcs.TrySetCanceled();
    }

    /// <summary>
    /// Removes a canceled writer from the queue and transitions its task to the canceled state. If removing the
    /// writer leaves the lock idle, the next acquisition is granted.
    /// </summary>
    /// <param name="node">The writer to cancel.</param>
    private void CancelWriter(LinkedListNode<TaskCompletionSource<Releaser>> node)
    {
        lock (_gate)
        {
            if (node.List is not null)
                _waitingWriters.Remove(node);

            // A canceled writer may have been the reason readers were waiting; if the lock is now idle, let the next
            // eligible acquisition proceed.
            if (!_writerActive && _readersActive == 0 && !GrantNextWriter())
                GrantAllReaders();
        }

        node.Value.TrySetCanceled();
    }
}
