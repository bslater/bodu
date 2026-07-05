// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultAsyncExtensions.MatchAsync.cs" company="Bodu Pty. Ltd.">
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
    /// Asynchronously collapses the awaited result to a single value by invoking the matching branch.
    /// </summary>
    /// <typeparam name="T">The value type of the source result.</typeparam>
    /// <typeparam name="TResult">The type produced by both branches.</typeparam>
    /// <param name="source">The task producing the result to collapse. Must not be <see langword="null" />.</param>
    /// <param name="onSuccess">
    /// The projection invoked with the carried value when the result represents success.
    /// </param>
    /// <param name="onFailure">
    /// The projection invoked with the carried error when the result represents failure.
    /// </param>
    /// <returns>A task that completes with the value produced by the invoked branch.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" />, <paramref name="onSuccess" />, or <paramref name="onFailure" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Exactly one branch is invoked. Argument validation runs synchronously, so a <see langword="null" /> argument
    /// faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<TResult> MatchAsync<T, TResult>(this Task<Result<T>> source, Func<T, TResult> onSuccess, Func<ResultError, TResult> onFailure)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(onSuccess);
        ThrowHelper.ThrowIfNull(onFailure);

        return MatchCoreAsync(source, onSuccess, onFailure);

        static async Task<TResult> MatchCoreAsync(Task<Result<T>> source, Func<T, TResult> onSuccess, Func<ResultError, TResult> onFailure)
        {
            var result = await source.ConfigureAwait(false);
            return result.Match(onSuccess, onFailure);
        }
    }

    /// <summary>
    /// Asynchronously collapses the awaited result to a single value by invoking and awaiting the matching asynchronous
    /// branch.
    /// </summary>
    /// <typeparam name="T">The value type of the source result.</typeparam>
    /// <typeparam name="TResult">The type produced by both branches.</typeparam>
    /// <param name="source">The task producing the result to collapse. Must not be <see langword="null" />.</param>
    /// <param name="onSuccess">
    /// The asynchronous projection invoked with the carried value when the result represents success.
    /// </param>
    /// <param name="onFailure">
    /// The asynchronous projection invoked with the carried error when the result represents failure.
    /// </param>
    /// <returns>A task that completes with the value produced by the awaited branch.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" />, <paramref name="onSuccess" />, or <paramref name="onFailure" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Exactly one branch is invoked. Argument validation runs synchronously, so a <see langword="null" /> argument
    /// faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<TResult> MatchAsync<T, TResult>(this Task<Result<T>> source, Func<T, Task<TResult>> onSuccess, Func<ResultError, Task<TResult>> onFailure)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(onSuccess);
        ThrowHelper.ThrowIfNull(onFailure);

        return MatchCoreAsync(source, onSuccess, onFailure);

        static async Task<TResult> MatchCoreAsync(Task<Result<T>> source, Func<T, Task<TResult>> onSuccess, Func<ResultError, Task<TResult>> onFailure)
        {
            var result = await source.ConfigureAwait(false);
            return result.TryGetValue(out var value)
                ? await onSuccess(value).ConfigureAwait(false)
                : await onFailure(result.Error).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously collapses the result to a single value by invoking and awaiting the matching asynchronous branch.
    /// </summary>
    /// <typeparam name="T">The value type of the source result.</typeparam>
    /// <typeparam name="TResult">The type produced by both branches.</typeparam>
    /// <param name="source">The result to collapse.</param>
    /// <param name="onSuccess">
    /// The asynchronous projection invoked with the carried value when the result represents success.
    /// </param>
    /// <param name="onFailure">
    /// The asynchronous projection invoked with the carried error when the result represents failure.
    /// </param>
    /// <returns>A task that completes with the value produced by the awaited branch.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Exactly one branch is invoked. Argument validation runs synchronously, so a <see langword="null" /> argument
    /// faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<TResult> MatchAsync<T, TResult>(this Result<T> source, Func<T, Task<TResult>> onSuccess, Func<ResultError, Task<TResult>> onFailure)
    {
        ThrowHelper.ThrowIfNull(onSuccess);
        ThrowHelper.ThrowIfNull(onFailure);

        return MatchCoreAsync(source, onSuccess, onFailure);

        static async Task<TResult> MatchCoreAsync(Result<T> source, Func<T, Task<TResult>> onSuccess, Func<ResultError, Task<TResult>> onFailure) =>
            source.TryGetValue(out var value)
                ? await onSuccess(value).ConfigureAwait(false)
                : await onFailure(source.Error).ConfigureAwait(false);
    }
}
