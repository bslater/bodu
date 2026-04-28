// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDeque.ICollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Threading;

namespace Bodu.Collections.Generic;

#pragma warning disable CA1010 // Generic interface should also be implemented
#pragma warning disable CA1710 // Identifiers should have correct suffix

public partial class ArrayDeque<T> :
#pragma warning restore CA1710 // Identifiers should have correct suffix
#pragma warning restore CA1010 // Generic interface should also be implemented
    System.Collections.ICollection
{
    [NonSerialized]
    private object? _syncRoot;

    /// <inheritdoc />
    public int Count => _storage.Count;

    /// <summary>
    /// Gets a value indicating whether access to the <see cref="ArrayDeque{T}"/> is synchronized (thread safe).
    /// </summary>
    /// <value>Always <see langword="false"/>. <see cref="ArrayDeque{T}"/> is not thread-safe.</value>
    /// <returns>Always <see langword="false"/>.</returns>
    bool ICollection.IsSynchronized => false;

    /// <summary>
    /// Gets an object that can be used to synchronize access to the <see cref="ArrayDeque{T}"/>.
    /// </summary>
    /// <value>A lazily-initialised synchronization root.</value>
    /// <returns>An object that can be locked by callers wanting external synchronization.</returns>
    object ICollection.SyncRoot
    {
        get
        {
            return _syncRoot ?? Interlocked.CompareExchange(ref _syncRoot, new object(), null) ?? _syncRoot!;
        }
    }

    /// <summary>
    /// Copies the deque's elements to a one-dimensional <see cref="Array"/>, starting at the specified index.
    /// </summary>
    /// <param name="array">The destination array. Must be single-dimensional and zero-based.</param>
    /// <param name="index">The zero-based starting index in <paramref name="array"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="array"/> is multidimensional, not zero-based, or has an incompatible element type.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    void ICollection.CopyTo(Array array, int index)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfNegative(index, nameof(index));
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, index + _storage.Count);

        try
        {
            _storage.CopyToInternal(array, index);
        }
        catch (ArrayTypeMismatchException ex)
        {
            throw new ArgumentException(ResourceStrings.Arg_Invalid_ArrayType, nameof(array), ex);
        }
    }
}
