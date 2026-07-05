// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultAsyncExtensions.TapAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

// Awaiting the caller-supplied source task and the tasks produced by caller-supplied delegates is the entire purpose
// of these combinators; every await uses ConfigureAwait(false) and the library uses no JoinableTaskFactory, so the
// foreign-task deadlock VSTHRD003 guards against cannot arise.
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks

public static partial class ResultAsyncExtensions
{
    /// <summary>
    /// Asynchronously invokes the specified action with the carried value when the awaited result represents success.
    /// </summary>
    /// <typeparam name="T">The value type of the source result.</typeparam>
    /// <param name="source">The task producing the result to observe. Must not be <see langword="null" />.</param>
    /// <param name="onSuccess">The side effect invoked with the carried value.</param>
    /// <returns>A task that completes with the awaited result, unchanged, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> or <paramref name="onSuccess" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The action is not invoked when the awaited result is a failure; the result itself is always returned. Argument
    /// validation runs synchronously, so a <see langword="null" /> argument faults at the call site rather than on the
    /// returned task.
    /// </para>
    /// </remarks>
    public static Task<Result<T>> TapAsync<T>(this Task<Result<T>> source, Action<T> onSuccess)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(onSuccess);

        return TapCoreAsync(source, onSuccess);

        static async Task<Result<T>> TapCoreAsync(Task<Result<T>> source, Action<T> onSuccess)
        {
            var result = await source.ConfigureAwait(false);
            return result.Tap(onSuccess);
        }
    }

    /// <summary>
    /// Asynchronously invokes and awaits the specified asynchronous callback with the carried value when the awaited
    /// result represents success.
    /// </summary>
    /// <typeparam name="T">The value type of the source result.</typeparam>
    /// <param name="source">The task producing the result to observe. Must not be <see langword="null" />.</param>
    /// <param name="onSuccess">The asynchronous side effect invoked with the carried value.</param>
    /// <returns>A task that completes with the awaited result, unchanged, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> or <paramref name="onSuccess" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The callback is not invoked when the awaited result is a failure; the result itself is always returned, and the
    /// callback is awaited before the returned task completes. Argument validation runs synchronously, so a
    /// <see langword="null" /> argument faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Result<T>> TapAsync<T>(this Task<Result<T>> source, Func<T, Task> onSuccess)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(onSuccess);

        return TapCoreAsync(source, onSuccess);

        static async Task<Result<T>> TapCoreAsync(Task<Result<T>> source, Func<T, Task> onSuccess)
        {
            var result = await source.ConfigureAwait(false);
            if (result.TryGetValue(out var value))
                await onSuccess(value).ConfigureAwait(false);

            return result;
        }
    }

    /// <summary>
    /// Asynchronously invokes and awaits the specified asynchronous callback with the carried value when the result
    /// represents success.
    /// </summary>
    /// <typeparam name="T">The value type of the source result.</typeparam>
    /// <param name="source">The result to observe.</param>
    /// <param name="onSuccess">The asynchronous side effect invoked with the carried value.</param>
    /// <returns>A task that completes with this result, unchanged, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The callback is not invoked when the result is a failure; the result itself is always returned, and the callback
    /// is awaited before the returned task completes. Argument validation runs synchronously, so a
    /// <see langword="null" /> callback faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Result<T>> TapAsync<T>(this Result<T> source, Func<T, Task> onSuccess)
    {
        ThrowHelper.ThrowIfNull(onSuccess);

        if (!source.TryGetValue(out var value))
            return Task.FromResult(source);

        return TapCoreAsync(source, value, onSuccess);

        static async Task<Result<T>> TapCoreAsync(Result<T> source, T value, Func<T, Task> onSuccess)
        {
            await onSuccess(value).ConfigureAwait(false);
            return source;
        }
    }

    /// <summary>
    /// Asynchronously invokes the specified action with the carried error when the awaited result represents failure.
    /// </summary>
    /// <typeparam name="T">The value type of the source result.</typeparam>
    /// <param name="source">The task producing the result to observe. Must not be <see langword="null" />.</param>
    /// <param name="onFailure">The side effect invoked with the carried error.</param>
    /// <returns>A task that completes with the awaited result, unchanged, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> or <paramref name="onFailure" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The action is not invoked when the awaited result is a success; the result itself is always returned. Argument
    /// validation runs synchronously, so a <see langword="null" /> argument faults at the call site rather than on the
    /// returned task.
    /// </para>
    /// </remarks>
    public static Task<Result<T>> TapErrorAsync<T>(this Task<Result<T>> source, Action<ResultError> onFailure)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(onFailure);

        return TapErrorCoreAsync(source, onFailure);

        static async Task<Result<T>> TapErrorCoreAsync(Task<Result<T>> source, Action<ResultError> onFailure)
        {
            var result = await source.ConfigureAwait(false);
            return result.TapError(onFailure);
        }
    }

    /// <summary>
    /// Asynchronously invokes and awaits the specified asynchronous callback with the carried error when the awaited
    /// result represents failure.
    /// </summary>
    /// <typeparam name="T">The value type of the source result.</typeparam>
    /// <param name="source">The task producing the result to observe. Must not be <see langword="null" />.</param>
    /// <param name="onFailure">The asynchronous side effect invoked with the carried error.</param>
    /// <returns>A task that completes with the awaited result, unchanged, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> or <paramref name="onFailure" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The callback is not invoked when the awaited result is a success; the result itself is always returned, and the
    /// callback is awaited before the returned task completes. Argument validation runs synchronously, so a
    /// <see langword="null" /> argument faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Result<T>> TapErrorAsync<T>(this Task<Result<T>> source, Func<ResultError, Task> onFailure)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(onFailure);

        return TapErrorCoreAsync(source, onFailure);

        static async Task<Result<T>> TapErrorCoreAsync(Task<Result<T>> source, Func<ResultError, Task> onFailure)
        {
            var result = await source.ConfigureAwait(false);
            if (result.TryGetError(out var error))
                await onFailure(error).ConfigureAwait(false);

            return result;
        }
    }

    /// <summary>
    /// Asynchronously invokes and awaits the specified asynchronous callback with the carried error when the result
    /// represents failure.
    /// </summary>
    /// <typeparam name="T">The value type of the source result.</typeparam>
    /// <param name="source">The result to observe.</param>
    /// <param name="onFailure">The asynchronous side effect invoked with the carried error.</param>
    /// <returns>A task that completes with this result, unchanged, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onFailure" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The callback is not invoked when the result is a success; the result itself is always returned, and the callback
    /// is awaited before the returned task completes. Argument validation runs synchronously, so a
    /// <see langword="null" /> callback faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Result<T>> TapErrorAsync<T>(this Result<T> source, Func<ResultError, Task> onFailure)
    {
        ThrowHelper.ThrowIfNull(onFailure);

        if (!source.TryGetError(out var error))
            return Task.FromResult(source);

        return TapErrorCoreAsync(source, error, onFailure);

        static async Task<Result<T>> TapErrorCoreAsync(Result<T> source, ResultError error, Func<ResultError, Task> onFailure)
        {
            await onFailure(error).ConfigureAwait(false);
            return source;
        }
    }
}
