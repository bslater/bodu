// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Multiset.ICollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public sealed partial class Multiset<T>
    : System.Collections.ICollection
{
    /// <summary>
    /// Lazily initialized synchronization root for <see cref="System.Collections.ICollection.SyncRoot" />.
    /// </summary>
    [NonSerialized]
    private object? _syncRoot;

    /// <summary>
    /// Gets a value indicating whether access to the <see cref="Multiset{T}" /> is synchronized (thread-safe). Always
    /// returns <see langword="false" />; <see cref="Multiset{T}" /> is not thread-safe.
    /// </summary>
    /// <value>Always <see langword="false" />.</value>
    /// <returns><see langword="false" />.</returns>
    /// <remarks>
    /// External synchronization is the caller's responsibility.
    /// </remarks>
    bool ICollection.IsSynchronized => false;

    /// <summary>
    /// Gets a lazily-initialized object that can be used to synchronize access to the <see cref="Multiset{T}" />.
    /// </summary>
    /// <value>A non-null object suitable as a <see cref="Monitor" /> target.</value>
    /// <returns>The synchronization root object.</returns>
    object ICollection.SyncRoot =>
        _syncRoot ?? Interlocked.CompareExchange(ref _syncRoot, new object(), null) ?? _syncRoot!;

    /// <summary>
    /// Copies all elements of the <see cref="Multiset{T}" /> to a one-dimensional <see cref="Array" />, starting at the
    /// specified index. Each element is copied as many times as its occurrence count.
    /// </summary>
    /// <param name="array">
    /// The destination array. Must be a single-dimensional, zero-based array of a compatible type.
    /// </param>
    /// <param name="index">The zero-based starting index in <paramref name="array" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is less than zero.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="array" /> is multidimensional, not zero-based, or has an incompatible element type; or the
    /// number of elements in the multiset exceeds the available space from <paramref name="index" /> to the end of
    /// <paramref name="array" />.
    /// </exception>
    void ICollection.CopyTo(Array array, int index)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfLessThan(index, 0);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, index + _count);

        try
        {
            foreach (KeyValuePair<T, int> pair in _items)
            {
                for (int i = 0; i < pair.Value; i++)
                    array.SetValue(pair.Key, index++);
            }
        }
        catch (InvalidCastException ex)
        {
            throw new ArgumentException(ResourceStrings.Arg_Invalid_ArrayType, nameof(array), ex);
        }
    }
}
