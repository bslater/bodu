// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSet{T}.Core.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentHashSet<T>
{
    /// <summary>
    /// Represents one generation of the set: the split-ordered list's head sentinel, the bucket shortcut array, and the
    /// element counter that belong to that list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bundling the three fields into one object gives <see cref="ConcurrentHashSet{T}.Clear" /> a single linearization
    /// point: swapping the root <c>_core</c> reference atomically replaces the list, its shortcuts, and its counter
    /// together, so counts can never leak across generations and an operation always works against the generation it
    /// initially read.
    /// </para>
    /// <para>
    /// <see cref="_buckets" /> holds lazily initialized shortcut pointers into the list; slot <c>b</c> is either
    /// <see langword="null" /> or the sentinel node for bucket <c>b</c>. The array length is always a power of two and
    /// is replaced (never mutated in length) by <see cref="ConcurrentHashSet{T}.TryGrow" /> via CAS. Slot 0 is
    /// populated with <see cref="_head" /> at construction; all other slots start <see langword="null" /> and are
    /// filled on first use.
    /// </para>
    /// </remarks>
    private sealed class Core
    {
        /// <summary>The permanent sentinel for bucket 0 (split-order key 0) — the entry point of the list. Never marked or removed.</summary>
        internal readonly Node _head;

        /// <summary>The power-of-two bucket shortcut array. Slots are published via CAS; the array reference itself is replaced by <see cref="ConcurrentHashSet{T}.TryGrow" /> via CAS when the set doubles.</summary>
        internal Node?[] _buckets;

        /// <summary>The number of elements in this generation, maintained with <see cref="Interlocked" /> increments and decrements after successful insert and mark operations.</summary>
        internal int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="Core" /> class with an empty list and the specified bucket
        /// count.
        /// </summary>
        /// <param name="bucketCount">The initial shortcut-array length. Must be a power of two.</param>
        /// <remarks>
        /// Plain (non-volatile) writes suffice here: the constructed instance is published to other threads only
        /// through a volatile or interlocked store of the <see cref="Core" /> reference itself, which orders these
        /// initializing writes before the publication.
        /// </remarks>
        internal Core(int bucketCount)
        {
            _head = Node.CreateSentinel(0UL, next: null);
            _buckets = new Node?[bucketCount];
            _buckets[0] = _head;
        }
    }
}
