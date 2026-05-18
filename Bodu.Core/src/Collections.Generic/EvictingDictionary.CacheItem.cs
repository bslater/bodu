// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionary.CacheItem.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionary<TKey, TValue>
{
#if !NET5_0_OR_GREATER

    /// <summary>
    /// Represents a cache entry storing the value and metadata used for eviction policies in the <see cref="EvictingDictionary{TKey, TValue}"/>.
    /// </summary>
    private sealed class CacheItem
#else
    /// <summary>
    /// Represents a cache entry storing the value and metadata used for eviction policies in the
    /// <see cref="EvictingDictionary{TKey, TValue}" />.
    /// </summary>
    private sealed record class CacheItem
#endif
    {
        /// <summary>
        /// Gets or sets the value associated with the cache entry.
        /// </summary>
        public TValue Value { get; set; }

        /// <summary>
        /// Gets or sets the linked list node that represents the key in the ordering structure. Used by recency- and
        /// access-order-based eviction policies such as LRU and MRU.
        /// </summary>
        public LinkedListNode<TKey>? Node { get; set; }

        /// <summary>
        /// Gets or sets the access frequency for the entry. Used by frequency-based eviction policies such as LFU.
        /// </summary>
        public int Frequency { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether this entry has been recently accessed. Used by the Second-Chance
        /// eviction policy to determine eligibility for eviction; when <see langword="true" />, the item is spared and
        /// the flag is cleared.
        /// </summary>
        public bool SecondChance { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem" /> class with the specified value.
        /// </summary>
        /// <param name="value">The value to store in the cache entry.</param>
        public CacheItem(TValue value) => Value = value;
    }
}
