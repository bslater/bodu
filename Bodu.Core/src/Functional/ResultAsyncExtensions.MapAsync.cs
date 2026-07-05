// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultAsyncExtensions.MapAsync.cs" company="Bodu Pty. Ltd.">
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
    /// Asynchronously projects the value carried by the awaited result using the specified selector.
    /// </summary>
    /// <typeparam name="T">The value type of the source result.</typeparam>
    /// <typeparam name="TResult">The type produced by the selector.</typeparam>
    /// <param name="source">The task producing the result to project. Must not be <see langword="null" />.</param>
    /// <param name="selector">The projection applied to the carried value.</param>
    /// <returns>
    /// A task that completes with <c>Success(selector(value))</c> when the awaited result represents success; otherwise
    /// a failure carrying the original error.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />, or the projection
    /// returned <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the awaited result is a failure. The projected value is lifted through the
    /// strict <see cref="Result.Success{T}(T)" /> factory, so a <see langword="null" /> projection result throws
    /// <see cref="ArgumentNullException" />, matching <see cref="Result{T}.Map{TResult}(Func{T, TResult})" />. Argument
    /// validation runs synchronously, so a <see langword="null" /> argument faults at the call site rather than on the
    /// returned task.
    /// </para>
    /// </remarks>
    public static Task<Result<TResult>> MapAsync<T, TResult>(this Task<Result<T>> source, Func<T, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);

        return MapCoreAsync(source, selector);

        static async Task<Result<TResult>> MapCoreAsync(Task<Result<T>> source, Func<T, TResult> selector)
        {
            var result = await source.ConfigureAwait(false);
            return result.Map(selector);
        }
    }

    /// <summary>
    /// Asynchronously projects the value carried by the awaited result using the specified asynchronous selector.
    /// </summary>
    /// <typeparam name="T">The value type of the source result.</typeparam>
    /// <typeparam name="TResult">The type produced by the selector.</typeparam>
    /// <param name="source">The task producing the result to project. Must not be <see langword="null" />.</param>
    /// <param name="selector">The asynchronous projection applied to the carried value.</param>
    /// <returns>
    /// A task that completes with <c>Success(await selector(value))</c> when the awaited result represents success;
    /// otherwise a failure carrying the original error.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />, or the awaited projection
    /// produced <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the awaited result is a failure. The projected value is lifted through the
    /// strict <see cref="Result.Success{T}(T)" /> factory, so a <see langword="null" /> awaited projection result
    /// throws <see cref="ArgumentNullException" />, matching <see cref="Result{T}.Map{TResult}(Func{T, TResult})" />.
    /// Argument validation runs synchronously, so a <see langword="null" /> argument faults at the call site rather
    /// than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Result<TResult>> MapAsync<T, TResult>(this Task<Result<T>> source, Func<T, Task<TResult>> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);

        return MapCoreAsync(source, selector);

        static async Task<Result<TResult>> MapCoreAsync(Task<Result<T>> source, Func<T, Task<TResult>> selector)
        {
            var result = await source.ConfigureAwait(false);
            if (!result.TryGetValue(out var value))
                return Result.Failure<TResult>(result.Error);

            var projected = await selector(value).ConfigureAwait(false);
            return Result.Success(projected);
        }
    }

    /// <summary>
    /// Asynchronously projects the carried value using the specified asynchronous selector.
    /// </summary>
    /// <typeparam name="T">The value type of the source result.</typeparam>
    /// <typeparam name="TResult">The type produced by the selector.</typeparam>
    /// <param name="source">The result to project.</param>
    /// <param name="selector">The asynchronous projection applied to the carried value.</param>
    /// <returns>
    /// A task that completes with <c>Success(await selector(value))</c> when the result represents success; otherwise a
    /// failure carrying the original error.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="selector" /> is <see langword="null" />, or the awaited projection produced
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the result is a failure. The projected value is lifted through the strict
    /// <see cref="Result.Success{T}(T)" /> factory, so a <see langword="null" /> awaited projection result throws
    /// <see cref="ArgumentNullException" />, matching <see cref="Result{T}.Map{TResult}(Func{T, TResult})" />. Argument
    /// validation runs synchronously, so a <see langword="null" /> selector faults at the call site rather than on the
    /// returned task.
    /// </para>
    /// </remarks>
    public static Task<Result<TResult>> MapAsync<T, TResult>(this Result<T> source, Func<T, Task<TResult>> selector)
    {
        ThrowHelper.ThrowIfNull(selector);

        if (!source.TryGetValue(out var value))
            return Task.FromResult(Result.Failure<TResult>(source.Error));

        return MapCoreAsync(value, selector);

        static async Task<Result<TResult>> MapCoreAsync(T value, Func<T, Task<TResult>> selector)
        {
            var projected = await selector(value).ConfigureAwait(false);
            return Result.Success(projected);
        }
    }
}
