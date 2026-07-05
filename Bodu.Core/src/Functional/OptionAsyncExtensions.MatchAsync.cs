// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionAsyncExtensions.MatchAsync.cs" company="Bodu Pty. Ltd.">
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
    /// Asynchronously collapses the awaited option to a single value by invoking the matching branch.
    /// </summary>
    /// <typeparam name="T">The value type of the source option.</typeparam>
    /// <typeparam name="TResult">The type produced by both branches.</typeparam>
    /// <param name="source">The task producing the option to collapse. Must not be <see langword="null" />.</param>
    /// <param name="onSome">The projection invoked with the contained value when one is present.</param>
    /// <param name="onNone">The factory invoked when no value is present.</param>
    /// <returns>A task that completes with the value produced by the invoked branch.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" />, <paramref name="onSome" />, or <paramref name="onNone" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Exactly one branch is invoked. Argument validation runs synchronously, so a <see langword="null" /> argument
    /// faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<TResult> MatchAsync<T, TResult>(this Task<Option<T>> source, Func<T, TResult> onSome, Func<TResult> onNone)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(onSome);
        ThrowHelper.ThrowIfNull(onNone);

        return MatchCoreAsync(source, onSome, onNone);

        static async Task<TResult> MatchCoreAsync(Task<Option<T>> source, Func<T, TResult> onSome, Func<TResult> onNone)
        {
            var option = await source.ConfigureAwait(false);
            return option.Match(onSome, onNone);
        }
    }

    /// <summary>
    /// Asynchronously collapses the awaited option to a single value by invoking and awaiting the matching asynchronous
    /// branch.
    /// </summary>
    /// <typeparam name="T">The value type of the source option.</typeparam>
    /// <typeparam name="TResult">The type produced by both branches.</typeparam>
    /// <param name="source">The task producing the option to collapse. Must not be <see langword="null" />.</param>
    /// <param name="onSome">The asynchronous projection invoked with the contained value when one is present.</param>
    /// <param name="onNone">The asynchronous factory invoked when no value is present.</param>
    /// <returns>A task that completes with the value produced by the awaited branch.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" />, <paramref name="onSome" />, or <paramref name="onNone" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Exactly one branch is invoked. Argument validation runs synchronously, so a <see langword="null" /> argument
    /// faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<TResult> MatchAsync<T, TResult>(this Task<Option<T>> source, Func<T, Task<TResult>> onSome, Func<Task<TResult>> onNone)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(onSome);
        ThrowHelper.ThrowIfNull(onNone);

        return MatchCoreAsync(source, onSome, onNone);

        static async Task<TResult> MatchCoreAsync(Task<Option<T>> source, Func<T, Task<TResult>> onSome, Func<Task<TResult>> onNone)
        {
            var option = await source.ConfigureAwait(false);
            return option.TryGetValue(out var value)
                ? await onSome(value).ConfigureAwait(false)
                : await onNone().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously collapses the option to a single value by invoking and awaiting the matching asynchronous branch.
    /// </summary>
    /// <typeparam name="T">The value type of the source option.</typeparam>
    /// <typeparam name="TResult">The type produced by both branches.</typeparam>
    /// <param name="source">The option to collapse.</param>
    /// <param name="onSome">The asynchronous projection invoked with the contained value when one is present.</param>
    /// <param name="onNone">The asynchronous factory invoked when no value is present.</param>
    /// <returns>A task that completes with the value produced by the awaited branch.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="onSome" /> or <paramref name="onNone" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Exactly one branch is invoked. Argument validation runs synchronously, so a <see langword="null" /> argument
    /// faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<TResult> MatchAsync<T, TResult>(this Option<T> source, Func<T, Task<TResult>> onSome, Func<Task<TResult>> onNone)
    {
        ThrowHelper.ThrowIfNull(onSome);
        ThrowHelper.ThrowIfNull(onNone);

        return MatchCoreAsync(source, onSome, onNone);

        static async Task<TResult> MatchCoreAsync(Option<T> source, Func<T, Task<TResult>> onSome, Func<Task<TResult>> onNone) =>
            source.TryGetValue(out var value)
                ? await onSome(value).ConfigureAwait(false)
                : await onNone().ConfigureAwait(false);
    }
}
