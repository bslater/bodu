// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.ContainsAll.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Determines whether the source sequence contains all of the specified items.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequences.</typeparam>
    /// <param name="source">The source sequence to search.</param>
    /// <param name="items">The items to verify against the source sequence.</param>
    /// <param name="comparer">
    /// An optional equality comparer to use; if <see langword="null" />, the default comparer is used.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if all items exist in <paramref name="source" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either <paramref name="source" /> or <paramref name="items" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Memory use scales with the number of <paramref name="items" />, not the size of <paramref name="source" />: the
    /// items are collected into a pending set and <paramref name="source" /> is streamed with a single enumeration,
    /// stopping as soon as every item has been seen.
    /// </remarks>
    public static bool ContainsAll<T>(
        this IEnumerable<T> source,
        IEnumerable<T> items,
        IEqualityComparer<T>? comparer = null)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(items);

        // Build the pending-needle set — O(|items|) memory — rather than materializing the haystack, whose size the
        // caller cannot bound (its sibling ContainsAny applies the same smaller-side discipline).
        var pending = new HashSet<T>(items, comparer);
        if (pending.Count == 0)
            return true;

        // Stream the source once, retiring needles as they are seen; short-circuit when none remain.
        foreach (T item in source)
        {
            if (pending.Remove(item) && pending.Count == 0)
                return true;
        }

        return false;
    }
}
