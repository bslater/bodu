// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EquatableArray{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// An immutable array wrapper with element-wise value equality, so generator pipeline models containing collections
/// compare structurally and the incremental cache can reuse unchanged outputs.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    /// <summary>The wrapped elements; <see langword="null" /> for a default instance.</summary>
    private readonly T[]? _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="EquatableArray{T}" /> struct wrapping the supplied elements.
    /// </summary>
    /// <param name="items">The elements to wrap. The array is not copied.</param>
    public EquatableArray(T[] items)
    {
        _items = items;
    }

    /// <summary>
    /// Gets the number of elements.
    /// </summary>
    /// <value>The element count.</value>
    public int Count => _items?.Length ?? 0;

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based element index.</param>
    /// <returns>The element at <paramref name="index" />.</returns>
    public T this[int index] => _items![index];

    /// <summary>
    /// Determines whether two arrays are element-wise equal.
    /// </summary>
    /// <param name="left">The first array.</param>
    /// <param name="right">The second array.</param>
    /// <returns><see langword="true" /> when the arrays are element-wise equal.</returns>
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two arrays are not element-wise equal.
    /// </summary>
    /// <param name="left">The first array.</param>
    /// <param name="right">The second array.</param>
    /// <returns><see langword="true" /> when the arrays differ.</returns>
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) =>
        !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(EquatableArray<T> other)
    {
        if (Count != other.Count)
            return false;

        for (int i = 0; i < Count; i++)
        {
            if (!this[i].Equals(other[i]))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is EquatableArray<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        int hash = 17;
        for (int i = 0; i < Count; i++)
            hash = (hash * 31) + this[i].GetHashCode();

        return hash;
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() =>
        ((IEnumerable<T>)(_items ?? [])).GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();
}
