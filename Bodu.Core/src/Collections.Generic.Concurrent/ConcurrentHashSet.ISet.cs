// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSet.ISet.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentHashSet<T> :
    ISet<T>
{
    /// <summary>
    /// Gets a value indicating whether the set is read-only.
    /// </summary>
    /// <value>Always <see langword="false" />.</value>
    /// <returns>Always <see langword="false" />; a <see cref="ConcurrentHashSet{T}" /> is always mutable.</returns>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds the specified element to the set through the <see cref="ICollection{T}.Add(T)" /> contract.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// Discards the boolean result of <see cref="Add(T)" />; callers that need to detect a duplicate add should invoke
    /// the public <see cref="Add(T)" /> overload directly.
    /// </remarks>
    void ICollection<T>.Add(T item) =>
        Add(item);

    /// <summary>
    /// Copies the elements of the set to the specified array, starting at the specified index.
    /// </summary>
    /// <param name="array">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is less than zero.</exception>
    /// <exception cref="ArgumentException">
    /// The number of elements in the set exceeds the available space in <paramref name="array" /> from
    /// <paramref name="arrayIndex" /> onward.
    /// </exception>
    /// <remarks>
    /// This method takes an atomic snapshot of the set before copying, so the destination reflects the set's contents
    /// at a single instant and is unaffected by concurrent modifications made afterward.
    /// </remarks>
    public void CopyTo(T[] array, int arrayIndex)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfNegative(arrayIndex);
        T[] snapshot = ToArray();
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex + snapshot.Length);

        Array.Copy(snapshot, 0, array, arrayIndex, snapshot.Length);
    }

    /// <summary>
    /// Modifies the set so that it contains every element that is present in either the set or
    /// <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection of elements to add to the set.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This operation is <b>not</b> atomic. It is applied as a sequence of individual atomic <see cref="Add" /> calls;
    /// a concurrent mutation from another thread may interleave with those calls, so the resulting set may not equal
    /// the union of any single observed state of the set with <paramref name="other" />.
    /// </remarks>
    public void UnionWith(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
            return;

        foreach (T item in other)
            Add(item);
    }

    /// <summary>
    /// Modifies the set so that it contains only elements that are also present in <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to intersect with the set.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This operation is <b>not</b> atomic. It materializes <paramref name="other" />, then removes the non-matching
    /// elements of a point-in-time snapshot through individual atomic <see cref="Remove" /> calls. A concurrent
    /// mutation may interleave, so the resulting set may not equal the intersection of any single observed state.
    /// </remarks>
    public void IntersectWith(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
            return;

        var retained = new HashSet<T>(other, _comparer);
        foreach (T item in ToArray())
        {
            if (!retained.Contains(item))
                Remove(item);
        }
    }

    /// <summary>
    /// Modifies the set so that it contains only elements that are not present in <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection of elements to remove from the set.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This operation is <b>not</b> atomic. It is applied as a sequence of individual atomic <see cref="Remove" />
    /// calls; a concurrent mutation may interleave with those calls.
    /// </remarks>
    public void ExceptWith(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
        {
            Clear();
            return;
        }

        foreach (T item in other)
            Remove(item);
    }

    /// <summary>
    /// Modifies the set so that it contains only elements that are present either in the set or in
    /// <paramref name="other" />, but not in both.
    /// </summary>
    /// <param name="other">The collection to apply the symmetric difference with.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This operation is <b>not</b> atomic. It materializes the distinct elements of <paramref name="other" />, then
    /// toggles each one through individual atomic <see cref="Add" /> and <see cref="Remove" /> calls. A concurrent
    /// mutation may interleave, so the resulting set may not equal the symmetric difference of any single observed
    /// state.
    /// </remarks>
    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        if (ReferenceEquals(other, this))
        {
            Clear();
            return;
        }

        foreach (T item in new HashSet<T>(other, _comparer))
        {
            if (!Add(item))
                Remove(item);
        }
    }

    /// <summary>
    /// Determines whether the set is a subset of <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if every element of the set's snapshot is also in <paramref name="other" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This operation is <b>not</b> atomic. It compares a point-in-time snapshot of the set against
    /// <paramref name="other" />; a concurrent mutation may interleave, so the result describes a relationship that may
    /// no longer hold when the call returns.
    /// </remarks>
    public bool IsSubsetOf(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        T[] snapshot = ToArray();
        if (snapshot.Length == 0)
            return true;

        var otherSet = new HashSet<T>(other, _comparer);
        foreach (T item in snapshot)
        {
            if (!otherSet.Contains(item))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the set is a superset of <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if every element of <paramref name="other" /> is also in the set; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This operation is <b>not</b> atomic. It tests each element of <paramref name="other" /> against the set through
    /// individual lock-free <see cref="Contains" /> calls; a concurrent mutation may interleave, so the result
    /// describes a relationship that may no longer hold when the call returns.
    /// </remarks>
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
    /// Determines whether the set is a proper (strict) subset of <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if the set's snapshot is a subset of <paramref name="other" /> and the two are not
    /// equal; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This operation is <b>not</b> atomic. It compares a point-in-time snapshot of the set against
    /// <paramref name="other" />; a concurrent mutation may interleave, so the result describes a relationship that may
    /// no longer hold when the call returns.
    /// </remarks>
    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        T[] snapshot = ToArray();
        var otherSet = new HashSet<T>(other, _comparer);

        if (snapshot.Length >= otherSet.Count)
            return false;

        foreach (T item in snapshot)
        {
            if (!otherSet.Contains(item))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the set is a proper (strict) superset of <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if the set's snapshot is a superset of <paramref name="other" /> and the two are not
    /// equal; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This operation is <b>not</b> atomic. It compares a point-in-time snapshot of the set against
    /// <paramref name="other" />; a concurrent mutation may interleave, so the result describes a relationship that may
    /// no longer hold when the call returns.
    /// </remarks>
    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        var otherSet = new HashSet<T>(other, _comparer);
        T[] snapshot = ToArray();

        if (snapshot.Length <= otherSet.Count)
            return false;

        var selfSet = new HashSet<T>(snapshot, _comparer);
        foreach (T item in otherSet)
        {
            if (!selfSet.Contains(item))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the set and <paramref name="other" /> share any elements.
    /// </summary>
    /// <param name="other">The collection to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if the set and <paramref name="other" /> share at least one element; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This operation is <b>not</b> atomic. It tests each element of <paramref name="other" /> against the set through
    /// individual lock-free <see cref="Contains" /> calls; a concurrent mutation may interleave.
    /// </remarks>
    public bool Overlaps(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        foreach (T item in other)
        {
            if (Contains(item))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the set contains exactly the same elements as <paramref name="other" />.
    /// </summary>
    /// <param name="other">The collection to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if the set's snapshot and <paramref name="other" /> contain the same elements (ignoring
    /// duplicates and order); otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This operation is <b>not</b> atomic. It compares a point-in-time snapshot of the set against
    /// <paramref name="other" />; a concurrent mutation may interleave, so the result describes a relationship that may
    /// no longer hold when the call returns.
    /// </remarks>
    public bool SetEquals(IEnumerable<T> other)
    {
        ThrowHelper.ThrowIfNull(other);

        var otherSet = new HashSet<T>(other, _comparer);
        T[] snapshot = ToArray();

        if (snapshot.Length != otherSet.Count)
            return false;

        foreach (T item in snapshot)
        {
            if (!otherSet.Contains(item))
                return false;
        }

        return true;
    }
}
