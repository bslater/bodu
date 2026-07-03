// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Scan.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Emits the running accumulator state after folding each element of the source sequence, starting from a seed.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
    /// <param name="source">The source sequence to fold.</param>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="accumulator">A function that combines the running accumulator with the next element.</param>
    /// <returns>
    /// A sequence containing the accumulator state produced after each source element is folded. The result is empty
    /// when <paramref name="source" /> is empty; the <paramref name="seed" /> is never emitted on its own.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="accumulator" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses deferred execution. The source sequence is not enumerated until the returned sequence is
    /// enumerated, and it is enumerated exactly once in a single forward pass using only O(1) working memory.
    /// </para>
    /// <para>
    /// This method differs from <see cref="System.Linq.Enumerable.Aggregate{TSource, TAccumulate}(IEnumerable{TSource}, TAccumulate, Func{TAccumulate, TSource, TAccumulate})" />:
    /// <c>Aggregate</c> returns only the final accumulator value, whereas this method streams each intermediate state.
    /// </para>
    /// </remarks>
    public static IEnumerable<TAccumulate> Scan<TSource, TAccumulate>(
        this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> accumulator) =>
        source.Scan(seed, accumulator, static state => state);

    /// <summary>
    /// Emits a projection of the running accumulator state after folding each element of the source sequence, starting
    /// from a seed.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="source">The source sequence to fold.</param>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="accumulator">A function that combines the running accumulator with the next element.</param>
    /// <param name="selector">A projection applied to each running accumulator state before it is emitted.</param>
    /// <returns>
    /// A sequence containing the result of <paramref name="selector" /> applied to the accumulator state produced after
    /// each source element is folded. The result is empty when <paramref name="source" /> is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" />, <paramref name="accumulator" />, or <paramref name="selector" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses deferred execution. The source sequence is not enumerated until the returned sequence is
    /// enumerated, and it is enumerated exactly once in a single forward pass using only O(1) working memory.
    /// </para>
    /// <para>
    /// <paramref name="accumulator" /> and <paramref name="selector" /> are each invoked exactly once per source element.
    /// </para>
    /// </remarks>
    public static IEnumerable<TResult> Scan<TSource, TAccumulate, TResult>(
        this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> accumulator,
        Func<TAccumulate, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(accumulator);
        ThrowHelper.ThrowIfNull(selector);

        return Iterator();

        IEnumerable<TResult> Iterator()
        {
            TAccumulate state = seed;

            foreach (TSource item in source)
            {
                state = accumulator(state, item);
                yield return selector(state);
            }
        }
    }
}
