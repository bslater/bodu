// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.WhereNotNull.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Filters a sequence of nullable references, yielding only the elements that are not <see langword="null" />.
    /// </summary>
    /// <typeparam name="TSource">The reference type of the elements.</typeparam>
    /// <param name="source">The sequence of possibly-<see langword="null" /> references to filter.</param>
    /// <returns>
    /// A sequence containing the non-<see langword="null" /> elements of <paramref name="source" />, in order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Execution is deferred: <paramref name="source" /> is not enumerated until the returned sequence is iterated,
    /// while the <see langword="null" /> argument check runs eagerly.
    /// </remarks>
    public static IEnumerable<TSource> WhereNotNull<TSource>(this IEnumerable<TSource?> source)
        where TSource : class
    {
        ThrowHelper.ThrowIfNull(source);

        return Iterator(source);

        static IEnumerable<TSource> Iterator(IEnumerable<TSource?> sequence)
        {
            foreach (TSource? item in sequence)
            {
                if (item is not null)
                    yield return item;
            }
        }
    }

    /// <summary>
    /// Filters a sequence of nullable value types, yielding the underlying value of each element that has one.
    /// </summary>
    /// <typeparam name="TSource">The value type of the elements.</typeparam>
    /// <param name="source">The sequence of <see cref="Nullable{T}" /> values to filter.</param>
    /// <returns>
    /// A sequence containing the value of every element of <paramref name="source" /> that is not
    /// <see langword="null" />, in order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Execution is deferred: <paramref name="source" /> is not enumerated until the returned sequence is iterated,
    /// while the <see langword="null" /> argument check runs eagerly.
    /// </remarks>
    public static IEnumerable<TSource> WhereNotNull<TSource>(this IEnumerable<TSource?> source)
        where TSource : struct
    {
        ThrowHelper.ThrowIfNull(source);

        return Iterator(source);

        static IEnumerable<TSource> Iterator(IEnumerable<TSource?> sequence)
        {
            foreach (TSource? item in sequence)
            {
                if (item.HasValue)
                    yield return item.GetValueOrDefault();
            }
        }
    }
}
