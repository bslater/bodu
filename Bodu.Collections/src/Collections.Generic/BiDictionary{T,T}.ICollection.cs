// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionary{T,T}.ICollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public sealed partial class BiDictionary<TKey, TValue> :
    System.Collections.ICollection
{
    /// <summary>The lazily allocated object returned by <see cref="ICollection.SyncRoot" />.</summary>
    private object? _syncRoot;

    /// <inheritdoc />
    bool ICollection.IsSynchronized => false;

    /// <inheritdoc />
    object ICollection.SyncRoot
    {
        get
        {
            // Lazy initialization using a compare-and-swap to avoid allocating under contention.
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
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, index + _forward.Count);

        foreach (System.Collections.Generic.KeyValuePair<TKey, TValue> kvp in _forward)
            array.SetValue(new DictionaryEntry(kvp.Key, kvp.Value), index++);
    }
}
