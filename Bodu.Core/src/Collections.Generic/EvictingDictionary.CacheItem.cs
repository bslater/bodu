// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="EvictingDictionary.CacheItem.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Bodu.Collections.Generic;

public partial class EvictingDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents a cache entry storing the value and metadata used for eviction policies in the <see cref="EvictingDictionary{TKey, TValue}" />.
    /// </summary>
#if !NET5_0_OR_GREATER

    private sealed class CacheItem
#else
    private sealed record class CacheItem
#endif
    {
        /// <summary>
        /// Gets the value associated with the cache entry.
        /// </summary>
        public TValue Value { get; private set; }

        /// <summary>
        /// Gets or sets the linked list node that represents the key in the ordering structure. Used by recency- and access-order-based
        /// eviction policies such as LRU and MRU.
        /// </summary>
        public LinkedListNode<TKey>? Node { get; set; }

        /// <summary>
        /// Gets or sets the access frequency for the entry. Used by frequency-based eviction policies such as LFU.
        /// </summary>
        public int Frequency { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets a flag indicating whether the item has been recently accessed. Used by the
        /// Second-Chance eviction policy to determine eligibility for eviction.
        /// </summary>
        public bool SecondChance { get; set; }

        public CacheItem(TValue value) => Value = value;
    }
}
