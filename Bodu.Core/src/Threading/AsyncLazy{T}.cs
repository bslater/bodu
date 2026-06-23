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
/// <see cref="Lazy{T}" /> of <see cref="Task{TResult}" /> with <see cref="LazyThreadSafetyMode.ExecutionAndPublication" />,
/// so the factory runs exactly once even under concurrent first access. The produced <see cref="Task{TResult}" /> is
/// cached and may be awaited any number of times, which is why this type exposes <see cref="Task{TResult}" /> rather
/// than a single-await <see cref="ValueTask{TResult}" />.
/// </para>
/// <para>
/// When constructed from a synchronous <see cref="Func{TResult}" />, the factory is offloaded to the thread pool via
/// <see cref="Task.Run{TResult}(Func{TResult})" /> so a blocking or CPU-bound factory does not run inline on the
/// first awaiter. A factory that throws produces a faulted task that is cached; every awaiter then observes the same
/// exception. There is no per-caller cancellation because the computation is shared.
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
[DebuggerDisplay("IsValueCreated = {IsValueCreated}")]
public sealed class AsyncLazy<T>
{
    private readonly Lazy<Task<T>> _instance;

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

        _instance = new Lazy<Task<T>>(() => Task.Run(valueFactory), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncLazy{T}" /> class that produces its value with an
    /// asynchronous factory.
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

        _instance = new Lazy<Task<T>>(taskFactory, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Gets a value indicating whether the factory has been invoked.
    /// </summary>
    /// <value><see langword="true" /> if initialization has started; otherwise, <see langword="false" />.</value>
    /// <returns><see langword="true" /> if the underlying value has been created; otherwise, <see langword="false" />.</returns>
    public bool IsValueCreated =>
        _instance.IsValueCreated;

    /// <summary>
    /// Gets the cached task that produces the value, invoking the factory on first access.
    /// </summary>
    /// <value>The shared <see cref="Task{TResult}" /> representing the lazily produced value.</value>
    /// <returns>The cached <see cref="Task{TResult}" /> for the lazily produced value.</returns>
    public Task<T> Value =>
        _instance.Value;

    /// <summary>
    /// Gets an awaiter that resolves to the lazily produced value, enabling <c>await</c> on the instance directly.
    /// </summary>
    /// <returns>A <see cref="TaskAwaiter{TResult}" /> for the cached value task.</returns>
    public TaskAwaiter<T> GetAwaiter() =>
        _instance.Value.GetAwaiter();

    /// <summary>
    /// Configures how the await on the lazily produced value is continued.
    /// </summary>
    /// <param name="continueOnCapturedContext"><see langword="true" /> to marshal the continuation back to the captured context; otherwise, <see langword="false" />.</param>
    /// <returns>A configured awaitable for the cached value task.</returns>
    public ConfiguredTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext) =>
        _instance.Value.ConfigureAwait(continueOnCapturedContext);
}
