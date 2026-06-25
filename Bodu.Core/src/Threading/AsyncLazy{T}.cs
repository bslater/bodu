// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLazy{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Bodu.Threading;

/// <summary>
/// Provides support for asynchronous lazy initialization: a value is produced at most once, on first access, and the
/// resulting task is cached and shared by every awaiter.
/// </summary>
/// <typeparam name="T">The type of the lazily produced value.</typeparam>
/// <remarks>
/// <para>
/// <see cref="AsyncLazy{T}" /> is the asynchronous analogue of <see cref="Lazy{T}" />. It wraps a
/// <see cref="Lazy{T}" /> of <see cref="Task{TResult}" /> with
/// <see cref="LazyThreadSafetyMode.ExecutionAndPublication" />, so the factory runs exactly once even under concurrent
/// first access. The produced <see cref="Task{TResult}" /> is cached and may be awaited any number of times, which is
/// why this type exposes <see cref="Task{TResult}" /> rather than a single-await <see cref="ValueTask{TResult}" />.
/// </para>
/// <para>
/// When constructed from a synchronous <see cref="Func{TResult}" />, the factory is offloaded to the thread pool via
/// <see cref="Task.Run{TResult}(Func{TResult})" /> so a blocking or CPU-bound factory does not run inline on the first
/// awaiter. A factory that throws produces a faulted task that is cached; every awaiter then observes the same
/// exception.
/// </para>
/// <para>
/// The shared computation is not canceled by any single caller. Use <see cref="GetValueAsync(CancellationToken)" /> to
/// abandon an individual wait without affecting the shared factory. A factory that attempts to obtain the value of the
/// same instance while it is still being produced is detected and fails fast with
/// <see cref="InvalidOperationException" /> rather than deadlocking.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// private readonly AsyncLazy<Config> _config =
///     new(async () => await LoadConfigAsync());
///
/// public async Task<int> GetTimeoutAsync()
/// {
///     Config config = await _config;   // factory runs once, result is cached
///     return config.TimeoutSeconds;
/// }
///]]>
/// </example>
[DebuggerDisplay("IsValueCreated = {IsValueCreated}, IsValueFactoryCompleted = {IsValueFactoryCompleted}")]
public sealed class AsyncLazy<T>
{
    /// <summary>The lazily initialized task that produces the value exactly once on first access.</summary>
    private readonly Lazy<Task<T>> _instance;

