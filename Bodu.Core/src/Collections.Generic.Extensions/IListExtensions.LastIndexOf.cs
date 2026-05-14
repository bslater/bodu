// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.LastIndexOf.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Generic.Extensions;

public static partial class IListExtensions
{
    /// <summary>
    /// Returns the zero-based index of the last element in the entire <see cref="IList{T}"/> that
    /// satisfies the specified predicate, searching backwards from the end of the list.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the list.</typeparam>
    /// <param name="list">The list to search. Must not be <see langword="null"/>.</param>
    /// <param name="predicate">A function that defines the conditions of the element to search for.</param>
    /// <returns>
    /// The zero-based index of the last matching element, or <c>-1</c> if no element satisfies
    /// <paramref name="predicate"/> (including the empty-list case).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="list"/> or <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    public static int LastIndexOf<TSource>(this IList<TSource> list, Func<TSource, bool> predicate)
    {
        ThrowHelper.ThrowIfNull(list);
        ThrowHelper.ThrowIfNull(predicate);

        return list.Count == 0
            ? -1
            : LastIndexOfCore(list, predicate, list.Count - 1, list.Count);
    }

    /// <summary>
    /// Returns the zero-based index of the last element that satisfies the specified predicate within
    /// the range of elements in the <see cref="IList{T}"/> that extends from the first element to the
    /// specified index.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the list.</typeparam>
    /// <param name="list">The list to search. Must not be <see langword="null"/>.</param>
    /// <param name="predicate">A function that defines the conditions of the element to search for.</param>
    /// <param name="startIndex">
    /// The zero-based starting index of the backward search. For a non-empty list this must be in the
    /// range <c>[0, Count - 1]</c>; for an empty list the only valid value is <c>-1</c>.
    /// </param>
    /// <returns>
    /// The zero-based index of the last matching element in the range, or <c>-1</c> if no element
    /// satisfies <paramref name="predicate"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="list"/> or <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="startIndex"/> falls outside the valid range for the current list size.
    /// </exception>
    public static int LastIndexOf<TSource>(this IList<TSource> list, Func<TSource, bool> predicate, int startIndex)
    {
        ThrowHelper.ThrowIfNull(list);
        ThrowHelper.ThrowIfNull(predicate);
        ValidateLastIndexOfStart(list, startIndex);

        return list.Count == 0
            ? -1
            : LastIndexOfCore(list, predicate, startIndex, startIndex + 1);
    }

    /// <summary>
    /// Returns the zero-based index of the last element that satisfies the specified predicate within
    /// the range of elements in the <see cref="IList{T}"/> that contains the specified number of
    /// elements and ends at the specified index.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the list.</typeparam>
    /// <param name="list">The list to search. Must not be <see langword="null"/>.</param>
    /// <param name="predicate">A function that defines the conditions of the element to search for.</param>
    /// <param name="startIndex">
    /// The zero-based starting index of the backward search. For a non-empty list this must be in the
    /// range <c>[0, Count - 1]</c>; for an empty list the only valid value is <c>-1</c>.
    /// </param>
    /// <param name="count">
    /// The number of elements to examine. Must be non-negative and must not extend before the start
    /// of the list relative to <paramref name="startIndex"/>.
    /// </param>
    /// <returns>
    /// The zero-based index of the last matching element in the range, or <c>-1</c> if no element
    /// satisfies <paramref name="predicate"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="list"/> or <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="startIndex"/> falls outside the valid range for the current list size, or
    /// <paramref name="count"/> is negative, or the range described by <paramref name="startIndex"/>
    /// and <paramref name="count"/> extends past the beginning of the list.
    /// </exception>
    public static int LastIndexOf<TSource>(this IList<TSource> list, Func<TSource, bool> predicate, int startIndex, int count)
    {
        ThrowHelper.ThrowIfNull(list);
        ThrowHelper.ThrowIfNull(predicate);
        ValidateLastIndexOfStart(list, startIndex);
        if (count < 0 || startIndex - count + 1 < 0) throw new ArgumentOutOfRangeException(nameof(count));

        return LastIndexOfCore(list, predicate, startIndex, count);
    }

    private static void ValidateLastIndexOfStart<TSource>(IList<TSource> list, int startIndex)
    {
        if (list.Count == 0)
        {
            if (startIndex != -1) throw new ArgumentOutOfRangeException(nameof(startIndex));
        }
        else if (startIndex < 0 || startIndex >= list.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }
    }

    private static int LastIndexOfCore<TSource>(IList<TSource> list, Func<TSource, bool> predicate, int startIndex, int count)
    {
        int end = startIndex - count;
        for (int i = startIndex; i > end; i--)
        {
            if (predicate(list[i]))
                return i;
        }

        return -1;
    }
}
