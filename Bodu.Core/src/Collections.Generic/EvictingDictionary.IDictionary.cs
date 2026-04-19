// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="EvictingDictionary.IDictionary.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------- //

using System;
using System.Collections;
using System.Collections.Generic;

namespace Bodu.Collections.Generic;

public partial class EvictingDictionary<TKey, TValue> :
    System.Collections.Generic.IDictionary<TKey, TValue>,
    System.Collections.IDictionary
{
    /// <inheritdoc />
    public int Count => _store.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    /// <remarks>
    /// Returns a live, order-preserving view of the dictionary's keys. The collection is cached per instance, so repeated reads of
    /// <see cref="Keys"/> do not allocate; enumeration is in the order defined by the current <see cref="EvictingDictionaryPolicy"/>.
    /// </remarks>
    public ICollection<TKey> Keys => _keys ??= new KeyCollection(this);

    /// <inheritdoc />
    /// <remarks>
    /// Returns a live, order-preserving view of the dictionary's values. The collection is cached per instance, so repeated reads of
    /// <see cref="Values"/> do not allocate; enumeration is in the order defined by the current <see cref="EvictingDictionaryPolicy"/>.
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
        get
        {
            if (TryGetValue(key, out TValue? value))
                return value;

            throw new KeyNotFoundException();
        }

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
    /// Adds the specified key and value to the dictionary, or replaces the value if the key already exists. If the dictionary has
    /// reached its capacity and the key is new, an existing entry will be evicted according to the configured
    /// <see cref="EvictingDictionaryPolicy"/>.
    /// </summary>
    /// <param name="key">The key of the element to add or update.</param>
    /// <param name="value">The value to associate with <paramref name="key"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if invoked from within an <see cref="ItemEvicting"/> or <see cref="ItemEvicted"/> event handler.
    /// </exception>
    /// <remarks>
    /// When an entry for <paramref name="key"/> already exists its value is updated in place without disturbing eviction metadata:
    /// position in the order list, LFU frequency, and the SecondChance reference flag are all preserved. This differs from
    /// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}.Add(TKey, TValue)"/>, which throws on duplicate keys.
    /// </remarks>
    public void Add(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowIfEvicting();

        if (_store.TryGetValue(key, out CacheItem? existing))
        {
            existing.Value = value;
            TouchInternal(key, existing);
            _version++;
            return;
        }

        if (_store.Count >= _capacity)
            EvictOne();

        var item = new CacheItem(value);

        if (_evictingPolicy is
            EvictingDictionaryPolicy.FirstInFirstOut or
            EvictingDictionaryPolicy.LeastRecentlyUsed or
            EvictingDictionaryPolicy.MostRecentlyUsed or
            EvictingDictionaryPolicy.SecondChance)
        {
            item.Node = _order.AddLast(key);
        }

        if (_evictingPolicy == EvictingDictionaryPolicy.LeastFrequentlyUsed)
            AddToFrequencyList(item.Frequency, key);

        _store[key] = item;
        _version++;
    }

    /// <summary>
    /// Adds the specified key/value pair to the dictionary. If the dictionary has reached its capacity, an existing entry will be evicted
    /// according to the configured <see cref="EvictingDictionaryPolicy"/>.
    /// </summary>
    /// <param name="item">The key/value pair to add to the dictionary.</param>
    /// <exception cref="ArgumentNullException"><c>item.Key</c> is <see langword="null"/>.</exception>
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <summary>
    /// Removes all entries from the dictionary and resets internal tracking counters.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if invoked from within an <see cref="ItemEvicting"/> or <see cref="ItemEvicted"/> event handler.
    /// </exception>
    /// <remarks>
    /// Clears the dictionary and resets all internal eviction metadata, including access order (for LeastRecentlyUsed and MostRecentlyUsed),
    /// frequency tracking (for LeastFrequentlyUsed), and counters such as <see cref="EvictingDictionary{TKey, TValue}.TotalTouches"/> and
    /// <see cref="EvictingDictionary{TKey, TValue}.EvictionCount"/>.
    /// </remarks>
    public void Clear()
    {
        ThrowIfEvicting();

        _store.Clear();
        _order?.Clear();
        _frequencyList?.Clear();
        _totalTouches = _evictionCount = 0;
        _version++;
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item) =>
        TryGetValue(item.Key, out TValue? val) && EqualityComparer<TValue>.Default.Equals(val, item.Value);

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => _store.ContainsKey(key);

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfLessThan(arrayIndex, 0);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex+ Count);

        foreach (KeyValuePair<TKey, TValue> kvp in GetOrderedItems())
            array[arrayIndex++] = kvp;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => GetOrderedItems().GetEnumerator();

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown if invoked from within an <see cref="ItemEvicting"/> or <see cref="ItemEvicted"/> event handler.
    /// </exception>
    public bool Remove(TKey key)
    {
        ThrowIfEvicting();

        if (_store.TryGetValue(key, out CacheItem? item))
        {
            if (_order is not null && item.Node is not null)
                _order.Remove(item.Node);

            if (_evictingPolicy == EvictingDictionaryPolicy.LeastFrequentlyUsed)
                RemoveFromFrequencyList(item.Frequency, key);

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
    /// When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value
    /// for the type of the value parameter.
    /// </param>
    /// <returns><see langword="true"/> if the dictionary contains an element with the specified key; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// A successful read counts as an access for the configured <see cref="EvictingDictionaryPolicy"/>: recency-tracked policies
    /// ( <see cref="EvictingDictionaryPolicy.LeastRecentlyUsed"/> and <see cref="EvictingDictionaryPolicy.MostRecentlyUsed"/>) reposition the key,
    /// <see cref="EvictingDictionaryPolicy.LeastFrequentlyUsed"/> increments its frequency, and <see cref="EvictingDictionaryPolicy.SecondChance"/>
    /// sets its reference flag. <see cref="EvictingDictionaryPolicy.FirstInFirstOut"/> and <see cref="EvictingDictionaryPolicy.RandomReplacement"/>
    /// are unaffected by reads.
    /// </para>
    /// <para>
    /// <see cref="EvictingDictionary{TKey, TValue}.TotalTouches"/> is incremented on every successful lookup regardless of policy.
    /// </para>
    /// </remarks>
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_store.TryGetValue(key, out CacheItem? item))
        {
            value = item.Value;
            TouchInternal(key, item);
            _totalTouches++;
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
