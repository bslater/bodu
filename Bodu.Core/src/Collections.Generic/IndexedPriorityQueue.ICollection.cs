// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedPriorityQueue.ICollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Bodu.Collections.Generic;

#pragma warning disable CA1010 // Generic interface should also be implemented
#pragma warning disable CA1710 // Identifiers should have correct suffix

public sealed partial class IndexedPriorityQueue<TElement, TPriority> :
#pragma warning restore CA1710 // Identifiers should have correct suffix
#pragma warning restore CA1010 // Generic interface should also be implemented
    ICollection
{
    /// <summary>
    /// Lazily-allocated synchronisation root returned by the explicit <see cref="ICollection.SyncRoot"/> implementation.
    /// </summary>
    [NonSerialized]
    private object? _syncRoot;

    /// <summary>
    /// Gets a value indicating whether access to the queue is synchronized (thread-safe). Always returns
    /// <see langword="false"/>; <see cref="IndexedPriorityQueue{TElement, TPriority}"/> is not thread-safe.
    /// </summary>
    /// <value>Always <see langword="false"/>.</value>
    /// <returns>Always <see langword="false"/>.</returns>
    bool ICollection.IsSynchronized => false;

    /// <summary>
    /// Gets a lazily-initialised object that can be used to synchronise access to the queue.
    /// </summary>
    /// <value>A non-null object suitable as a <see cref="Monitor"/> target.</value>
    /// <returns>The synchronisation root.</returns>
    object ICollection.SyncRoot =>
        _syncRoot ?? Interlocked.CompareExchange(ref _syncRoot, new object(), null) ?? _syncRoot!;

    /// <summary>
    /// Copies the queue's element-priority pairs to the specified array, starting at the specified index,
    /// in heap-storage order.
    /// </summary>
    /// <param name="array">The destination array. Must be single-dimensional, zero-based, and have an element type compatible with <see cref="KeyValuePair{TElement, TPriority}"/>.</param>
    /// <param name="index">The zero-based starting index in <paramref name="array"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="array"/> is multidimensional, not zero-based, has an incompatible element type, or has insufficient room from <paramref name="index"/> onward.
    /// </exception>
    void ICollection.CopyTo(Array array, int index)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfNegative(index);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, index + _size);

        try
        {
            if (array is KeyValuePair<TElement, TPriority>[] typedArray)
            {
                for (int i = 0; i < _size; i++)
                {
                    (TElement Element, TPriority Priority) node = _nodes[i];
                    typedArray[index + i] = new KeyValuePair<TElement, TPriority>(node.Element, node.Priority);
                }
            }
            else
            {
                for (int i = 0; i < _size; i++)
                {
                    (TElement Element, TPriority Priority) node = _nodes[i];
                    array.SetValue(new KeyValuePair<TElement, TPriority>(node.Element, node.Priority), index + i);
                }
            }
        }
        catch (ArrayTypeMismatchException ex)
        {
            throw new ArgumentException(ResourceStrings.Arg_Invalid_ArrayType, nameof(array), ex);
        }
        catch (InvalidCastException ex)
        {
            throw new ArgumentException(ResourceStrings.Arg_Invalid_ArrayType, nameof(array), ex);
        }
    }
}
