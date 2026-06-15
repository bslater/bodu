// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SingleFlightCoordinator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Financial;

/// <summary>
/// Coordinates concurrent asynchronous operations keyed by <typeparamref name="TKey" /> so that at most one operation
/// per key runs at a time, with every concurrent caller for that key awaiting the same shared task.
/// </summary>
/// <typeparam name="TKey">The type that identifies an operation; equal keys share one in-flight result.</typeparam>
/// <remarks>
/// <para>
/// This is the classic single-flight (request-coalescing) pattern: when several callers request the same key while a
/// fetch is already running, they join the running task instead of starting duplicate work. The in-flight entry is
/// removed as soon as the task completes — including when it faults or is cancelled — so a failure never poisons the
/// key and the next caller starts a fresh attempt. Callers that join an in-flight operation observe its outcome,
/// including its exception.
/// </para>
/// <para>
/// A given key must always be used with the same result type. Instances are safe for concurrent use.
/// </para>
/// </remarks>
public sealed class SingleFlightCoordinator<TKey>
    where TKey : notnull
{
    /// <summary>
    /// The promise tasks for operations currently in flight, keyed by <typeparamref name="TKey" />. The first caller to
    /// register a key owns running the operation and completing the promise; concurrent callers await the same promise.
    /// </summary>
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

        return RunCoreAsync(key, operation);
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

        return RunCoreAsync(key, operation);
    }

    /// <summary>
    /// Registers and runs the operation for <paramref name="key" /> when no operation is in flight, or joins and awaits
    /// the in-flight operation otherwise.
    /// </summary>
    /// <param name="key">The key that identifies the operation.</param>
    /// <param name="operation">The asynchronous operation to run when this caller wins registration.</param>
    /// <returns>A task that completes when the operation for <paramref name="key" /> completes.</returns>
    private async Task RunCoreAsync(TKey key, Func<Task> operation)
    {
        var promise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task inFlight = _inFlight.GetOrAdd(key, promise.Task);

        if (!ReferenceEquals(inFlight, promise.Task))
        {
            // Another caller registered first; join its operation and observe the shared outcome.
            await inFlight.ConfigureAwait(false);
            return;
        }

        // This caller won registration and owns running the operation and fulfilling the promise.
        try
        {
            await operation().ConfigureAwait(false);
            promise.SetResult();
        }
        catch (Exception ex)
        {
            promise.SetException(ex);
            throw;
        }
        finally
        {
            _inFlight.TryRemove(new KeyValuePair<TKey, Task>(key, promise.Task));
        }
    }

    /// <summary>
    /// Registers and runs the result-producing operation for <paramref name="key" /> when no operation is in flight, or
    /// joins and awaits the in-flight operation otherwise.
    /// </summary>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="key">The key that identifies the operation.</param>
    /// <param name="operation">The asynchronous operation to run when this caller wins registration.</param>
    /// <returns>A task that yields the result of the operation for <paramref name="key" />.</returns>
    private async Task<TResult> RunCoreAsync<TResult>(TKey key, Func<Task<TResult>> operation)
    {
        var promise = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task inFlight = _inFlight.GetOrAdd(key, promise.Task);

        if (!ReferenceEquals(inFlight, promise.Task))
        {
            // Another caller registered first; join its operation. The key is bound to a single result type by contract.
            return await ((Task<TResult>)inFlight).ConfigureAwait(false);
        }

        // This caller won registration and owns running the operation and fulfilling the promise.
        try
        {
            TResult result = await operation().ConfigureAwait(false);
            promise.SetResult(result);
            return result;
        }
        catch (Exception ex)
        {
            promise.SetException(ex);
            throw;
        }
        finally
        {
            _inFlight.TryRemove(new KeyValuePair<TKey, Task>(key, promise.Task));
        }
    }
}
