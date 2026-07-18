// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionary{T,T}.IDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Globalization;

namespace Bodu.Collections.Generic;

public sealed partial class LayeredDictionary<TKey, TValue> :
    System.Collections.Generic.IDictionary<TKey, TValue>,
    System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>
{
    /// <inheritdoc />
    /// <remarks>
    /// Counts the distinct keys across all layers under <see cref="Comparer" />. This walks every layer and costs O(n)
    /// in the total number of entries across all layers on every call; it is not a cached value.
    /// </remarks>
    public int Count
    {
        get
        {
            // With a single layer there is nothing to deduplicate, so the layer's own count is authoritative
            // and the seen-key set allocation is skipped entirely.
            if (_layers.Length == 1)
                return _layers[0].Count;

            var seen = new HashSet<TKey>(_comparer);
            foreach (IDictionary<TKey, TValue> layer in _layers)
            {
                foreach (TKey key in layer.Keys)
                    seen.Add(key);
            }

            return seen.Count;
        }
    }

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    /// <remarks>
    /// Returns a read-only snapshot of the distinct keys across all layers, in first-wins enumeration order,
    /// materialized at the time of the call. The returned collection is not write-through — its mutating members throw
    /// <see cref="NotSupportedException" /> — and later mutations of the layers are not reflected in a previously
    /// returned collection.
    /// </remarks>
    public ICollection<TKey> Keys
    {
        get
        {
            var keys = new List<TKey>();
            foreach (KeyValuePair<TKey, TValue> kvp in this)
                keys.Add(kvp.Key);

            return keys.AsReadOnly();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns a read-only snapshot of the first-wins values for the distinct keys across all layers, materialized at
    /// the time of the call. The returned collection is not write-through — its mutating members throw
    /// <see cref="NotSupportedException" /> — and later mutations of the layers are not reflected in a previously
    /// returned collection.
    /// </remarks>
    public ICollection<TValue> Values
    {
        get
        {
            var values = new List<TValue>();
            foreach (KeyValuePair<TKey, TValue> kvp in this)
                values.Add(kvp.Value);

            return values.AsReadOnly();
        }
    }

    /// <inheritdoc />
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    /// <inheritdoc cref="System.Collections.Generic.IDictionary{TKey, TValue}.this" />
    /// <remarks>
    /// <para>
    /// The getter searches the layers in order and returns the value from the first layer containing the key; deeper
    /// entries with the same key are shadowed.
    /// </para>
    /// <para>
    /// The setter writes to the first layer only. Assigning a key that resolves from a deeper layer creates or updates
    /// a first-layer entry that shadows the deeper one; the deeper entry itself is never modified.
    /// </para>
    /// </remarks>
    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? value)
            ? value
            : throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.KeyNotFound_Dictionary, key));

        set
        {
            ThrowHelper.ThrowIfNull(key);

            _layers[0][key] = value;
        }
    }

    /// <summary>
    /// Adds the specified key and value to the first layer.
    /// </summary>
    /// <param name="key">The key of the element to add. Must not be <see langword="null" />.</param>
    /// <param name="value">The value to associate with <paramref name="key" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The first layer already contains an element with the same key.</exception>
    /// <remarks>
    /// Only the first layer is consulted for the duplicate check: adding a key that exists solely in deeper layers
    /// succeeds and the new first-layer entry shadows the deeper value. The operation throws only when the first layer
    /// itself already contains the key.
    /// </remarks>
    public void Add(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);
        if (_layers[0].ContainsKey(key)) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_DuplicateDictionaryKey, nameof(key));

        _layers[0].Add(key, value);
    }

    /// <summary>
    /// Adds the specified key/value pair to the first layer.
    /// </summary>
    /// <param name="item">The key/value pair to add to the first layer.</param>
    /// <exception cref="ArgumentNullException"><c>item.Key</c> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The first layer already contains an element with the same key.</exception>
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <summary>
    /// Removes all entries from the first layer.
    /// </summary>
    /// <remarks>
    /// Matching Python <c>ChainMap</c> semantics, only the first layer is cleared; deeper layers are untouched, so any
    /// keys they contain remain visible through the view — including keys the cleared entries previously shadowed.
    /// </remarks>
    public void Clear() => _layers[0].Clear();

    /// <inheritdoc />
    /// <remarks>
    /// The pair matches when the view resolves <c>item.Key</c> (first-wins) to a value equal to <c>item.Value</c> under
    /// <see cref="System.Collections.Generic.EqualityComparer{T}.Default" />. A pair whose value exists only in a
    /// shadowed deeper layer does not match.
    /// </remarks>
    public bool Contains(KeyValuePair<TKey, TValue> item) =>
        TryGetValue(item.Key, out TValue? value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);

    /// <inheritdoc />
    /// <remarks>
    /// Returns <see langword="true" /> when any layer contains the key, each layer deciding membership with its own
    /// comparer.
    /// </remarks>
    public bool ContainsKey(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        foreach (IDictionary<TKey, TValue> layer in _layers)
        {
            if (layer.ContainsKey(key))
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Copies the merged view — distinct keys with first-wins values — in enumeration order.
    /// </remarks>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfLessThan(arrayIndex, 0);

        // Single pass: write the merged view through while walking the layers, tracking seen keys once
        // rather than walking every layer twice (once for Count, once for the enumerator). The merged
        // count is only known when the walk completes, so the length check runs afterwards; when the
        // array is too short the elements that fit have already been written.
        var seen = new HashSet<TKey>(_comparer);
        int index = arrayIndex;

        foreach (IDictionary<TKey, TValue> layer in _layers)
        {
            foreach (KeyValuePair<TKey, TValue> kvp in layer)
            {
                if (seen.Add(kvp.Key) && index < array.Length)
                    array[index++] = kvp;
            }
        }

        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex, seen.Count);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Enumerates the distinct keys across all layers with first-wins values: each layer is walked in order and a key
    /// is yielded the first time it is seen (under <see cref="Comparer" />); later occurrences are skipped.
    /// </para>
    /// <para>
    /// Enumeration is lazy and reads the live layers as it walks them; mutating a layer during enumeration follows that
    /// layer's own enumerator-invalidation rules.
    /// </para>
    /// </remarks>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        var seen = new HashSet<TKey>(_comparer);
        foreach (IDictionary<TKey, TValue> layer in _layers)
        {
            foreach (KeyValuePair<TKey, TValue> kvp in layer)
            {
                if (seen.Add(kvp.Key))
                    yield return kvp;
            }
        }
    }

    /// <summary>
    /// Removes the entry with the specified key from the first layer.
    /// </summary>
    /// <param name="key">The key of the entry to remove. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the entry was removed from the first layer; <see langword="false" /> if the first
    /// layer did not contain the key — including when the key exists only in deeper layers.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// Matching Python <c>ChainMap</c> semantics, only the first layer is mutated. Removing a first-layer entry that
    /// shadowed a deeper entry makes the deeper value visible again ("unshadowing"): the key remains present in the
    /// view with the next layer's value. A key that exists only in deeper layers is not removed and the method returns
    /// <see langword="false" />.
    /// </remarks>
    public bool Remove(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        return _layers[0].Remove(key);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The pair is removed only when the view resolves it (per <see cref="Contains(KeyValuePair{TKey, TValue})" />) and
    /// the first layer contains the key; deeper layers are never mutated.
    /// </remarks>
    public bool Remove(KeyValuePair<TKey, TValue> item) => Contains(item) && Remove(item.Key);

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key, searching the layers in order.
    /// </summary>
    /// <param name="key">The key of the value to retrieve. Must not be <see langword="null" />.</param>
    /// <param name="value">
    /// When this method returns, contains the value from the first layer containing the key, if any layer contains it;
    /// otherwise, the default value for the type of the value parameter.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if any layer contains an element with the specified key; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetValue(TKey key, out TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        foreach (IDictionary<TKey, TValue> layer in _layers)
        {
            if (layer.TryGetValue(key, out value!))
                return true;
        }

        value = default!;
        return false;
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
