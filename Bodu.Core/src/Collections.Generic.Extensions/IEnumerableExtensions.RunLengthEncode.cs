// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.RunLengthEncode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Compresses each maximal run of consecutive equal elements in the source sequence into a <c>(Value, Count)</c>
    /// tuple, using the default equality comparer.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <param name="source">The source sequence to encode.</param>
    /// <returns>
    /// A sequence of <c>(Value, Count)</c> tuples, one per maximal run of adjacent equal elements in
    /// <paramref name="source" />. The result is empty when <paramref name="source" /> is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This method uses deferred execution and enumerates the source exactly once in a single forward pass using only
    /// O(1) working memory. Equality is compared with <see cref="EqualityComparer{T}.Default" />.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Compress each maximal run of adjacent equal characters into a (Value, Count) pair.
    /// foreach (var (value, count) in "aaabbc".RunLengthEncode())
    ///     Console.WriteLine($"{value} x {count}");
    /// // => a x 3
    /// // => b x 2
    /// // => c x 1
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<(TSource Value, int Count)> RunLengthEncode<TSource>(this IEnumerable<TSource> source) =>
        source.RunLengthEncode(null);

    /// <summary>
    /// Compresses each maximal run of consecutive equal elements in the source sequence into a <c>(Value, Count)</c>
    /// tuple, using the specified equality comparer.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <param name="source">The source sequence to encode.</param>
    /// <param name="comparer">
    /// The comparer used to decide whether adjacent elements belong to the same run, or <see langword="null" /> to use
    /// <see cref="EqualityComparer{T}.Default" />.
    /// </param>
    /// <returns>
    /// A sequence of <c>(Value, Count)</c> tuples, one per maximal run of adjacent equal elements in
    /// <paramref name="source" />. The result is empty when <paramref name="source" /> is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when a single run contains more than <see cref="int.MaxValue" /> elements.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses deferred execution and enumerates the source exactly once in a single forward pass using only
    /// O(1) working memory. This is adjacent compression, not global counting: a value that reappears after a different
    /// value begins a new run.
    /// </para>
    /// <para>
    /// When bounded over an infinite source, a run that never ends never yields; combine with
    /// <see cref="System.Linq.Enumerable.Take{TSource}(IEnumerable{TSource}, int)" /> only when runs are known to
    /// change.
    /// </para>
    /// </remarks>
    public static IEnumerable<(TSource Value, int Count)> RunLengthEncode<TSource>(
        this IEnumerable<TSource> source,
        IEqualityComparer<TSource>? comparer)
    {
        ThrowHelper.ThrowIfNull(source);

        comparer ??= EqualityComparer<TSource>.Default;

        return Iterator();

        IEnumerable<(TSource Value, int Count)> Iterator()
        {
            using IEnumerator<TSource> enumerator = source.GetEnumerator();

            if (!enumerator.MoveNext())
                yield break;

            TSource currentValue = enumerator.Current;
            int count = 1;

            while (enumerator.MoveNext())
            {
                TSource item = enumerator.Current;

                if (comparer.Equals(currentValue, item))
                {
                    if (count == int.MaxValue)
                        throw new OverflowException(ResourceStrings.Overflow_Invalid_RunLengthExceedsMaxValue);

                    count++;
                    continue;
                }

                yield return (currentValue, count);
                currentValue = item;
                count = 1;
            }

            yield return (currentValue, count);
        }
    }
}
