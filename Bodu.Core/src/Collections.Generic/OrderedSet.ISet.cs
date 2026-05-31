// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSet.ISet.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class OrderedSet<T>
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
        _storage.Add(item);

    /// <summary>
    /// Modifies the current set so that it contains every element that is present in either this set or
    /// <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to union with. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public void UnionWith(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        _storage.AddRange(other);
    }

    /// <summary>
    /// Modifies the current set so that it contains only elements that are also present in <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to intersect with. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public void IntersectWith(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (_storage.Count == 0)
            return;

        if (ReferenceEquals(other, this))
            return;

        OrderedSetStorage<T> projection = BuildProjection(other);
        _storage.RemoveWhere(item => !projection.Contains(item));
    }

    /// <summary>
    /// Modifies the current set so that it contains only elements that are not present in <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to subtract. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public void ExceptWith(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (_storage.Count == 0)
            return;

        if (ReferenceEquals(other, this))
        {
            _storage.Clear();
            return;
        }

        foreach (T item in other)
            _storage.Remove(item);
    }

    /// <summary>
    /// Modifies the current set so that it contains only elements that are present either in the current set or in
    /// <paramref name="other" />, but not in both.
    /// </summary>
    /// <param name="other">
    /// The collection to apply symmetric difference with. Must not be <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
        {
            _storage.Clear();
            return;
        }

        OrderedSetStorage<T> projection = BuildProjection(other);

        for (var i = 0; i < projection.Count; i++)
        {
            T item = projection._items[i];
            if (!_storage.Add(item))
                _storage.Remove(item);
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

        if (_storage.Count == 0)
            return true;

        OrderedSetStorage<T> projection = BuildProjection(other);

        if (_storage.Count > projection.Count)
            return false;

        for (var i = 0; i < _storage._count; i++)
        {
            if (!projection.Contains(_storage._items[i]))
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
            if (!_storage.Contains(item))
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

        OrderedSetStorage<T> projection = BuildProjection(other);

        if (_storage.Count >= projection.Count)
            return false;

        for (var i = 0; i < _storage._count; i++)
        {
            if (!projection.Contains(_storage._items[i]))
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

        OrderedSetStorage<T> projection = BuildProjection(other);

        if (_storage.Count <= projection.Count)
            return false;

        for (var i = 0; i < projection._count; i++)
        {
            if (!_storage.Contains(projection._items[i]))
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

        if (_storage.Count == 0)
            return false;

        foreach (T item in other)
        {
            if (_storage.Contains(item))
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

        OrderedSetStorage<T> projection = BuildProjection(other);

        if (_storage.Count != projection.Count)
            return false;

        for (var i = 0; i < _storage._count; i++)
        {
            if (!projection.Contains(_storage._items[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Builds a temporary <see cref="OrderedSetStorage{T}" /> projection of <paramref name="other" /> for O(1)
    /// membership tests, deduplicating along the way.
    /// </summary>
    /// <param name="other">The source enumerable.</param>
    /// <returns>A new storage instance containing each distinct element of <paramref name="other" />.</returns>
    private OrderedSetStorage<T> BuildProjection(IEnumerable<T> other)
    {
        var hint = other is ICollection<T> col ? col.Count
                 : other is IReadOnlyCollection<T> rc ? rc.Count
                 : 0;

        OrderedSetStorage<T> projection = new(hint, _storage.Comparer);

        projection.AddRange(other);

        return projection;
    }
}
