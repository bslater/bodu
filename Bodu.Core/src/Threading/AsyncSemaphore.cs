// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncSemaphore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Threading;

/// <summary>
/// Provides a lightweight asynchronous counting semaphore that admits a bounded number of concurrent holders and
/// releases waiters in strict first-in, first-out order.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AsyncSemaphore" /> is a queue-based asynchronous analogue of <see cref="SemaphoreSlim" />. Callers await
/// <see cref="WaitAsync()" /> to consume a permit and call <see cref="Release()" /> to return one. The convenience
/// method <see cref="LockAsync()" /> pairs a wait with a disposable <see cref="Releaser" /> so a permit can be scoped
/// with a <c>using</c> statement.
/// </para>
/// <para>
/// Unlike <see cref="SemaphoreSlim" />, waiters are released in <b>strict FIFO order</b>: the longest-waiting caller is
/// always satisfied first. Each waiter is represented by a <see cref="TaskCompletionSource{TResult}" /> created with
/// <see cref="TaskCreationOptions.RunContinuationsAsynchronously" />, so a thread calling <see cref="Release()" /> is
/// never hijacked to run a waiter's continuation inline. The type owns no operating-system handle and therefore does
/// not implement <see cref="IDisposable" />.
/// </para>
/// <para>
/// Cancellation follows the package-wide rule: an available permit is taken even when the supplied token is already
/// canceled. A token only cancels a wait that must queue.
/// </para>
/// <para>
/// <see cref="Release(int)" /> first hands permits directly to queued waiters in FIFO order and only stores the
/// remainder as available permits. Permits transferred to waiters never appear in <see cref="CurrentCount" />, and the
/// configured maximum applies only to the stored available count after queued waiters have been satisfied.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// private readonly AsyncSemaphore _throttle = new(initialCount: 4);
///
/// public async Task DownloadAsync(Uri uri)
/// {
///     using (await _throttle.LockAsync())
///     {
///         // At most four downloads run concurrently.
///         await HttpGetAsync(uri);
///     }
/// }
///]]>
/// </example>
[DebuggerDisplay("CurrentCount = {CurrentCount}, MaxCount = {_maxCount}, Waiters = {WaiterCount}")]
public sealed partial class AsyncSemaphore
{
    private readonly object _gate = new();
    private readonly LinkedList<TaskCompletionSource<bool>> _waiters = new();
    private readonly int _maxCount;
    private int _currentCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncSemaphore" /> class with the specified number of permits and
    /// no upper bound on the permit count.
    /// </summary>
    /// <param name="initialCount">The initial number of permits available.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialCount" /> is negative.</exception>
    public AsyncSemaphore(int initialCount)
        : this(initialCount, int.MaxValue)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncSemaphore" /> class with the specified number of permits and
    /// maximum permit count.
    /// </summary>
    /// <param name="initialCount">The initial number of permits available.</param>
    /// <param name="maxCount">The maximum number of permits the semaphore can hold.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="initialCount" /> is negative, <paramref name="maxCount" /> is less than one, or
    /// <paramref name="initialCount" /> is greater than <paramref name="maxCount" />.
    /// </exception>
    public AsyncSemaphore(int initialCount, int maxCount)
    {
        ThrowHelper.ThrowIfNegative(initialCount);
        ThrowHelper.ThrowIfLessThan(maxCount, 1);
        ThrowHelper.ThrowIfGreaterThan(initialCount, maxCount);

        _currentCount = initialCount;
        _maxCount = maxCount;
    }

    /// <summary>
    /// Gets the number of permits currently available.
    /// </summary>
    /// <value>The number of permits that can be taken without waiting.</value>
    /// <returns>The number of permits currently available.</returns>
    public int CurrentCount
    {
        get
        {
            lock (_gate)
            {
                return _currentCount;
            }
        }
    }

    /// <summary>
    /// Gets the number of callers currently queued waiting for a permit.
    /// </summary>
    /// <value>The number of queued waiters.</value>
    /// <returns>The number of callers awaiting a permit. Intended for diagnostics.</returns>
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
    /// Asynchronously waits to take a permit.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> that completes when a permit has been taken.</returns>
    /// <remarks>
    /// The returned <see cref="ValueTask" /> must be awaited exactly once.
    /// </remarks>
    public ValueTask WaitAsync() =>
        WaitAsync(CancellationToken.None);

