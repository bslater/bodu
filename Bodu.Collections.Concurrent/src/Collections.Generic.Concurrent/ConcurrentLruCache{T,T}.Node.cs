// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCache{T,T}.Node.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentLruCache<TKey, TValue>
{
    /// <summary>
    /// Represents a cache entry: an immutable key/value pair plus the mutable recency and lifecycle metadata used by
    /// the pseudo-LRU maintenance cycle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value is deliberately immutable: updating a key replaces the whole node rather than mutating
    /// <see cref="Value" /> in place, because lock-free readers dereference the value with no synchronization and an
    /// in-place write of a multi-word struct could be observed torn.
    /// </para>
    /// <para>
    /// <see cref="State" /> tracks which queue the node logically belongs to. Only the maintenance-lock holder moves a
    /// node between the Hot/Warm/Cold states; the transition to <see cref="StateRemoved" /> may additionally be
    /// performed by <see cref="ConcurrentLruCache{TKey, TValue}.TryRemove" />, <see cref="ConcurrentLruCache{TKey,
    /// TValue}.Clear" />, or a replacing write, so every state move is made through an interlocked transition and a
    /// failed transition means the node died concurrently and must be dropped.
    /// </para>
    /// </remarks>
    private sealed class Node
    {
        /// <summary>The node is logically in the hot queue, where new entries land.</summary>
        internal const int StateHot = 0;

        /// <summary>The node is logically in the warm queue, holding entries that have proven their reuse.</summary>
        internal const int StateWarm = 1;

        /// <summary>The node is logically in the cold queue, one unaccessed pass away from eviction.</summary>
        internal const int StateCold = 2;

        /// <summary>The node is dead: evicted, explicitly removed, replaced, or cleared. A dead node still queued is dropped when dequeued.</summary>
        internal const int StateRemoved = 3;

        /// <summary>The key the node is stored under. Immutable.</summary>
        private readonly TKey _key;

        /// <summary>The cached value. Immutable — updates replace the node.</summary>
        private readonly TValue _value;

        /// <summary>Indicates whether the entry has been read since the maintenance cycle last examined it. Set lock-free by readers; cleared by the maintainer.</summary>
        private volatile bool _wasAccessed;

        /// <summary>The node's current lifecycle state; one of the <c>State*</c> constants. Mutated only through interlocked transitions.</summary>
        private int _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="Node" /> class in the specified initial state.
        /// </summary>
        /// <param name="key">The key the node is stored under.</param>
        /// <param name="value">The cached value.</param>
        /// <param name="state">The initial lifecycle state; one of the <c>State*</c> constants.</param>
        internal Node(TKey key, TValue value, int state)
        {
            _key = key;
            _value = value;
            _state = state;
        }

        /// <summary>
        /// Gets the key the node is stored under.
        /// </summary>
        internal TKey Key => _key;

        /// <summary>
        /// Gets the cached value.
        /// </summary>
        internal TValue Value => _value;

        /// <summary>
        /// Gets or sets a value indicating whether the entry has been read since the maintenance cycle last examined
        /// it.
        /// </summary>
        internal bool WasAccessed
        {
            get => _wasAccessed;
            set => _wasAccessed = value;
        }

        /// <summary>
        /// Gets the node's current lifecycle state with acquire semantics.
        /// </summary>
        internal int State => Volatile.Read(ref _state);

        /// <summary>
        /// Unconditionally marks the node dead. Used by explicit removal, replacement, and clear, where the caller has
        /// already claimed the node by removing it from the dictionary.
        /// </summary>
        internal void MarkRemoved() =>
            Volatile.Write(ref _state, StateRemoved);

        /// <summary>
        /// Attempts an interlocked lifecycle transition.
        /// </summary>
        /// <param name="expectedState">The state the node must currently be in.</param>
        /// <param name="newState">The state to move to.</param>
        /// <returns>
        /// <see langword="true" /> if the transition was applied; <see langword="false" /> if the node was no longer in
        /// <paramref name="expectedState" /> — which under this type's protocol means it was concurrently removed.
        /// </returns>
        internal bool TryTransition(int expectedState, int newState) =>
            Interlocked.CompareExchange(ref _state, newState, expectedState) == expectedState;
    }
}
