// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBuffer.ICollection.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBuffer<T>
    : System.Collections.ICollection
{
    /// <summary>
    /// Gets an approximate count of elements currently contained in the buffer.
    /// </summary>
    /// <value>
    /// The number of elements observed at the time of the call. Under concurrency, the value may be transiently stale.
    /// For a stable point-in-time count, call <see cref="ToArray" /> and use its length.
    /// </value>
    /// <remarks>
    /// <para>
    /// The count is computed as the difference between the tail and head positions, clamped to the range
    /// <c>[0, <see cref="Capacity"/>]</c>. Because these two positions are read independently, the result is
    /// approximate.
    /// </para>
    /// <para>
    /// The clamping guards against both the transient appearance of a negative difference under concurrent modification
    /// and the correct handling of signed counter wrapping after sustained high-volume use.
    /// </para>
    /// </remarks>
    public int Count
    {
        get
        {
            var head = Volatile.Read(ref _head);
            var tail = Volatile.Read(ref _tail);
            var diff = tail - head;
            if (diff < 0) diff = 0;
            if (diff > _capacity) diff = _capacity;
            return diff;
        }
    }

    /// <summary>
    /// Gets a value indicating whether access to the <see cref="ConcurrentCircularBuffer{T}" /> is synchronized (thread
    /// safe).
    /// </summary>
    /// <value>
    /// Always <see langword="false" />. <see cref="ConcurrentCircularBuffer{T}" /> manages its own internal
    /// synchronization and does not expose a public lock object.
    /// </value>
    /// <remarks>
    /// Thread safety is achieved through internal lock-free or fine-grained locking mechanisms. Callers should not
    /// attempt to coordinate access externally via <see cref="ICollection.SyncRoot" />, as that property is not
    /// supported.
    /// </remarks>
    bool ICollection.IsSynchronized => false;

    /// <summary>
    /// Gets an object that can be used to synchronize access to the collection. Not supported on this type —
    /// <see cref="ConcurrentCircularBuffer{T}" /> manages its own internal synchronization.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Always thrown. Use the thread-safe members of this class directly.
    /// </exception>
    /// <remarks>
    /// Exposing a <see cref="ICollection.SyncRoot" /> would allow callers to take the same lock used internally,
    /// undermining the concurrency guarantees of the collection. This matches the behavior of
    /// <see cref="System.Collections.Concurrent.ConcurrentQueue{T}" /> and other BCL concurrent collections.
    /// </remarks>
    object ICollection.SyncRoot =>
        throw new NotSupportedException(ResourceStrings.Op_NotSupported_ConcurrentSyncRoot);

    /// <summary>
    /// Copies the elements of the <see cref="ConcurrentCircularBuffer{T}" /> to a one-dimensional, zero-based
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
    /// incompatible with <typeparamref name="T" />, or the number of elements in the buffer exceeds the available space
    /// from <paramref name="index" /> to the end of <paramref name="array" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is less than zero.</exception>
    /// <remarks>
    /// <para>
    /// This method takes an atomic snapshot of the buffer before copying. The destination array reflects the state of
    /// the buffer at the moment the snapshot was taken and is not affected by concurrent modifications made after that
    /// point.
    /// </para>
    /// <para>
    /// This method supports the non-generic <see cref="ICollection" /> interface and is intended for interop scenarios.
    /// For strongly typed copies, use <see cref="CopyTo(T[], int)" /> instead.
    /// </para>
    /// </remarks>
    void ICollection.CopyTo(Array array, int index)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfNegative(index, nameof(index));
        T[] snap = ToArray();
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, index + snap.Length);

        try
        {
            Array.Copy(snap, 0, array, index, snap.Length);
        }
        catch (ArrayTypeMismatchException ex)
        {
            throw new ArgumentException(ResourceStrings.Arg_Invalid_ArrayType, nameof(array), ex);
        }
    }
}
