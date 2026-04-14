// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBuffer.IEnumerable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;

namespace Bodu.Collections.Generic;

public partial class CircularBuffer<T> :
    System.Collections.Generic.IEnumerable<T>
{
    /// <summary>
    /// Returns an enumerator that iterates over the buffer's contents in FIFO order.
    /// </summary>
    /// <returns>An <see cref="Enumerator"/> that provides forward-only, read-only access to the buffer's elements.</returns>
    /// <remarks>
    /// <para>
    /// The enumerator reads elements directly from the buffer's internal storage using a version token captured at the moment the
    /// enumerator is created. Any structural modification to the buffer — including <see cref="Enqueue"/>, <see cref="TryEnqueue"/>,
    /// <see cref="Dequeue"/>, <see cref="TryDequeue"/>, <see cref="Clear"/>, or <see cref="TrimExcess"/> — invalidates the enumerator.
    /// A subsequent call to <see cref="Enumerator.MoveNext"/> or <see cref="Enumerator.Reset"/> will throw
    /// <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// For a stable point-in-time copy that is unaffected by subsequent modifications, use <see cref="ToArray"/> instead.
    /// </para>
    /// <para>
    /// <b>Note:</b> This type is not thread-safe. If concurrent access is required, use
    /// <see cref="Bodu.Collections.Generic.Concurrent.ConcurrentCircularBuffer{T}"/> instead.
    /// </para>
    /// </remarks>
    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
}
