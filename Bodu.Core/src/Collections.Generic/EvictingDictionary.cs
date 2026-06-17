// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a fixed-capacity dictionary that automatically removes entries based on a chosen eviction policy, such as
/// First-In-First-Out (FirstInFirstOut), Least Recently Used (LeastRecentlyUsed), or Least Frequently Used
/// (LeastFrequentlyUsed).
/// </summary>
/// <typeparam name="TKey">Specifies the type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">Specifies the type of values in the dictionary.</typeparam>
/// <remarks>
/// <para>
/// <see cref="EvictingDictionary{TKey, TValue}" /> maintains a maximum number of key-value pairs and automatically
/// evicts items when capacity is exceeded. Eviction is determined by a specified
/// <see cref="EvictingDictionaryPolicy" />, allowing this dictionary to behave like a queue, an access-order cache, or
/// a frequency-based cache.
/// </para>
/// <para>
/// Keys must be non-null (the type parameter is constrained by <see langword="notnull" />). Values may be
/// <see langword="null" /> when <typeparamref name="TValue" /> is a reference type. Custom key equality is supported
/// via <see cref="System.Collections.Generic.IEqualityComparer{T}" />.
/// </para>
/// <para>
/// Calling <see cref="EvictingDictionary{TKey, TValue}.Add(TKey, TValue)" /> (or assigning via the indexer) with a key
/// that already exists replaces the existing entry rather than throwing — this differs from
/// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" />'s strict <c>Add</c> semantics, and resets the
/// entry's eviction metadata.
/// </para>
/// <para>
/// <see cref="EvictingDictionary{TKey, TValue}" /> is not thread-safe. Concurrent reads and writes (including reads,
/// which mutate eviction metadata for some policies) require external synchronization.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
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
///]]>
/// </code>
/// </example>
/// </remarks>
[DebuggerDisplay("Count: {Count}, Capacity: {_capacity}, Policy: {_evictingPolicy}")]
[DebuggerTypeProxy(typeof(EvictingDictionaryDebugView<,>))]
public partial class EvictingDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// The capacity used when the dictionary is constructed without an explicit capacity.
    /// </summary>
    private const int DefaultCapacity = 16;

    /// <summary>
    /// The eviction policy used when the dictionary is constructed without an explicit policy.
    /// </summary>
    private const EvictingDictionaryPolicy DefaultPolicy = EvictingDictionaryPolicy.LeastRecentlyUsed;

    /// <summary>
    /// The equality comparer used for key identity and hash-table lookup.
    /// </summary>
    private readonly IEqualityComparer<TKey> _comparer;

    /// <summary>
    /// Maps access frequency to the keys observed at that frequency, used by the least-frequently-used policy.
    /// </summary>
    private readonly SortedDictionary<int, LinkedList<TKey>> _frequencyList = null!;

    /// <summary>
    /// The backing store mapping each key to its cached value and bookkeeping metadata.
    /// </summary>
    private readonly Dictionary<TKey, CacheItem> _store;

    /// <summary>
    /// Tracks key recency ordering, used by the least-recently-used policy.
    /// </summary>
    private readonly LinkedList<TKey> _order = null!;

    /// <summary>
    /// Indicates whether an eviction is currently in progress, used to suppress re-entrant bookkeeping.
    /// </summary>
    private bool _isEvicting;

    /// <summary>
    /// The cached key collection returned by the keys accessor, allocated on first access.
    /// </summary>
    private KeyCollection? _keys;

    /// <summary>
    /// The cached value collection returned by the values accessor, allocated on first access.
    /// </summary>
    private ValueCollection? _values;

    /// <summary>
    /// The modification counter used to detect concurrent mutation during enumeration.
    /// </summary>
    /// <remarks>
    /// Incremented on every mutation (add, remove, clear, in-place replace) so enumerators can detect concurrent
    /// modification and fail fast rather than produce undefined ordering.
    /// </remarks>
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with the default
    /// capacity and eviction policy.
    /// </summary>
    /// <remarks>
    /// Creates an empty dictionary with a capacity of <see cref="DefaultCapacity" /> items, using
    /// <see cref="DefaultPolicy" /> for eviction when capacity is exceeded, and the default key comparer (
    /// <see cref="EqualityComparer{TKey}.Default" />).
    /// </remarks>
    public EvictingDictionary()
        : this(DefaultCapacity, DefaultPolicy, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with the specified
    /// capacity and the default eviction policy.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// Creates an empty dictionary with the specified capacity, using <see cref="DefaultPolicy" /> for eviction when
    /// capacity is exceeded, and the default key comparer ( <see cref="EqualityComparer{TKey}.Default" />).
    /// </remarks>
    public EvictingDictionary(int capacity)
        : this(capacity, DefaultPolicy, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with the specified
    /// capacity and eviction policy.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// Creates an empty dictionary with the specified capacity, using the specified eviction policy, and the default
    /// key comparer ( <see cref="EqualityComparer{TKey}.Default" />).
    /// </remarks>
    public EvictingDictionary(int capacity, EvictingDictionaryPolicy policy)
        : this(capacity, policy, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with the specified
    /// capacity, using the default eviction policy and the specified key comparer.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="comparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// Creates an empty dictionary with the specified capacity, using <see cref="DefaultPolicy" /> for eviction when
    /// capacity is exceeded, and the specified key comparer (or <see cref="EqualityComparer{TKey}.Default" /> if
    /// <paramref name="comparer" /> is <see langword="null" />).
    /// </remarks>
    public EvictingDictionary(int capacity, IEqualityComparer<TKey>? comparer)
        : this(capacity, DefaultPolicy, comparer) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with the specified
    /// capacity, eviction policy, and key comparer.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <param name="comparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Creates an empty dictionary with the specified capacity and eviction policy, using the specified key comparer.
    /// </para>
    /// <para>
    /// Initializes the internal storage for key/value pairs, and, where applicable, the eviction tracking structure:
    /// FIFO, LRU, MRU, and SecondChance use a linked list; LFU uses a sorted dictionary of frequency lists;
    /// RandomReplacement does not require additional tracking.
    /// </para>
    /// </remarks>
    public EvictingDictionary(int capacity, EvictingDictionaryPolicy policy, IEqualityComparer<TKey>? comparer)
    {
        ThrowHelper.ThrowIfZeroOrNegative(capacity);

        Capacity = capacity;
        Policy = policy;
        _comparer = comparer ?? EqualityComparer<TKey>.Default;

        _store = new Dictionary<TKey, CacheItem>(_comparer);

        switch (Policy)
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
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with elements copied
    /// from the specified sequence, using the default capacity and eviction policy.
    /// </summary>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// Creates a dictionary containing the elements from <paramref name="source" />.
    /// </para>
    /// <para>
    /// Uses a capacity of <see cref="DefaultCapacity" />, <see cref="DefaultPolicy" /> for eviction, and the default
    /// key comparer. If more elements are provided than the capacity allows, entries are evicted according to the
    /// policy.
    /// </para>
    /// </remarks>
    public EvictingDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source)
        : this(DefaultCapacity, source, DefaultPolicy, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with elements copied
    /// from the specified sequence, using the specified capacity and the default eviction policy.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Creates a dictionary containing the elements from <paramref name="source" />.
    /// </para>
    /// <para>
    /// Uses the specified capacity, <see cref="DefaultPolicy" /> for eviction, and the default key comparer. If more
    /// elements are provided than the capacity allows, entries are evicted according to the policy.
    /// </para>
    /// </remarks>
    public EvictingDictionary(int capacity, IEnumerable<KeyValuePair<TKey, TValue>> source)
        : this(capacity, source, DefaultPolicy, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with elements copied
    /// from the specified sequence, using the default capacity and the specified eviction policy.
    /// </summary>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// Uses a capacity of <see cref="DefaultCapacity" />, the specified eviction policy, and the default key comparer.
    /// If more elements are provided than the capacity allows, entries are evicted according to the policy.
    /// </remarks>
    public EvictingDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source, EvictingDictionaryPolicy policy)
        : this(DefaultCapacity, source, policy, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with elements copied
    /// from the specified sequence, using the specified capacity and eviction policy.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// Uses the specified capacity, the specified eviction policy, and the default key comparer. If more elements are
    /// provided than the capacity allows, entries are evicted according to the policy.
    /// </remarks>
    public EvictingDictionary(int capacity, IEnumerable<KeyValuePair<TKey, TValue>> source, EvictingDictionaryPolicy policy)
        : this(capacity, source, policy, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with elements copied
    /// from the specified sequence, using the specified capacity, eviction policy, and key comparer.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <param name="comparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// Uses the specified capacity, the specified eviction policy, and the specified key comparer (or
    /// <see cref="EqualityComparer{TKey}.Default" /> if <paramref name="comparer" /> is <see langword="null" />). If
    /// more elements are provided than the capacity allows, entries are evicted according to the policy.
    /// </remarks>
    public EvictingDictionary(int capacity, IEnumerable<KeyValuePair<TKey, TValue>> source, EvictingDictionaryPolicy policy, IEqualityComparer<TKey>? comparer)
        : this(capacity, policy, comparer)
    {
        ThrowHelper.ThrowIfNull(source);

        foreach (KeyValuePair<TKey, TValue> kvp in source)
            Add(kvp.Key, kvp.Value);
    }

    /// <summary>
    /// Occurs immediately <b>after</b> an item is evicted from the <see cref="EvictingDictionary{TKey, TValue}" /> due
    /// to capacity limits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised after the item has been removed from the collection, based on the configured
    /// <see cref="EvictingDictionaryPolicy" /> (e.g., FirstInFirstOut, LeastRecentlyUsed, or LeastFrequentlyUsed).
    /// </para>
    /// <para>
    /// Consumers can use this event to record historical data, notify observers, or synchronize external caches. The
    /// key and value provided are no longer present in the dictionary.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var cache = new EvictingDictionary<string, int>(capacity: 2, EvictingDictionaryPolicy.FirstInFirstOut);
    /// cache.ItemEvicted += (key, value) =>
    /// {
    ///     Console.WriteLine($"[AfterEvict] {key} = {value}");
    /// };
    ///
    /// cache.Add("A", 1);
    /// cache.Add("B", 2);
    /// cache.Add("C", 3); // Triggers ItemEvicted for "A".
    ///]]>
    /// </code>
    /// </example>
    public event Action<TKey, TValue>? ItemEvicted;

    /// <summary>
    /// Occurs immediately <b>before</b> an item is evicted from the <see cref="EvictingDictionary{TKey, TValue}" /> due
    /// to capacity limits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised before the item is removed from the collection, allowing consumers to inspect the key and
    /// value before eviction occurs.
    /// </para>
    /// <para>
    /// Common use cases include diagnostics, logging, cache warm-up, or state mirroring. This event is informational
    /// and cannot cancel or delay eviction.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var cache = new EvictingDictionary<string, int>(capacity: 2, EvictingDictionaryPolicy.FirstInFirstOut);
    /// cache.ItemEvicting += (key, value) =>
    /// {
    ///     Console.WriteLine($"[BeforeEvict] {key} = {value}");
    /// };
    ///
    /// cache.Add("A", 1);
    /// cache.Add("B", 2);
    /// cache.Add("C", 3); // Triggers ItemEvicting for "A".
    ///]]>
    /// </code>
    /// </example>
    public event Action<TKey, TValue>? ItemEvicting;

    /// <summary>
    /// Gets the maximum number of items that can be stored in the dictionary before eviction occurs.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// Gets the total number of items evicted from the dictionary since creation.
    /// </summary>
    public long EvictionCount { get; private set; }

    /// <summary>
    /// Gets the eviction policy configured for this dictionary.
    /// </summary>
    public EvictingDictionaryPolicy Policy { get; }

    /// <summary>
    /// Gets the total number of times any key has been accessed or touched.
    /// </summary>
    public long TotalTouches { get; private set; }

    /// <summary>
    /// Returns the key that would be evicted next based on the current eviction policy and internal state.
    /// </summary>
    /// <returns>
    /// The key that is next in line for eviction, or <see langword="default" /> if the dictionary is empty.
    /// </returns>
    /// <remarks>
    /// The eviction candidate depends on the selected <see cref="EvictingDictionaryPolicy" />:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <b>FirstInFirstOut</b>: returns the oldest inserted key.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>LeastRecentlyUsed</b>: returns the least recently accessed key.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>MostRecentlyUsed</b>: returns the most recently accessed key.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>LeastFrequentlyUsed</b>: returns the key with the fewest total accesses.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>RandomReplacement</b>: returns an arbitrary key from the dictionary.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>SecondChance</b>: returns the first key that has not been accessed recently; falls back to FIFO if all have
    /// second chances.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    public TKey? PeekEvictionCandidate()
    {
        return Policy switch
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
    /// Marks the specified key as recently accessed without retrieving its value. If the eviction policy involves usage
    /// tracking, this updates the internal usage metadata.
    /// </summary>
    /// <param name="key">The key to touch.</param>
    /// <returns>
    /// <see langword="true" /> if the key exists and was marked as accessed; otherwise, <see langword="false" />.
    /// </returns>
    public bool Touch(TKey key)
    {
        if (_store.TryGetValue(key, out CacheItem? item))
        {
            TouchInternal(key, item);
            TotalTouches++;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Marks the specified key as recently accessed without retrieving its value, and throws an exception if the key
    /// does not exist in the dictionary.
    /// </summary>
    /// <param name="key">The key to touch.</param>
    /// <exception cref="KeyNotFoundException">
    /// The specified <paramref name="key" /> does not exist in the dictionary.
    /// </exception>
    /// <remarks>
    /// If the eviction policy is <see cref="EvictingDictionaryPolicy.LeastRecentlyUsed" /> or
    /// <see cref="EvictingDictionaryPolicy.LeastFrequentlyUsed" />, this updates the internal usage metadata.
    /// </remarks>
    public void TouchOrThrow(TKey key)
    {
        if (!Touch(key))
            throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, ResourceStrings.KeyNotFound_Dictionary, key));
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
    /// Removes the next item to be evicted based on the current eviction policy. Raises the <see cref="ItemEvicting" />
    /// and <see cref="ItemEvicted" /> events and updates eviction metrics.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the configured eviction policy fails to produce a candidate while the dictionary is at or above its
    /// capacity. This indicates that the internal tracking structures have become desynchronized from the underlying
    /// store.
    /// </exception>
    private void EvictOne()
    {
        // Track success explicitly: relying on `keyToRemove is not null` is unreliable when TKey is a value type
        // because default(TKey) (e.g. 0) may itself be a valid key.
        bool found = false;
        TKey keyToRemove = default!;

        switch (Policy)
        {
            case EvictingDictionaryPolicy.FirstInFirstOut:
            case EvictingDictionaryPolicy.LeastRecentlyUsed:
                if (_order?.First is { } firstNode)
                {
                    keyToRemove = firstNode.Value;
                    found = true;
                }

                break;

            case EvictingDictionaryPolicy.MostRecentlyUsed:
                if (_order?.Last is { } lastNode)
                {
                    keyToRemove = lastNode.Value;
                    found = true;
                }

                break;

            case EvictingDictionaryPolicy.LeastFrequentlyUsed:
                if (_frequencyList?.Count > 0
                    && _frequencyList.First().Value?.First is LinkedListNode<TKey> lfuNode)
                {
                    keyToRemove = lfuNode.Value;
                    found = true;
                }

                break;

            case EvictingDictionaryPolicy.RandomReplacement:
                if (_store.Count > 0)
                {
                    keyToRemove = _store.Keys.ElementAt(Random.Shared.Next(_store.Count));
                    found = true;
                }

                break;

            case EvictingDictionaryPolicy.SecondChance:
                if (_order?.Count > 0)
                {
                    keyToRemove = GetSecondChanceCandidate();
                    found = true;
                }

                break;
        }

        if (found && _store.TryGetValue(keyToRemove, out CacheItem? item))
        {
            // Mark eviction in progress around each event so a handler attempting to mutate the dictionary
            // fails fast via ThrowIfEvicting rather than corrupting internal state mid-eviction.
            try
            {
                _isEvicting = true;
                ItemEvicting?.Invoke(keyToRemove, item.Value);
            }
            finally
            {
                _isEvicting = false;
            }

            EvictionCount++;
            Remove(keyToRemove);

            try
            {
                _isEvicting = true;
                ItemEvicted?.Invoke(keyToRemove, item.Value);
            }
            finally
            {
                _isEvicting = false;
            }

            return;
        }

        // No candidate was produced. If the store is still at capacity the caller (Add) will exceed the limit, so fail loudly
        // rather than silently corrupting the invariant.
        if (_store.Count >= Capacity) throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ResourceStrings.Op_Invalid_EvictionProducedNoCandidate, Policy));
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if called while an eviction event is being dispatched. Prevents
    /// handlers from re-entering the dictionary and corrupting its state.
    /// </summary>
    private void ThrowIfEvicting()
    {
        if (_isEvicting) throw new InvalidOperationException(ResourceStrings.Op_Invalid_ReentrancyNotAllowed);
    }

    /// <summary>
    /// Returns an ordered enumeration of key-value pairs based on the current eviction policy and internal tracking
    /// state.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{T}" /> of <see cref="KeyValuePair{TKey, TValue}" /> in the order determined by the
    /// current eviction policy.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The eviction policy is unrecognized or unsupported for ordering.
    /// </exception>
    /// <remarks>
    /// This method is used primarily for diagnostics, testing, or enumeration purposes, and reflects the internal
    /// priority used for eviction, not insertion order.
    /// </remarks>
    private IEnumerable<KeyValuePair<TKey, TValue>> GetOrderedItems()
    {
        int version = _version;

        switch (Policy)
        {
            case EvictingDictionaryPolicy.FirstInFirstOut:
            case EvictingDictionaryPolicy.LeastRecentlyUsed:
            case EvictingDictionaryPolicy.SecondChance:
                if (_order is null)
                    yield break;

                foreach (TKey key in _order)
                {
                    ThrowIfVersionChanged(version);

                    if (_store.TryGetValue(key, out CacheItem? item))
                        yield return new KeyValuePair<TKey, TValue>(key, item.Value);
                }

                break;

            case EvictingDictionaryPolicy.MostRecentlyUsed:
                if (_order is null)
                    yield break;

                for (LinkedListNode<TKey>? node = _order.Last; node is not null; node = node.Previous)
                {
                    ThrowIfVersionChanged(version);

                    if (_store.TryGetValue(node.Value, out CacheItem? item))
                        yield return new KeyValuePair<TKey, TValue>(node.Value, item.Value);
                }

                break;

            case EvictingDictionaryPolicy.LeastFrequentlyUsed:
                if (_frequencyList is null)
                    yield break;

                foreach (KeyValuePair<int, LinkedList<TKey>> freq in _frequencyList)
                {
                    foreach (TKey key in freq.Value)
                    {
                        ThrowIfVersionChanged(version);

                        if (_store.TryGetValue(key, out CacheItem? item))
                            yield return new KeyValuePair<TKey, TValue>(key, item.Value);
                    }
                }

                break;

            case EvictingDictionaryPolicy.RandomReplacement:
#if NETSTANDARD2_0
                foreach (var pair in _store)
                {
                    ThrowIfVersionChanged(version);
                    yield return new KeyValuePair<TKey, TValue>(pair.Key, pair.Value.Value);
                }
#else
                foreach ((TKey key, CacheItem item) in _store)
                {
                    ThrowIfVersionChanged(version);
                    yield return new KeyValuePair<TKey, TValue>(key, item.Value);
                }
#endif
                break;

            default:
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ResourceStrings.Op_Invalid_UnknownEvictionPolicy, Policy));
        }
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if <paramref name="capturedVersion" /> no longer matches the
    /// current <see cref="_version" />, signaling that the dictionary was modified during enumeration.
    /// </summary>
    /// <param name="capturedVersion">The version observed at the start of enumeration.</param>
    private void ThrowIfVersionChanged(int capturedVersion)
    {
        if (_version != capturedVersion)
            throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionModified);
    }

    /// <summary>
    /// Finds the next candidate for eviction using the Second-Chance algorithm. Items with their second-chance flag set
    /// are moved to the end of the list and cleared. If no eligible item is found, the oldest item is returned.
    /// </summary>
    /// <returns>The key to evict according to the Second-Chance strategy.</returns>
    /// <remarks>
    /// Called only from <see cref="EvictOne" /> after that method has already verified the order list is non-empty, so
    /// no defensive empty-list checks are performed here. The clock sweep preserves the total node count by removing
    /// and re-appending each cycled item, so <c>_order.First</c> is guaranteed to be non-null after the loop completes.
    /// </remarks>
    private TKey GetSecondChanceCandidate()
    {
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

            // Clock algorithm: clear the reference bit and cycle the item to the tail, giving it one extra pass before eviction.
            item.SecondChance = false;
            _order.Remove(current);
            item.Node = _order.AddLast(key);
        }

        return _order.First!.Value;
    }

    /// <summary>
    /// Returns the key that would be selected next by the Second-Chance algorithm, without modifying any state.
    /// </summary>
    /// <returns>
    /// The candidate key for eviction, or the oldest key if all items have their second-chance flag set.
    /// </returns>
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
    /// Removes the specified key from the LeastFrequentlyUsed frequency bucket for the given frequency. Cleans up the
    /// bucket if it becomes empty.
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
        switch (Policy)
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
