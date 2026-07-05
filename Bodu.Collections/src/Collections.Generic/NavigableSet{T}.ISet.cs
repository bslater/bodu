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
    /// Builds a one-shot projection of <paramref name="other" /> under this set's comparer and removes the non-members
    /// — O((n + m) log m) overall.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public void IntersectWith(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (_count == 0)
            return;

        if (ReferenceEquals(other, this))
            return;

        NavigableSet<T> projection = BuildProjection(other);
        var stale = new List<T>();

        foreach (T item in this)
        {
            if (!projection.Contains(item))
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
    /// Builds a one-shot deduplicated projection of <paramref name="other" /> and toggles membership per element — O((n
    /// + m) log(n + m)) overall.
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

        NavigableSet<T> projection = BuildProjection(other);

        foreach (T item in projection)
        {
            if (!Add(item))
                Remove(item);
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

        NavigableSet<T> projection = BuildProjection(other);

        if (_count > projection.Count)
            return false;

        foreach (T item in this)
        {
            if (!projection.Contains(item))
                return false;
        }

        return true;
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

        NavigableSet<T> projection = BuildProjection(other);

        if (_count >= projection.Count)
            return false;

        foreach (T item in this)
        {
            if (!projection.Contains(item))
                return false;
        }

        return true;
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

        NavigableSet<T> projection = BuildProjection(other);

        if (_count <= projection.Count)
            return false;

        foreach (T item in projection)
        {
            if (!Contains(item))
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

        NavigableSet<T> projection = BuildProjection(other);

        if (_count != projection.Count)
            return false;

        foreach (T item in this)
        {
            if (!projection.Contains(item))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Builds a temporary <see cref="NavigableSet{T}" /> projection of <paramref name="other" /> under this set's
    /// comparer for O(log m) membership tests, deduplicating along the way.
    /// </summary>
    /// <param name="other">The source enumerable.</param>
    /// <returns>A new set containing each comparer-distinct element of <paramref name="other" />.</returns>
    private NavigableSet<T> BuildProjection(IEnumerable<T> other) =>
        new(other, _comparer);
}
