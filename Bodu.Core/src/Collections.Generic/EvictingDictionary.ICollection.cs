// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="EvictingDictionary.ICollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------- //

using System;
using System.Collections;
using System.Threading;

namespace Bodu.Collections.Generic;

public partial class EvictingDictionary<TKey, TValue> :
    System.Collections.ICollection
{
    private object? _syncRoot;

    /// <inheritdoc />
    bool ICollection.IsSynchronized => false;

    /// <inheritdoc />
    object ICollection.SyncRoot
    {
        get
        {
            // Lazy initialisation using a compare-and-swap to avoid allocating under contention.
            return _syncRoot ?? Interlocked.CompareExchange(ref _syncRoot, new object(), null) ?? _syncRoot!;
        }
    }

    /// <inheritdoc />
    void ICollection.CopyTo(Array array, int index)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfLessThan(index, 0);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, index + _store.Count);

        foreach (System.Collections.Generic.KeyValuePair<TKey, TValue> kvp in GetOrderedItems())
            array.SetValue(new DictionaryEntry(kvp.Key, kvp.Value), index++);
    }
}
