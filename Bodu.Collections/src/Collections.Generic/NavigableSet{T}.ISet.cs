// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSet{T}.ISet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class NavigableSet<T>
{
    /// <summary>
    /// Adds <paramref name="item" /> via the <see cref="ICollection{T}.Add(T)" /> contract.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <remarks>
    /// Discards the boolean result of <see cref="Add(T)" />; callers that need to detect a duplicate-add should invoke
    /// the typed <see cref="Add(T)" /> overload directly.
    /// </remarks>
    void ICollection<T>.Add(T item) =>
        Add(item);

    /// <summary>
    /// Modifies the current set so that it contains every element that is present in either this set or
    /// <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to union with. Must not be <see langword="null" />.</param>
    /// <remarks>
    /// Adds element-at-a-time — O(m log(n + m)) for m elements in <paramref name="other" />.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public void UnionWith(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
            return;

        foreach (T item in other)
            Add(item);
    }

    /// <summary>
    /// Modifies the current set so that it contains only elements that are also present in <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to intersect with. Must not be <see langword="null" />.</param>
    /// <remarks>
    /// Sorts <paramref name="other" /> into a scratch array under this set's comparer and merge-walks it against the
    /// in-order enumeration to find the non-members — O(n + m log m) overall, with no tree nodes allocated for
    /// <paramref name="other" />.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public void IntersectWith(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (_count == 0)
            return;

        if (ReferenceEquals(other, this))
            return;

        T[] sorted = BuildSortedOperand(other, out int uniqueCount);
        var stale = new List<T>();

        int j = 0;
        foreach (T item in this)
        {
            while (j < uniqueCount && _comparer.Compare(sorted[j], item) < 0)
                j++;

            if (j == uniqueCount || _comparer.Compare(sorted[j], item) != 0)
                stale.Add(item);
        }

        foreach (T item in stale)
            Remove(item);
    }

    /// <summary>
    /// Modifies the current set so that it contains only elements that are not present in <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to subtract. Must not be <see langword="null" />.</param>
    /// <remarks>
    /// Removes element-at-a-time — O(m log n) for m elements in <paramref name="other" />.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public void ExceptWith(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (_count == 0)
            return;

        if (ReferenceEquals(other, this))
        {
            Clear();
            return;
        }

        foreach (T item in other)
            Remove(item);
    }

    /// <summary>
    /// Modifies the current set so that it contains only elements that are present either in the current set or in
    /// <paramref name="other" />, but not in both.
    /// </summary>
    /// <param name="other">
    /// The collection to apply symmetric difference with. Must not be <see langword="null" />.
    /// </param>
    /// <remarks>
    /// Sorts and deduplicates <paramref name="other" /> into a scratch array and toggles membership per distinct
    /// element — O(m log m + m log n) overall, with no tree nodes allocated for <paramref name="other" />.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
        {
            Clear();
            return;
        }

        T[] sorted = BuildSortedOperand(other, out int uniqueCount);

        for (int i = 0; i < uniqueCount; i++)
        {
            if (!Add(sorted[i]))
                Remove(sorted[i]);
        }
    }

    /// <summary>
    /// Determines whether the current set is a subset of <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to test against. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if every element of the current set is also in <paramref name="other" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public bool IsSubsetOf(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (_count == 0)
            return true;

        if (ReferenceEquals(other, this))
            return true;

        T[] sorted = BuildSortedOperand(other, out int uniqueCount);

        return _count <= uniqueCount && IsMergeSubsetOf(sorted, uniqueCount);
    }

    /// <summary>
    /// Determines whether the current set is a superset of <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to test against. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if every element of <paramref name="other" /> is also in the current set; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public bool IsSupersetOf(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        foreach (T item in other)
        {
            if (!Contains(item))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the current set is a proper (strict) subset of <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to test against. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the current set is a subset of <paramref name="other" /> and the two are not equal;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
            return false;

        T[] sorted = BuildSortedOperand(other, out int uniqueCount);

        return _count < uniqueCount && IsMergeSubsetOf(sorted, uniqueCount);
    }

    /// <summary>
    /// Determines whether the current set is a proper (strict) superset of <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to test against. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the current set is a superset of <paramref name="other" /> and the two are not equal;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
            return false;

        T[] sorted = BuildSortedOperand(other, out int uniqueCount);

        if (_count <= uniqueCount)
            return false;

        for (int i = 0; i < uniqueCount; i++)
        {
            if (!Contains(sorted[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the current set and <paramref name="other" /> share any elements.
    /// </summary>
    /// <param name="other">The collection to test against. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the two collections share at least one element; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public bool Overlaps(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (_count == 0)
            return false;

        foreach (T item in other)
        {
            if (Contains(item))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the current set contains exactly the same elements as <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to compare against. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the two collections contain the same elements (ignoring duplicates and order);
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public bool SetEquals(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
            return true;

        T[] sorted = BuildSortedOperand(other, out int uniqueCount);

        if (_count != uniqueCount)
            return false;

        int index = 0;
        foreach (T item in this)
        {
            if (_comparer.Compare(sorted[index], item) != 0)
                return false;

            index++;
        }

        return true;
    }

    /// <summary>
    /// Materializes <paramref name="other" /> as a sorted, comparer-deduplicated scratch array for merge-based
    /// membership tests — a single array allocation with no tree nodes built.
    /// </summary>
    /// <param name="other">The source enumerable.</param>
    /// <param name="uniqueCount">
    /// When this method returns, contains the number of comparer-distinct leading elements of the returned array.
    /// </param>
    /// <returns>
    /// An array whose first <paramref name="uniqueCount" /> elements are the distinct elements of
    /// <paramref name="other" /> in ascending comparer order.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="other" /> contains a <see langword="null" /> element.
    /// </exception>
    private T[] BuildSortedOperand(IEnumerable<T> other, out int uniqueCount)
    {
        T[] items = other.ToArray();
        foreach (T item in items)
        {
            // Matches the null policy (and parameter name) of the bulk-load constructor that the previous
            // projection path funneled every operand through.
            if (item is null) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_NullCollectionElement, "source");
        }

        if (items.Length == 0)
        {
            uniqueCount = 0;
            return items;
        }

        Array.Sort(items, _comparer);

        // Deduplicate in place, keeping the first occurrence of each comparer-equal run.
        int unique = 1;
        for (int i = 1; i < items.Length; i++)
        {
            if (_comparer.Compare(items[i], items[unique - 1]) != 0)
                items[unique++] = items[i];
        }

        uniqueCount = unique;
        return items;
    }

    /// <summary>
    /// Determines whether every element of this set appears in the sorted, deduplicated operand via a single merge walk
    /// against the in-order enumeration.
    /// </summary>
    /// <param name="sorted">The sorted operand produced by <see cref="BuildSortedOperand" />.</param>
    /// <param name="uniqueCount">The number of distinct leading elements of <paramref name="sorted" />.</param>
    /// <returns>
    /// <see langword="true" /> if every element of the set is present in the operand; otherwise,
    /// <see langword="false" />.
    /// </returns>
    private bool IsMergeSubsetOf(T[] sorted, int uniqueCount)
    {
        int j = 0;
        foreach (T item in this)
        {
            while (j < uniqueCount && _comparer.Compare(sorted[j], item) < 0)
                j++;

            if (j == uniqueCount || _comparer.Compare(sorted[j], item) != 0)
                return false;

            j++;
        }

        return true;
    }
}
