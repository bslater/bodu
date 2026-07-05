// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionAsyncExtensions.MapAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

// Awaiting the caller-supplied source task and the tasks produced by caller-supplied delegates is the entire purpose
// of these combinators; every await uses ConfigureAwait(false) and the library uses no JoinableTaskFactory, so the
// foreign-task deadlock VSTHRD003 guards against cannot arise.
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks

public static partial class OptionAsyncExtensions
{
    /// <summary>
    /// Asynchronously projects the value contained by the awaited option using the specified selector.
    /// </summary>
    /// <typeparam name="T">The value type of the source option.</typeparam>
    /// <typeparam name="TResult">The type produced by the selector.</typeparam>
    /// <param name="source">The task producing the option to project. Must not be <see langword="null" />.</param>
    /// <param name="selector">The projection applied to the contained value.</param>
    /// <returns>
    /// A task that completes with <c>Some(selector(value))</c> when the awaited option carries a value and the
    /// projection is non-<see langword="null" />; otherwise <c>None</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the awaited option is <c>None</c>. A <see langword="null" /> projection result
    /// maps to <c>None</c> (the lenient lift), matching <see cref="Option{T}.Map{TResult}(Func{T, TResult})" />.
    /// Argument validation runs synchronously, so a <see langword="null" /> argument faults at the call site rather
    /// than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Option<TResult>> MapAsync<T, TResult>(this Task<Option<T>> source, Func<T, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);

        return MapCoreAsync(source, selector);

        static async Task<Option<TResult>> MapCoreAsync(Task<Option<T>> source, Func<T, TResult> selector)
        {
            var option = await source.ConfigureAwait(false);
            return option.Map(selector);
        }
    }

    /// <summary>
    /// Asynchronously projects the value contained by the awaited option using the specified asynchronous selector.
    /// </summary>
    /// <typeparam name="T">The value type of the source option.</typeparam>
    /// <typeparam name="TResult">The type produced by the selector.</typeparam>
    /// <param name="source">The task producing the option to project. Must not be <see langword="null" />.</param>
    /// <param name="selector">The asynchronous projection applied to the contained value.</param>
    /// <returns>
    /// A task that completes with <c>Some(await selector(value))</c> when the awaited option carries a value and the
    /// awaited projection is non-<see langword="null" />; otherwise <c>None</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the awaited option is <c>None</c>. A <see langword="null" /> awaited projection
    /// result maps to <c>None</c> (the lenient lift), matching <see cref="Option{T}.Map{TResult}(Func{T, TResult})" />.
    /// Argument validation runs synchronously, so a <see langword="null" /> argument faults at the call site rather
    /// than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Option<TResult>> MapAsync<T, TResult>(this Task<Option<T>> source, Func<T, Task<TResult>> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);

        return MapCoreAsync(source, selector);

        static async Task<Option<TResult>> MapCoreAsync(Task<Option<T>> source, Func<T, Task<TResult>> selector)
        {
            var option = await source.ConfigureAwait(false);
            if (!option.TryGetValue(out var value))
                return Option<TResult>.None;

            return await selector(value).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously projects the contained value using the specified asynchronous selector.
    /// </summary>
    /// <typeparam name="T">The value type of the source option.</typeparam>
    /// <typeparam name="TResult">The type produced by the selector.</typeparam>
    /// <param name="source">The option to project.</param>
    /// <param name="selector">The asynchronous projection applied to the contained value.</param>
    /// <returns>
    /// A task that completes with <c>Some(await selector(value))</c> when the option carries a value and the awaited
    /// projection is non-<see langword="null" />; otherwise <c>None</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the option is <c>None</c>. A <see langword="null" /> awaited projection result
    /// maps to <c>None</c> (the lenient lift), matching <see cref="Option{T}.Map{TResult}(Func{T, TResult})" />.
    /// Argument validation runs synchronously, so a <see langword="null" /> selector faults at the call site rather
    /// than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Option<TResult>> MapAsync<T, TResult>(this Option<T> source, Func<T, Task<TResult>> selector)
    {
        ThrowHelper.ThrowIfNull(selector);

        if (!source.TryGetValue(out var value))
            return Task.FromResult(Option<TResult>.None);

        return MapCoreAsync(value, selector);

        static async Task<Option<TResult>> MapCoreAsync(T value, Func<T, Task<TResult>> selector) =>
            await selector(value).ConfigureAwait(false);
    }
}
