// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollection.ICollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Threading;

namespace Bodu.Collections.Generic;

#pragma warning disable CA1010 // Generic interface should also be implemented
#pragma warning disable CA1710 // Identifiers should have correct suffix

public abstract partial class RingBackedCollection<T> :
#pragma warning restore CA1710 // Identifiers should have correct suffix
#pragma warning restore CA1010 // Generic interface should also be implemented
    System.Collections.ICollection
{
    [NonSerialized]
    private object? _syncRoot;

    /// <inheritdoc />
    public int Count => _count;

    /// <summary>
    /// Gets a value indicating whether access to the collection is synchronized (thread-safe). Always
    /// returns <see langword="false"/>; ring-backed collections are not thread-safe by themselves.
    /// </summary>
    /// <value>Always <see langword="false"/>.</value>
    /// <returns>Always <see langword="false"/>.</returns>
    /// <remarks>
    /// External synchronization is the caller's responsibility. For a thread-safe FIFO buffer, see
    /// <see cref="Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer{T}"/>.
    /// </remarks>
    bool ICollection.IsSynchronized => false;

    /// <summary>
    /// Gets a lazily-initialised object that can be used to synchronize access to the collection.
    /// </summary>
    /// <value>A non-null object suitable as a <see cref="Monitor"/> target.</value>
    /// <returns>The synchronization root.</returns>
    object ICollection.SyncRoot =>
        _syncRoot ?? Interlocked.CompareExchange(ref _syncRoot, new object(), null) ?? _syncRoot!;

    /// <summary>
    /// Copies the collection's elements to a one-dimensional <see cref="Array"/>, starting at the specified
    /// index, in head-to-tail logical order.
    /// </summary>
    /// <param name="array">The destination array. Must be single-dimensional and zero-based.</param>
    /// <param name="index">The zero-based starting index in <paramref name="array"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="array"/> is multidimensional, not zero-based, or has an incompatible element type.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero.</exception>
    void ICollection.CopyTo(Array array, int index)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfNegative(index, nameof(index));
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, index + _count);

        try
        {
            CopyToInternal(array, index);
        }
        catch (ArrayTypeMismatchException ex)
        {
            throw new ArgumentException(ResourceStrings.Arg_Invalid_ArrayType, nameof(array), ex);
        }
    }
}
