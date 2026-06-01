// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionary.cs" company="Bodu Pty. Ltd.">
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
///<![CDATA[
/// Multiple values under a single key — values are retained in insertion order.
/// var map = new MultiValueDictionary<string, int>();
/// map.Add("odd",  1);
/// map.Add("odd",  3);
/// map.Add("even", 2);
///
/// Console.WriteLine(map.Count);    // 3 — total key-value entries
/// Console.WriteLine(map.KeyCount); // 2 — distinct keys
///
/// The indexer returns a live read-only view; absent keys yield an empty list rather than throwing.
/// foreach (int value in map["odd"])
///     Console.WriteLine(value);
///
/// Flatten into one KeyValuePair per stored value.
/// foreach (KeyValuePair<string, int> pair in map.Flatten())
///     Console.WriteLine($"{pair.Key}: {pair.Value}");
///]]>
/// </example>
[DebuggerDisplay("KeyCount = {KeyCount}, Count = {Count}")]
[DebuggerTypeProxy(typeof(MultiValueDictionaryDebugView<,>))]
[Serializable]
public sealed partial class MultiValueDictionary<TKey, TValue>
    : IReadOnlyCollection<KeyValuePair<TKey, IReadOnlyList<TValue>>>
    where TKey : notnull
{
    /// <summary>
    /// Shared empty read-only list returned when a key is absent.
    /// </summary>
    private static readonly IReadOnlyList<TValue> s_emptyValues = Array.AsReadOnly(Array.Empty<TValue>());

    /// <summary>
    /// The equality comparer used to determine key equality.
    /// </summary>
    private readonly IEqualityComparer<TKey> _comparer;

    /// <summary>
    /// The backing dictionary mapping each key to its value bucket.
    /// </summary>
    private readonly Dictionary<TKey, ValueBucket> _map;

    /// <summary>
    /// The total number of value entries across all keys.
    /// </summary>
    private int _count;

    /// <summary>
    /// Incremented on every structural change; used by enumerators to detect concurrent modification.
    /// </summary>
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
    {
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
        _map = new Dictionary<TKey, ValueBucket>(_comparer);
    }

    /// <summary>
    /// Gets the equality comparer used to determine equality of keys.
    /// </summary>
    /// <value>The <see cref="IEqualityComparer{T}" /> instance used to compare keys.</value>
    public IEqualityComparer<TKey> Comparer => _comparer;

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
    /// Appends <paramref name="value" /> to the list of values associated with <paramref name="key" />.
    /// </summary>
    /// <param name="key">The key to add the value under.</param>
    /// <param name="value">The value to append.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public void Add(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!_map.TryGetValue(key, out ValueBucket? bucket))
        {
            bucket = new ValueBucket();
            _map[key] = bucket;
        }

        bucket.Values.Add(value);
        _count++;
        _version++;
    }

    /// <summary>
    /// Appends each element of <paramref name="values" /> to the list associated with <paramref name="key" />.
    /// </summary>
    /// <param name="key">The key to add the values under.</param>
    /// <param name="values">The values to append.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or <paramref name="values" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The operation is atomic with respect to this dictionary. If the source sequence throws while being enumerated,
    /// the dictionary is left unchanged. An empty source is treated as a no-op and does not invalidate active
    /// enumerators.
    /// </remarks>
    public void AddRange(TKey key, IEnumerable<TValue> values)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(values);

        TValue[] items = values as TValue[] ?? [.. values];

        if (items.Length == 0)
            return;

        if (!_map.TryGetValue(key, out ValueBucket? bucket))
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
    public bool ContainsValue(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        return _map.TryGetValue(key, out ValueBucket? bucket) &&
            bucket.Values.Contains(value);
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
            : throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, ResourceStrings.KeyNotFound_Dictionary, key));
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
    /// Removes one occurrence of <paramref name="value" /> from the list associated with <paramref name="key" />.
    /// </summary>
    /// <param name="key">The key under which to remove the value.</param>
    /// <param name="value">The value to remove.</param>
    /// <returns><see langword="true" /> if a value was removed; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public bool Remove(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!_map.TryGetValue(key, out ValueBucket? bucket))
            return false;

        if (!bucket.Values.Remove(value))
            return false;

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
        var version = _version;

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
            this.Values = capacity > 0
                ? new List<TValue>(capacity)
                : [];

            this.ReadOnlyValues = this.Values.AsReadOnly();
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
            throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionModified);
    }
}
