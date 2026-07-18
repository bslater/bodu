// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionary{T,T}.ICollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

using Bodu.Collections.Generic.Internal;

namespace Bodu.Collections.Generic;

public partial class SequencedDictionary<TKey, TValue> :
    System.Collections.ICollection,
    IOrderedDictionaryView<TKey, TValue>
{
    /// <summary>The lazily allocated object returned by <see cref="ICollection.SyncRoot" />.</summary>
    private object? _syncRoot;

    /// <inheritdoc />
    bool ICollection.IsSynchronized => false;

    /// <inheritdoc />
    object ICollection.SyncRoot =>
        DictionaryViewCore.GetSyncRoot(ref _syncRoot);

    /// <inheritdoc />
    int IOrderedDictionaryView<TKey, TValue>.ViewCount => _store.Count;

    /// <inheritdoc />
    /// <remarks>
    /// The sequenced dictionary has no expiry or other pre-copy work; this is a no-op.
    /// </remarks>
    void IOrderedDictionaryView<TKey, TValue>.PrepareViewCopy()
    {
    }

    /// <inheritdoc />
    IEnumerator<KeyValuePair<TKey, TValue>> IOrderedDictionaryView<TKey, TValue>.GetViewEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    void ICollection.CopyTo(Array array, int index) =>
        DictionaryViewCore.CopyEntriesTo(this, array, index);
}
