// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncReaderWriterLock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Threading;

/// <summary>
/// Provides an asynchronous, writer-preferring reader/writer lock whose acquisitions can be awaited without blocking a
/// thread.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AsyncReaderWriterLock" /> permits any number of concurrent readers or a single exclusive writer. Read
/// access is acquired with <see cref="ReaderAsync()" /> and write access with <see cref="WriterAsync()" />; both return
/// a <see cref="Releaser" /> whose disposal releases the corresponding access, typically scoped with a <c>using</c>
/// statement.
/// </para>
/// <para>
/// <b>Fairness.</b> The lock is <b>writer-preferring</b>: while a writer is active or queued, newly arriving readers
/// wait, and queued writers are granted in strict FIFO order. When a writer releases and no writer is queued, all
/// waiting readers are admitted together (batch admission). This prevents writer starvation, but a sustained stream of
/// writers can starve readers; choose this lock only when that trade-off is acceptable.
/// </para>
/// <para>
/// <b>Reentrancy and upgrade.</b> The lock is <b>not reentrant</b> and provides <b>no upgradeable read mode</b>. A
/// caller must never acquire write access while already holding read access (or vice versa) on the same flow: doing so
/// deadlocks, because the second acquisition waits for access the same flow already holds. To transition from reading
/// to writing, release the read access first, then acquire the write access and re-validate state, accepting that the
/// gap is not atomic.
/// </para>
/// <para>
/// <b>Cancellation.</b> Following the package-wide rule, access that can be granted immediately is granted even when
/// the supplied token is already canceled; the token only cancels an acquisition that must queue, and cancellation
/// removes only the calling waiter.
/// </para>
/// <para>
/// <b>Continuations and disposal.</b> All waiter completion sources use
/// <see cref="TaskCreationOptions.RunContinuationsAsynchronously" />, so releasing threads never run waiter
/// continuations inline while holding the internal gate. Disposing the lock faults any still-waiting acquisition with
/// <see cref="ObjectDisposedException" />; dispose only when no acquisitions are expected to be granted afterwards.
/// </para>
/// <para>
/// <b>Releaser ownership.</b> Each <see cref="Releaser" /> is idempotent: disposing it more than once (including copies
/// of the same value) releases the access exactly once, so accidental double-disposal cannot corrupt the reader count
/// or grant overlapping access.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// private readonly AsyncReaderWriterLock _lock = new();
///
/// // Readers run concurrently with one another.
/// async Task<int> CountAsync()
/// {
///     using (await _lock.ReaderAsync())
///         return _cache.Count;
/// }
///
/// // A writer runs exclusively, excluding all readers for its duration.
/// async Task SetAsync(string key, int value)
/// {
///     using (await _lock.WriterAsync())
///         _cache[key] = value;
/// }
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Readers = {_readersActive}, WriterActive = {_writerActive}, WaitingReaders = {WaitingReaderCount}, WaitingWriters = {WaitingWriterCount}")]
public sealed partial class AsyncReaderWriterLock
    : IDisposable
{
    /// <summary>The synchronization object guarding all mutable lock state.</summary>
    private readonly object _gate = new();

    /// <summary>The readers waiting to acquire shared access, released together when no writer is active.</summary>
    private readonly List<TaskCompletionSource<Releaser>> _waitingReaders = new();

    /// <summary>The writers waiting to acquire exclusive access, granted in first-in, first-out order.</summary>
    private readonly LinkedList<TaskCompletionSource<Releaser>> _waitingWriters = new();

    /// <summary>The number of readers currently holding shared access.</summary>
    private int _readersActive;

    /// <summary>Indicates whether a writer currently holds exclusive access.</summary>
    private bool _writerActive;

    /// <summary>Indicates whether the lock has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncReaderWriterLock" /> class in the released state.
    /// </summary>
    public AsyncReaderWriterLock()
    {
    }

    /// <summary>
    /// Gets the number of callers currently queued waiting for read access.
    /// </summary>
    /// <value>The number of queued readers.</value>
    internal int WaitingReaderCount
    {
        get
        {
            lock (_gate)
            {
                return _waitingReaders.Count;
            }
        }
    }

    /// <summary>
    /// Gets the number of callers currently queued waiting for write access.
    /// </summary>
    /// <value>The number of queued writers.</value>
    internal int WaitingWriterCount
    {
        get
        {
            lock (_gate)
            {
                return _waitingWriters.Count;
            }
        }
    }

    /// <summary>
    /// Asynchronously acquires shared (read) access.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask{TResult}" /> yielding a <see cref="Releaser" /> whose disposal releases read access.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    public ValueTask<Releaser> ReaderAsync() =>
        ReaderAsync(CancellationToken.None);

    /// <summary>
    /// Asynchronously acquires shared (read) access, observing a cancellation request while waiting.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the pending acquisition.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}" /> yielding a <see cref="Releaser" /> whose disposal releases read access.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was canceled before access was acquired.
    /// </exception>
    /// <remarks>
    /// The returned <see cref="ValueTask{TResult}" /> must be awaited exactly once. Read access that can be granted
    /// immediately is granted even when <paramref name="cancellationToken" /> is already canceled; the token only
    /// cancels an acquisition that must queue.
    /// </remarks>
    public ValueTask<Releaser> ReaderAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource<Releaser> tcs;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Success wins: immediately available read access is granted before an already-canceled token is honored.
            if (!_writerActive && _waitingWriters.Count == 0)
            {
                _readersActive++;
                return new ValueTask<Releaser>(CreateReleaser(isWriter: false));
            }

            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<Releaser>(cancellationToken);

            tcs = new TaskCompletionSource<Releaser>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waitingReaders.Add(tcs);
        }

        return AwaitReaderAsync(tcs, cancellationToken);
    }

    /// <summary>
    /// Asynchronously acquires exclusive (write) access.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask{TResult}" /> yielding a <see cref="Releaser" /> whose disposal releases write access.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    public ValueTask<Releaser> WriterAsync() =>
        WriterAsync(CancellationToken.None);

    /// <summary>
    /// Asynchronously acquires exclusive (write) access, observing a cancellation request while waiting.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the pending acquisition.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}" /> yielding a <see cref="Releaser" /> whose disposal releases write access.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was canceled before access was acquired.
    /// </exception>
    /// <remarks>
    /// The returned <see cref="ValueTask{TResult}" /> must be awaited exactly once. Write access that can be granted
    /// immediately is granted even when <paramref name="cancellationToken" /> is already canceled; the token only
    /// cancels an acquisition that must queue.
    /// </remarks>
    public ValueTask<Releaser> WriterAsync(CancellationToken cancellationToken)
    {
        LinkedListNode<TaskCompletionSource<Releaser>> node;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Success wins: immediately available write access is granted before an already-canceled token is honored.
            if (!_writerActive && _readersActive == 0 && _waitingWriters.Count == 0)
            {
                _writerActive = true;
                return new ValueTask<Releaser>(CreateReleaser(isWriter: true));
            }

            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<Releaser>(cancellationToken);

            node = _waitingWriters.AddLast(new TaskCompletionSource<Releaser>(TaskCreationOptions.RunContinuationsAsynchronously));
        }

        return AwaitWriterAsync(node, cancellationToken);
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

            toFault = new List<TaskCompletionSource<Releaser>>(_waitingReaders);
            toFault.AddRange(_waitingWriters);
            _waitingReaders.Clear();
            _waitingWriters.Clear();
        }

        foreach (TaskCompletionSource<Releaser> tcs in toFault)
            tcs.TrySetException(new ObjectDisposedException(nameof(AsyncReaderWriterLock), ResourceStrings.Op_Invalid_AsyncPrimitiveDisposedWaiters));
    }

    /// <summary>
    /// Creates a releaser bound to this lock with a fresh idempotency guard so that disposing the releaser, or any copy
    /// of it, releases the access exactly once.
    /// </summary>
    /// <param name="isWriter">
    /// <see langword="true" /> for a write releaser; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>A <see cref="Releaser" /> for the granted access.</returns>
    private Releaser CreateReleaser(bool isWriter) =>
        new(this, isWriter, new Releaser.ReleaseGuard());

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
    /// Grants the next queued writer, if one exists, skipping any whose task was already canceled. Must be called while
    /// holding <see cref="_gate" />.
    /// </summary>
    /// <returns><see langword="true" /> if a writer was granted; otherwise, <see langword="false" />.</returns>
    private bool GrantNextWriter()
    {
        while (_waitingWriters.First is { } first)
        {
            _waitingWriters.RemoveFirst();
            if (first.Value.TrySetResult(CreateReleaser(isWriter: true)))
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
        foreach (TaskCompletionSource<Releaser> tcs in _waitingReaders)
        {
            if (tcs.TrySetResult(CreateReleaser(isWriter: false)))
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
        using (cancellationToken.Register(
            static state =>
            {
                (AsyncReaderWriterLock? owner, TaskCompletionSource<Releaser>? waiter, CancellationToken token) = ((AsyncReaderWriterLock Owner, TaskCompletionSource<Releaser> Waiter, CancellationToken Token))state!;
                owner.CancelReader(waiter, token);
            }, (this, tcs, cancellationToken)))
            {
                // The reader's task is completed by a writer release on this same lock, not work scheduled elsewhere, and the
                // type uses no JoinableTaskFactory, so the foreign-task deadlock VSTHRD003 guards against cannot arise.
    #pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
                return await tcs.Task.ConfigureAwait(false);
    #pragma warning restore VSTHRD003
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
        using (cancellationToken.Register(
            static state =>
            {
                (AsyncReaderWriterLock? owner, LinkedListNode<TaskCompletionSource<Releaser>>? waiter, CancellationToken token) = ((AsyncReaderWriterLock Owner, LinkedListNode<TaskCompletionSource<Releaser>> Node, CancellationToken Token))state!;
                owner.CancelWriter(waiter, token);
            }, (this, node, cancellationToken)))
            {
                // The writer's task is completed by a reader/writer release on this same lock, not work scheduled elsewhere,
                // and the type uses no JoinableTaskFactory, so the foreign-task deadlock VSTHRD003 guards against cannot arise.
    #pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
                return await node.Value.Task.ConfigureAwait(false);
    #pragma warning restore VSTHRD003
            }
    }

    /// <summary>
    /// Removes a canceled reader from the wait list and transitions its task to the canceled state.
    /// </summary>
    /// <param name="tcs">The reader to cancel.</param>
    /// <param name="cancellationToken">The token whose cancellation triggered the removal.</param>
    private void CancelReader(TaskCompletionSource<Releaser> tcs, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _waitingReaders.Remove(tcs);
        }

        tcs.TrySetCanceled(cancellationToken);
    }

    /// <summary>
    /// Removes a canceled writer from the queue and transitions its task to the canceled state. If removing the writer
    /// leaves the lock idle, the next acquisition is granted.
    /// </summary>
    /// <param name="node">The writer to cancel.</param>
    /// <param name="cancellationToken">The token whose cancellation triggered the removal.</param>
    private void CancelWriter(LinkedListNode<TaskCompletionSource<Releaser>> node, CancellationToken cancellationToken)
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

        node.Value.TrySetCanceled(cancellationToken);
    }
}
