// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionAsyncExtensions.BindAsync.cs" company="Bodu Pty. Ltd.">
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
    /// Asynchronously projects the value contained by the awaited option into another option using the specified
    /// option-returning selector.
    /// </summary>
    /// <typeparam name="T">The value type of the source option.</typeparam>
    /// <typeparam name="TResult">The value type of the resulting option.</typeparam>
    /// <param name="source">The task producing the option to project. Must not be <see langword="null" />.</param>
    /// <param name="selector">The option-returning projection applied to the contained value.</param>
    /// <returns>
    /// A task that completes with the option produced by the selector when the awaited option carries a value;
    /// otherwise <c>None</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the awaited option is <c>None</c>. Argument validation runs synchronously, so a
    /// <see langword="null" /> argument faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Option<TResult>> BindAsync<T, TResult>(this Task<Option<T>> source, Func<T, Option<TResult>> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);

        return BindCoreAsync(source, selector);

        static async Task<Option<TResult>> BindCoreAsync(Task<Option<T>> source, Func<T, Option<TResult>> selector)
        {
            var option = await source.ConfigureAwait(false);
            return option.Bind(selector);
        }
    }

    /// <summary>
    /// Asynchronously projects the value contained by the awaited option into another option using the specified
    /// asynchronous option-returning selector.
    /// </summary>
    /// <typeparam name="T">The value type of the source option.</typeparam>
    /// <typeparam name="TResult">The value type of the resulting option.</typeparam>
    /// <param name="source">The task producing the option to project. Must not be <see langword="null" />.</param>
    /// <param name="selector">The asynchronous option-returning projection applied to the contained value.</param>
    /// <returns>
    /// A task that completes with the option produced by the awaited selector when the awaited option carries a value;
    /// otherwise <c>None</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the awaited option is <c>None</c>. Argument validation runs synchronously, so a
    /// <see langword="null" /> argument faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Option<TResult>> BindAsync<T, TResult>(this Task<Option<T>> source, Func<T, Task<Option<TResult>>> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);

        return BindCoreAsync(source, selector);

        static async Task<Option<TResult>> BindCoreAsync(Task<Option<T>> source, Func<T, Task<Option<TResult>>> selector)
        {
            var option = await source.ConfigureAwait(false);
            if (!option.TryGetValue(out var value))
                return Option<TResult>.None;

            return await selector(value).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously projects the contained value into another option using the specified asynchronous
    /// option-returning selector.
    /// </summary>
    /// <typeparam name="T">The value type of the source option.</typeparam>
    /// <typeparam name="TResult">The value type of the resulting option.</typeparam>
    /// <param name="source">The option to project.</param>
    /// <param name="selector">The asynchronous option-returning projection applied to the contained value.</param>
    /// <returns>
    /// A task that completes with the option produced by the awaited selector when the option carries a value;
    /// otherwise <c>None</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the option is <c>None</c>. Argument validation runs synchronously, so a
    /// <see langword="null" /> selector faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Option<TResult>> BindAsync<T, TResult>(this Option<T> source, Func<T, Task<Option<TResult>>> selector)
    {
        ThrowHelper.ThrowIfNull(selector);

        // The selector's task already has the exact result type, so it is returned directly — no state machine needed.
        return source.TryGetValue(out var value)
            ? selector(value)
            : Task.FromResult(Option<TResult>.None);
    }
}
