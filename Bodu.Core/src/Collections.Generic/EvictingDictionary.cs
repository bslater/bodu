// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="EvictingDictionary.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------- //

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a fixed-capacity dictionary that automatically removes entries based on a chosen eviction policy, such as
/// First-In-First-Out (FirstInFirstOut), Least Recently Used (LeastRecentlyUsed), or Least Frequently Used (LeastFrequentlyUsed).
/// </summary>
/// <typeparam name="TKey">Specifies the type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">Specifies the type of values in the dictionary.</typeparam>
/// <remarks>
/// <para>
/// <see cref="EvictingDictionary{TKey, TValue}" /> maintains a maximum number of key-value pairs and automatically evicts items when
/// capacity is exceeded. Eviction is determined by a specified <see cref="EvictingDictionaryPolicy" />, allowing this dictionary to behave like a
/// queue, an access-order cache, or a frequency-based cache.
/// </para>
/// <para>
/// <see cref="EvictingDictionary{TKey, TValue}" /> allows <see langword="null" /> keys and values (for reference types) and supports
/// custom key equality via <see cref="System.Collections.Generic.IEqualityComparer{T}" />.
/// </para>
/// <example>
/// <code language="csharp">
/// <![CDATA[
/// // Create an evicting dictionary with capacity for 2 items using LRU eviction.
/// var cache = new EvictingDictionary<string, int>(capacity: 2, EvictingDictionaryPolicy.LeastRecentlyUsed);
///
/// // Add two entries.
/// cache["A"] = 1;
/// cache["B"] = 2;
///
/// // Touch "A" to mark it as recently used.
/// cache.Touch("A");
///
/// // Add a third entry; "B" is now the least recently used and will be evicted.
/// cache["C"] = 3;
///
/// // Dictionary now contains: { "A": 1, "C": 3 }
/// foreach (var kvp in cache)
///     Console.WriteLine($"{kvp.Key} = {kvp.Value}");
///
/// // Output:
/// // A = 1
/// // C = 3
/// ]]>
/// </code>
/// </example>
/// </remarks>
[DebuggerDisplay("Count: {Count}, Capacity: {_capacity}, Policy: {_evictingPolicy}")]
[DebuggerTypeProxy(typeof(EvictingDictionaryDebugView<,>))]
[Serializable]
public partial class EvictingDictionary<TKey, TValue>
    where TKey : notnull
{
    private const int DefaultCapacity = 16;
    private const EvictingDictionaryPolicy DefaultPolicy = EvictingDictionaryPolicy.LeastRecentlyUsed;

    private readonly int _capacity;
    private readonly IEqualityComparer<TKey> _comparer;
    private readonly EvictingDictionaryPolicy _evictingPolicy;
    private readonly SortedDictionary<int, LinkedList<TKey>> _frequencyList = null!;
    private readonly Dictionary<TKey, CacheItem> _store;
    private long _evictionCount;
    private LinkedList<TKey> _order = null!;
    private long _totalTouches;

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with the default capacity and eviction policy.
    /// </summary>
    /// <remarks>
    /// Creates an empty dictionary with a capacity of <see cref="DefaultCapacity" /> items, using <see cref="DefaultPolicy" /> for eviction
    /// when capacity is exceeded, and the default key comparer ( <see cref="EqualityComparer{TKey}.Default" />).
    /// </remarks>
    public EvictingDictionary()
        : this(DefaultCapacity, DefaultPolicy, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with the specified capacity and the default
    /// eviction policy.
    /// </summary>
    /// <param name="capacity">The maximum number of key/value pairs the dictionary can contain. Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than or equal to zero.</exception>
    /// <remarks>
    /// Creates an empty dictionary with the specified capacity, using <see cref="DefaultPolicy" /> for eviction when capacity is exceeded,
    /// and the default key comparer ( <see cref="EqualityComparer{TKey}.Default" />).
    /// </remarks>
    public EvictingDictionary(int capacity)
        : this(capacity, DefaultPolicy, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with the specified capacity and eviction policy.
    /// </summary>
    /// <param name="capacity">The maximum number of key/value pairs the dictionary can contain. Must be positive.</param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than or equal to zero.</exception>
    /// <remarks>
    /// Creates an empty dictionary with the specified capacity, using the specified eviction policy, and the default key comparer ( <see cref="EqualityComparer{TKey}.Default" />).
    /// </remarks>
    public EvictingDictionary(int capacity, EvictingDictionaryPolicy policy)
        : this(capacity, policy, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with the specified capacity, using the
    /// default eviction policy and the specified key comparer.
    /// </summary>
    /// <param name="capacity">The maximum number of key/value pairs the dictionary can contain. Must be positive.</param>
    /// <param name="comparer">The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than or equal to zero.</exception>
    /// <remarks>
    /// Creates an empty dictionary with the specified capacity, using <see cref="DefaultPolicy" /> for eviction when capacity is exceeded,
    /// and the specified key comparer (or <see cref="EqualityComparer{TKey}.Default" /> if <paramref name="comparer" /> is <see langword="null" />).
    /// </remarks>
    public EvictingDictionary(int capacity, IEqualityComparer<TKey>? comparer)
        : this(capacity, DefaultPolicy, comparer) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with the specified capacity, eviction
    /// policy, and key comparer.
    /// </summary>
    /// <param name="capacity">The maximum number of key/value pairs the dictionary can contain. Must be positive.</param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <param name="comparer">The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than or equal to zero.</exception>
    /// <remarks>
    /// <para>Creates an empty dictionary with the specified capacity and eviction policy, using the specified key comparer.</para>
    /// <para>
    /// Initializes the internal storage for key/value pairs, and, where applicable, the eviction tracking structure: FIFO, LRU, MRU, and
    /// SecondChance use a linked list; LFU uses a sorted dictionary of frequency lists; RandomReplacement does not require additional tracking.
    /// </para>
    /// </remarks>
    public EvictingDictionary(int capacity, EvictingDictionaryPolicy policy, IEqualityComparer<TKey>? comparer)
    {
        ThrowHelper.ThrowIfZeroOrNegative(capacity);

        _capacity = capacity;
        _evictingPolicy = policy;
        _comparer = comparer ?? EqualityComparer<TKey>.Default;

        _store = new Dictionary<TKey, CacheItem>(_comparer);

        switch (_evictingPolicy)
        {
            case EvictingDictionaryPolicy.FirstInFirstOut:
            case EvictingDictionaryPolicy.LeastRecentlyUsed:
            case EvictingDictionaryPolicy.MostRecentlyUsed:
            case EvictingDictionaryPolicy.SecondChance:
                _order = new LinkedList<TKey>();
                break;

            case EvictingDictionaryPolicy.LeastFrequentlyUsed:
                _frequencyList = new SortedDictionary<int, LinkedList<TKey>>();
                break;

            case EvictingDictionaryPolicy.RandomReplacement:
                break;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with elements copied from the specified
    /// sequence, using the default capacity and eviction policy.
    /// </summary>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>Creates a dictionary containing the elements from <paramref name="source" />.</para>
    /// <para>
    /// Uses a capacity of <see cref="DefaultCapacity" />, <see cref="DefaultPolicy" /> for eviction, and the default key comparer. If more
    /// elements are provided than the capacity allows, entries are evicted according to the policy.
    /// </para>
    /// </remarks>
    public EvictingDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source)
        : this(DefaultCapacity, source, DefaultPolicy, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with elements copied from the specified
    /// sequence, using the specified capacity and the default eviction policy.
    /// </summary>
    /// <param name="capacity">The maximum number of key/value pairs the dictionary can contain. Must be positive.</param>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than or equal to zero.</exception>
    /// <remarks>
    /// <para>Creates a dictionary containing the elements from <paramref name="source" />.</para>
    /// <para>
    /// Uses the specified capacity, <see cref="DefaultPolicy" /> for eviction, and the default key comparer. If more elements are provided
    /// than the capacity allows, entries are evicted according to the policy.
    /// </para>
    /// </remarks>
    public EvictingDictionary(int capacity, IEnumerable<KeyValuePair<TKey, TValue>> source)
        : this(capacity, source, DefaultPolicy, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with elements copied from the specified
    /// sequence, using the default capacity and the specified eviction policy.
    /// </summary>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// Uses a capacity of <see cref="DefaultCapacity" />, the specified eviction policy, and the default key comparer. If more elements are
    /// provided than the capacity allows, entries are evicted according to the policy.
    /// </remarks>
    public EvictingDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source, EvictingDictionaryPolicy policy)
        : this(DefaultCapacity, source, policy, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with elements copied from the specified
    /// sequence, using the specified capacity and eviction policy.
    /// </summary>
    /// <param name="capacity">The maximum number of key/value pairs the dictionary can contain. Must be positive.</param>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than or equal to zero.</exception>
    /// <remarks>
    /// Uses the specified capacity, the specified eviction policy, and the default key comparer. If more elements are provided than the
    /// capacity allows, entries are evicted according to the policy.
    /// </remarks>
    public EvictingDictionary(int capacity, IEnumerable<KeyValuePair<TKey, TValue>> source, EvictingDictionaryPolicy policy)
        : this(capacity, source, policy, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with elements copied from the specified
    /// sequence, using the specified capacity, eviction policy, and key comparer.
    /// </summary>
    /// <param name="capacity">The maximum number of key/value pairs the dictionary can contain. Must be positive.</param>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <param name="comparer">The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than or equal to zero.</exception>
    /// <remarks>
    /// Uses the specified capacity, the specified eviction policy, and the specified key comparer (or
    /// <see cref="EqualityComparer{TKey}.Default" /> if <paramref name="comparer" /> is <see langword="null" />). If more elements are
    /// provided than the capacity allows, entries are evicted according to the policy.
    /// </remarks>
    public EvictingDictionary(int capacity, IEnumerable<KeyValuePair<TKey, TValue>> source, EvictingDictionaryPolicy policy, IEqualityComparer<TKey>? comparer)
        : this(capacity, policy, comparer)
    {
        ThrowHelper.ThrowIfNull(source);

        foreach (KeyValuePair<TKey, TValue> kvp in source)
            Add(kvp.Key, kvp.Value);
    }

    /// <summary>
    /// Occurs immediately <b>after</b> an item is evicted from the
    /// <see cref="EvictingDictionary{TKey, TValue}" /> due to capacity limits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised after the item has been removed from the collection, based on the
    /// configured <see cref="EvictingDictionaryPolicy" /> (e.g., FirstInFirstOut, LeastRecentlyUsed, or LeastFrequentlyUsed).
    /// </para>
    /// <para>
    /// Consumers can use this event to record historical data, notify observers, or synchronise
    /// external caches. The key and value provided are no longer present in the dictionary.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// var cache = new EvictingDictionary<string, int>(capacity: 2, EvictingDictionaryPolicy.FirstInFirstOut);
    /// cache.ItemEvicted += (key, value) =>
    /// {
    ///     Console.WriteLine($"[AfterEvict] {key} = {value}");
    /// };
    ///
    /// cache.Add("A", 1);
    /// cache.Add("B", 2);
    /// cache.Add("C", 3); // Triggers ItemEvicted for "A".
    /// ]]>
    /// </code>
    /// </example>
    public event Action<TKey, TValue>? ItemEvicted;

    /// <summary>
    /// Occurs immediately <b>before</b> an item is evicted from the
    /// <see cref="EvictingDictionary{TKey, TValue}" /> due to capacity limits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised before the item is removed from the collection, allowing consumers
    /// to inspect the key and value before eviction occurs.
    /// </para>
    /// <para>
    /// Common use cases include diagnostics, logging, cache warm-up, or state mirroring. This
    /// event is informational and cannot cancel or delay eviction.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// var cache = new EvictingDictionary<string, int>(capacity: 2, EvictingDictionaryPolicy.FirstInFirstOut);
    /// cache.ItemEvicting += (key, value) =>
    /// {
    ///     Console.WriteLine($"[BeforeEvict] {key} = {value}");
    /// };
    ///
    /// cache.Add("A", 1);
    /// cache.Add("B", 2);
    /// cache.Add("C", 3); // Triggers ItemEvicting for "A".
    /// ]]>
    /// </code>
    /// </example>
    public event Action<TKey, TValue>? ItemEvicting;

    /// <summary>
    /// Gets the maximum number of items that can be stored in the dictionary before eviction occurs.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the total number of items evicted from the dictionary since creation.
    /// </summary>
    public long EvictionCount => _evictionCount;

    /// <summary>
    /// Gets the eviction policy configured for this dictionary.
    /// </summary>
    public EvictingDictionaryPolicy Policy => _evictingPolicy;

    /// <summary>
    /// Gets the total number of times any key has been accessed or touched.
    /// </summary>
    public long TotalTouches => _totalTouches;

    /// <summary>
    /// Returns the key that would be evicted next based on the current eviction policy and internal state.
    /// </summary>
    /// <returns>The key that is next in line for eviction, or <see langword="default" /> if the dictionary is empty.</returns>
    /// <remarks>
    /// The eviction candidate depends on the selected <see cref="EvictingDictionaryPolicy" />:
    /// <list type="bullet">
    /// <item>
    /// <description><b>FirstInFirstOut</b>: returns the oldest inserted key.</description>
    /// </item>
    /// <item>
    /// <description><b>LeastRecentlyUsed</b>: returns the least recently accessed key.</description>
    /// </item>
    /// <item>
    /// <description><b>MostRecentlyUsed</b>: returns the most recently accessed key.</description>
    /// </item>
    /// <item>
    /// <description><b>LeastFrequentlyUsed</b>: returns the key with the fewest total accesses.</description>
    /// </item>
    /// <item>
    /// <description><b>RandomReplacement</b>: returns an arbitrary key from the dictionary.</description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>SecondChance</b>: returns the first key that has not been accessed recently; falls back to FIFO if all have second chances.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    public TKey? PeekEvictionCandidate()
    {
        return _evictingPolicy switch
        {
            EvictingDictionaryPolicy.FirstInFirstOut or EvictingDictionaryPolicy.LeastRecentlyUsed
                when _order?.First is not null => _order.First.Value,

            EvictingDictionaryPolicy.MostRecentlyUsed
                when _order?.Last is not null => _order.Last.Value,

            EvictingDictionaryPolicy.LeastFrequentlyUsed
                when _frequencyList?.Count > 0 && _frequencyList.First().Value.First is not null
                => _frequencyList.First().Value.First!.Value,

            EvictingDictionaryPolicy.RandomReplacement
                when _store.Count > 0 => _store.Keys.First(),

            EvictingDictionaryPolicy.SecondChance when _order is not null => PeekSecondChanceCandidate(),

            _ => default
        };
    }

    /// <summary>
    /// Marks the specified key as recently accessed without retrieving its value. If the eviction policy involves usage tracking, this
    /// updates the internal usage metadata.
    /// </summary>
    /// <param name="key">The key to touch.</param>
    /// <returns><see langword="true" /> if the key exists and was marked as accessed; otherwise, <see langword="false" />.</returns>
    public bool Touch(TKey key)
    {
        if (_store.TryGetValue(key, out CacheItem? item))
        {
            TouchInternal(key, item);
            _totalTouches++;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Marks the specified key as recently accessed without retrieving its value, and throws an exception if the key does not exist in the dictionary.
    /// </summary>
    /// <param name="key">The key to touch.</param>
    /// <exception cref="KeyNotFoundException">The specified <paramref name="key" /> does not exist in the dictionary.</exception>
    /// <remarks>
    /// If the eviction policy is <see cref="EvictingDictionaryPolicy.LeastRecentlyUsed" /> or
    /// <see cref="EvictingDictionaryPolicy.LeastFrequentlyUsed" />, this updates the internal usage metadata.
    /// </remarks>
    public void TouchOrThrow(TKey key)
    {
        if (!Touch(key))
            throw new KeyNotFoundException($"The key '{key}' was not found in the dictionary.");
    }

    /// <summary>
    /// Adds the specified key to the LeastFrequentlyUsed frequency bucket for the given frequency.
    /// </summary>
    /// <param name="frequency">The new frequency count.</param>
    /// <param name="key">The key to add.</param>
    private void AddToFrequencyList(int frequency, TKey key)
    {
        if (!_frequencyList.TryGetValue(frequency, out LinkedList<TKey>? list))
        {
            list = new LinkedList<TKey>();
            _frequencyList[frequency] = list;
        }

        list.AddLast(key);
    }

    /// <summary>
    /// Removes the next item to be evicted based on the current eviction policy. Raises the <see cref="ItemEvicting" /> and
    /// <see cref="ItemEvicted" /> events and updates eviction metrics.
    /// </summary>
    private void EvictOne()
    {
        TKey? keyToRemove = _evictingPolicy switch
        {
            EvictingDictionaryPolicy.FirstInFirstOut or EvictingDictionaryPolicy.LeastRecentlyUsed
                when _order?.First is not null => _order.First.Value,

            EvictingDictionaryPolicy.MostRecentlyUsed
                when _order?.Last is not null => _order.Last.Value,

            EvictingDictionaryPolicy.LeastFrequentlyUsed
                when _frequencyList?.Count > 0 &&
                     _frequencyList.First().Value?.First is LinkedListNode<TKey> node => node.Value,

            EvictingDictionaryPolicy.RandomReplacement
                when _store.Count > 0 => _store.Keys.First(),

            EvictingDictionaryPolicy.SecondChance when _order is not null =>
                GetSecondChanceCandidate(),

            _ => default
        };

        if (keyToRemove is not null && _store.TryGetValue(keyToRemove, out CacheItem? item))
        {
            ItemEvicting?.Invoke(keyToRemove, item.Value);
            _evictionCount++;
            Remove(keyToRemove);
            ItemEvicted?.Invoke(keyToRemove, item.Value);
        }
    }

    /// <summary>
    /// Returns an ordered enumeration of key-value pairs based on the current eviction policy and internal tracking state.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{T}" /> of <see cref="KeyValuePair{TKey, TValue}" /> in the order determined by the current eviction policy.
    /// </returns>
    /// <exception cref="InvalidOperationException">The eviction policy is unrecognised or unsupported for ordering.</exception>
    /// <remarks>
    /// This method is used primarily for diagnostics, testing, or enumeration purposes, and reflects the internal priority used for
    /// eviction, not insertion order.
    /// </remarks>
    private IEnumerable<KeyValuePair<TKey, TValue>> GetOrderedItems()
    {
        switch (_evictingPolicy)
        {
            case EvictingDictionaryPolicy.FirstInFirstOut:
            case EvictingDictionaryPolicy.LeastRecentlyUsed:
            case EvictingDictionaryPolicy.SecondChance:
                if (_order is null)
                    yield break;

                foreach (TKey key in _order)
                {
                    if (_store.TryGetValue(key, out CacheItem? item))
                        yield return new KeyValuePair<TKey, TValue>(key, item.Value);
                }

                break;

            case EvictingDictionaryPolicy.MostRecentlyUsed:
                if (_order is null)
                    yield break;

                // MRU: iterate from most recently used (tail) to least (head).
                for (LinkedListNode<TKey>? node = _order.Last; node is not null; node = node.Previous)
                {
                    if (_store.TryGetValue(node.Value, out CacheItem? item))
                        yield return new KeyValuePair<TKey, TValue>(node.Value, item.Value);
                }

                break;

            case EvictingDictionaryPolicy.LeastFrequentlyUsed:
                if (_frequencyList is null)
                    yield break;

                // SortedDictionary is already in ascending key order; no secondary sort is required.
                foreach (KeyValuePair<int, LinkedList<TKey>> freq in _frequencyList)
                {
                    foreach (TKey key in freq.Value)
                    {
                        if (_store.TryGetValue(key, out CacheItem? item))
                            yield return new KeyValuePair<TKey, TValue>(key, item.Value);
                    }
                }

                break;

            case EvictingDictionaryPolicy.RandomReplacement:
#if NETSTANDARD2_0
                foreach (var pair in _store)
                    yield return new KeyValuePair<TKey, TValue>(pair.Key, pair.Value.Value);
#else
                foreach ((TKey key, CacheItem item) in _store)
                    yield return new KeyValuePair<TKey, TValue>(key, item.Value);
#endif
                break;

            default:
                throw new InvalidOperationException($"Unknown eviction policy: {_evictingPolicy}");
        }
    }

    /// <summary>
    /// Finds the next candidate for eviction using the Second-Chance algorithm. Items with their second-chance flag set are moved to the
    /// end of the list and cleared. If no eligible item is found, the oldest item is returned.
    /// </summary>
    /// <returns>The key to evict according to the Second-Chance strategy.</returns>
    /// <exception cref="InvalidOperationException">The internal order list is empty.</exception>
    private TKey GetSecondChanceCandidate()
    {
        if (_order is null || _order.Count == 0)
            throw new InvalidOperationException("No eviction candidate available: the order list is empty.");

        // Walk the list using explicit node references so we can perform O(1) removal and re-insertion
        // without allocating a snapshot copy via ToList().
        LinkedListNode<TKey>? node = _order.First;
        while (node is not null)
        {
            TKey key = node.Value;
            LinkedListNode<TKey> current = node;
            node = node.Next;

            if (!_store.TryGetValue(key, out CacheItem? item))
                continue;

            if (!item.SecondChance)
                return key;

            // Give the item a second chance: clear the flag and cycle it to the tail.
            item.SecondChance = false;
            _order.Remove(current);
            item.Node = _order.AddLast(key);
        }

        // All items had their second-chance flag set; fall back to the oldest remaining entry.
        if (_order.First is not null)
            return _order.First.Value;

        throw new InvalidOperationException("No eviction candidate found after second-chance evaluation.");
    }

    /// <summary>
    /// Returns the key that would be selected next by the Second-Chance algorithm, without modifying any state.
    /// </summary>
    /// <returns>The candidate key for eviction, or the oldest key if all items have their second-chance flag set.</returns>
    private TKey? PeekSecondChanceCandidate()
    {
        foreach (TKey key in _order)
        {
            if (_store.TryGetValue(key, out CacheItem? item) && !item.SecondChance)
                return key;
        }

        return _order.First is not null ? _order.First.Value : default;
    }

    /// <summary>
    /// Removes the specified key from the LeastFrequentlyUsed frequency bucket for the given frequency. Cleans up the bucket if it becomes empty.
    /// </summary>
    /// <param name="frequency">The current frequency count of the key.</param>
    /// <param name="key">The key to remove.</param>
    private void RemoveFromFrequencyList(int frequency, TKey key)
    {
        if (_frequencyList.TryGetValue(frequency, out LinkedList<TKey>? list))
        {
            list.Remove(key);

            if (list.Count == 0)
                _frequencyList.Remove(frequency);
        }
    }

    /// <summary>
    /// Handles internal usage tracking logic based on the current eviction policy.
    /// </summary>
    /// <param name="key">The key that was accessed.</param>
    /// <param name="item">The associated cache item for the key.</param>
    private void TouchInternal(TKey key, CacheItem item)
    {
        switch (_evictingPolicy)
        {
            case EvictingDictionaryPolicy.LeastRecentlyUsed:
            case EvictingDictionaryPolicy.MostRecentlyUsed:
                if (_order is not null)
                {
                    if (item.Node is not null)
                        _order.Remove(item.Node);

                    item.Node = _order.AddLast(key);
                }

                break;

            case EvictingDictionaryPolicy.LeastFrequentlyUsed:
                RemoveFromFrequencyList(item.Frequency, key);
                item.Frequency++;
                AddToFrequencyList(item.Frequency, key);
                break;

            case EvictingDictionaryPolicy.SecondChance:
                item.SecondChance = true;
                break;
        }
    }
}
