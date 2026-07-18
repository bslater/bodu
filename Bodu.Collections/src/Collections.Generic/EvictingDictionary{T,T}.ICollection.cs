// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionary{T,T}.ICollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

using Bodu.Collections.Generic.Internal;

namespace Bodu.Collections.Generic;

public partial class EvictingDictionary<TKey, TValue> :
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
    /// Purges expired entries (raising the eviction events) so the count validated by the shared copy loop matches the
    /// elements written. Runs after argument-shape validation, so a caller error cannot trigger evictions.
    /// </remarks>
    void IOrderedDictionaryView<TKey, TValue>.PrepareViewCopy() =>
        PurgeExpired();

    /// <inheritdoc />
    IEnumerator<KeyValuePair<TKey, TValue>> IOrderedDictionaryView<TKey, TValue>.GetViewEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    void ICollection.CopyTo(Array array, int index) =>
        DictionaryViewCore.CopyEntriesTo(this, array, index);
}
