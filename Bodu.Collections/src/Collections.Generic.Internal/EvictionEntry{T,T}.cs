// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictionEntry{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Represents a cache entry storing the value and the metadata used by the eviction policies shared between
/// <see cref="EvictingDictionary{TKey, TValue}" /> and the segments of
/// <c>ConcurrentEvictingDictionary&lt;TKey, TValue&gt;</c>.
/// </summary>
/// <typeparam name="TKey">The type of keys tracked by the owning cache.</typeparam>
/// <typeparam name="TValue">The type of values stored by the owning cache.</typeparam>
/// <remarks>
/// The entry itself is not thread-safe: the non-concurrent dictionary mutates it single-threaded, and the concurrent
/// dictionary only ever reads or mutates it while the owning segment's stripe lock is held, so the mutable members
/// require no additional synchronization.
/// </remarks>
internal sealed class EvictionEntry<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Gets or sets the value associated with the cache entry.
    /// </summary>
    public TValue Value { get; set; }

    /// <summary>
    /// Gets or sets the linked list node that represents the key in the policy's tracking structure: the access order
    /// list for recency-based policies (FIFO, LRU, MRU, SecondChance), or the current frequency bucket for
    /// LeastFrequentlyUsed. Storing the node keeps bucket removal O(1) and independent of the key comparer.
    /// </summary>
    public LinkedListNode<TKey>? Node { get; set; }

    /// <summary>
    /// Gets or sets the access frequency for the entry. Used by frequency-based eviction policies such as LFU.
    /// </summary>
    public int Frequency { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether this entry has been recently accessed. Used by the Second-Chance
    /// eviction policy to determine eligibility for eviction; when <see langword="true" />, the item is spared and the
    /// flag is cleared.
    /// </summary>
    public bool SecondChance { get; set; }

    /// <summary>
    /// Gets or sets the absolute expiration deadline for the entry, expressed as UTC ticks from the owning dictionary's
    /// time provider. <see cref="long.MaxValue" /> means the entry never expires. Only meaningful when the dictionary
    /// has an expiration configuration.
    /// </summary>
    public long ExpiresAtTicks { get; set; } = long.MaxValue;

    /// <summary>
    /// Gets or sets the effective time-to-live for the entry in ticks (the per-entry override when supplied, otherwise
    /// the dictionary default). Zero means the entry has no lifetime. Used to recompute <see cref="ExpiresAtTicks" />
    /// on sliding refresh. Only meaningful when the dictionary has an expiration configuration.
    /// </summary>
    public long TtlTicks { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictionEntry{TKey, TValue}" /> class with the specified value.
    /// </summary>
    /// <param name="value">The value to store in the cache entry.</param>
    public EvictionEntry(TValue value) => Value = value;
}
