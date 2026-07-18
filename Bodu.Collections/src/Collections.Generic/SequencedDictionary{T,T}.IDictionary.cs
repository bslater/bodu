// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionary{T,T}.IDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Globalization;

namespace Bodu.Collections.Generic;

public partial class SequencedDictionary<TKey, TValue> :
    System.Collections.Generic.IDictionary<TKey, TValue>,
    System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>,
    System.Collections.IDictionary
{
    /// <inheritdoc />
    public int Count => _store.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    /// <remarks>
    /// Returns a live, order-preserving view of the dictionary's keys. The collection is cached per instance, so
    /// repeated reads of <see cref="Keys" /> do not allocate; enumeration follows the dictionary's iteration order.
    /// </remarks>
    public ICollection<TKey> Keys => _keys ??= new KeyCollection(this);

    /// <inheritdoc />
    /// <remarks>
    /// Returns a live, order-preserving view of the dictionary's values. The collection is cached per instance, so
    /// repeated reads of <see cref="Values" /> do not allocate; enumeration follows the dictionary's iteration order.
    /// </remarks>
    public ICollection<TValue> Values => _values ??= new ValueCollection(this);

    /// <inheritdoc />
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    /// <inheritdoc />
    bool IDictionary.IsFixedSize => false;

    /// <inheritdoc />
    bool IDictionary.IsReadOnly => false;

    /// <inheritdoc />
    ICollection IDictionary.Keys => (ICollection)Keys;

    /// <inheritdoc />
    ICollection IDictionary.Values => (ICollection)Values;

    /// <inheritdoc cref="System.Collections.Generic.IDictionary{TKey, TValue}.this" />
    /// <remarks>
    /// In access-order mode, both reading and assigning through the indexer move the affected entry to the end of the
    /// iteration order. Assigning a new key appends it to the end; assigning an existing key updates its value in
    /// place.
    /// </remarks>
    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? value)
            ? value
            : throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.KeyNotFound_Dictionary, key));

        set
        {
            ThrowHelper.ThrowIfNull(key);

            if (_store.TryGetValue(key, out Entry? existing))
            {
                existing.Value = value;

                if (_accessOrder)
                    MoveToTail(existing.Node);
                else
                    _version++;

                return;
            }

            LinkedListNode<TKey> node = _order.AddLast(key);
            _store[key] = new Entry(value, node);
            _version++;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Following the <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> contract for
    /// <see cref="IDictionary.this" />, the getter throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> key and returns <see langword="null" /> for a non-null key of an incompatible type.
    /// </remarks>
    object? IDictionary.this[object key]
    {
        get
        {
            ThrowHelper.ThrowIfNull(key);

            return key is TKey typedKey && TryGetValue(typedKey, out TValue? value) ? value : null;
        }

        set
        {
            ThrowHelper.ThrowIfNotOfType<TKey>(key);
            ThrowHelper.ThrowIfNotOfType<TValue>(value);
            this[(TKey)key] = (TValue)value!;
        }
    }

    /// <summary>
    /// Adds the specified key and value to the dictionary, appending the new entry to the end of the iteration order.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value to associate with <paramref name="key" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">An element with the same key already exists in the dictionary.</exception>
    /// <remarks>
    /// This method follows the strict
    /// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}.Add(TKey, TValue)" /> contract and throws on a
    /// duplicate key. To insert or overwrite without throwing, assign through the indexer.
    /// </remarks>
    public void Add(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);
        if (_store.ContainsKey(key)) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_DuplicateDictionaryKey, nameof(key));

        LinkedListNode<TKey> node = _order.AddLast(key);
        _store[key] = new Entry(value, node);
        _version++;
    }

    /// <summary>
    /// Adds the specified key/value pair to the dictionary, appending it to the end of the iteration order.
    /// </summary>
    /// <param name="item">The key/value pair to add to the dictionary.</param>
    /// <exception cref="ArgumentNullException"><c>item.Key</c> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">An element with the same key already exists in the dictionary.</exception>
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <summary>
    /// Removes all entries from the dictionary.
    /// </summary>
    public void Clear()
    {
        _store.Clear();
        _order.Clear();
        _version++;
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item) =>
        _store.TryGetValue(item.Key, out Entry? entry) && EqualityComparer<TValue>.Default.Equals(entry.Value, item.Value);

    /// <inheritdoc />
    /// <remarks>
    /// This method never changes the iteration order, even in access-order mode.
    /// </remarks>
    public bool ContainsKey(TKey key) => _store.ContainsKey(key);

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfLessThan(arrayIndex, 0);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex + Count);

        foreach (KeyValuePair<TKey, TValue> kvp in this)
            array[arrayIndex++] = kvp;
    }

    /// <inheritdoc />
    public bool Remove(TKey key)
    {
        if (_store.TryGetValue(key, out Entry? entry))
        {
            _order.Remove(entry.Node);
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
    /// In access-order mode a successful lookup counts as an access and moves the entry to the end of the iteration
    /// order; in insertion-order mode the lookup does not change the order.
    /// </remarks>
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_store.TryGetValue(key, out Entry? entry))
        {
            value = entry.Value;

            if (_accessOrder)
                MoveToTail(entry.Node);

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
    void IDictionary.Remove(object key)
    {
        ThrowHelper.ThrowIfNotOfType<TKey>(key);

        Remove((TKey)key);
    }
}
