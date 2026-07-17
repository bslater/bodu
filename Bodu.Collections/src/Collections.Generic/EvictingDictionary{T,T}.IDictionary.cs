// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionary{T,T}.IDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class EvictingDictionary<TKey, TValue> :
    System.Collections.Generic.IDictionary<TKey, TValue>,
    System.Collections.IDictionary
{
    /// <summary>
    /// Gets the number of key/value pairs physically stored in the dictionary.
    /// </summary>
    /// <value>The raw stored count, including expired entries that have not yet been purged.</value>
    /// <remarks>
    /// <para>
    /// When time-based expiration is configured, this property deliberately reports the raw stored count — <em>including</em>
    /// expired-but-unpurged entries — so it remains an O(1) read that never touches the clock. Expired entries are
    /// invisible to <see cref="ContainsKey" />, <see cref="TryGetValue" />, the indexer getter, and enumeration, so
    /// <see cref="Count" /> may exceed the number of entries those members observe.
    /// </para>
    /// <para>
    /// Call <see cref="EvictingDictionary{TKey, TValue}.RemoveExpired" /> to purge expired entries and reconcile
    /// <see cref="Count" /> with the live set. Without an expiration configuration the raw count and the live count are
    /// always identical.
    /// </para>
    /// </remarks>
    public int Count => _store.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    /// <remarks>
    /// Returns a live, order-preserving view of the dictionary's keys. The collection is cached per instance, so
    /// repeated reads of <see cref="Keys" /> do not allocate; enumeration is in the order defined by the current
    /// <see cref="EvictingDictionaryPolicy" />.
    /// </remarks>
    public ICollection<TKey> Keys => _keys ??= new KeyCollection(this);

    /// <inheritdoc />
    /// <remarks>
    /// Returns a live, order-preserving view of the dictionary's values. The collection is cached per instance, so
    /// repeated reads of <see cref="Values" /> do not allocate; enumeration is in the order defined by the current
    /// <see cref="EvictingDictionaryPolicy" />.
    /// </remarks>
    public ICollection<TValue> Values => _values ??= new ValueCollection(this);

    /// <inheritdoc />
    bool IDictionary.IsFixedSize => false;

    /// <inheritdoc />
    bool IDictionary.IsReadOnly => false;

    /// <inheritdoc />
    ICollection IDictionary.Keys => (ICollection)Keys;

    /// <inheritdoc />
    ICollection IDictionary.Values => (ICollection)Values;

    /// <inheritdoc cref="System.Collections.Generic.IDictionary{TKey, TValue}.this"/>
    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? value) ? value : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    /// <inheritdoc />
    object? IDictionary.this[object key]
    {
        get => key is TKey typedKey && TryGetValue(typedKey, out TValue? value) ? value : null;

        set
        {
            ThrowHelper.ThrowIfNotOfType<TKey>(key);
            ThrowHelper.ThrowIfNotOfType<TValue>(value);
            this[(TKey)key] = (TValue)value!;
        }
    }

    /// <summary>
    /// Adds the specified key and value to the dictionary, or replaces the value if the key already exists. If the
    /// dictionary has reached its capacity and the key is new, an existing entry will be evicted according to the
    /// configured <see cref="EvictingDictionaryPolicy" />.
    /// </summary>
    /// <param name="key">The key of the element to add or update.</param>
    /// <param name="value">The value to associate with <paramref name="key" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if invoked from within an <see cref="ItemEvicting" /> or <see cref="ItemEvicted" /> event handler.
    /// </exception>
    /// <remarks>
    /// <para>
    /// When an entry for <paramref name="key" /> already exists its value is updated in place and the write counts as
    /// a touch against the eviction policy: recency-based policies move the entry to the most-recently-used position,
    /// LeastFrequentlyUsed increments its accumulated frequency, and SecondChance marks it recently referenced. The
    /// entry keeps its accumulated metadata rather than being treated as newly inserted. This differs from
    /// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}.Add(TKey, TValue)" />, which throws on duplicate
    /// keys.
    /// </para>
    /// <para>
    /// When time-based expiration is configured, each write is a fresh lease: the entry's lifetime restarts using the
    /// dictionary default <see cref="EvictingDictionaryExpiration.TimeToLive" /> (any per-entry override from a
    /// previous <see cref="EvictingDictionary{TKey, TValue}.Add(TKey, TValue, TimeSpan)" /> is discarded). If the
    /// existing entry has expired it is lazily removed first and the new entry is added with fresh eviction metadata.
    /// On capacity pressure, expired entries are purged before the policy selects a victim.
    /// </para>
    /// </remarks>
    public void Add(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowIfEvicting();

        AddCore(key, value, null);
    }

    /// <summary>
    /// Implements the shared add-or-replace path for <see cref="Add(TKey, TValue)" /> and the time-to-live overloads.
    /// </summary>
    /// <param name="key">The key of the element to add or update.</param>
    /// <param name="value">The value to associate with <paramref name="key" />.</param>
    /// <param name="ttlOverride">
    /// The per-entry time-to-live, or <see langword="null" /> to apply the dictionary default.
    /// </param>
    private void AddCore(TKey key, TValue value, TimeSpan? ttlOverride)
    {
        if (_store.TryGetValue(key, out CacheItem? existing))
        {
            if (_timeProvider is not null && existing.ExpiresAtTicks <= GetNowTicks())
            {
                // The existing entry has expired: remove it as an expiry eviction and fall through to a fresh add so
                // the new entry starts with fresh eviction metadata.
                RemoveExpiredEntry(key, existing);
            }
            else
            {
                existing.Value = value;
                SetExpiration(existing, ttlOverride);
                TouchInternal(key, existing);
                _version++;
                return;
            }
        }

        if (_store.Count >= Capacity)
        {
            // Expired entries are preferred victims: purge them before consulting the capacity policy.
            PurgeExpired();

            if (_store.Count >= Capacity)
                EvictOne();
        }

        var item = new CacheItem(value);
        SetExpiration(item, ttlOverride);

        if (Policy is
            EvictingDictionaryPolicy.FirstInFirstOut or
            EvictingDictionaryPolicy.LeastRecentlyUsed or
            EvictingDictionaryPolicy.MostRecentlyUsed or
            EvictingDictionaryPolicy.SecondChance)
        {
            item.Node = _order.AddLast(key);
        }

        if (Policy == EvictingDictionaryPolicy.LeastFrequentlyUsed)
            item.Node = AddToFrequencyList(item.Frequency, key);

        _store[key] = item;
        _version++;
    }

    /// <summary>
    /// Adds the specified key/value pair to the dictionary. If the dictionary has reached its capacity, an existing
    /// entry will be evicted according to the configured <see cref="EvictingDictionaryPolicy" />.
    /// </summary>
    /// <param name="item">The key/value pair to add to the dictionary.</param>
    /// <exception cref="ArgumentNullException"><c>item.Key</c> is <see langword="null" />.</exception>
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <summary>
    /// Removes all entries from the dictionary and resets internal tracking counters.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if invoked from within an <see cref="ItemEvicting" /> or <see cref="ItemEvicted" /> event handler.
    /// </exception>
    /// <remarks>
    /// Clears the dictionary and resets all internal eviction metadata, including access order (for LeastRecentlyUsed
    /// and MostRecentlyUsed), frequency tracking (for LeastFrequentlyUsed), and counters such as
    /// <see cref="EvictingDictionary{TKey, TValue}.TotalTouches" /> and
    /// <see cref="EvictingDictionary{TKey, TValue}.EvictionCount" />.
    /// </remarks>
    public void Clear()
    {
        ThrowIfEvicting();

        _store.Clear();
        _order?.Clear();
        _frequencyList?.Clear();
        TotalTouches = EvictionCount = 0;
        _version++;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Like <see cref="ContainsKey" />, this is a pure read with respect to the capacity policy: it does not update
    /// recency or frequency metadata, slide expiration, or count as a touch. (Reads that should influence eviction
    /// order go through <see cref="TryGetValue" /> or the indexer.)
    /// </remarks>
    public bool Contains(KeyValuePair<TKey, TValue> item) =>
        TryGetLiveItem(item.Key, slide: false, out CacheItem? cached) && EqualityComparer<TValue>.Default.Equals(cached.Value, item.Value);

    /// <inheritdoc />
    /// <remarks>
    /// This is a pure read with respect to the capacity policy — it does not update recency or frequency metadata. When
    /// time-based expiration is configured, an expired entry counts as absent (it is lazily removed and
    /// <see langword="false" /> is returned), and a hit refreshes the deadline under
    /// <see cref="EvictingDictionaryExpirationKind.Sliding" />.
    /// </remarks>
    public bool ContainsKey(TKey key) => TryGetLiveItem(key, slide: true, out _);

    /// <inheritdoc />
    /// <remarks>
    /// When time-based expiration is configured, expired entries are purged (raising the eviction events) before the
    /// copy, so <see cref="Count" /> read <em>after</em> the call matches the number of elements written. Callers that
    /// size a destination from <see cref="Count" /> before invoking this method (as LINQ's <c>ToArray</c> does) should
    /// call <see cref="EvictingDictionary{TKey, TValue}.RemoveExpired" /> first to avoid trailing default slots.
    /// </remarks>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        // Argument-shape validation precedes the purge so a caller error cannot trigger evictions or raise the
        // eviction events; the purge then runs before the length check so the count validated matches the elements written.
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfLessThan(arrayIndex, 0);

        PurgeExpired();

        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex + Count);

        foreach (KeyValuePair<TKey, TValue> kvp in GetOrderedItems())
            array[arrayIndex++] = kvp;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => GetOrderedItems().GetEnumerator();

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown if invoked from within an <see cref="ItemEvicting" /> or <see cref="ItemEvicted" /> event handler.
    /// </exception>
    /// <remarks>
    /// <see cref="Remove(TKey)" /> operates on physically stored entries: when time-based expiration is configured it
    /// also removes an expired-but-unpurged entry and returns <see langword="true" />, without raising the eviction
    /// events (consistent with <see cref="Count" /> reporting the raw stored count).
    /// </remarks>
    public bool Remove(TKey key)
    {
        ThrowIfEvicting();

        if (_store.TryGetValue(key, out CacheItem? item))
        {
            if (_order is not null && item.Node is not null)
                _order.Remove(item.Node);

            if (Policy == EvictingDictionaryPolicy.LeastFrequentlyUsed)
                RemoveFromFrequencyList(item);

            _store.Remove(key);
            _version++;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item) => Contains(item) && Remove(item.Key);

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with the specified key, if the key is found; otherwise,
    /// the default value for the type of the value parameter.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the dictionary contains an element with the specified key; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A successful read counts as an access for the configured <see cref="EvictingDictionaryPolicy" />:
    /// recency-tracked policies ( <see cref="EvictingDictionaryPolicy.LeastRecentlyUsed" /> and
    /// <see cref="EvictingDictionaryPolicy.MostRecentlyUsed" />) reposition the key,
    /// <see cref="EvictingDictionaryPolicy.LeastFrequentlyUsed" /> increments its frequency, and
    /// <see cref="EvictingDictionaryPolicy.SecondChance" /> sets its reference flag.
    /// <see cref="EvictingDictionaryPolicy.FirstInFirstOut" /> and
    /// <see cref="EvictingDictionaryPolicy.RandomReplacement" /> are unaffected by reads.
    /// </para>
    /// <para>
    /// <see cref="EvictingDictionary{TKey, TValue}.TotalTouches" /> is incremented on every successful lookup
    /// regardless of policy.
    /// </para>
    /// <para>
    /// When time-based expiration is configured, an expired entry counts as absent: it is lazily removed (raising the
    /// eviction events) and <see langword="false" /> is returned. A hit refreshes the entry's deadline under
    /// <see cref="EvictingDictionaryExpirationKind.Sliding" />.
    /// </para>
    /// </remarks>
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (TryGetLiveItem(key, slide: true, out CacheItem? item))
        {
            value = item.Value;
            TouchInternal(key, item);
            TotalTouches++;
            return true;
        }

        value = default!;
        return false;
    }

    /// <inheritdoc />
    void IDictionary.Add(object key, object? value)
    {
        ThrowHelper.ThrowIfNotOfType<TKey>(key);
        ThrowHelper.ThrowIfNotOfType<TValue>(value);

        Add((TKey)key, (TValue)value!);
    }

    /// <inheritdoc />
    bool IDictionary.Contains(object key) => key is TKey typedKey && ContainsKey(typedKey);

    /// <inheritdoc />
    IDictionaryEnumerator IDictionary.GetEnumerator() => new DictionaryEnumerator(this);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetOrderedItems().GetEnumerator();

    /// <inheritdoc />
    void IDictionary.Remove(object key)
    {
        ThrowHelper.ThrowIfNotOfType<TKey>(key);

        Remove((TKey)key);
    }
}
