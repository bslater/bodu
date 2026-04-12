// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="EvictingDictionary.IDictionary.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------- //

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    public ICollection<TKey> Keys => GetOrderedItems().Select(kvp => kvp.Key).ToList();

    /// <inheritdoc />
    public ICollection<TValue> Values => GetOrderedItems().Select(item => item.Value).ToList();

    /// <inheritdoc />
    bool IDictionary.IsFixedSize => false;

    /// <inheritdoc />
    bool IDictionary.IsReadOnly => false;

    /// <inheritdoc />
    ICollection IDictionary.Keys => (ICollection)Keys;

    /// <inheritdoc />
    ICollection IDictionary.Values => (ICollection)Values;

    /// <inheritdoc cref="System.Collections.Generic.IDictionary{TKey, TValue}.this" />
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
    /// Adds the specified key and value to the dictionary. If the dictionary has reached its capacity, an existing entry will be evicted
    /// according to the configured <see cref="EvictingDictionaryPolicy" />.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public void Add(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        // Remove an existing entry for this key so that the replacement is tracked correctly
        // by the eviction policy (position and frequency are reset on re-insertion).
        if (_store.ContainsKey(key))
            Remove(key);

        if (_store.Count >= _capacity)
            EvictOne();

        CacheItem item = new CacheItem(value);

        if (_evictingPolicy is EvictingDictionaryPolicy.FirstInFirstOut
            || _evictingPolicy is EvictingDictionaryPolicy.LeastRecentlyUsed
            || _evictingPolicy is EvictingDictionaryPolicy.MostRecentlyUsed
            || _evictingPolicy is EvictingDictionaryPolicy.SecondChance)
            item.Node = _order.AddLast(key);

        if (_evictingPolicy == EvictingDictionaryPolicy.LeastFrequentlyUsed)
            AddToFrequencyList(item.Frequency, key);

        _store[key] = item;
    }

    /// <summary>
    /// Adds the specified key/value pair to the dictionary. If the dictionary has reached its capacity, an existing entry will be evicted
    /// according to the configured <see cref="EvictingDictionaryPolicy" />.
    /// </summary>
    /// <param name="item">The key/value pair to add to the dictionary.</param>
    /// <exception cref="ArgumentNullException"><c>item.Key</c> is <see langword="null" />.</exception>
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <summary>
    /// Removes all entries from the dictionary and resets internal tracking counters.
    /// </summary>
    /// <remarks>
    /// Clears the dictionary and resets all internal eviction metadata, including access order (for LeastRecentlyUsed and MostRecentlyUsed),
    /// frequency tracking (for LeastFrequentlyUsed), and counters such as <see cref="EvictingDictionary{TKey, TValue}.TotalTouches" /> and
    /// <see cref="EvictingDictionary{TKey, TValue}.EvictionCount" />.
    /// </remarks>
    public void Clear()
    {
        _store.Clear();
        _order?.Clear();
        _frequencyList?.Clear();
        _totalTouches = _evictionCount = 0;
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
        ThrowHelper.ThrowIfArrayIsNotSingleDimension(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfLessThan(arrayIndex, 0);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex, Count);

        foreach (KeyValuePair<TKey, TValue> kvp in GetOrderedItems())
            array[arrayIndex++] = kvp;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => GetOrderedItems().GetEnumerator();

    /// <inheritdoc />
    public bool Remove(TKey key)
    {
        if (_store.TryGetValue(key, out CacheItem? item))
        {
            // Use the stored node reference for O(1) removal from the order list for all
            // order-tracked policies (FIFO, LRU, MRU, SecondChance).
            if (_order is not null && item.Node is not null)
                _order.Remove(item.Node);

            if (_evictingPolicy == EvictingDictionaryPolicy.LeastFrequentlyUsed)
                RemoveFromFrequencyList(item.Frequency, key);

            _store.Remove(key);
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
    /// <returns><see langword="true" /> if the dictionary contains an element with the specified key; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// If the eviction policy is <see cref="EvictingDictionaryPolicy.LeastRecentlyUsed" /> or
    /// <see cref="EvictingDictionaryPolicy.LeastFrequentlyUsed" />, accessing a key through this method will update its usage metadata.
    /// </remarks>
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_store.TryGetValue(key, out CacheItem? item))
        {
            value = item.Value;

            if (_evictingPolicy is EvictingDictionaryPolicy.LeastRecentlyUsed
                or EvictingDictionaryPolicy.LeastFrequentlyUsed)
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