    /// <summary>Tracks whether the value factory is currently running, to detect reentrant access.</summary>
    private readonly AsyncLocal<bool> _factoryRunning = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncLazy{T}" /> class that produces its value with a synchronous
    /// factory offloaded to the thread pool.
    /// </summary>
    /// <param name="valueFactory">The delegate invoked once to produce the value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="valueFactory" /> is <see langword="null" />.</exception>
    [SuppressMessage(
        "Usage",
        "VSTHRD011:Use AsyncLazy<T>",
        Justification = "This type is the AsyncLazy<T> implementation; it intentionally wraps Lazy<Task<T>> to cache and share the initialization task, which is the pattern VSTHRD011 recommends.")]
    public AsyncLazy(Func<T> valueFactory)
    {
        ThrowHelper.ThrowIfNull(valueFactory);

        _instance = new Lazy<Task<T>>(() => RunFactoryAsync(() => Task.Run(valueFactory)), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncLazy{T}" /> class that produces its value with an asynchronous
    /// factory.
    /// </summary>
    /// <param name="taskFactory">The delegate invoked once to begin producing the value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="taskFactory" /> is <see langword="null" />.</exception>
    [SuppressMessage(
        "Usage",
        "VSTHRD011:Use AsyncLazy<T>",
        Justification = "This type is the AsyncLazy<T> implementation; it intentionally wraps Lazy<Task<T>> to cache and share the initialization task, which is the pattern VSTHRD011 recommends.")]
    public AsyncLazy(Func<Task<T>> taskFactory)
    {
        ThrowHelper.ThrowIfNull(taskFactory);

        _instance = new Lazy<Task<T>>(() => RunFactoryAsync(taskFactory), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Gets a value indicating whether the factory has been invoked.
    /// </summary>
    /// <value><see langword="true" /> if initialization has started; otherwise, <see langword="false" />.</value>
    public bool IsValueCreated =>
        _instance.IsValueCreated;

    /// <summary>
    /// Gets a value indicating whether the factory has finished producing the value (successfully or with a fault).
    /// </summary>
    /// <value><see langword="true" /> if the value task has completed; otherwise, <see langword="false" />.</value>
    public bool IsValueFactoryCompleted =>
        _instance.IsValueCreated && _instance.Value.IsCompleted;

    /// <summary>
    /// Gets the cached task that produces the value, invoking the factory on first access.
    /// </summary>
    /// <value>The shared <see cref="Task{TResult}" /> representing the lazily produced value.</value>
    /// <exception cref="InvalidOperationException">
    /// The value factory accessed the value of the same instance while it was being produced.
    /// </exception>
    public Task<T> Value =>
        GetSharedTaskAsync();

    /// <summary>
    /// Gets an awaiter that resolves to the lazily produced value, enabling <c>await</c> on the instance directly.
    /// </summary>
    /// <returns>A <see cref="TaskAwaiter{TResult}" /> for the cached value task.</returns>
    /// <exception cref="InvalidOperationException">
    /// The value factory accessed the value of the same instance while it was being produced.
    /// </exception>
    public TaskAwaiter<T> GetAwaiter() =>
        GetSharedTaskAsync().GetAwaiter();

    /// <summary>
    /// Configures how the await on the lazily produced value is continued.
    /// </summary>
    /// <param name="continueOnCapturedContext">
    /// <see langword="true" /> to marshal the continuation back to the captured context; otherwise,
    /// <see langword="false" />.
    /// </param>
    /// <returns>A configured awaitable for the cached value task.</returns>
    /// <exception cref="InvalidOperationException">
    /// The value factory accessed the value of the same instance while it was being produced.
    /// </exception>
    public ConfiguredTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext) =>
        GetSharedTaskAsync().ConfigureAwait(continueOnCapturedContext);

    /// <summary>
    /// Asynchronously gets the lazily produced value, abandoning only this caller's wait if the token is canceled.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel this caller's wait.</param>
    /// <returns>A <see cref="Task{TResult}" /> that completes with the shared value.</returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken" /> was canceled before the value became available.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The value factory accessed the value of the same instance while it was being produced.
    /// </exception>
    /// <remarks>
    /// Cancellation cancels only the returned wait; the shared initialization continues for other callers.
    /// </remarks>
    /// <example>
    ///<![CDATA[
    /// // Abandon only this caller's wait on timeout; the shared factory keeps running for others.
    /// using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    /// Config config = await lazy.GetValueAsync(cts.Token);
    ///]]>
    /// </example>
    public Task<T> GetValueAsync(CancellationToken cancellationToken)
    {
        Task<T> task = GetSharedTaskAsync();
        return task.IsCompleted || !cancellationToken.CanBeCanceled
            ? task
            : task.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Returns the shared value task, rejecting reentrant access from the value factory of the same instance.
    /// </summary>
    /// <returns>The cached <see cref="Task{TResult}" /> for the lazily produced value.</returns>
    /// <exception cref="InvalidOperationException">
    /// The value factory accessed the value of the same instance while it was being produced.
    /// </exception>
    private Task<T> GetSharedTaskAsync()
    {
        if (_factoryRunning.Value)
            throw new InvalidOperationException(ResourceStrings.Op_Invalid_AsyncLazyReentrant);

        return _instance.Value;
    }

    /// <summary>
    /// Runs the factory in an execution flow marked for reentrancy detection. The initial yield ensures the marker is
    /// established off the triggering caller's context so it is observed only within the factory's own flow.
    /// </summary>
    /// <param name="factory">The delegate that begins producing the value.</param>
    /// <returns>A <see cref="Task{TResult}" /> for the produced value.</returns>
    private async Task<T> RunFactoryAsync(Func<Task<T>> factory)
    {
        await Task.Yield();

        _factoryRunning.Value = true;
        return await factory().ConfigureAwait(false);
    }
}
