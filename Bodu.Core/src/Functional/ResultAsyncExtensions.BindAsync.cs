// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultAsyncExtensions.BindAsync.cs" company="Bodu Pty. Ltd.">
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
    /// Asynchronously projects the value carried by the awaited result into another result using the specified
    /// result-returning selector.
    /// </summary>
    /// <typeparam name="T">The value type of the source result.</typeparam>
    /// <typeparam name="TResult">The value type of the resulting result.</typeparam>
    /// <param name="source">The task producing the result to project. Must not be <see langword="null" />.</param>
    /// <param name="selector">The result-returning projection applied to the carried value.</param>
    /// <returns>
    /// A task that completes with the result produced by the selector when the awaited result represents success;
    /// otherwise a failure carrying the original error.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the awaited result is a failure. Argument validation runs synchronously, so a
    /// <see langword="null" /> argument faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Result<TResult>> BindAsync<T, TResult>(this Task<Result<T>> source, Func<T, Result<TResult>> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);

        return BindCoreAsync(source, selector);

        static async Task<Result<TResult>> BindCoreAsync(Task<Result<T>> source, Func<T, Result<TResult>> selector)
        {
            var result = await source.ConfigureAwait(false);
            return result.Bind(selector);
        }
    }

    /// <summary>
    /// Asynchronously projects the value carried by the awaited result into another result using the specified
    /// asynchronous result-returning selector.
    /// </summary>
    /// <typeparam name="T">The value type of the source result.</typeparam>
    /// <typeparam name="TResult">The value type of the resulting result.</typeparam>
    /// <param name="source">The task producing the result to project. Must not be <see langword="null" />.</param>
    /// <param name="selector">The asynchronous result-returning projection applied to the carried value.</param>
    /// <returns>
    /// A task that completes with the result produced by the awaited selector when the awaited result represents
    /// success; otherwise a failure carrying the original error.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the awaited result is a failure. Argument validation runs synchronously, so a
    /// <see langword="null" /> argument faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Result<TResult>> BindAsync<T, TResult>(this Task<Result<T>> source, Func<T, Task<Result<TResult>>> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);

        return BindCoreAsync(source, selector);

        static async Task<Result<TResult>> BindCoreAsync(Task<Result<T>> source, Func<T, Task<Result<TResult>>> selector)
        {
            var result = await source.ConfigureAwait(false);
            if (!result.TryGetValue(out var value))
                return Result.Failure<TResult>(result.Error);

            return await selector(value).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously projects the carried value into another result using the specified asynchronous result-returning
    /// selector.
    /// </summary>
    /// <typeparam name="T">The value type of the source result.</typeparam>
    /// <typeparam name="TResult">The value type of the resulting result.</typeparam>
    /// <param name="source">The result to project.</param>
    /// <param name="selector">The asynchronous result-returning projection applied to the carried value.</param>
    /// <returns>
    /// A task that completes with the result produced by the awaited selector when the result represents success;
    /// otherwise a failure carrying the original error.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the result is a failure. Argument validation runs synchronously, so a
    /// <see langword="null" /> selector faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Result<TResult>> BindAsync<T, TResult>(this Result<T> source, Func<T, Task<Result<TResult>>> selector)
    {
        ThrowHelper.ThrowIfNull(selector);

        // The selector's task already has the exact result type, so it is returned directly — no state machine needed.
        return source.TryGetValue(out var value)
            ? selector(value)
            : Task.FromResult(Result.Failure<TResult>(source.Error));
    }
}
