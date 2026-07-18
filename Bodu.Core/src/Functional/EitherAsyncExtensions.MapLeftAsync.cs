// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherAsyncExtensions.MapLeftAsync.cs" company="Bodu Pty. Ltd.">
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
    /// Asynchronously projects the left value of the awaited either using the specified selector, passing a right value
    /// through untouched.
    /// </summary>
    /// <typeparam name="TLeft">The type of the value carried on the left side.</typeparam>
    /// <typeparam name="TRight">The type of the value carried on the right side.</typeparam>
    /// <typeparam name="TResult">The left type produced by the selector.</typeparam>
    /// <param name="source">The task producing the either to project. Must not be <see langword="null" />.</param>
    /// <param name="selector">The projection applied to the left value when the left side is active.</param>
    /// <returns>
    /// A task that completes with <c>Left(selector(value))</c> when the left side is active; otherwise a right-carrying
    /// either with the original right value.
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
    /// The selector is not invoked when the right side is active. The lift is strict, matching
    /// <see cref="Either{TLeft, TRight}.MapLeft{TResult}(Func{TLeft, TResult})" />. Argument validation runs
    /// synchronously, so a <see langword="null" /> argument faults at the call site rather than on the returned task.
    /// </para>
    /// </remarks>
    public static Task<Either<TResult, TRight>> MapLeftAsync<TLeft, TRight, TResult>(this Task<Either<TLeft, TRight>> source, Func<TLeft, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);

        return MapLeftCoreAsync(source, selector);

        static async Task<Either<TResult, TRight>> MapLeftCoreAsync(Task<Either<TLeft, TRight>> source, Func<TLeft, TResult> selector)
        {
            var either = await source.ConfigureAwait(false);
            return either.MapLeft(selector);
        }
    }

    /// <summary>
    /// Asynchronously projects the left value of the awaited either using the specified asynchronous selector, passing
    /// a right value through untouched.
    /// </summary>
    /// <typeparam name="TLeft">The type of the value carried on the left side.</typeparam>
    /// <typeparam name="TRight">The type of the value carried on the right side.</typeparam>
    /// <typeparam name="TResult">The left type produced by the selector.</typeparam>
    /// <param name="source">The task producing the either to project. Must not be <see langword="null" />.</param>
    /// <param name="selector">
    /// The asynchronous projection applied to the left value when the left side is active.
    /// </param>
    /// <returns>
    /// A task that completes with <c>Left(await selector(value))</c> when the left side is active; otherwise a
    /// right-carrying either with the original right value.
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
    /// The selector is not invoked when the right side is active. The lift is strict: a <see langword="null" /> awaited
    /// projection result is rejected by the <see cref="Either{TResult, TRight}.Left(TResult)" /> factory. Argument
    /// validation runs synchronously, so a <see langword="null" /> argument faults at the call site rather than on the
    /// returned task.
    /// </para>
    /// </remarks>
    public static Task<Either<TResult, TRight>> MapLeftAsync<TLeft, TRight, TResult>(this Task<Either<TLeft, TRight>> source, Func<TLeft, Task<TResult>> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);

        return MapLeftCoreAsync(source, selector);

        static async Task<Either<TResult, TRight>> MapLeftCoreAsync(Task<Either<TLeft, TRight>> source, Func<TLeft, Task<TResult>> selector)
        {
            var either = await source.ConfigureAwait(false);
            if (either.TryGetLeft(out var left))
                return Either<TResult, TRight>.Left(await selector(left).ConfigureAwait(false));
            if (either.TryGetRight(out var right))
                return Either<TResult, TRight>.Right(right);

            throw new InvalidOperationException(ResourceStrings.Op_Invalid_EitherUninitialized);
        }
    }

    /// <summary>
    /// Asynchronously projects the left value using the specified asynchronous selector, passing a right value through
    /// untouched.
    /// </summary>
    /// <typeparam name="TLeft">The type of the value carried on the left side.</typeparam>
    /// <typeparam name="TRight">The type of the value carried on the right side.</typeparam>
    /// <typeparam name="TResult">The left type produced by the selector.</typeparam>
    /// <param name="source">The either to project.</param>
    /// <param name="selector">
    /// The asynchronous projection applied to the left value when the left side is active.
    /// </param>
    /// <returns>
    /// A task that completes with <c>Left(await selector(value))</c> when the left side is active; otherwise a
    /// right-carrying either with the original right value.
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
    /// The selector is not invoked when the right side is active. The lift is strict: a <see langword="null" /> awaited
    /// projection result is rejected by the <see cref="Either{TResult, TRight}.Left(TResult)" /> factory. Argument
    /// validation runs synchronously, so a <see langword="null" /> selector faults at the call site rather than on the
    /// returned task.
    /// </para>
    /// </remarks>
    public static Task<Either<TResult, TRight>> MapLeftAsync<TLeft, TRight, TResult>(this Either<TLeft, TRight> source, Func<TLeft, Task<TResult>> selector)
    {
        ThrowHelper.ThrowIfNull(selector);

        return MapLeftCoreAsync(source, selector);

        static async Task<Either<TResult, TRight>> MapLeftCoreAsync(Either<TLeft, TRight> source, Func<TLeft, Task<TResult>> selector)
        {
            if (source.TryGetLeft(out var left))
                return Either<TResult, TRight>.Left(await selector(left).ConfigureAwait(false));
            if (source.TryGetRight(out var right))
                return Either<TResult, TRight>.Right(right);

            throw new InvalidOperationException(ResourceStrings.Op_Invalid_EitherUninitialized);
        }
    }
}
