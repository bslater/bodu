// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.CartesianProduct.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Emits the Cartesian product of two sequences as <c>(First, Second)</c> tuples, pairing every element of the
    /// first sequence with every element of the second.
    /// </summary>
    /// <typeparam name="TFirst">The type of elements in the first sequence.</typeparam>
    /// <typeparam name="TSecond">The type of elements in the second sequence.</typeparam>
    /// <param name="first">The first sequence, whose elements drive the outer loop of the product.</param>
    /// <param name="second">The second sequence, whose elements drive the inner loop of the product.</param>
    /// <returns>
    /// A sequence of <c>(First, Second)</c> tuples containing every pairing, ordered by the position of the first
    /// element and then by the position of the second. The result is empty when either input is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses deferred execution. <paramref name="first" /> is streamed and enumerated exactly once;
    /// <paramref name="second" /> is materialized into a buffer the first time a product row is needed and every
    /// subsequent pass replays that buffer, so <paramref name="second" /> is also enumerated exactly once. When
    /// <paramref name="first" /> is empty, <paramref name="second" /> is never enumerated.
    /// </para>
    /// <para>
    /// Because <paramref name="second" /> is buffered in full, it must be finite; <paramref name="first" /> may be
    /// infinite and bounded downstream with
    /// <see cref="System.Linq.Enumerable.Take{TSource}(IEnumerable{TSource}, int)" />.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Pair every number with every letter.
    /// foreach (var (number, letter) in new[] { 1, 2 }.CartesianProduct(new[] { "a", "b" }))
    ///     Console.WriteLine($"{number}{letter}");
    /// // => 1a
    /// // => 1b
    /// // => 2a
    /// // => 2b
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<(TFirst First, TSecond Second)> CartesianProduct<TFirst, TSecond>(
        this IEnumerable<TFirst> first,
        IEnumerable<TSecond> second) =>
        first.CartesianProduct(second, static (firstElement, secondElement) => (firstElement, secondElement));

    /// <summary>
    /// Applies a projection to every pairing in the Cartesian product of two sequences.
    /// </summary>
    /// <typeparam name="TFirst">The type of elements in the first sequence.</typeparam>
    /// <typeparam name="TSecond">The type of elements in the second sequence.</typeparam>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="first">The first sequence, whose elements drive the outer loop of the product.</param>
    /// <param name="second">The second sequence, whose elements drive the inner loop of the product.</param>
    /// <param name="resultSelector">
    /// A projection applied to each pairing, receiving the first-sequence element and the second-sequence element.
    /// </param>
    /// <returns>
    /// A sequence containing the result of <paramref name="resultSelector" /> applied to every pairing, ordered by the
    /// position of the first element and then by the position of the second. The result is empty when either input is
    /// empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="first" />, <paramref name="second" />, or <paramref name="resultSelector" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses deferred execution. <paramref name="first" /> is streamed and enumerated exactly once;
    /// <paramref name="second" /> is materialized into a buffer the first time a product row is needed and every
    /// subsequent pass replays that buffer, so <paramref name="second" /> is also enumerated exactly once. When
    /// <paramref name="first" /> is empty, <paramref name="second" /> is never enumerated.
    /// </para>
    /// <para>
    /// <paramref name="resultSelector" /> is invoked exactly once per emitted pairing and is never invoked when either
    /// input is empty.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Project each pairing into a formatted coordinate.
    /// string[] cells = new[] { 'A', 'B' }.CartesianProduct(new[] { 1, 2 }, (column, row) => $"{column}{row}").ToArray();
    /// // => cells == [ "A1", "A2", "B1", "B2" ]
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<TResult> CartesianProduct<TFirst, TSecond, TResult>(
        this IEnumerable<TFirst> first,
        IEnumerable<TSecond> second,
        Func<TFirst, TSecond, TResult> resultSelector)
    {
        ThrowHelper.ThrowIfNull(first);
        ThrowHelper.ThrowIfNull(second);
        ThrowHelper.ThrowIfNull(resultSelector);

        return Iterator();

        IEnumerable<TResult> Iterator()
        {
            // The inner sequence is materialized once, on the first outer element, and replayed per outer element.
            TSecond[]? buffer = null;

            foreach (TFirst firstElement in first)
            {
                buffer ??= second.ToArray();

                if (buffer.Length == 0)
                    yield break;

                foreach (TSecond secondElement in buffer)
                    yield return resultSelector(firstElement, secondElement);
            }
        }
    }
}
