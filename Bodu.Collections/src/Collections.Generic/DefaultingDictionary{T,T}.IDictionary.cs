// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultingDictionary{T,T}.IDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public sealed partial class DefaultingDictionary<TKey, TValue> :
    System.Collections.Generic.IDictionary<TKey, TValue>,
    System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>
{
    /// <inheritdoc />
    /// <remarks>
    /// Counts only actually-stored entries; reading the count never materializes defaults.
    /// </remarks>
    public int Count => _inner.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    /// <remarks>
    /// Returns the inner dictionary's live key collection over the actually-stored entries. The collection is read-only
    /// through this surface.
    /// </remarks>
    public ICollection<TKey> Keys => _inner.Keys;

    /// <inheritdoc />
    /// <remarks>
    /// Returns the inner dictionary's live value collection over the actually-stored entries. The collection is
    /// read-only through this surface.
    /// </remarks>
    public ICollection<TValue> Values => _inner.Values;

    /// <inheritdoc />
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    /// <inheritdoc cref="System.Collections.Generic.IDictionary{TKey, TValue}.this" />
    /// <remarks>
    /// <para>
    /// The getter never throws <see cref="KeyNotFoundException" />: when the key is absent it invokes
    /// <see cref="ValueFactory" />, stores the produced value, and returns it. When the key is present the stored value
    /// is returned and the factory is not invoked. Only this getter materializes defaults — every other read surface
    /// observes stored entries only.
    /// </para>
    /// <para>
    /// If the factory mutates the dictionary — including assigning the very key being materialized — the factory's
    /// return value is stored last and wins; see the reentrancy contract in the type-level remarks.
    /// </para>
    /// </remarks>
    public TValue this[TKey key]
    {
        get
        {
            if (_inner.TryGetValue(key, out TValue? value))
                return value;

            // Store after invoking so a reentrant factory's own write for this key is deterministically overwritten.
            TValue created = _valueFactory(key);
            _inner[key] = created;
            return created;
        }

        set => _inner[key] = value;
    }

    /// <summary>
    /// Adds the specified key and value to the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to add. Must not be <see langword="null" />.</param>
    /// <param name="value">The value to associate with <paramref name="key" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">An element with the same key already exists in the dictionary.</exception>
    public void Add(TKey key, TValue value) => _inner.Add(key, value);

    /// <summary>
    /// Adds the specified key/value pair to the dictionary.
    /// </summary>
    /// <param name="item">The key/value pair to add to the dictionary.</param>
    /// <exception cref="ArgumentNullException"><c>item.Key</c> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">An element with the same key already exists in the dictionary.</exception>
    public void Add(KeyValuePair<TKey, TValue> item) => _inner.Add(item.Key, item.Value);

    /// <summary>
    /// Removes all entries from the dictionary.
    /// </summary>
    public void Clear() => _inner.Clear();

    /// <inheritdoc />
    /// <remarks>
    /// Matches only actually-stored pairs; the lookup never materializes defaults. Value equality is determined by
    /// <see cref="System.Collections.Generic.EqualityComparer{T}.Default" />.
    /// </remarks>
    public bool Contains(KeyValuePair<TKey, TValue> item) =>
        ((ICollection<KeyValuePair<TKey, TValue>>)_inner).Contains(item);

    /// <inheritdoc />
    /// <remarks>
    /// Returns <see langword="true" /> only for actually-stored keys; the lookup never materializes defaults.
    /// </remarks>
    public bool ContainsKey(TKey key) => _inner.ContainsKey(key);

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfLessThan(arrayIndex, 0);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex + Count);

        foreach (KeyValuePair<TKey, TValue> kvp in _inner)
            array[arrayIndex++] = kvp;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Enumerates only actually-stored entries in the inner dictionary's unspecified, insertion-biased order;
    /// enumeration never materializes defaults. Enumerator invalidation matches
    /// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" />.
    /// </remarks>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _inner.GetEnumerator();

    /// <summary>
    /// Removes the entry with the specified key from the dictionary.
    /// </summary>
    /// <param name="key">The key of the entry to remove. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if an actually-stored entry was found and removed; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// Removal never materializes defaults: removing an absent key simply returns <see langword="false" />, and a
    /// subsequent indexer read of that key invokes the factory again.
    /// </remarks>
    public bool Remove(TKey key) => _inner.Remove(key);

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item) =>
        ((ICollection<KeyValuePair<TKey, TValue>>)_inner).Remove(item);

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key without materializing a default.
    /// </summary>
    /// <param name="key">The key of the value to retrieve. Must not be <see langword="null" />.</param>
    /// <param name="value">
    /// When this method returns, contains the stored value associated with the specified key, if the key is found;
    /// otherwise, the default value for the type of the value parameter.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the dictionary contains an actually-stored entry with the specified key; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// Unlike the indexer getter, this method never invokes <see cref="ValueFactory" /> — a missing key reports
    /// <see langword="false" /> and stores nothing, matching Python's <c>defaultdict.get</c>.
    /// </remarks>
    public bool TryGetValue(TKey key, out TValue value) => _inner.TryGetValue(key, out value!);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
