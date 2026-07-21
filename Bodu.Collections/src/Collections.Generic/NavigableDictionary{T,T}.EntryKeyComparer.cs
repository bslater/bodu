// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionary{T,T}.EntryKeyComparer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class NavigableDictionary<TKey, TValue>
{
    /// <summary>
    /// Orders key/value pairs by key using the dictionary's key comparer, allowing the bulk-load constructor to sort
    /// with <see cref="Array.Sort{T}(T[], IComparer{T})" /> directly instead of routing every comparison through a
    /// <see cref="Comparison{T}" /> delegate wrapper.
    /// </summary>
    private sealed class EntryKeyComparer
        : IComparer<KeyValuePair<TKey, TValue>>
    {
        /// <summary>The key comparer that defines the entry ordering.</summary>
        private readonly IComparer<TKey> _keyComparer;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntryKeyComparer" /> class.
        /// </summary>
        /// <param name="keyComparer">The key comparer that defines the entry ordering.</param>
        public EntryKeyComparer(IComparer<TKey> keyComparer)
        {
            _keyComparer = keyComparer;
        }

        /// <inheritdoc />
        public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) =>
            _keyComparer.Compare(x.Key, y.Key);
    }
}
