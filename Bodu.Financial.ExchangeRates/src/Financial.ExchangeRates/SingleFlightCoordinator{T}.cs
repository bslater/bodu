// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SingleFlightCoordinator{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Coordinates concurrent asynchronous operations keyed by <typeparamref name="TKey" /> so that at most one operation
/// per key runs at a time, with every concurrent caller for that key awaiting the same shared task.
/// </summary>
/// <typeparam name="TKey">The type that identifies an operation; equal keys share one in-flight result.</typeparam>
/// <remarks>
/// <para>
/// This is the classic single-flight (request-coalescing) pattern: when several callers request the same key while a
/// fetch is already running, they join the running task instead of starting duplicate work. The in-flight entry is
/// removed as soon as the operation completes — including when it faults — so a failure never poisons the key and the
/// next caller starts a fresh attempt. Callers that join an in-flight operation observe its outcome, including its
/// exception.
/// </para>
/// <para>
/// <strong>Cancellation.</strong> The cancellation-aware overloads decouple the shared operation's lifetime from any
/// single caller's token. Once started, the operation runs to completion and fulfills the shared promise even if every
/// caller abandons its wait; the operation itself is invoked with <see cref="CancellationToken.None" /> because its
/// result populates shared state for the next caller. A caller's <c>cancellationToken</c> governs only that caller's
/// wait: when it is signalled the caller observes <see cref="OperationCanceledException" /> for itself and stops
/// waiting, without cancelling the shared operation or affecting any other caller. This makes the coalesced work safe
/// for idempotent cache-warming fetches, where one caller's cancellation must never fault the other joiners. A
/// reference-counted variant that cancels the shared operation only once <em>all</em> participants have cancelled is a
/// deliberate non-goal here. The token-less overloads carry no caller token into the operation and never cancel a
/// caller's wait.
/// </para>
/// <para>
/// A given key must always be used with the same result type. Instances are safe for concurrent use.
/// </para>
/// </remarks>
public sealed class SingleFlightCoordinator<TKey>
    where TKey : notnull
{
    /// <summary>The promise tasks for operations currently in flight, keyed by <typeparamref name="TKey" />. The first caller to register a key owns running the operation and completing the promise; concurrent callers await the same promise.</summary>
    private readonly ConcurrentDictionary<TKey, Task> _inFlight = new();

    /// <summary>
    /// Runs <paramref name="operation" /> for <paramref name="key" />, or joins the operation already in flight for
    /// that key, then awaits its completion.
    /// </summary>
    /// <param name="key">The key that identifies the operation.</param>
    /// <param name="operation">
    /// The asynchronous operation to run when none is in flight for <paramref name="key" />.
    /// </param>
    /// <returns>A task that completes when the operation for <paramref name="key" /> completes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="operation" /> is <see langword="null" />.
    /// </exception>
    public Task RunAsync(TKey key, Func<Task> operation)
    {
        ThrowHelper.ThrowIfNull(operation);

        return RunCoreAsync(key, _ => operation(), CancellationToken.None);
    }

    /// <summary>
    /// Runs <paramref name="operation" /> for <paramref name="key" />, or joins the operation already in flight for
    /// that key, then awaits its completion, abandoning this caller's wait when <paramref name="cancellationToken" />
    /// is signalled.
    /// </summary>
    /// <param name="key">The key that identifies the operation.</param>
    /// <param name="operation">
    /// The asynchronous operation to run when none is in flight for <paramref name="key" />, invoked with a token
    /// decoupled from any caller.
    /// </param>
    /// <param name="cancellationToken">A token that abandons this caller's wait on the shared result.</param>
    /// <returns>A task that completes when the operation for <paramref name="key" /> completes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="operation" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken" /> is signalled before the shared operation completes; the shared
    /// operation continues regardless.
    /// </exception>
    public Task RunAsync(TKey key, Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        ThrowHelper.ThrowIfNull(operation);

        return RunCoreAsync(key, operation, cancellationToken);
    }

    /// <summary>
    /// Runs <paramref name="operation" /> for <paramref name="key" />, or joins the operation already in flight for
    /// that key, then awaits and returns its result.
    /// </summary>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="key">The key that identifies the operation.</param>
    /// <param name="operation">
    /// The asynchronous operation to run when none is in flight for <paramref name="key" />.
    /// </param>
    /// <returns>A task that yields the result of the operation for <paramref name="key" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="operation" /> is <see langword="null" />.
    /// </exception>
    public Task<TResult> RunAsync<TResult>(TKey key, Func<Task<TResult>> operation)
    {
        ThrowHelper.ThrowIfNull(operation);

        return RunCoreAsync(key, _ => operation(), CancellationToken.None);
    }

    /// <summary>
    /// Runs <paramref name="operation" /> for <paramref name="key" />, or joins the operation already in flight for
    /// that key, then awaits and returns its result, abandoning this caller's wait when
    /// <paramref name="cancellationToken" /> is signalled.
    /// </summary>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="key">The key that identifies the operation.</param>
    /// <param name="operation">
    /// The asynchronous operation to run when none is in flight for <paramref name="key" />, invoked with a token
    /// decoupled from any caller.
    /// </param>
    /// <param name="cancellationToken">A token that abandons this caller's wait on the shared result.</param>
    /// <returns>A task that yields the result of the operation for <paramref name="key" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="operation" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken" /> is signalled before the shared operation completes; the shared
    /// operation continues regardless.
    /// </exception>
    public Task<TResult> RunAsync<TResult>(TKey key, Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
    {
        ThrowHelper.ThrowIfNull(operation);

        return RunCoreAsync(key, operation, cancellationToken);
    }

    /// <summary>
    /// Registers and drives the operation for <paramref name="key" /> when no operation is in flight, or joins the
    /// in-flight operation otherwise, then awaits the shared result under this caller's token.
    /// </summary>
    /// <param name="key">The key that identifies the operation.</param>
    /// <param name="operation">The asynchronous operation to run when this caller wins registration.</param>
    /// <param name="cancellationToken">A token that abandons this caller's wait on the shared result.</param>
    /// <returns>
    /// A task that completes when the operation for <paramref name="key" /> completes for this caller.
    /// </returns>
    /// <remarks>
    /// The winner starts a detached fulfillment task that runs <paramref name="operation" /> with
    /// <see cref="CancellationToken.None" />, completes the shared promise, and removes the in-flight entry, so the
    /// operation reaches completion for every joiner even when the winner abandons its own wait. Every caller awaits
    /// the shared promise through <see cref="Task.WaitAsync(CancellationToken)" /> so cancellation is observed per
    /// caller and never propagates to the shared operation.
    /// </remarks>
    private Task RunCoreAsync(TKey key, Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        var promise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task inFlight = _inFlight.GetOrAdd(key, promise.Task);

        if (ReferenceEquals(inFlight, promise.Task))
        {
            // This caller won registration; drive the operation independently of any caller's wait so it always
            // completes and fulfills the promise for joiners, even if every caller abandons its wait.
            _ = FulfillAsync(key, operation, promise);
        }

        // Every caller — winner and joiner alike — observes only its own cancellation while awaiting the shared result.
        return inFlight.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Registers and drives the result-producing operation for <paramref name="key" /> when no operation is in flight,
    /// or joins the in-flight operation otherwise, then awaits the shared result under this caller's token.
    /// </summary>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="key">The key that identifies the operation.</param>
    /// <param name="operation">The asynchronous operation to run when this caller wins registration.</param>
    /// <param name="cancellationToken">A token that abandons this caller's wait on the shared result.</param>
    /// <returns>A task that yields the result of the operation for <paramref name="key" /> for this caller.</returns>
    /// <remarks>
    /// The winner starts a detached fulfillment task that runs <paramref name="operation" /> with
    /// <see cref="CancellationToken.None" />, completes the shared promise, and removes the in-flight entry, so the
    /// operation reaches completion for every joiner even when the winner abandons its own wait. Every caller awaits
    /// the shared promise through <see cref="Task{TResult}.WaitAsync(CancellationToken)" /> so cancellation is observed
    /// per caller and never propagates to the shared operation. The key is bound to a single result type by contract.
    /// </remarks>
    private Task<TResult> RunCoreAsync<TResult>(TKey key, Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
    {
        var promise = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task inFlight = _inFlight.GetOrAdd(key, promise.Task);

        if (ReferenceEquals(inFlight, promise.Task))
        {
            // This caller won registration; drive the operation independently of any caller's wait so it always
            // completes and fulfills the promise for joiners, even if every caller abandons its wait.
            _ = FulfillAsync(key, operation, promise);
        }

        // Every caller — winner and joiner alike — observes only its own cancellation while awaiting the shared result.
        return ((Task<TResult>)inFlight).WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Runs the operation to completion, fulfills the shared promise with its outcome, and releases the key.
    /// </summary>
    /// <param name="key">The key whose in-flight entry is released once the operation completes.</param>
    /// <param name="operation">
    /// The asynchronous operation to run, invoked with <see cref="CancellationToken.None" />.
    /// </param>
    /// <param name="promise">The shared promise every current and future joiner for the key awaits.</param>
    /// <returns>A task that completes when the operation has completed and the promise has been fulfilled.</returns>
    /// <remarks>
    /// Runs detached from the winning caller's wait. The operation receives <see cref="CancellationToken.None" /> so it
    /// runs to completion regardless of which caller cancels, and the in-flight entry is removed in the
    /// <see langword="finally" /> block so a later call for the key starts a fresh attempt.
    /// </remarks>
    private async Task FulfillAsync(TKey key, Func<CancellationToken, Task> operation, TaskCompletionSource promise)
    {
        try
        {
            await operation(CancellationToken.None).ConfigureAwait(false);
            promise.SetResult();
        }
        catch (Exception ex)
        {
            promise.SetException(ex);
        }
        finally
        {
            _inFlight.TryRemove(new KeyValuePair<TKey, Task>(key, promise.Task));
        }
    }

    /// <summary>
    /// Runs the result-producing operation to completion, fulfills the shared promise with its outcome, and releases
    /// the key.
    /// </summary>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="key">The key whose in-flight entry is released once the operation completes.</param>
    /// <param name="operation">
    /// The asynchronous operation to run, invoked with <see cref="CancellationToken.None" />.
    /// </param>
    /// <param name="promise">The shared promise every current and future joiner for the key awaits.</param>
    /// <returns>A task that completes when the operation has completed and the promise has been fulfilled.</returns>
    /// <remarks>
    /// Runs detached from the winning caller's wait. The operation receives <see cref="CancellationToken.None" /> so it
    /// runs to completion regardless of which caller cancels, and the in-flight entry is removed in the
    /// <see langword="finally" /> block so a later call for the key starts a fresh attempt.
    /// </remarks>
    private async Task FulfillAsync<TResult>(TKey key, Func<CancellationToken, Task<TResult>> operation, TaskCompletionSource<TResult> promise)
    {
        try
        {
            TResult result = await operation(CancellationToken.None).ConfigureAwait(false);
            promise.SetResult(result);
        }
        catch (Exception ex)
        {
            promise.SetException(ex);
        }
        finally
        {
            _inFlight.TryRemove(new KeyValuePair<TKey, Task>(key, promise.Task));
        }
    }
}
