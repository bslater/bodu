// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Threading;

/// <summary>
/// Provides an asynchronous, non-reentrant mutual-exclusion primitive whose acquisition can be awaited without
/// blocking a thread.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AsyncLock" /> is the asynchronous analogue of the C# <c>lock</c> statement. Because a held lock may
/// need to span an <c>await</c>, the lock is acquired with <see cref="LockAsync()" /> and released by disposing the
/// <see cref="Releaser" /> it returns, typically with a <c>using</c> statement:
/// </para>
/// <para>
/// The lock is backed by a binary <see cref="SemaphoreSlim" />. When the lock is free, acquisition completes
/// synchronously and allocates nothing; only a contended acquisition allocates the awaited task. Acquisition order
/// is approximately first-in, first-out but is not guaranteed under heavy contention.
/// </para>
/// <para>
/// The lock is <b>not reentrant</b>: a caller that already holds the lock and attempts to acquire it again on the
/// same logical flow will deadlock, exactly as a second <see cref="SemaphoreSlim.Wait()" /> on a depleted binary
/// semaphore would.
/// </para>
/// </remarks>
/// <example>
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
/// </example>
[DebuggerDisplay("AsyncLock")]
public sealed partial class AsyncLock : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncLock" /> class in the released state.
    /// </summary>
    public AsyncLock()
    {
    }

    /// <summary>
    /// Asynchronously acquires the lock.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask{TResult}" /> that completes once the lock is held, yielding a <see cref="Releaser" />
    /// whose disposal releases the lock.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    /// <remarks>The returned <see cref="ValueTask{TResult}" /> must be awaited exactly once.</remarks>
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
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> was canceled before the lock was acquired.</exception>
    /// <remarks>
    /// The returned <see cref="ValueTask{TResult}" /> must be awaited exactly once. If the lock is free the result is
    /// produced synchronously and no allocation occurs; otherwise the caller waits until the lock is released or the
    /// token is canceled.
    /// </remarks>
    public ValueTask<Releaser> LockAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var wait = _semaphore.WaitAsync(cancellationToken);
        return wait.IsCompletedSuccessfully
            ? new ValueTask<Releaser>(new Releaser(this))
            : AwaitAcquireAsync(wait);
    }

    /// <summary>
    /// Releases the lock. Invoked by <see cref="Releaser.Dispose" />.
    /// </summary>
    internal void Release() =>
        _semaphore.Release();

    /// <summary>
    /// Releases the resources used by the lock. Any callers still waiting observe an <see cref="ObjectDisposedException" />.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _semaphore.Dispose();
    }

    /// <summary>
    /// Completes acquisition after a contended wait and produces the releaser.
    /// </summary>
    /// <param name="wait">The pending semaphore wait to observe.</param>
    /// <returns>A <see cref="Releaser" /> whose disposal releases the lock.</returns>
    private async ValueTask<Releaser> AwaitAcquireAsync(Task wait)
    {
        await wait.ConfigureAwait(false);
        return new Releaser(this);
    }
}
