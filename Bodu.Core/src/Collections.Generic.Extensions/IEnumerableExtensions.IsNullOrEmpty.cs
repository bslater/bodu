// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.IsNullOrEmpty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Determines whether <paramref name="source" /> is <see langword="null" /> or contains no elements.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">The sequence to test. May be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="source" /> is <see langword="null" /> or yields no elements;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// When <paramref name="source" /> implements <see cref="ICollection{T}" /> or
    /// <see cref="IReadOnlyCollection{T}" /> the element count is read directly; otherwise the sequence is probed for a
    /// single element. A non-counted sequence is advanced by at most one step and is never fully enumerated.
    /// </remarks>
    public static bool IsNullOrEmpty<TSource>(this IEnumerable<TSource>? source)
    {
        if (source is null)
            return true;

        if (source is ICollection<TSource> collection)
            return collection.Count == 0;

        if (source is IReadOnlyCollection<TSource> readOnlyCollection)
            return readOnlyCollection.Count == 0;

        using IEnumerator<TSource> enumerator = source.GetEnumerator();
        return !enumerator.MoveNext();
    }
}
