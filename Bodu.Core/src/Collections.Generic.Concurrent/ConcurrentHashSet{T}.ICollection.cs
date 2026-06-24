// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSet{T}.ICollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentHashSet<T> :
    System.Collections.ICollection
{
    /// <summary>
    /// Gets a value indicating whether access to the <see cref="ConcurrentHashSet{T}" /> is synchronized (thread safe).
    /// </summary>
    /// <value>
    /// Always <see langword="false" />. <see cref="ConcurrentHashSet{T}" /> manages its own internal synchronization
    /// and does not expose a public lock object.
    /// </value>
    /// <remarks>
    /// Thread safety is achieved through internal lock striping. Callers should not attempt to coordinate access
    /// externally via <see cref="ICollection.SyncRoot" />, as that property is not supported.
    /// </remarks>
    bool ICollection.IsSynchronized => false;

    /// <summary>
    /// Gets an object that can be used to synchronize access to the collection. Not supported on this type —
    /// <see cref="ConcurrentHashSet{T}" /> manages its own internal synchronization.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Always thrown. Use the thread-safe members of this class directly.
    /// </exception>
    /// <remarks>
    /// Exposing a <see cref="ICollection.SyncRoot" /> would allow callers to take the same locks used internally,
    /// undermining the concurrency guarantees of the collection. This matches the behavior of
    /// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}" /> and other BCL concurrent
    /// collections.
    /// </remarks>
    object ICollection.SyncRoot =>
        throw new NotSupportedException(ResourceStrings.Op_NotSupported_ConcurrentSyncRoot);

    /// <summary>
    /// Copies the elements of the <see cref="ConcurrentHashSet{T}" /> to a one-dimensional, zero-based
    /// <see cref="Array" />, starting at the specified index.
    /// </summary>
    /// <param name="array">
    /// The destination array. Must not be <see langword="null" />, must be single-dimensional, and must have zero-based
    /// indexing.
    /// </param>
    /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="array" /> is multidimensional, does not have zero-based indexing, its element type is
    /// incompatible with <typeparamref name="T" />, or the number of elements in the set exceeds the available space
    /// from <paramref name="index" /> to the end of <paramref name="array" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is less than zero.</exception>
    /// <remarks>
    /// This method takes an atomic snapshot of the set before copying. The destination array reflects the state of the
    /// set at the moment the snapshot was taken and is not affected by concurrent modifications made afterward.
    /// </remarks>
    void ICollection.CopyTo(Array array, int index)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfNegative(index, nameof(index));
        T[] snapshot = ToArray();
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, index, snapshot.Length);

        try
        {
            Array.Copy(snapshot, 0, array, index, snapshot.Length);
        }
        catch (Exception ex) when (ex is ArrayTypeMismatchException or InvalidCastException)
        {
            throw new ArgumentException(ResourceStrings.Arg_Invalid_ArrayType, nameof(array), ex);
        }
    }
}
