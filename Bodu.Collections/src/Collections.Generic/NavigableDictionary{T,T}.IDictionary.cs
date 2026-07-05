// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionary{T,T}.IDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Collections.Generic;

public sealed partial class NavigableDictionary<TKey, TValue>
{
    /// <summary>
    /// Gets a value indicating whether the dictionary is read-only.
    /// </summary>
    /// <value>Always <see langword="false" />.</value>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets a live view of the dictionary's keys in ascending comparer order.
    /// </summary>
    /// <value>A read-only collection over the current keys, smallest first.</value>
    /// <remarks>
    /// The view is live and cached per instance, so repeated reads of <see cref="Keys" /> do not allocate; enumeration
    /// follows ascending key order and is fail-fast against structural mutation.
    /// </remarks>
    public ICollection<TKey> Keys => _keys ??= new KeyCollection(this);

    /// <summary>
    /// Gets a live view of the dictionary's values in ascending key order.
    /// </summary>
    /// <value>A read-only collection over the current values, ordered by their keys.</value>
    /// <remarks>
    /// The view is live and cached per instance, so repeated reads of <see cref="Values" /> do not allocate;
    /// enumeration follows ascending key order and is fail-fast against structural mutation.
    /// </remarks>
    public ICollection<TValue> Values => _values ??= new ValueCollection(this);

    /// <inheritdoc />
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get or set. Must not be <see langword="null" />.</param>
    /// <value>The value associated with <paramref name="key" />.</value>
    /// <remarks>
    /// Assigning through the indexer upserts: a new key is inserted at its comparer position, and an existing key has
    /// its value overwritten in place. Overwriting a value is not a structural mutation and does not invalidate
    /// in-flight enumerators.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">
    /// The property is read and the dictionary does not contain <paramref name="key" />.
    /// </exception>
    public TValue this[TKey key]
    {
        get
        {
            ThrowHelper.ThrowIfNull(key);

            Node? node = FindNode(key);
            return node != null
                ? node.Value
                : throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.KeyNotFound_Dictionary, key));
        }

        set
        {
            ThrowHelper.ThrowIfNull(key);

            Node? node = FindNode(key);
            if (node != null)
            {
                node.Value = value;
                return;
            }

            TryInsert(key, value);
        }
    }

    /// <summary>
    /// Adds <paramref name="item" /> via the <see cref="ICollection{T}.Add(T)" /> contract.
    /// </summary>
    /// <param name="item">The key/value pair to add.</param>
    /// <exception cref="ArgumentNullException"><c>item.Key</c> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The dictionary already contains a comparer-equal key.</exception>
    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) =>
        Add(item.Key, item.Value);

    /// <summary>
    /// Determines whether the dictionary contains an entry matching both the key and the value of
    /// <paramref name="item" />.
    /// </summary>
    /// <param name="item">The key/value pair to locate.</param>
    /// <returns>
    /// <see langword="true" /> if an entry with a comparer-equal key exists whose value equals <c>item.Value</c> under
    /// <see cref="EqualityComparer{T}.Default" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><c>item.Key</c> is <see langword="null" />.</exception>
    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) =>
        TryGetValue(item.Key, out TValue value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);

    /// <summary>
    /// Removes the entry matching both the key and the value of <paramref name="item" />.
    /// </summary>
    /// <param name="item">The key/value pair to remove.</param>
    /// <returns>
    /// <see langword="true" /> if a matching entry was found and removed; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><c>item.Key</c> is <see langword="null" />.</exception>
    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) =>
        TryGetValue(item.Key, out TValue value)
            && EqualityComparer<TValue>.Default.Equals(value, item.Value)
            && Remove(item.Key);

    /// <summary>
    /// Copies the entries to the specified array in ascending key order, starting at <paramref name="arrayIndex" />.
    /// </summary>
    /// <param name="array">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="arrayIndex">The destination start index.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="array" /> does not have enough space starting at <paramref name="arrayIndex" />.
    /// </exception>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfLessThan(arrayIndex, 0);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex, _count);

        int index = arrayIndex;
        foreach (KeyValuePair<TKey, TValue> entry in this)
            array[index++] = entry;
    }
}
