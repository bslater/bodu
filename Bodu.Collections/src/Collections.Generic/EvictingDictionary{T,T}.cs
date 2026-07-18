// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionary{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;

using Bodu.Collections.Generic.Internal;

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
/// that already exists replaces the existing entry's value rather than throwing — this differs from
/// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" />'s strict <c>Add</c> semantics. The replacement
/// counts as a touch against the eviction policy: recency-based policies move the entry to the most-recently-used
/// position, LeastFrequentlyUsed increments its accumulated frequency, and SecondChance marks it recently referenced.
/// The entry is not treated as newly inserted, and its accumulated metadata is not reset.
/// </para>
/// <para>
/// Supplying an <see cref="EvictingDictionaryExpiration" /> at construction adds time-based expiry orthogonal to the
/// capacity policy: entries carry a time-to-live (a per-dictionary default and/or per-entry overrides), expired entries
/// are invisible to lookups and enumeration even before they are physically removed, and removal happens lazily — when
/// an access touches an expired key, when capacity pressure purges expired entries ahead of a policy eviction, or when
/// <see cref="EvictingDictionary{TKey, TValue}.RemoveExpired" /> is called explicitly. Note that
/// <see cref="EvictingDictionary{TKey, TValue}.Count" /> reports the raw stored count <em>including</em>
/// expired-but-unpurged entries; call <see cref="EvictingDictionary{TKey, TValue}.RemoveExpired" /> to reconcile.
/// Without an expiration configuration the dictionary performs no clock reads.
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
    /// <summary>The capacity used when the dictionary is constructed without an explicit capacity.</summary>
    private const int DefaultCapacity = 16;

    /// <summary>The eviction policy used when the dictionary is constructed without an explicit policy.</summary>
    private const EvictingDictionaryPolicy DefaultPolicy = EvictingDictionaryPolicy.LeastRecentlyUsed;

    /// <summary>The equality comparer used for key identity and hash-table lookup.</summary>
    private readonly IEqualityComparer<TKey> _comparer;

    /// <summary>The shared eviction-policy engine that owns the store and policy tracking structures and implements candidate selection, touch re-linking, and frequency-bucket bookkeeping.</summary>
    private readonly EvictionPolicyCore<TKey, TValue> _core;

    /// <summary>Maps access frequency to the keys observed at that frequency, used by the least-frequently-used policy. Alias of the engine's structure for direct enumeration access.</summary>
    private readonly SortedDictionary<int, LinkedList<TKey>> _frequencyList;

    /// <summary>The backing store mapping each key to its cached value and bookkeeping metadata. Alias of the engine's store for direct lookup access.</summary>
    private readonly Dictionary<TKey, EvictionEntry<TKey, TValue>> _store;

    /// <summary>Tracks key recency ordering, used by the least-recently-used policy. Alias of the engine's structure for direct enumeration access.</summary>
    private readonly LinkedList<TKey> _order;

    /// <summary>Indicates whether an eviction is currently in progress, used to suppress re-entrant bookkeeping.</summary>
    private bool _isEvicting;

    /// <summary>The cached key collection returned by the keys accessor, allocated on first access.</summary>
    private KeyCollection? _keys;

    /// <summary>The cached value collection returned by the values accessor, allocated on first access.</summary>
    private ValueCollection? _values;

    /// <summary>The modification counter used to detect concurrent mutation during enumeration.</summary>
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
        : this(DefaultCapacity, DefaultPolicy, null, null) { }

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
        : this(capacity, DefaultPolicy, null, null) { }

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
        : this(capacity, policy, null, null) { }

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
        : this(capacity, policy, comparer, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with the specified
    /// capacity and time-based expiration, using the default eviction policy and key comparer.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="expiration">
    /// The time-based expiration configuration, or <see langword="null" /> to disable expiry.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// Creates an empty dictionary with the specified capacity and expiration configuration, using
    /// <see cref="DefaultPolicy" /> for eviction when capacity is exceeded and the default key comparer.
    /// </remarks>
    public EvictingDictionary(int capacity, EvictingDictionaryExpiration? expiration)
        : this(capacity, DefaultPolicy, null, expiration) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with the specified
    /// capacity, eviction policy, and time-based expiration, using the default key comparer.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <param name="expiration">
    /// The time-based expiration configuration, or <see langword="null" /> to disable expiry.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// Creates an empty dictionary with the specified capacity, eviction policy, and expiration configuration, using
    /// the default key comparer ( <see cref="EqualityComparer{TKey}.Default" />).
    /// </remarks>
    public EvictingDictionary(int capacity, EvictingDictionaryPolicy policy, EvictingDictionaryExpiration? expiration)
        : this(capacity, policy, null, expiration) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with the specified
    /// capacity, eviction policy, key comparer, and time-based expiration.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <param name="comparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <param name="expiration">
    /// The time-based expiration configuration, or <see langword="null" /> to disable expiry.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Creates an empty dictionary with the specified capacity, eviction policy, key comparer, and expiration
    /// configuration.
    /// </para>
    /// <para>
    /// Initializes the internal storage for key/value pairs, and, where applicable, the eviction tracking structure:
    /// FIFO, LRU, MRU, and SecondChance use a linked list; LFU uses a sorted dictionary of frequency lists;
    /// RandomReplacement does not require additional tracking. When <paramref name="expiration" /> is
    /// <see langword="null" /> the dictionary performs no clock reads and behaves as a capacity-only cache.
    /// </para>
    /// </remarks>
    public EvictingDictionary(int capacity, EvictingDictionaryPolicy policy, IEqualityComparer<TKey>? comparer, EvictingDictionaryExpiration? expiration)
    {
        ThrowHelper.ThrowIfZeroOrNegative(capacity);

        Capacity = capacity;
        Policy = policy;
        _comparer = comparer ?? EqualityComparer<TKey>.Default;

        Expiration = expiration;
        _timeProvider = expiration?.TimeProvider;
        _slidingExpiration = expiration?.Kind == EvictingDictionaryExpirationKind.Sliding;

        // The engine creates the tracking structures the policy requires; the aliases below give the
        // enumeration and lookup paths direct field access to the same objects.
        _core = new EvictionPolicyCore<TKey, TValue>(policy, _comparer);
        _store = _core.Store;
        _order = _core.Order!;
        _frequencyList = _core.FrequencyList!;
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
        : this(capacity, source, policy, comparer, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictingDictionary{TKey, TValue}" /> class, with elements copied
    /// from the specified sequence, using the specified capacity, eviction policy, key comparer, and time-based
    /// expiration.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <param name="comparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <param name="expiration">
    /// The time-based expiration configuration, or <see langword="null" /> to disable expiry.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// Uses the specified capacity, eviction policy, key comparer, and expiration configuration. Copied entries receive
    /// the default <see cref="EvictingDictionaryExpiration.TimeToLive" /> (when configured); if more elements are
    /// provided than the capacity allows, entries are evicted according to the policy.
    /// </remarks>
    public EvictingDictionary(int capacity, IEnumerable<KeyValuePair<TKey, TValue>> source, EvictingDictionaryPolicy policy, IEqualityComparer<TKey>? comparer, EvictingDictionaryExpiration? expiration)
        : this(capacity, policy, comparer, expiration)
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
    /// <b>SecondChance</b>: returns the first key that has not been accessed recently; falls back to FIFO if all have
    /// second chances.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// This is a pure read that never touches the clock: when time-based expiration is configured it may return an
    /// expired-but-unpurged key, even though capacity pressure purges expired entries before consulting the policy.
    /// </para>
    /// </remarks>
    public TKey? PeekEvictionCandidate() =>
        _core.PeekEvictionCandidate();

    /// <summary>
    /// Marks the specified key as recently accessed without retrieving its value. If the eviction policy involves usage
    /// tracking, this updates the internal usage metadata.
    /// </summary>
    /// <param name="key">The key to touch.</param>
    /// <returns>
    /// <see langword="true" /> if the key exists and was marked as accessed; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// When time-based expiration is configured, an expired entry counts as absent: it is lazily removed (raising the
    /// eviction events) and <see langword="false" /> is returned. <see cref="Touch" /> affects only the capacity-policy
    /// metadata — it does not refresh a sliding expiration deadline; use a read access ( <see cref="TryGetValue" /> or
    /// the indexer getter) to slide. Like <see cref="ContainsKey" />, it is a pure read with respect to the deadline.
    /// </remarks>
    public bool Touch(TKey key)
    {
        if (TryGetLiveItem(key, slide: false, out EvictionEntry<TKey, TValue>? item))
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
            throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.KeyNotFound_Dictionary, key));
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
        // The engine reports success explicitly rather than via a null sentinel because default(TKey) may itself be
        // a valid key when TKey is a value type.
        if (_core.TrySelectEvictionCandidate(out TKey keyToRemove) && _store.TryGetValue(keyToRemove, out EvictionEntry<TKey, TValue>? item))
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
        if (_store.Count >= Capacity) throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.Op_Invalid_EvictionProducedNoCandidate, Policy));
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if called while an eviction event is being dispatched. Prevents
    /// handlers from re-entering the dictionary and corrupting its state.
    /// </summary>
    private void ThrowIfEvicting()
    {
        if (_isEvicting) throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_ReentrancyNotAllowed);
    }

    /// <summary>
    /// Handles internal usage tracking logic based on the current eviction policy, delegating the policy bookkeeping to
    /// the shared engine.
    /// </summary>
    /// <param name="key">The key that was accessed.</param>
    /// <param name="item">The associated cache item for the key.</param>
    /// <remarks>
    /// Touches that structurally reposition the entry (LeastRecentlyUsed, MostRecentlyUsed, LeastFrequentlyUsed)
    /// increment <see cref="_version" /> so in-flight enumerators fail fast instead of silently observing a reordered —
    /// or, for the backwards-walked MostRecentlyUsed order, truncated — sequence. SecondChance only flips the reference
    /// flag, which does not disturb enumeration order.
    /// </remarks>
    private void TouchInternal(TKey key, EvictionEntry<TKey, TValue> item)
    {
        if (_core.Touch(key, item))
            _version++;
    }
}
