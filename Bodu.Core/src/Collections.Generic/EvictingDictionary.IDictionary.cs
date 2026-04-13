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
    public int Count => this._store.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public ICollection<TKey> Keys => this.GetOrderedItems().Select(kvp => kvp.Key).ToList();

    /// <inheritdoc />
    public ICollection<TValue> Values => this.GetOrderedItems().Select(item => item.Value).ToList();

    /// <inheritdoc />
    bool IDictionary.IsFixedSize => false;

    /// <inheritdoc />
    bool IDictionary.IsReadOnly => false;

    /// <inheritdoc />
    ICollection IDictionary.Keys => (ICollection)this.Keys;

    /// <inheritdoc />
    ICollection IDictionary.Values => (ICollection)this.Values;

    /// <inheritdoc cref="System.Collections.Generic.IDictionary{TKey, TValue}.this" />
    public TValue this[TKey key]
    {
        get
        {
            if (this.TryGetValue(key, out TValue? value))
                return value;

            throw new KeyNotFoundException();
        }

        set => this.Add(key, value);
    }

    /// <inheritdoc />
    object? IDictionary.this[object key]
    {
        get => key is TKey typedKey && this.TryGetValue(typedKey, out TValue? value) ? value : null;

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
        if (this._store.ContainsKey(key))
            this.Remove(key);

        if (this._store.Count >= this._capacity)
            this.EvictOne();

        CacheItem item = new CacheItem(value);

        if (this._evictingPolicy is EvictingDictionaryPolicy.FirstInFirstOut
            || this._evictingPolicy is EvictingDictionaryPolicy.LeastRecentlyUsed
            || this._evictingPolicy is EvictingDictionaryPolicy.MostRecentlyUsed
            || this._evictingPolicy is EvictingDictionaryPolicy.SecondChance)
            item.Node = this._order.AddLast(key);

        if (this._evictingPolicy == EvictingDictionaryPolicy.LeastFrequentlyUsed)
            this.AddToFrequencyList(item.Frequency, key);

        this._store[key] = item;
    }

    /// <summary>
    /// Adds the specified key/value pair to the dictionary. If the dictionary has reached its capacity, an existing entry will be evicted
    /// according to the configured <see cref="EvictingDictionaryPolicy" />.
    /// </summary>
    /// <param name="item">The key/value pair to add to the dictionary.</param>
    /// <exception cref="ArgumentNullException"><c>item.Key</c> is <see langword="null" />.</exception>
    public void Add(KeyValuePair<TKey, TValue> item) => this.Add(item.Key, item.Value);

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
        this._store.Clear();
        this._order?.Clear();
        this._frequencyList?.Clear();
        this._totalTouches = this._evictionCount = 0;
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item) =>
        this.TryGetValue(item.Key, out TValue? val) && EqualityComparer<TValue>.Default.Equals(val, item.Value);

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => this._store.ContainsKey(key);

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayIsNotSingleDimension(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfLessThan(arrayIndex, 0);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex, this.Count);

        foreach (KeyValuePair<TKey, TValue> kvp in this.GetOrderedItems())
            array[arrayIndex++] = kvp;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => this.GetOrderedItems().GetEnumerator();

    /// <inheritdoc />
    public bool Remove(TKey key)
    {
        if (this._store.TryGetValue(key, out CacheItem? item))
        {
            // Use the stored node reference for O(1) removal from the order list for all
            // order-tracked policies (FIFO, LRU, MRU, SecondChance).
            if (this._order is not null && item.Node is not null)
                this._order.Remove(item.Node);

            if (this._evictingPolicy == EvictingDictionaryPolicy.LeastFrequentlyUsed)
                this.RemoveFromFrequencyList(item.Frequency, key);

            this._store.Remove(key);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item) => this.Contains(item) && this.Remove(item.Key);

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
        if (this._store.TryGetValue(key, out CacheItem? item))
        {
            value = item.Value;

            if (this._evictingPolicy is EvictingDictionaryPolicy.LeastRecentlyUsed
                or EvictingDictionaryPolicy.LeastFrequentlyUsed)
                this.TouchInternal(key, item);

            this._totalTouches++;
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

        this.Add((TKey)key, (TValue)value!);
    }

    /// <inheritdoc />
    bool IDictionary.Contains(object key) => key is TKey typedKey && this.ContainsKey(typedKey);

    /// <inheritdoc />
    IDictionaryEnumerator IDictionary.GetEnumerator() => new DictionaryEnumerator(this);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => this.GetOrderedItems().GetEnumerator();

    /// <inheritdoc />
    void IDictionary.Remove(object key)
    {
        ThrowHelper.ThrowIfNotOfType<TKey>(key);

        this.Remove((TKey)key);
    }
}
