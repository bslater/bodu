// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncManualResetEvent.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Threading;

/// <summary>
/// Provides an asynchronous, manually reset signaling primitive: once set, every current and future waiter is released
/// until the event is explicitly reset.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AsyncManualResetEvent" /> is the asynchronous analogue of <see cref="ManualResetEventSlim" />. Callers
/// await <see cref="WaitAsync()" />; a call to <see cref="Set" /> completes all outstanding waits and leaves the event
/// signaled so that subsequent waits complete immediately. <see cref="Reset" /> returns the event to the unsignaled
/// state.
/// </para>
/// <para>
/// The event is backed by a single shared <see cref="TaskCompletionSource{TResult}" /> created with
/// <see cref="TaskCreationOptions.RunContinuationsAsynchronously" />, so the thread that calls <see cref="Set" /> is
/// never hijacked to run waiter continuations inline.
/// </para>
/// <para>
/// Cancellation follows the package-wide rule: when the event is already set, <see cref="WaitAsync()" /> completes
/// immediately even if the supplied token is already canceled. A <see cref="Set" /> that races with a
/// <see cref="Reset" /> is observed consistently: a waiter that captured the completed task before the reset still
/// completes, while a wait that begins after the reset observes the new unsignaled state.
/// </para>
/// </remarks>
[DebuggerDisplay("IsSet = {IsSet}")]
public sealed class AsyncManualResetEvent
{
    /// <summary>The completion source whose task represents the signaled state; replaced on each reset.</summary>
    private TaskCompletionSource<bool> _source;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncManualResetEvent" /> class in the unsignaled state.
    /// </summary>
    public AsyncManualResetEvent()
        : this(false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncManualResetEvent" /> class.
    /// </summary>
    /// <param name="initialState">
    /// <see langword="true" /> to create the event in the signaled state; otherwise, <see langword="false" />.
    /// </param>
    public AsyncManualResetEvent(bool initialState)
    {
        _source = CreateSource();
        if (initialState)
            _source.SetResult(true);
    }

    /// <summary>
    /// Gets a value indicating whether the event is currently signaled.
    /// </summary>
    /// <value><see langword="true" /> if the event is set; otherwise, <see langword="false" />.</value>
    public bool IsSet =>
        Volatile.Read(ref _source).Task.IsCompletedSuccessfully;

    /// <summary>
    /// Asynchronously waits until the event is signaled.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> that completes when the event is set.</returns>
    public ValueTask WaitAsync() =>
        WaitAsync(CancellationToken.None);

    /// <summary>
    /// Asynchronously waits until the event is signaled, observing a cancellation request while waiting.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the wait.</param>
    /// <returns>A <see cref="ValueTask" /> that completes when the event is set.</returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was canceled before the event was set.
    /// </exception>
    /// <remarks>
    /// Cancellation affects only the calling waiter; other waiters on the same event are unaffected.
    /// </remarks>
    public ValueTask WaitAsync(CancellationToken cancellationToken)
    {
        Task<bool> task = Volatile.Read(ref _source).Task;
        if (task.IsCompletedSuccessfully)
            return ValueTask.CompletedTask;

        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled(cancellationToken);

        return new ValueTask(task.WaitAsync(cancellationToken));
    }

    /// <summary>
    /// Sets the event, releasing every current and future waiter until the event is reset.
    /// </summary>
    public void Set() =>
        Volatile.Read(ref _source).TrySetResult(true);

    /// <summary>
    /// Resets the event to the unsignaled state. Has no effect if the event is already unsignaled.
    /// </summary>
    public void Reset()
    {
        while (true)
        {
            TaskCompletionSource<bool> current = Volatile.Read(ref _source);
            if (!current.Task.IsCompleted)
                return;

            if (Interlocked.CompareExchange(ref _source, CreateSource(), current) == current)
                return;
        }
    }

    /// <summary>
    /// Creates a fresh, incomplete completion source configured to run continuations asynchronously.
    /// </summary>
    /// <returns>A new <see cref="TaskCompletionSource{TResult}" />.</returns>
    private static TaskCompletionSource<bool> CreateSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
