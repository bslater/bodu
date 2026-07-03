// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Pairwise.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Emits each adjacent pair of elements from the source sequence as a <c>(Previous, Current)</c> tuple.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <param name="source">The source sequence to pair.</param>
    /// <returns>
    /// A sequence of <c>(Previous, Current)</c> tuples, one for every adjacent pair of elements in
    /// <paramref name="source" />. The result is empty when <paramref name="source" /> contains fewer than two
    /// elements.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses deferred execution. The source sequence is not enumerated until the returned sequence is
    /// enumerated, and it is enumerated exactly once in a single forward pass using only O(1) working memory.
    /// </para>
    /// <para>
    /// This is the primitive for neighbour comparison — monotonic checks, gap detection, deltas, and change detection.
    /// Because only the previous element is retained, the operator can be bounded over an infinite source with
    /// <see cref="System.Linq.Enumerable.Take{TSource}(IEnumerable{TSource}, int)" />.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Emit each adjacent (previous, current) pair.
    /// foreach (var (previous, current) in new[] { 10, 13, 9 }.Pairwise())
    ///     Console.WriteLine($"{previous} -> {current}");
    /// // => 10 -> 13
    /// // => 13 -> 9
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<(TSource Previous, TSource Current)> Pairwise<TSource>(this IEnumerable<TSource> source) =>
        source.Pairwise(static (previous, current) => (previous, current));

    /// <summary>
    /// Applies a projection to each adjacent pair of elements from the source sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="source">The source sequence to pair.</param>
    /// <param name="selector">
    /// A projection applied to each adjacent pair, receiving the previous and current elements.
    /// </param>
    /// <returns>
    /// A sequence containing the result of <paramref name="selector" /> applied to every adjacent pair of elements in
    /// <paramref name="source" />. The result is empty when <paramref name="source" /> contains fewer than two
    /// elements.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses deferred execution. The source sequence is not enumerated until the returned sequence is
    /// enumerated, and it is enumerated exactly once in a single forward pass using only O(1) working memory.
    /// </para>
    /// <para>
    /// <paramref name="selector" /> is invoked exactly once per emitted pair and is never invoked for an empty or
    /// single-element source.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Project each adjacent pair into the delta between the two values.
    /// int[] deltas = new[] { 10, 13, 9 }.Pairwise((previous, current) => current - previous).ToArray();
    /// // => deltas == [ 3, -4 ]
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<TResult> Pairwise<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, TSource, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);

        return Iterator();

        IEnumerable<TResult> Iterator()
        {
            using IEnumerator<TSource> enumerator = source.GetEnumerator();

            if (!enumerator.MoveNext())
                yield break;

            TSource previous = enumerator.Current;

            while (enumerator.MoveNext())
            {
                TSource current = enumerator.Current;
                yield return selector(previous, current);
                previous = current;
            }
        }
    }
}
