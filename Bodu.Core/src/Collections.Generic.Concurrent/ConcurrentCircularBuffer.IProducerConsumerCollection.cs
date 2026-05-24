// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBuffer.IProducerConsumerCollection.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBuffer<T> :
    IProducerConsumerCollection<T>
{
    /// <summary>
    /// Attempts to add <paramref name="item" /> to the buffer using the producer-side path.
    /// </summary>
    /// <param name="item">The element to add. May be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the element was enqueued; <see langword="false" /> if the buffer is full and
    /// <see cref="AllowOverwrite" /> is <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Forwards to <see cref="TryEnqueue(T)" />. Provided to satisfy <see cref="IProducerConsumerCollection{T}" /> so
    /// the buffer can be wrapped by <see cref="BlockingCollection{T}" /> for blocking-consumer scenarios.
    /// </remarks>
    bool IProducerConsumerCollection<T>.TryAdd(T item) => TryEnqueue(item);

    /// <summary>
    /// Attempts to remove and return the oldest element from the buffer.
    /// </summary>
    /// <param name="item">
    /// When this method returns <see langword="true" />, contains the removed element; when it returns
    /// <see langword="false" />, contains the default value of <typeparamref name="T" />.
    /// </param>
    /// <returns><see langword="true" /> if an element was removed; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// Forwards to <see cref="TryDequeue(out T?)" />. Because <typeparamref name="T" /> is constrained to a reference
    /// type, the <see cref="MaybeNullWhenAttribute" /> on the interface signature is honored: the out parameter is
    /// <see langword="null" /> when the method returns <see langword="false" />.
    /// </remarks>
    bool IProducerConsumerCollection<T>.TryTake([MaybeNullWhen(false)] out T item)
    {
        var taken = TryDequeue(out T? value);
        item = value!;
        return taken;
    }
}
