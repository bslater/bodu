// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBuffer{T}.IEnumerable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentCircularBuffer<T> :
    System.Collections.Generic.IEnumerable<T>
{
    /// <summary>
    /// Returns an enumerator that iterates over a point-in-time snapshot of the buffer's contents.
    /// </summary>
    /// <returns>
    /// An <see cref="Enumerator" /> that iterates through the elements in FIFO order, from oldest to newest, as they
    /// existed at the moment the enumerator was created.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Enumeration operates on an atomic snapshot captured via <see cref="ToArray" /> at the moment this method is
    /// called. Any elements enqueued or dequeued after the enumerator is created are not reflected in the enumerated
    /// sequence.
    /// </para>
    /// <para>
    /// Because the enumerator operates on a snapshot, it will never throw
    /// <see cref="System.InvalidOperationException" /> due to concurrent modification — unlike enumerators on
    /// non-concurrent collections.
    /// </para>
    /// </remarks>
    public Enumerator GetEnumerator() => new(this);

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
}
