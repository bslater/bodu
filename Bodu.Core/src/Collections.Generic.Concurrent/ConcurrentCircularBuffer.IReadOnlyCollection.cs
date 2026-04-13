// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBuffer.IReadOnlyCollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentCircularBuffer<T> :
    System.Collections.Generic.IReadOnlyCollection<T>
{
    /// <summary>
    /// Gets an approximate count of elements currently contained in the buffer.
    /// </summary>
    /// <value>
    /// The number of elements observed at the time of the call. Under concurrency, the value may be transiently stale. For a stable
    /// point-in-time count, call <see cref="ToArray" /> and use its length.
    /// </value>
    /// <remarks>
    /// <para>
    /// The count is computed as the difference between the tail and head positions, clamped to the range
    /// <c>[0, <see cref="Capacity"/>]</c>. Because these two positions are read independently, the result is approximate.
    /// </para>
    /// <para>
    /// The clamping guards against both the transient appearance of a negative difference under concurrent modification and the
    /// correct handling of signed counter wrapping after sustained high-volume use.
    /// </para>
    /// </remarks>
    public int Count
    {
        get
        {
            int head = Volatile.Read(ref this._head);
            int tail = Volatile.Read(ref this._tail);
            int diff = tail - head;
            if (diff < 0) diff = 0; // correct under signed counter wrapping; also guards transient races
            if (diff > this._capacity) diff = this._capacity; // clamp to capacity
            return diff;
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates over a snapshot of the buffer's contents.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> that iterates through the buffer in order from oldest to newest.</returns>
    /// <remarks>
    /// <para>
    /// The enumerator captures a snapshot at the moment of creation via <see cref="ToArray" />. Modifications to the buffer after the
    /// enumerator is created are not reflected in the enumerated results.
    /// </para>
    /// </remarks>
    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
}
