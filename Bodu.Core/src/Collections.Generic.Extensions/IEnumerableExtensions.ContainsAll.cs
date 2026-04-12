// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.ContainsAll.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0

using Bodu.Collections.Generic.Internal;

#endif

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Determines whether the source sequence contains all of the specified items.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequences.</typeparam>
    /// <param name="source">The source sequence to search.</param>
    /// <param name="items">The items to verify against the source sequence.</param>
    /// <param name="comparer">An optional equality comparer to use; if <see langword="null" />, the default comparer is used.</param>
    /// <returns><see langword="true" /> if all items exist in <paramref name="source" />; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either <paramref name="source" /> or <paramref name="items" /> is <see langword="null" />.</exception>
    public static bool ContainsAll<T>(
        this IEnumerable<T> source,
        IEnumerable<T> items,
        IEqualityComparer<T>? comparer = null)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(items);

        items = SequenceUtility.EnsureMaterialized(items);

        // Fast empty check without allocating an enumerator.
        // EnsureMaterialized returns ICollection<T> where possible, making this O(1).
        if (items is ICollection<T> itemsCollection && itemsCollection.Count == 0)
            return true;

        // Materialise the source into a HashSet for O(1) membership lookups.
        HashSet<T> sourceSet = new HashSet<T>(source, comparer);
        if (sourceSet.Count == 0)
            return false;

#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions

        // Using a foreach loop instead of LINQ to:
        // - Avoid delegate allocation from predicate-based .Any(...)
        // - Ensure optimal short-circuit performance
        // - Preserve debuggability and allow future extensions with minimal cost
        foreach (T? item in items)
        {
            if (!sourceSet.Contains(item))
                return false;
        }

#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions

        return true;
    }
}