    /// <summary>
    /// Asynchronously waits to take a permit, observing a cancellation request while waiting.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the pending wait.</param>
    /// <returns>A <see cref="ValueTask" /> that completes when a permit has been taken.</returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was canceled before a permit was taken.
    /// </exception>
    /// <remarks>
    /// The returned <see cref="ValueTask" /> must be awaited exactly once. An available permit is taken even when
    /// <paramref name="cancellationToken" /> is already canceled; the token only cancels a wait that must queue, and
    /// cancellation removes only the calling waiter from the queue.
    /// </remarks>
    public ValueTask WaitAsync(CancellationToken cancellationToken)
    {
        LinkedListNode<TaskCompletionSource<bool>> node;
        lock (_gate)
        {
            // Success wins: an available permit is taken before an already-canceled token is honored.
            if (_currentCount > 0)
            {
                _currentCount--;
                return ValueTask.CompletedTask;
            }

            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            node = _waiters.AddLast(new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
        }

        return AwaitWaiterAsync(node, cancellationToken);
    }

    /// <summary>
    /// Asynchronously takes a permit and returns a disposable releaser that returns it when disposed.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask{TResult}" /> yielding a <see cref="Releaser" /> whose disposal returns the permit.
    /// </returns>
    public ValueTask<Releaser> LockAsync() =>
        LockAsync(CancellationToken.None);

    /// <summary>
    /// Asynchronously takes a permit and returns a disposable releaser that returns it when disposed, observing a
    /// cancellation request while waiting.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the pending wait.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}" /> yielding a <see cref="Releaser" /> whose disposal returns the permit.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was canceled before a permit was taken.
    /// </exception>
    public ValueTask<Releaser> LockAsync(CancellationToken cancellationToken)
    {
        ValueTask wait = WaitAsync(cancellationToken);
        return wait.IsCompletedSuccessfully
            ? new ValueTask<Releaser>(new Releaser(this))
            : AwaitReleaserAsync(wait);
    }

    /// <summary>
    /// Returns a single permit to the semaphore, releasing the next waiter if one is queued.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Releasing would raise the permit count above the configured maximum.
    /// </exception>
    public void Release() =>
        Release(1);

    /// <summary>
    /// Returns the specified number of permits to the semaphore, releasing queued waiters in FIFO order.
    /// </summary>
    /// <param name="releaseCount">The number of permits to return.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="releaseCount" /> is less than one.</exception>
    /// <exception cref="InvalidOperationException">
    /// Releasing would raise the permit count above the configured maximum.
    /// </exception>
    public void Release(int releaseCount)
    {
        ThrowHelper.ThrowIfZeroOrNegative(releaseCount);

        lock (_gate)
        {
            int remaining = releaseCount;

            // Hand permits to queued waiters in FIFO order. A waiter whose task was already canceled is dropped
            // without consuming a permit.
            while (remaining > 0 && _waiters.First is { } first)
            {
                _waiters.RemoveFirst();
                if (first.Value.TrySetResult(true))
                    remaining--;
            }

            if (remaining == 0)
                return;

            if (_currentCount + remaining > _maxCount)
                throw new InvalidOperationException(ResourceStrings.Op_Invalid_SemaphoreReleaseExceedsMax);

            _currentCount += remaining;
        }
    }

    /// <summary>
    /// Returns a single permit. Invoked by <see cref="Releaser.Dispose" />.
    /// </summary>
    internal void ReleaseFromReleaser() =>
        Release(1);

    /// <summary>
    /// Completes a contended wait, disposing the cancellation registration when the wait resolves.
    /// </summary>
    /// <param name="node">The queued waiter to observe.</param>
    /// <param name="cancellationToken">A token used to cancel the pending wait.</param>
    /// <returns>A <see cref="ValueTask" /> that completes when the permit is taken.</returns>
    private async ValueTask AwaitWaiterAsync(LinkedListNode<TaskCompletionSource<bool>> node, CancellationToken cancellationToken)
    {
        using (cancellationToken.Register(static state =>
        {
            (AsyncSemaphore? owner, LinkedListNode<TaskCompletionSource<bool>>? waiter, CancellationToken token) = ((AsyncSemaphore Owner, LinkedListNode<TaskCompletionSource<bool>> Node, CancellationToken Token))state!;
            owner.CancelWaiter(waiter, token);
        }, (this, node, cancellationToken)))
        {
            await node.Value.Task.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Completes a contended <see cref="LockAsync()" /> and produces the releaser.
    /// </summary>
    /// <param name="wait">The pending wait to observe.</param>
    /// <returns>A <see cref="Releaser" /> whose disposal returns the permit.</returns>
    private async ValueTask<Releaser> AwaitReleaserAsync(ValueTask wait)
    {
        await wait.ConfigureAwait(false);
        return new Releaser(this);
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
