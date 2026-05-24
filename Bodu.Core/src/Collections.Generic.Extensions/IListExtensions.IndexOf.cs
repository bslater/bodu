// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.IndexOf.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IListExtensions
{
    /// <summary>
    /// Returns the zero-based index of the first element in the entire <see cref="IList{T}" /> that satisfies the
    /// specified predicate.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the list.</typeparam>
    /// <param name="list">The list to search. Must not be <see langword="null" />.</param>
    /// <param name="predicate">A function that defines the conditions of the element to search for.</param>
    /// <returns>
    /// The zero-based index of the first matching element, or <c>-1</c> if no element satisfies
    /// <paramref name="predicate" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="list" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    public static int IndexOf<TSource>(this IList<TSource> list, Func<TSource, bool> predicate)
    {
        ThrowHelper.ThrowIfNull(list);
        ThrowHelper.ThrowIfNull(predicate);

        return IndexOfCore(list, predicate, 0, list.Count);
    }

    /// <summary>
    /// Returns the zero-based index of the first element that satisfies the specified predicate within the range of
    /// elements in the <see cref="IList{T}" /> that extends from the specified index to the end of the list.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the list.</typeparam>
    /// <param name="list">The list to search. Must not be <see langword="null" />.</param>
    /// <param name="predicate">A function that defines the conditions of the element to search for.</param>
    /// <param name="index">
    /// The zero-based starting index of the search. A value equal to <see cref="ICollection{T}.Count" /> is permitted
    /// and produces an empty search; <c>0</c> is valid on an empty list.
    /// </param>
    /// <returns>
    /// The zero-based index of the first matching element in the range, or <c>-1</c> if no element satisfies
    /// <paramref name="predicate" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="list" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than <see cref="ICollection{T}.Count" />.
    /// </exception>
    public static int IndexOf<TSource>(this IList<TSource> list, Func<TSource, bool> predicate, int index)
    {
        ThrowHelper.ThrowIfNull(list);
        ThrowHelper.ThrowIfNull(predicate);
        return index < 0 || index > list.Count
            ? throw new ArgumentOutOfRangeException(nameof(index))
            : IndexOfCore(list, predicate, index, list.Count - index);
    }

    /// <summary>
    /// Returns the zero-based index of the first element that satisfies the specified predicate within the range of
    /// elements in the <see cref="IList{T}" /> that starts at the specified index and contains the specified number of
    /// elements.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the list.</typeparam>
    /// <param name="list">The list to search. Must not be <see langword="null" />.</param>
    /// <param name="predicate">A function that defines the conditions of the element to search for.</param>
    /// <param name="index">
    /// The zero-based starting index of the search. A value equal to <see cref="ICollection{T}.Count" /> is permitted
    /// and produces an empty search; <c>0</c> is valid on an empty list.
    /// </param>
    /// <param name="count">
    /// The number of elements to examine. Must be non-negative and must not extend past the end of the list when added
    /// to <paramref name="index" />.
    /// </param>
    /// <returns>
    /// The zero-based index of the first matching element in the range, or <c>-1</c> if no element satisfies
    /// <paramref name="predicate" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="list" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or greater than <see cref="ICollection{T}.Count" />, or
    /// <paramref name="count" /> is negative, or <paramref name="index" /> plus <paramref name="count" /> exceeds
    /// <see cref="ICollection{T}.Count" />.
    /// </exception>
    public static int IndexOf<TSource>(this IList<TSource> list, Func<TSource, bool> predicate, int index, int count)
    {
        ThrowHelper.ThrowIfNull(list);
        ThrowHelper.ThrowIfNull(predicate);
        return index < 0 || index > list.Count
            ? throw new ArgumentOutOfRangeException(nameof(index))
            : count < 0 || index + count > list.Count
                ? throw new ArgumentOutOfRangeException(nameof(count))
                : IndexOfCore(list, predicate, index, count);
    }

    private static int IndexOfCore<TSource>(IList<TSource> list, Func<TSource, bool> predicate, int index, int count)
    {
        var end = index + count;
        for (var i = index; i < end; i++)
        {
            if (predicate(list[i]))
                return i;
        }

        return -1;
    }
}
