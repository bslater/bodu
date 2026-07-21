// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherAsyncExtensions.MatchAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

// Awaiting the caller-supplied source task and the tasks produced by caller-supplied delegates is the entire purpose
// of these combinators; every await uses ConfigureAwait(false) and the library uses no JoinableTaskFactory, so the
// foreign-task deadlock VSTHRD003 guards against cannot arise.
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks

public static partial class EitherAsyncExtensions
{
    /// <summary>
    /// Asynchronously collapses the awaited either to a single value by invoking the branch for the active side.
    /// </summary>
    /// <typeparam name="TLeft">The type of the value carried on the left side.</typeparam>
    /// <typeparam name="TRight">The type of the value carried on the right side.</typeparam>
    /// <typeparam name="TResult">The type produced by both branches.</typeparam>
    /// <param name="source">The task producing the either to collapse. Must not be <see langword="null" />.</param>
    /// <param name="onLeft">The projection invoked with the left value when the left side is active.</param>
    /// <param name="onRight">The projection invoked with the right value when the right side is active.</param>
    /// <returns>A task that completes with the value produced by the invoked branch.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" />, <paramref name="onLeft" />, or <paramref name="onRight" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The awaited either is <c>default(Either&lt;TLeft, TRight&gt;)</c> and carries neither side.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Exactly one branch is invoked. Argument validation runs synchronously, so a <see langword="null" /> argument
    /// faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<TResult> MatchAsync<TLeft, TRight, TResult>(this Task<Either<TLeft, TRight>> source, Func<TLeft, TResult> onLeft, Func<TRight, TResult> onRight)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(onLeft);
        ThrowHelper.ThrowIfNull(onRight);

        return MatchCoreAsync(source, onLeft, onRight);

        static async Task<TResult> MatchCoreAsync(Task<Either<TLeft, TRight>> source, Func<TLeft, TResult> onLeft, Func<TRight, TResult> onRight)
        {
            var either = await source.ConfigureAwait(false);
            return either.Match(onLeft, onRight);
        }
    }

    /// <summary>
    /// Asynchronously collapses the awaited either to a single value by invoking and awaiting the asynchronous branch
    /// for the active side.
    /// </summary>
    /// <typeparam name="TLeft">The type of the value carried on the left side.</typeparam>
    /// <typeparam name="TRight">The type of the value carried on the right side.</typeparam>
    /// <typeparam name="TResult">The type produced by both branches.</typeparam>
    /// <param name="source">The task producing the either to collapse. Must not be <see langword="null" />.</param>
    /// <param name="onLeft">
    /// The asynchronous projection invoked with the left value when the left side is active.
    /// </param>
    /// <param name="onRight">
    /// The asynchronous projection invoked with the right value when the right side is active.
    /// </param>
    /// <returns>A task that completes with the value produced by the awaited branch.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" />, <paramref name="onLeft" />, or <paramref name="onRight" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The awaited either is <c>default(Either&lt;TLeft, TRight&gt;)</c> and carries neither side.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Exactly one branch is invoked. Argument validation runs synchronously, so a <see langword="null" /> argument
    /// faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<TResult> MatchAsync<TLeft, TRight, TResult>(this Task<Either<TLeft, TRight>> source, Func<TLeft, Task<TResult>> onLeft, Func<TRight, Task<TResult>> onRight)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(onLeft);
        ThrowHelper.ThrowIfNull(onRight);

        return MatchCoreAsync(source, onLeft, onRight);

        static async Task<TResult> MatchCoreAsync(Task<Either<TLeft, TRight>> source, Func<TLeft, Task<TResult>> onLeft, Func<TRight, Task<TResult>> onRight)
        {
            var either = await source.ConfigureAwait(false);
            if (either.TryGetLeft(out var left))
                return await onLeft(left).ConfigureAwait(false);
            if (either.TryGetRight(out var right))
                return await onRight(right).ConfigureAwait(false);

            throw new InvalidOperationException(ResourceStrings.Op_Invalid_EitherUninitialized);
        }
    }

    /// <summary>
    /// Asynchronously collapses the either to a single value by invoking and awaiting the asynchronous branch for the
    /// active side.
    /// </summary>
    /// <typeparam name="TLeft">The type of the value carried on the left side.</typeparam>
    /// <typeparam name="TRight">The type of the value carried on the right side.</typeparam>
    /// <typeparam name="TResult">The type produced by both branches.</typeparam>
    /// <param name="source">The either to collapse.</param>
    /// <param name="onLeft">
    /// The asynchronous projection invoked with the left value when the left side is active.
    /// </param>
    /// <param name="onRight">
    /// The asynchronous projection invoked with the right value when the right side is active.
    /// </param>
    /// <returns>A task that completes with the value produced by the awaited branch.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="onLeft" /> or <paramref name="onRight" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The either is <c>default(Either&lt;TLeft, TRight&gt;)</c> and carries neither side.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Exactly one branch is invoked. Argument validation runs synchronously, so a <see langword="null" /> argument
    /// faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<TResult> MatchAsync<TLeft, TRight, TResult>(this Either<TLeft, TRight> source, Func<TLeft, Task<TResult>> onLeft, Func<TRight, Task<TResult>> onRight)
    {
        ThrowHelper.ThrowIfNull(onLeft);
        ThrowHelper.ThrowIfNull(onRight);

        if (source.TryGetLeft(out var left))
            return onLeft(left);
        if (source.TryGetRight(out var right))
            return onRight(right);

        throw new InvalidOperationException(ResourceStrings.Op_Invalid_EitherUninitialized);
    }
}
