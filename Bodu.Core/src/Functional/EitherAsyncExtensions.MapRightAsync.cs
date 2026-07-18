// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherAsyncExtensions.MapRightAsync.cs" company="Bodu Pty. Ltd.">
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
    /// Asynchronously projects the right value of the awaited either using the specified selector, passing a left value
    /// through untouched.
    /// </summary>
    /// <typeparam name="TLeft">The type of the value carried on the left side.</typeparam>
    /// <typeparam name="TRight">The type of the value carried on the right side.</typeparam>
    /// <typeparam name="TResult">The right type produced by the selector.</typeparam>
    /// <param name="source">The task producing the either to project. Must not be <see langword="null" />.</param>
    /// <param name="selector">The projection applied to the right value when the right side is active.</param>
    /// <returns>
    /// A task that completes with <c>Right(selector(value))</c> when the right side is active; otherwise a
    /// left-carrying either with the original left value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />, or the projection
    /// returned <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The awaited either is <c>default(Either&lt;TLeft, TRight&gt;)</c> and carries neither side.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the left side is active. The lift is strict, matching
    /// <see cref="Either{TLeft, TRight}.MapRight{TResult}(Func{TRight, TResult})" />. Argument validation runs
    /// synchronously, so a <see langword="null" /> argument faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Either<TLeft, TResult>> MapRightAsync<TLeft, TRight, TResult>(this Task<Either<TLeft, TRight>> source, Func<TRight, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);

        return MapRightCoreAsync(source, selector);

        static async Task<Either<TLeft, TResult>> MapRightCoreAsync(Task<Either<TLeft, TRight>> source, Func<TRight, TResult> selector)
        {
            var either = await source.ConfigureAwait(false);
            return either.MapRight(selector);
        }
    }

    /// <summary>
    /// Asynchronously projects the right value of the awaited either using the specified asynchronous selector, passing
    /// a left value through untouched.
    /// </summary>
    /// <typeparam name="TLeft">The type of the value carried on the left side.</typeparam>
    /// <typeparam name="TRight">The type of the value carried on the right side.</typeparam>
    /// <typeparam name="TResult">The right type produced by the selector.</typeparam>
    /// <param name="source">The task producing the either to project. Must not be <see langword="null" />.</param>
    /// <param name="selector">
    /// The asynchronous projection applied to the right value when the right side is active.
    /// </param>
    /// <returns>
    /// A task that completes with <c>Right(await selector(value))</c> when the right side is active; otherwise a
    /// left-carrying either with the original left value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />, or the awaited projection
    /// produced <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The awaited either is <c>default(Either&lt;TLeft, TRight&gt;)</c> and carries neither side.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the left side is active. The lift is strict: a <see langword="null" /> awaited
    /// projection result is rejected by the <see cref="Either{TLeft, TResult}.Right(TResult)" /> factory. Argument
    /// validation runs synchronously, so a <see langword="null" /> argument faults at the call site rather than on the
    /// returned task.
    /// </para>
    /// </remarks>
    public static Task<Either<TLeft, TResult>> MapRightAsync<TLeft, TRight, TResult>(this Task<Either<TLeft, TRight>> source, Func<TRight, Task<TResult>> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);

        return MapRightCoreAsync(source, selector);

        static async Task<Either<TLeft, TResult>> MapRightCoreAsync(Task<Either<TLeft, TRight>> source, Func<TRight, Task<TResult>> selector)
        {
            var either = await source.ConfigureAwait(false);
            if (either.TryGetRight(out var right))
                return Either<TLeft, TResult>.Right(await selector(right).ConfigureAwait(false));
            if (either.TryGetLeft(out var left))
                return Either<TLeft, TResult>.Left(left);

            throw new InvalidOperationException(ResourceStrings.Op_Invalid_EitherUninitialized);
        }
    }

    /// <summary>
    /// Asynchronously projects the right value using the specified asynchronous selector, passing a left value through
    /// untouched.
    /// </summary>
    /// <typeparam name="TLeft">The type of the value carried on the left side.</typeparam>
    /// <typeparam name="TRight">The type of the value carried on the right side.</typeparam>
    /// <typeparam name="TResult">The right type produced by the selector.</typeparam>
    /// <param name="source">The either to project.</param>
    /// <param name="selector">
    /// The asynchronous projection applied to the right value when the right side is active.
    /// </param>
    /// <returns>
    /// A task that completes with <c>Right(await selector(value))</c> when the right side is active; otherwise a
    /// left-carrying either with the original left value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="selector" /> is <see langword="null" />, or the awaited projection produced
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The either is <c>default(Either&lt;TLeft, TRight&gt;)</c> and carries neither side.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selector is not invoked when the left side is active. The lift is strict: a <see langword="null" /> awaited
    /// projection result is rejected by the <see cref="Either{TLeft, TResult}.Right(TResult)" /> factory. Argument
    /// validation runs synchronously, so a <see langword="null" /> selector faults at the call site rather than on the
    /// returned task.
    /// </para>
    /// </remarks>
    public static Task<Either<TLeft, TResult>> MapRightAsync<TLeft, TRight, TResult>(this Either<TLeft, TRight> source, Func<TRight, Task<TResult>> selector)
    {
        ThrowHelper.ThrowIfNull(selector);

        return MapRightCoreAsync(source, selector);

        static async Task<Either<TLeft, TResult>> MapRightCoreAsync(Either<TLeft, TRight> source, Func<TRight, Task<TResult>> selector)
        {
            if (source.TryGetRight(out var right))
                return Either<TLeft, TResult>.Right(await selector(right).ConfigureAwait(false));
            if (source.TryGetLeft(out var left))
                return Either<TLeft, TResult>.Left(left);

            throw new InvalidOperationException(ResourceStrings.Op_Invalid_EitherUninitialized);
        }
    }
}
