// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBuffer{T}.EvictionHandlers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentCircularBuffer<T>
{
    /// <summary>
    /// Caches the materialized invocation list for the most recently observed <see cref="ItemEvicted" /> delegate, so
    /// the overwrite eviction path does not allocate a fresh <see cref="Delegate" /> array per evicted item.
    /// </summary>
    /// <remarks>
    /// The reference is read and replaced with plain (non-volatile) accesses: a stale read merely re-materializes the
    /// list, and because the source delegate and its handler array travel in one immutable object, a racing refresh can
    /// never publish a mismatched pair.
    /// </remarks>
    private EvictionHandlers? _evictionHandlers;

    /// <summary>
    /// Pairs an <see cref="ItemEvicted" /> delegate instance with its materialized invocation list, allowing the
    /// eviction path to reuse the array until the subscription list changes.
    /// </summary>
    private sealed class EvictionHandlers
    {
        /// <summary>The multicast delegate the invocation list was materialized from.</summary>
        internal readonly Action<T> Source;

        /// <summary>The materialized invocation list of <see cref="Source" />.</summary>
        internal readonly Delegate[] Handlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="EvictionHandlers" /> class.
        /// </summary>
        /// <param name="source">The delegate whose invocation list is materialized. Must not be <see langword="null" />.</param>
        internal EvictionHandlers(Action<T> source)
        {
            Source = source;
            Handlers = source.GetInvocationList();
        }
    }
}
