// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionary.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a mutable dictionary that maps each key to a collection of zero or more values (a multimap).
/// Multiple values may be associated with the same key, and values are stored in insertion order per key.
/// </summary>
/// <typeparam name="TKey">The type of keys. Must not be <see langword="null"/>.</typeparam>
/// <typeparam name="TValue">The type of values associated with each key.</typeparam>
/// <remarks>
/// <para>
/// <see cref="MultiValueDictionary{TKey, TValue}"/> fills the gap left by the BCL's
/// <see cref="ILookup{TKey, TElement}"/> — which is read-only and produced at construction time — by
/// providing a fully mutable one-to-many map. Keys must not be <see langword="null"/>; values may be
/// <see langword="null"/> when <typeparamref name="TValue"/> is a reference type.
/// </para>
/// <para>
/// The <see cref="Count"/> property returns the total number of key–value entries across all keys.
/// Use <see cref="KeyCount"/> to obtain the number of distinct keys currently held. The indexer
/// <c>this[TKey]</c> always returns an <see cref="IReadOnlyList{T}"/> — it returns an empty list rather
/// than throwing when the key is absent, enabling safe iteration without a preceding
/// <see cref="ContainsKey"/> check.
/// </para>
/// <para>
/// Common use cases include graph adjacency lists, inverted indexes, grouping tasks by owner, and any
/// scenario where a single key naturally maps to multiple values over the lifetime of the collection.
/// </para>
/// <para>
/// <see cref="MultiValueDictionary{TKey, TValue}"/> is not thread-safe. Concurrent reads and writes
/// require external synchronisation.
/// </para>
/// </remarks>
[DebuggerDisplay("KeyCount = {KeyCount}, Count = {Count}")]
[DebuggerTypeProxy(typeof(MultiValueDictionaryDebugView<,>))]
[Serializable]
public sealed partial class MultiValueDictionary<TKey, TValue>
    : IReadOnlyCollection<KeyValuePair<TKey, IReadOnlyList<TValue>>>
    where TKey : notnull
{
    /// <summary>Shared empty list returned by the indexer and <see cref="TryGetValues"/> when a key is absent.</summary>
    private static readonly IReadOnlyList<TValue> EmptyValues = Array.Empty<TValue>();

    /// <summary>The equality comparer used to determine key equality.</summary>
    private readonly IEqualityComparer<TKey> _comparer;

    /// <summary>The backing dictionary mapping each key to its ordered list of values.</summary>
    private Dictionary<TKey, List<TValue>> _map;

    /// <summary>The total number of value entries across all keys.</summary>
    private int _count;

    /// <summary>Incremented on every structural change; used by enumerators to detect concurrent modification.</summary>
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionary{TKey, TValue}"/> class that is
    /// empty and uses the default equality comparer for keys.
    /// </summary>
    public MultiValueDictionary()
        : this((IEqualityComparer<TKey>?)null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionary{TKey, TValue}"/> class that is
    /// empty and uses the specified equality comparer for keys.
    /// </summary>
    /// <param name="comparer">
    /// The equality comparer used to compare keys. If <see langword="null"/>, the default equality
    /// comparer for <typeparamref name="TKey"/> is used.
    /// </param>
    public MultiValueDictionary(IEqualityComparer<TKey>? comparer)
    {
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
        _map = new Dictionary<TKey, List<TValue>>(_comparer);
    }

    /// <summary>
    /// Gets the equality comparer used to determine equality of keys in this
    /// <see cref="MultiValueDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <returns>The <see cref="IEqualityComparer{T}"/> instance used to compare keys.</returns>
    public IEqualityComparer<TKey> Comparer => _comparer;

    /// <summary>
    /// Gets the total number of key–value entries stored across all keys.
    /// </summary>
    /// <returns>
    /// The total number of values, summed across all keys. For the number of distinct keys, use <see cref="KeyCount"/>.
    /// </returns>
    public int Count => _count;

    /// <summary>
    /// Gets the number of distinct keys currently held in the <see cref="MultiValueDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <returns>The number of distinct keys.</returns>
    public int KeyCount => _map.Count;

    /// <summary>
    /// Gets a read-only view of all keys in the <see cref="MultiValueDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <returns>
    /// An <see cref="IReadOnlyCollection{T}"/> containing all distinct keys. The order of keys is not guaranteed.
    /// </returns>
    public IReadOnlyCollection<TKey> Keys => _map.Keys;

    /// <summary>
    /// Gets the values associated with <paramref name="key"/> as a read-only list.
    /// </summary>
    /// <param name="key">The key whose values are to be retrieved.</param>
    /// <returns>
    /// A read-only list of values associated with <paramref name="key"/>, in insertion order. Returns an
    /// empty list when the key is absent — never throws <see cref="KeyNotFoundException"/>.
    /// </returns>
    /// <remarks>
    /// This indexer always returns a non-null list. When the key is absent the returned list is empty, so
    /// callers may safely iterate the result without first checking <see cref="ContainsKey"/>.
    /// </remarks>
    public IReadOnlyList<TValue> this[TKey key] =>
        _map.TryGetValue(key, out List<TValue>? values) ? values : EmptyValues;

    /// <summary>
    /// Appends <paramref name="value"/> to the list of values associated with <paramref name="key"/>.
    /// Creates a new key entry if the key does not yet exist.
    /// </summary>
    /// <param name="key">The key to add the value under. Must not be <see langword="null"/>.</param>
    /// <param name="value">The value to append.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public void Add(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!_map.TryGetValue(key, out List<TValue>? list))
        {
            list = new List<TValue>();
            _map[key] = list;
        }

        list.Add(value);
        _count++;
        _version++;
    }

    /// <summary>
    /// Appends each element of <paramref name="values"/> to the list associated with <paramref name="key"/>.
    /// Creates a new key entry if the key does not yet exist.
    /// </summary>
    /// <param name="key">The key to add the values under. Must not be <see langword="null"/>.</param>
    /// <param name="values">The values to append. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is <see langword="null"/>, or <paramref name="values"/> is <see langword="null"/>.
    /// </exception>
    public void AddRange(TKey key, IEnumerable<TValue> values)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(values);

        if (!_map.TryGetValue(key, out List<TValue>? list))
        {
            list = new List<TValue>();
            _map[key] = list;
        }

        int before = list.Count;
        list.AddRange(values);
        int added = list.Count - before;

        if (added > 0)
        {
            _count += added;
            _version++;
        }
        else if (before == 0)
        {
            // Clean up a newly created empty list if values was empty.
            _map.Remove(key);
        }
    }

    /// <summary>
    /// Removes all keys and their associated values from the <see cref="MultiValueDictionary{TKey, TValue}"/>.
    /// </summary>
    public void Clear()
    {
        _map.Clear();
        _count = 0;
        _version++;
    }

    /// <summary>
    /// Determines whether <paramref name="key"/> has at least one value in the
    /// <see cref="MultiValueDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <param name="key">The key to locate. Must not be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="key"/> exists and has at least one associated value;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public bool ContainsKey(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        return _map.ContainsKey(key);
    }

    /// <summary>
    /// Determines whether the value <paramref name="value"/> is associated with <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to search under. Must not be <see langword="null"/>.</param>
    /// <param name="value">The value to locate.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="key"/> exists and one of its values equals
    /// <paramref name="value"/> according to the default equality comparer; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public bool ContainsValue(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        return _map.TryGetValue(key, out List<TValue>? list) && list.Contains(value);
    }

    /// <summary>
    /// Returns the values associated with <paramref name="key"/> as a read-only list.
    /// </summary>
    /// <param name="key">The key whose values are to be returned. Must not be <see langword="null"/>.</param>
    /// <returns>
    /// A read-only list of values in insertion order. The list is a live view; it reflects any
    /// subsequent additions or removals for the same key.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException"><paramref name="key"/> does not exist in the dictionary.</exception>
    public IReadOnlyList<TValue> GetValues(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!_map.TryGetValue(key, out List<TValue>? list))
            throw new KeyNotFoundException($"The key '{key}' was not present in the dictionary.");

        return list;
    }

    /// <summary>
    /// Attempts to return the values associated with <paramref name="key"/> without throwing an exception.
    /// </summary>
    /// <param name="key">The key whose values are to be returned. Must not be <see langword="null"/>.</param>
    /// <param name="values">
    /// When this method returns <see langword="true"/>, contains the values associated with
    /// <paramref name="key"/> in insertion order; otherwise, an empty list.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="key"/> exists and has at least one value;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public bool TryGetValues(TKey key, out IReadOnlyList<TValue> values)
    {
        ThrowHelper.ThrowIfNull(key);

        if (_map.TryGetValue(key, out List<TValue>? list))
        {
            values = list;
            return true;
        }

        values = EmptyValues;
        return false;
    }

    /// <summary>
    /// Removes one occurrence of <paramref name="value"/> from the list associated with
    /// <paramref name="key"/>. Removes the key entry entirely when its value list becomes empty.
    /// </summary>
    /// <param name="key">The key under which to remove the value. Must not be <see langword="null"/>.</param>
    /// <param name="value">The value to remove.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="value"/> was found under <paramref name="key"/> and
    /// removed; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public bool Remove(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!_map.TryGetValue(key, out List<TValue>? list))
            return false;

        if (!list.Remove(value))
            return false;

        _count--;
        _version++;

        if (list.Count == 0)
            _map.Remove(key);

        return true;
    }

    /// <summary>
    /// Removes <paramref name="key"/> and all of its associated values from the
    /// <see cref="MultiValueDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <param name="key">The key to remove. Must not be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="key"/> was found and removed; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public bool RemoveAll(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        if (!_map.TryGetValue(key, out List<TValue>? list))
            return false;

        _count -= list.Count;
        _map.Remove(key);
        _version++;
        return true;
    }

    /// <summary>
    /// Returns a flat sequence of all key–value pairs, one pair per value entry across all keys.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> in which each entry
    /// represents one value for one key. The order of keys is not guaranteed; values within a key appear
    /// in insertion order.
    /// </returns>
    /// <remarks>
    /// The sequence is invalidated by any structural modification; iterating after a modification throws
    /// <see cref="InvalidOperationException"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The dictionary was modified after enumeration began.</exception>
    public IEnumerable<KeyValuePair<TKey, TValue>> Flatten()
    {
        int version = _version;
        foreach (KeyValuePair<TKey, List<TValue>> entry in _map)
        {
            foreach (TValue value in entry.Value)
            {
                if (version != _version)
                    throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionModified);

                yield return new KeyValuePair<TKey, TValue>(entry.Key, value);
            }
        }
    }
}
