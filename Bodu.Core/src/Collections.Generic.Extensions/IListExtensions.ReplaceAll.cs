// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.ReplaceAll.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IListExtensions
{
    /// <summary>
    /// Replaces every occurrence of <paramref name="oldItem" /> in the <see cref="IList{T}" /> with
    /// <paramref name="newItem" />, using <see cref="EqualityComparer{T}.Default" /> to compare elements.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the list.</typeparam>
    /// <param name="list">The list to modify in place. Must not be <see langword="null" />.</param>
    /// <param name="oldItem">The value to locate and replace.</param>
    /// <param name="newItem">The replacement value.</param>
    /// <returns>The number of elements that were replaced.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="list" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// Elements are assigned in place through the list's indexer; no insertions or removals are performed, so the list
    /// size and the relative position of every element are preserved.
    /// </remarks>
    public static int ReplaceAll<TSource>(this IList<TSource> list, TSource oldItem, TSource newItem) =>
        ReplaceAll(list, oldItem, newItem, comparer: null);

    /// <summary>
    /// Replaces every occurrence of <paramref name="oldItem" /> in the <see cref="IList{T}" /> with
    /// <paramref name="newItem" />, using the supplied <see cref="IEqualityComparer{T}" /> to compare elements.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the list.</typeparam>
    /// <param name="list">The list to modify in place. Must not be <see langword="null" />.</param>
    /// <param name="oldItem">The value to locate and replace.</param>
    /// <param name="newItem">The replacement value.</param>
    /// <param name="comparer">
    /// The equality comparer used to locate occurrences of <paramref name="oldItem" />. When <see langword="null" />,
    /// <see cref="EqualityComparer{T}.Default" /> is used.
    /// </param>
    /// <returns>The number of elements that were replaced.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="list" /> is <see langword="null" />.</exception>
    public static int ReplaceAll<TSource>(this IList<TSource> list, TSource oldItem, TSource newItem, IEqualityComparer<TSource>? comparer)
    {
        ThrowHelper.ThrowIfNull(list);
        comparer ??= EqualityComparer<TSource>.Default;

        int count = 0;
        int length = list.Count;
        for (int i = 0; i < length; i++)
        {
            if (comparer.Equals(list[i], oldItem))
            {
                list[i] = newItem;
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Replaces every element in the <see cref="IList{T}" /> that satisfies the specified predicate with
    /// <paramref name="newItem" />.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the list.</typeparam>
    /// <param name="list">The list to modify in place. Must not be <see langword="null" />.</param>
    /// <param name="newItem">The replacement value to assign to every matching slot.</param>
    /// <param name="predicate">A function that defines the conditions of the elements to replace.</param>
    /// <returns>The number of elements that were replaced.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="list" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The list is walked once with a fixed length captured before iteration begins. Predicate matches against
    /// <paramref name="newItem" /> do not cause additional passes or infinite recursion: a slot is tested exactly once.
    /// </remarks>
    public static int ReplaceAll<TSource>(this IList<TSource> list, TSource newItem, Func<TSource, bool> predicate)
    {
        ThrowHelper.ThrowIfNull(list);
        ThrowHelper.ThrowIfNull(predicate);

        int count = 0;
        int length = list.Count;
        for (int i = 0; i < length; i++)
        {
            if (predicate(list[i]))
            {
                list[i] = newItem;
                count++;
            }
        }

        return count;
    }
}
