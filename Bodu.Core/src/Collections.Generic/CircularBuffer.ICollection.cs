// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBuffer.ICollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Threading;

namespace Bodu.Collections.Generic;

#pragma warning disable CA1010 // Generic interface should also be implemented
#pragma warning disable CA1710 // Identifiers should have correct suffix

public partial class CircularBuffer<T> :
#pragma warning restore CA1710 // Identifiers should have correct suffix
#pragma warning restore CA1010 // Generic interface should also be implemented
    System.Collections.ICollection
{
    [NonSerialized]
    private object? _syncRoot;

    /// <inheritdoc />
    public int Count => _count;

    /// <summary>
    /// Gets a value indicating whether access to the <see cref="CircularBuffer{T}" /> is synchronized (thread safe).
    /// </summary>
    /// <value>Always returns <see langword="false" />. <see cref="CircularBuffer{T}" /> is not thread-safe.</value>
    /// <remarks>
    /// If thread safety is required, external synchronization is the responsibility of the caller. For a thread-safe alternative, consider
    /// using <see cref="Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer{T}" />.
    /// </remarks>
    bool ICollection.IsSynchronized => false;

    /// <summary>
    /// Gets an object that can be used to synchronize access to the <see cref="CircularBuffer{T}" />.
    /// </summary>
    /// <value>An object that can be used to synchronize access to the collection.</value>
    /// <remarks>
    /// This property returns a lazily-initialized synchronization root. Even though the buffer itself is not thread-safe, consumers can use
    /// this object with <see cref="System.Threading.Monitor.Enter(object)" /> to implement external synchronization. For a built-in
    /// thread-safe implementation, see <see cref="Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer{T}" />.
    /// </remarks>
    object ICollection.SyncRoot
    {
        get
        {
            // Thread-safe lazy initialization
            return _syncRoot ?? Interlocked.CompareExchange(ref _syncRoot, new object(), null) ?? _syncRoot!;
        }
    }

    /// <summary>
    /// Copies the elements of the <see cref="CircularBuffer{T}" /> to a one-dimensional <see cref="Array" />, starting at the specified index.
    /// </summary>
    /// <param name="array">The destination array. Must be single-dimensional and have zero-based indexing.</param>
    /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="array" /> is not compatible with the buffer element type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index" /> is less than 0.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the number of elements in the buffer is greater than the available space from <paramref name="index" /> to the end of the
    /// target array.
    /// </exception>
    /// <remarks>
    /// This method supports the non-generic <see cref="ICollection" /> interface and is intended for interop scenarios. For strongly typed
    /// copies, use <see cref="CopyTo(T[], int)" /> instead.
    /// </remarks>
    void ICollection.CopyTo(Array array, int index)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayIsNotSingleDimension(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, index, _count);

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
