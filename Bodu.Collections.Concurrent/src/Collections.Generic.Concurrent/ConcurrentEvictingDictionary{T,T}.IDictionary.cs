// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionary{T,T}.IDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentEvictingDictionary<TKey, TValue> :
    IDictionary<TKey, TValue>
{
    /// <summary>
    /// Gets a value indicating whether the dictionary is read-only.
    /// </summary>
    /// <value>Always <see langword="false" />; the dictionary supports additions, updates, and removals.</value>
    public bool IsReadOnly => false;

    /// <inheritdoc />
    /// <remarks>
    /// Returns a point-in-time snapshot of the live keys wrapped as a read-only <see cref="ICollection{TKey}" />;
    /// mutating the returned collection is not supported. This is the same snapshot produced by the public
    /// <see cref="Keys" /> property.
    /// </remarks>
    ICollection<TKey> IDictionary<TKey, TValue>.Keys => (ICollection<TKey>)Keys;

    /// <inheritdoc />
    /// <remarks>
    /// Returns a point-in-time snapshot of the live values wrapped as a read-only <see cref="ICollection{TValue}" />;
    /// mutating the returned collection is not supported. This is the same snapshot produced by the public
    /// <see cref="Values" /> property.
    /// </remarks>
    ICollection<TValue> IDictionary<TKey, TValue>.Values => (ICollection<TValue>)Values;

    /// <summary>
    /// Removes the entry with the specified key.
    /// </summary>
    /// <param name="key">The key of the entry to remove.</param>
    /// <returns>
    /// <see langword="true" /> if an entry was found and removed; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This is the non-out companion to <see cref="TryRemove" />, sharing its semantics: it operates on physically
    /// stored entries (removing an expired-but-unpurged entry and returning <see langword="true" />), is not an
    /// eviction (it does not raise <see cref="ItemEvicted" /> or increment <see cref="EvictionCount" />), and locks
    /// only the segment that owns <paramref name="key" />.
    /// </remarks>
    public bool Remove(TKey key) =>
        TryRemove(key, out _);

    /// <summary>
    /// Determines whether the dictionary contains a live entry equal to the specified key/value pair.
    /// </summary>
    /// <param name="item">The key/value pair to locate.</param>
    /// <returns>
    /// <see langword="true" /> if a live entry exists whose key matches <c>item.Key</c> and whose value equals
    /// <c>item.Value</c> under <see cref="EqualityComparer{TValue}.Default" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><c>item.Key</c> is <see langword="null" />.</exception>
    /// <remarks>
    /// Like <see cref="ContainsKey" />, this is a pure read with respect to the capacity policy: it does not reposition
    /// the key, update frequency metadata, or increment <see cref="TotalTouches" />. An expired-but-unpurged entry
    /// counts as absent and is lazily removed as an eviction. The operation locks only the segment that owns the key.
    /// </remarks>
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        ThrowHelper.ThrowIfNull(item.Key);

        List<KeyValuePair<TKey, TValue>>? evicted = null;
        int index = GetSegmentIndex(item.Key);
        bool found;
        lock (_locks[index])
        {
            found = _segments[index].TryPeek(item.Key, ref evicted, out TValue value)
                && EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }

        OnEvicted(evicted);
        return found;
    }

    /// <summary>
    /// Removes the entry equal to the specified key/value pair.
    /// </summary>
    /// <param name="item">The key/value pair to remove.</param>
    /// <returns>
    /// <see langword="true" /> if a live entry whose key matches <c>item.Key</c> and whose value equals
    /// <c>item.Value</c> was found and removed; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><c>item.Key</c> is <see langword="null" />.</exception>
    /// <remarks>
    /// The value comparison and removal happen atomically under the owning segment's lock, so a concurrent update of
    /// the value cannot cause a mismatched entry to be removed. An explicit removal is not an eviction — it does not
    /// raise <see cref="ItemEvicted" /> — though a lazily removed expired entry encountered along the way is surfaced
    /// as one.
    /// </remarks>
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        ThrowHelper.ThrowIfNull(item.Key);

        List<KeyValuePair<TKey, TValue>>? evicted = null;
        int index = GetSegmentIndex(item.Key);
        bool removed;
        lock (_locks[index])
        {
            removed = _segments[index].TryRemovePair(item.Key, item.Value, ref evicted);
        }

        OnEvicted(evicted);
        return removed;
    }

    /// <summary>
    /// Copies the dictionary's live entries into a point-in-time snapshot written to the specified array, starting at
    /// the specified index.
    /// </summary>
    /// <param name="array">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is less than zero.</exception>
    /// <exception cref="ArgumentException">
    /// The number of entries in the snapshot exceeds the available space from <paramref name="arrayIndex" /> to the end
    /// of <paramref name="array" />.
    /// </exception>
    /// <remarks>
    /// This method takes an atomic snapshot of the dictionary (via <see cref="ToArray" />) before copying, so the
    /// destination reflects the state at the moment the snapshot was taken and is unaffected by concurrent
    /// modifications made afterward.
    /// </remarks>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfNegative(arrayIndex, nameof(arrayIndex));

        KeyValuePair<TKey, TValue>[] snapshot = ToArray();
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex, snapshot.Length);

        Array.Copy(snapshot, 0, array, arrayIndex, snapshot.Length);
    }

    /// <summary>
    /// Adds the specified key and value to the dictionary, throwing if a live entry for the key already exists.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value to associate with <paramref name="key" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">A live entry for <paramref name="key" /> already exists.</exception>
    /// <remarks>
    /// This is the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}" /> add-or-throw contract, and it is
    /// deliberately distinct from the public add-or-replace <see cref="Add(TKey, TValue)" />: it delegates to
    /// <see cref="TryAdd(TKey, TValue)" /> and raises <see cref="ArgumentException" /> when the add is rejected. The
    /// operation is atomic and locks only the segment that owns <paramref name="key" />.
    /// </remarks>
    void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
    {
        if (!TryAdd(key, value))
            throw new ArgumentException(ConcurrentCollectionsResourceStrings.Arg_Invalid_DuplicateDictionaryKey, nameof(key));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Follows the add-or-throw contract of
    /// <see cref="System.Collections.Generic.IDictionary{TKey, TValue}.Add(TKey, TValue)" />: a live entry for
    /// <c>item.Key</c> causes an <see cref="ArgumentException" />.
    /// </remarks>
    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) =>
        ((IDictionary<TKey, TValue>)this).Add(item.Key, item.Value);
}
