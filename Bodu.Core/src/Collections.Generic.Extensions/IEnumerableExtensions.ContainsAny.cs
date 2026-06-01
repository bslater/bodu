// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.ContainsAny.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0

using Bodu.Collections.Generic.Internal;

#endif

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Determines whether the source sequence contains any of the specified items.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequences.</typeparam>
    /// <param name="source">The source sequence to search.</param>
    /// <param name="items">The items to locate within the source sequence.</param>
    /// <param name="comparer">
    /// An optional equality comparer to use for element comparisons; if <see langword="null" />, the default comparer
    /// is used.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if any item in <paramref name="items" /> exists in <paramref name="source" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either <paramref name="source" /> or <paramref name="items" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method avoids repeated enumeration of either sequence by building a <see cref="HashSet{T}" /> for O(1)
    /// membership lookups.
    /// </para>
    /// <para>
    /// The set is built from whichever collection has fewer elements, when both sizes are known, to minimize allocation
    /// and fill cost. When sizes are unknown or equal, the set is built from <paramref name="items" />, which is always
    /// materialized and is typically the smaller "needle" set in common usage.
    /// </para>
    /// </remarks>
    public static bool ContainsAny<T>(
        this IEnumerable<T> source,
        IEnumerable<T> items,
        IEqualityComparer<T>? comparer = null)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(items);

        items = SequenceUtility.EnsureMaterialized(items);

        // Capture the item count once; EnsureMaterialized returns ICollection<T> where possible,
        // making this check O(1) and avoiding enumerator allocation for the empty-sequence fast path.
        var itemsCount = items is ICollection<T> ic ? ic.Count : (int?)null;

        if (itemsCount == 0)
            return false;

        // Build the membership HashSet from the smaller of the two collections to minimize both
        // allocation and fill cost. When source is known to be smaller, build from source and
        // iterate items; otherwise, build from items (always materialized, typically smaller).
        var sourceIsSmaller = itemsCount.HasValue
            && source is ICollection<T> sourceCollection
            && sourceCollection.Count < itemsCount.Value;

        if (sourceIsSmaller)
        {
            var sourceSet = new HashSet<T>(source, comparer);
            if (sourceSet.Count == 0)
                return false;

#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions

            foreach (T? item in items)
            {
                if (sourceSet.Contains(item))
                    return true;
            }

#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
        }
        else
        {
            var itemSet = new HashSet<T>(items, comparer);

            // Guard against the case where items was a non-ICollection empty sequence that
            // bypassed the earlier count check (itemsCount was null, so the == 0 fast path
            // did not fire). An empty itemSet means no match is possible.
            if (itemSet.Count == 0)
                return false;

#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions

            foreach (T? element in source)
            {
                if (itemSet.Contains(element))
                    return true;
            }

#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
        }

        return false;
    }
}
