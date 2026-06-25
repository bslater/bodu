// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionary{T,T}.Entry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents a stored entry, pairing the value with the ordering node that records the entry's position in the
    /// iteration order.
    /// </summary>
    private sealed class Entry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Entry" /> class with the specified value and ordering node.
        /// </summary>
        /// <param name="value">The value associated with the entry.</param>
        /// <param name="node">The linked-list node that holds the entry's key in the iteration order.</param>
        public Entry(TValue value, LinkedListNode<TKey> node)
        {
            Value = value;
            Node = node;
        }

        /// <summary>
        /// Gets or sets the value associated with the entry.
        /// </summary>
        /// <value>The stored value.</value>
        public TValue Value { get; set; }

        /// <summary>
        /// Gets the linked-list node that represents the entry's key in the iteration order, enabling O(1) removal and
        /// repositioning.
        /// </summary>
        /// <value>The ordering node for the entry's key.</value>
        public LinkedListNode<TKey> Node { get; }
    }
}
