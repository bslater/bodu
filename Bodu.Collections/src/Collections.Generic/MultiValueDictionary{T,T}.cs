// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionary{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a mutable dictionary that maps each key to zero or more values.
/// </summary>
/// <typeparam name="TKey">The type of keys.</typeparam>
/// <typeparam name="TValue">The type of values associated with each key.</typeparam>
/// <remarks>
/// <para>
/// <see cref="MultiValueDictionary{TKey, TValue}" /> is a mutable one-to-many map, sometimes referred to as a multimap.
/// A single key can have multiple associated values, and values for the same key are retained in insertion order.
/// </para>
/// <para>
/// The value storage is selected at construction through <see cref="MultiValueBacking" /> and reported by
/// <see cref="Backing" />. The default, <see cref="MultiValueBacking.List" />, is a list multimap: every added value is
/// retained, including per-key duplicates. <see cref="MultiValueBacking.Set" /> is an order-preserving set multimap:
/// values are deduplicated per key using <see cref="ValueComparer" />, and the insertion order of each value's first
/// occurrence is preserved. Under <see cref="MultiValueBacking.Set" /> backing, each add performs a linear scan of the
/// key's existing values — a trade-off that keeps the values ordered and preserves the <see cref="IReadOnlyList{T}" />
/// view contract.
/// </para>
/// <para>
/// The <see cref="Count" /> property returns the total number of key-value entries across all keys. Use
/// <see cref="KeyCount" /> to obtain the number of distinct keys currently held.
/// </para>
/// <para>
/// The indexer returns the values for a key when the key is present, or an empty read-only list when the key is absent.
/// Use <see cref="GetValues" /> when absence should be treated as an error, or <see cref="TryGetValues" /> when absence
/// should be handled without throwing.
/// </para>
/// <para>
/// Values returned by the indexer, <see cref="GetValues" />, <see cref="TryGetValues" />, and enumeration are live
/// read-only views. They reflect later dictionary changes for the same key, but they do not expose the mutable backing
/// <see cref="List{T}" /> used internally.
/// </para>
/// <para>
/// Enumerators are invalidated by structural modification. Adding values, removing values, removing keys, or clearing
/// the dictionary after enumeration begins causes the next enumeration step to throw
/// <see cref="InvalidOperationException" />. Operations that do not change the dictionary, such as removing a missing
/// key or adding an empty range, do not invalidate existing enumerators.
/// </para>
/// <para>
/// The dictionary's regular enumeration yields one entry per distinct key, where each entry contains the key and its
/// associated read-only value list. Use <see cref="Flatten" /> to enumerate one
/// <see cref="KeyValuePair{TKey, TValue}" /> per stored value.
/// </para>
/// <para>
/// This type is not thread-safe. Concurrent reads and writes require external synchronization.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Multiple values under a single key — values are retained in insertion order.
/// var map = new MultiValueDictionary<string, int>();
/// map.Add("odd",  1);
/// map.Add("odd",  3);
/// map.Add("even", 2);
///
/// Console.WriteLine(map.Count);    // 3 — total key-value entries
/// Console.WriteLine(map.KeyCount); // 2 — distinct keys
///
/// // The indexer returns a live read-only view; absent keys yield an empty list rather than throwing.
/// foreach (int value in map["odd"])
///     Console.WriteLine(value);
///
/// // Flatten into one KeyValuePair per stored value.
/// foreach (KeyValuePair<string, int> pair in map.Flatten())
///     Console.WriteLine($"{pair.Key}: {pair.Value}");
///]]>
/// </code>
/// </example>
[DebuggerDisplay("KeyCount = {KeyCount}, Count = {Count}")]
[DebuggerTypeProxy(typeof(MultiValueDictionaryDebugView<,>))]
[Serializable]
public sealed partial class MultiValueDictionary<TKey, TValue>
    : IReadOnlyCollection<KeyValuePair<TKey, IReadOnlyList<TValue>>>
    where TKey : notnull
{
    /// <summary>Shared empty read-only list returned when a key is absent.</summary>
    private static readonly IReadOnlyList<TValue> s_emptyValues = Array.AsReadOnly(Array.Empty<TValue>());

    /// <summary>The backing selected at construction, controlling whether per-key values are deduplicated.</summary>
    private readonly MultiValueBacking _backing;

    /// <summary>The equality comparer used to determine key equality.</summary>
    private readonly IEqualityComparer<TKey> _comparer;

    /// <summary>The backing dictionary mapping each key to its value bucket.</summary>
    private readonly Dictionary<TKey, ValueBucket> _map;

    /// <summary>The equality comparer used to determine value equality under <see cref="MultiValueBacking.Set" /> backing.</summary>
    private readonly IEqualityComparer<TValue> _valueComparer;

    /// <summary>The total number of value entries across all keys.</summary>
    private int _count;

    /// <summary>Incremented on every structural change; used by enumerators to detect concurrent modification.</summary>
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionary{TKey, TValue}" /> class that is empty and uses
    /// the default equality comparer for keys.
    /// </summary>
    public MultiValueDictionary()
        : this((IEqualityComparer<TKey>?)null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionary{TKey, TValue}" /> class that is empty and uses
    /// the specified equality comparer for keys.
    /// </summary>
    /// <param name="comparer">The equality comparer used to compare keys.</param>
    public MultiValueDictionary(IEqualityComparer<TKey>? comparer)
        : this(MultiValueBacking.List, comparer, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionary{TKey, TValue}" /> class that is empty, uses
    /// the specified value backing, and uses the default equality comparers for keys and values.
    /// </summary>
    /// <param name="backing">The backing that controls how values are stored per key.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="backing" /> is not a defined <see cref="MultiValueBacking" /> value.
    /// </exception>
    public MultiValueDictionary(MultiValueBacking backing)
        : this(backing, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionary{TKey, TValue}" /> class that is empty, uses
    /// the specified value backing and key comparer, and uses the default equality comparer for values.
    /// </summary>
    /// <param name="backing">The backing that controls how values are stored per key.</param>
    /// <param name="keyComparer">The equality comparer used to compare keys.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="backing" /> is not a defined <see cref="MultiValueBacking" /> value.
    /// </exception>
    public MultiValueDictionary(MultiValueBacking backing, IEqualityComparer<TKey>? keyComparer)
        : this(backing, keyComparer, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionary{TKey, TValue}" /> class that is empty and uses
    /// the specified value backing, key comparer, and value comparer.
    /// </summary>
    /// <param name="backing">The backing that controls how values are stored per key.</param>
    /// <param name="keyComparer">The equality comparer used to compare keys.</param>
    /// <param name="valueComparer">The equality comparer used to compare values.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="backing" /> is not a defined <see cref="MultiValueBacking" /> value.
    /// </exception>
    /// <remarks>
    /// The backing and value comparer are immutable after construction. The value comparer is consulted only under
    /// <see cref="MultiValueBacking.Set" /> backing; under <see cref="MultiValueBacking.List" /> backing it is stored
    /// and reported by <see cref="ValueComparer" /> but value operations use the default equality comparer.
    /// </remarks>
    public MultiValueDictionary(MultiValueBacking backing, IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(backing);

        _backing = backing;
        _comparer = keyComparer ?? EqualityComparer<TKey>.Default;
        _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
        _map = new Dictionary<TKey, ValueBucket>(_comparer);
    }

    /// <summary>
    /// Gets the backing that controls how values are stored per key.
    /// </summary>
    /// <value>The <see cref="MultiValueBacking" /> value selected at construction.</value>
    public MultiValueBacking Backing => _backing;

    /// <summary>
    /// Gets the equality comparer used to determine equality of keys.
    /// </summary>
    /// <value>The <see cref="IEqualityComparer{T}" /> instance used to compare keys.</value>
    public IEqualityComparer<TKey> Comparer => _comparer;

    /// <summary>
    /// Gets the equality comparer used to determine equality of values.
    /// </summary>
    /// <value>
    /// The <see cref="IEqualityComparer{T}" /> instance supplied at construction, or the default comparer.
    /// </value>
    /// <remarks>
    /// The comparer is consulted only under <see cref="MultiValueBacking.Set" /> backing, where it drives duplicate
    /// suppression in <see cref="Add" /> and <see cref="AddRange" /> and value matching in <see cref="Remove" /> and
    /// <see cref="ContainsValue" />. Under <see cref="MultiValueBacking.List" /> backing the property still reports the
    /// construction value, but value operations use the default equality comparer.
    /// </remarks>
    public IEqualityComparer<TValue> ValueComparer => _valueComparer;

    /// <summary>
    /// Gets the total number of key-value entries stored across all keys.
    /// </summary>
    /// <value>The total number of values, summed across all keys.</value>
    public int Count => _count;

    /// <summary>
    /// Gets the number of distinct keys currently held in the dictionary.
    /// </summary>
    /// <value>The number of distinct keys.</value>
    public int KeyCount => _map.Count;

    /// <summary>
    /// Gets the number of elements yielded by the dictionary's enumeration, which equals the number of distinct keys.
    /// </summary>
    /// <value>The number of distinct keys, equivalent to <see cref="KeyCount" />.</value>
    /// <remarks>
    /// The dictionary's enumeration yields one entry per key, so this matches <see cref="KeyCount" /> rather than the
    /// total value count exposed by the public <see cref="Count" /> property.
    /// </remarks>
    int IReadOnlyCollection<KeyValuePair<TKey, IReadOnlyList<TValue>>>.Count => _map.Count;

    /// <summary>
    /// Gets a read-only view of all keys in the dictionary.
    /// </summary>
    /// <value>A collection containing all distinct keys.</value>
    public IReadOnlyCollection<TKey> Keys => _map.Keys;

    /// <summary>
    /// Gets the values associated with <paramref name="key" /> as a read-only list.
    /// </summary>
    /// <param name="key">The key whose values are retrieved.</param>
    /// <returns>
    /// A live read-only list of values associated with <paramref name="key" />, or an empty list when the key is
    /// absent.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The returned list reflects later changes made through the dictionary, but it does not expose the mutable backing
    /// list.
    /// </remarks>
    public IReadOnlyList<TValue> this[TKey key]
    {
        get
        {
            ThrowHelper.ThrowIfNull(key);

            return _map.TryGetValue(key, out ValueBucket? bucket)
                ? bucket.ReadOnlyValues
                : s_emptyValues;
        }
    }

    /// <summary>
    /// Appends <paramref name="value" /> to the values associated with <paramref name="key" />.
    /// </summary>
    /// <param name="key">The key to add the value under.</param>
    /// <param name="value">The value to append.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Under <see cref="MultiValueBacking.Set" /> backing, a value already associated with <paramref name="key" />
    /// according to <see cref="ValueComparer" /> is ignored: the dictionary is left unchanged and active enumerators
    /// remain valid. Under <see cref="MultiValueBacking.List" /> backing, every value is appended, including
    /// duplicates.
    /// </remarks>
    public void Add(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!_map.TryGetValue(key, out ValueBucket? bucket))
        {
            bucket = new ValueBucket();
            _map[key] = bucket;
        }
        else if (_backing == MultiValueBacking.Set && IndexOfValue(bucket, value) >= 0)
        {
            // Duplicate under set backing: no structural change, so enumerators stay valid.
            return;
        }

        bucket.Values.Add(value);
        _count++;
        _version++;
    }

    /// <summary>
    /// Appends each element of <paramref name="values" /> to the values associated with <paramref name="key" />.
    /// </summary>
    /// <param name="key">The key to add the values under.</param>
    /// <param name="values">The values to append.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or <paramref name="values" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The operation is atomic with respect to this dictionary. If the source sequence throws while being enumerated,
    /// the dictionary is left unchanged. An empty source is treated as a no-op and does not invalidate active
    /// enumerators.
    /// </para>
    /// <para>
    /// Under <see cref="MultiValueBacking.Set" /> backing, values already associated with <paramref name="key" />
    /// according to <see cref="ValueComparer" /> are skipped, and duplicates within <paramref name="values" /> itself
    /// collapse to their first occurrence. A source whose values are all duplicates is treated as a no-op and does not
    /// invalidate active enumerators.
    /// </para>
    /// </remarks>
    public void AddRange(TKey key, IEnumerable<TValue> values)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(values);

        TValue[] items = values as TValue[] ?? [.. values];

        if (items.Length == 0)
            return;

        _map.TryGetValue(key, out ValueBucket? bucket);

        if (_backing == MultiValueBacking.Set)
        {
            items = FilterNovelValues(bucket, items);

            if (items.Length == 0)
                return;
        }

        if (bucket is null)
        {
            bucket = new ValueBucket(items.Length);
            _map[key] = bucket;
        }

        bucket.Values.AddRange(items);
        _count += items.Length;
        _version++;
    }

    /// <summary>
    /// Removes all keys and their associated values from the dictionary.
    /// </summary>
    /// <remarks>
    /// Each removed bucket's value list is cleared so that any outstanding read-only views previously handed out by
    /// <see cref="GetValues" />, <see cref="TryGetValues" />, or the indexer reflect the removal.
    /// </remarks>
    public void Clear()
    {
        if (_map.Count == 0)
            return;

        foreach (ValueBucket bucket in _map.Values)
            bucket.Values.Clear();

        _map.Clear();
        _count = 0;
        _version++;
    }

    /// <summary>
    /// Determines whether <paramref name="key" /> has at least one value.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="key" /> exists; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public bool ContainsKey(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        return _map.ContainsKey(key);
    }

    /// <summary>
    /// Determines whether <paramref name="value" /> is associated with <paramref name="key" />.
    /// </summary>
    /// <param name="key">The key to search under.</param>
    /// <param name="value">The value to locate.</param>
    /// <returns>
    /// <see langword="true" /> if the value is found under the key; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Under <see cref="MultiValueBacking.Set" /> backing, values are matched using <see cref="ValueComparer" />; under
    /// <see cref="MultiValueBacking.List" /> backing, the default equality comparer for <typeparamref name="TValue" />
    /// is used.
    /// </remarks>
    public bool ContainsValue(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!_map.TryGetValue(key, out ValueBucket? bucket))
            return false;

        return _backing == MultiValueBacking.Set
            ? IndexOfValue(bucket, value) >= 0
            : bucket.Values.Contains(value);
    }

    /// <summary>
    /// Returns the values associated with <paramref name="key" /> as a read-only list.
    /// </summary>
    /// <param name="key">The key whose values are returned.</param>
    /// <returns>A live read-only list of values in insertion order.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when <paramref name="key" /> does not exist in the dictionary.
    /// </exception>
    public IReadOnlyList<TValue> GetValues(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        return _map.TryGetValue(key, out ValueBucket? bucket)
            ? (IReadOnlyList<TValue>)bucket.ReadOnlyValues
            : throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.KeyNotFound_Dictionary, key));
    }

    /// <summary>
    /// Attempts to return the values associated with <paramref name="key" />.
    /// </summary>
    /// <param name="key">The key whose values are returned.</param>
    /// <param name="values">The values associated with <paramref name="key" /> when the key exists.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="key" /> exists; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public bool TryGetValues(TKey key, out IReadOnlyList<TValue> values)
    {
        ThrowHelper.ThrowIfNull(key);

        if (_map.TryGetValue(key, out ValueBucket? bucket))
        {
            values = bucket.ReadOnlyValues;
            return true;
        }

        values = s_emptyValues;
        return false;
    }

    /// <summary>
    /// Removes one occurrence of <paramref name="value" /> from the values associated with <paramref name="key" />.
    /// </summary>
    /// <param name="key">The key under which to remove the value.</param>
    /// <param name="value">The value to remove.</param>
    /// <returns><see langword="true" /> if a value was removed; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Under <see cref="MultiValueBacking.Set" /> backing, the value to remove is matched using
    /// <see cref="ValueComparer" />; under <see cref="MultiValueBacking.List" /> backing, the default equality comparer
    /// for <typeparamref name="TValue" /> is used.
    /// </remarks>
    public bool Remove(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!_map.TryGetValue(key, out ValueBucket? bucket))
            return false;

        if (_backing == MultiValueBacking.Set)
        {
            int index = IndexOfValue(bucket, value);

            if (index < 0)
                return false;

            bucket.Values.RemoveAt(index);
        }
        else if (!bucket.Values.Remove(value))
        {
            return false;
        }

        _count--;
        _version++;

        if (bucket.Values.Count == 0)
            _map.Remove(key);

        return true;
    }

    /// <summary>
    /// Removes <paramref name="key" /> and all of its associated values from the dictionary.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="key" /> was found and removed; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public bool RemoveAll(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!_map.TryGetValue(key, out ValueBucket? bucket))
            return false;

        _count -= bucket.Values.Count;
        bucket.Values.Clear();
        _map.Remove(key);
        _version++;

        return true;
    }

    /// <summary>
    /// Returns a flat sequence of all key-value pairs, one pair per value entry across all keys.
    /// </summary>
    /// <returns>An enumerable in which each item represents one value for one key.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the dictionary is modified after enumeration begins.
    /// </exception>
    /// <remarks>
    /// The order of keys is not guaranteed. Values within a key appear in insertion order.
    /// </remarks>
    public IEnumerable<KeyValuePair<TKey, TValue>> Flatten()
    {
        int version = _version;

        foreach (KeyValuePair<TKey, ValueBucket> entry in _map)
        {
            ThrowIfModified(version);

            foreach (TValue value in entry.Value.Values)
            {
                ThrowIfModified(version);

                yield return new KeyValuePair<TKey, TValue>(entry.Key, value);

                ThrowIfModified(version);
            }
        }
    }

    /// <summary>
    /// Returns the values from <paramref name="items" /> that are novel under <see cref="MultiValueBacking.Set" />
    /// backing, skipping values already present in <paramref name="bucket" /> and collapsing duplicates within the
    /// batch to their first occurrence.
    /// </summary>
    /// <param name="bucket">The existing bucket for the key, or <see langword="null" /> when the key is absent.</param>
    /// <param name="items">The candidate values, in source order.</param>
    /// <returns>The novel values in first-occurrence order; empty when every candidate is a duplicate.</returns>
    private TValue[] FilterNovelValues(ValueBucket? bucket, TValue[] items)
    {
        var novel = new List<TValue>(items.Length);

        foreach (TValue item in items)
        {
            if (bucket is not null && IndexOfValue(bucket, item) >= 0)
                continue;

            var seenInBatch = false;

            for (var i = 0; i < novel.Count; i++)
            {
                if (_valueComparer.Equals(novel[i], item))
                {
                    seenInBatch = true;
                    break;
                }
            }

            if (!seenInBatch)
                novel.Add(item);
        }

        return [.. novel];
    }

    /// <summary>
    /// Returns the index of the first value in <paramref name="bucket" /> equal to <paramref name="value" /> according
    /// to <see cref="ValueComparer" />, or -1 when no match exists.
    /// </summary>
    /// <param name="bucket">The bucket whose values are scanned.</param>
    /// <param name="value">The value to locate.</param>
    /// <returns>The zero-based index of the first match, or -1 when the value is absent.</returns>
    /// <remarks>
    /// The linear scan is the documented trade-off of <see cref="MultiValueBacking.Set" /> backing: it preserves
    /// insertion order and the <see cref="IReadOnlyList{T}" /> view contract at the cost of O(n) duplicate detection
    /// per operation within a key's values.
    /// </remarks>
    private int IndexOfValue(ValueBucket bucket, TValue value)
    {
        List<TValue> values = bucket.Values;

        for (var i = 0; i < values.Count; i++)
        {
            if (_valueComparer.Equals(values[i], value))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Stores the mutable value list for a key together with the cached read-only view returned to callers.
    /// </summary>
    [Serializable]
    private sealed class ValueBucket
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueBucket" /> class.
        /// </summary>
        /// <param name="capacity">The initial capacity for the mutable value list.</param>
        public ValueBucket(int capacity = 0)
        {
            Values = capacity > 0
                ? new List<TValue>(capacity)
                : [];

            ReadOnlyValues = Values.AsReadOnly();
        }

        /// <summary>
        /// Gets the mutable values owned by the dictionary.
        /// </summary>
        public List<TValue> Values { get; }

        /// <summary>
        /// Gets the read-only view returned to callers.
        /// </summary>
        public ReadOnlyCollection<TValue> ReadOnlyValues { get; }
    }

    /// <summary>
    /// Throws when the supplied version no longer matches the current dictionary version.
    /// </summary>
    /// <param name="version">The version captured by an active enumerator.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the dictionary has been structurally modified.
    /// </exception>
    private void ThrowIfModified(int version)
    {
        if (version != _version)
            throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);
    }
}
