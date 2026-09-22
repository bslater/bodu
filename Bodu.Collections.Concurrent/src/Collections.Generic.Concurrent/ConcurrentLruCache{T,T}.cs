// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCache{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Bodu.Collections.Generic.Concurrent;

/// <summary>
/// Provides a thread-safe, fixed-capacity cache with lock-free reads and a segmented pseudo-LRU eviction policy.
/// </summary>
/// <typeparam name="TKey">
/// Specifies the type of keys in the cache (constraint: <c>where TKey : notnull</c>).
/// </typeparam>
/// <typeparam name="TValue">Specifies the type of values in the cache.</typeparam>
/// <remarks>
/// <para>
/// <see cref="ConcurrentLruCache{TKey, TValue}" /> is the read-optimized bounded cache of the package: entries live in
/// a <see cref="ConcurrentDictionary{TKey, TValue}" /> and recency is tracked by three internal FIFO queues — <em>hot</em>
/// (new arrivals), <em>warm</em> (entries that have proven reuse), and <em>cold</em> (entries one unaccessed pass from
/// eviction). A successful lookup is entirely lock-free: it performs the dictionary probe and sets the entry's accessed
/// flag with a single volatile write. Queue maintenance — promoting accessed entries, demoting idle ones, evicting from
/// the cold end — is amortized onto writers: after each mutating operation at most one thread briefly cycles the queues
/// while contending writers proceed without waiting.
/// </para>
/// <para>
/// The policy is a <em>pseudo</em>-LRU: it approximates least-recently-used ordering the way production caches
/// (BitFaster.Caching, Caffeine) do, favoring read throughput over exact recency ordering. For exact, selectable
/// eviction policies — at the cost of taking a segment lock on every operation, including reads — use
/// <see cref="ConcurrentEvictingDictionary{TKey, TValue}" />. The two types also differ in their capacity contract:
/// this cache's <see cref="Count" /> may <em>transiently</em> exceed <see cref="Capacity" /> by at most the number of
/// concurrently in-flight writers. That bound is enforced by write back-pressure: a writer that observes the cache over
/// capacity converges maintenance before returning, so sustained insert pressure against a full cache serializes writes
/// on the maintenance lock while reads stay lock-free. Explicitly removed entries keep their queue slot until the
/// maintenance cycle drains them, so internal occupancy accounting is eventual rather than instantaneous.
/// </para>
/// <para>
/// <see cref="GetOrAdd(TKey, Func{TKey, TValue})" /> follows
/// <see cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})" /> semantics: racing callers may
/// each invoke the factory, and exactly one produced value is stored and returned to everyone. This differs from
/// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})" />, whose factory is
/// single-flight; choose that type when a duplicated factory invocation is expensive enough to matter more than
/// lock-free reads.
/// </para>
/// <para>
/// Hit and miss telemetry is maintained on cache-line-padded striped counters so the lock-free read path never contends
/// on a shared counter; <see cref="HitCount" />, <see cref="MissCount" />, and <see cref="HitRatio" /> aggregate on
/// demand. Evictions raise the post-commit <see cref="ItemEvicted" /> event after all internal coordination has been
/// released, with handler exceptions suppressed (except <see cref="OutOfMemoryException" />) — the package's
/// established concurrent eviction-event contract.
/// </para>
/// <para>
/// Enumeration and <see cref="ToArray" /> observe a coherent point-in-time snapshot of the backing dictionary and never
/// throw because of concurrent modification; the order of entries is unspecified. Time-based expiration is not
/// supported in this version.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var cache = new ConcurrentLruCache<string, byte[]>(capacity: 1024);
///
/// Parallel.ForEach(requests, request =>
/// {
///     byte[] payload = cache.GetOrAdd(request.Key, key => LoadPayload(key));
///     Serve(request, payload);
/// });
///
/// Console.WriteLine($"Hit ratio: {cache.HitRatio:P1}");
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Count ≈ {ApproximateCount}, Capacity = {Capacity}")]
[DebuggerTypeProxy(typeof(ConcurrentLruCacheDebugView<,>))]
public sealed partial class ConcurrentLruCache<TKey, TValue>
    where TKey : notnull
{
    /// <summary>The capacity used when the cache is constructed without an explicit capacity.</summary>
    private const int DefaultCapacity = 16;

    /// <summary>The equality comparer used for key identity and hash-table lookup. Never <see langword="null" />.</summary>
    private readonly IEqualityComparer<TKey> _comparer;

    /// <summary>The backing store mapping each key to its cache node. Reads are lock-free.</summary>
    private readonly ConcurrentDictionary<TKey, Node> _dictionary;

    /// <summary>The capacity slice of the hot queue, where new entries land.</summary>
    private readonly int _hotCapacity;

    /// <summary>The queue of recently added entries awaiting their first maintenance verdict.</summary>
    private readonly ConcurrentQueue<Node> _hotQueue = new();

    /// <summary>The counter of hits taken on the lock-free read path; striped to avoid reader contention.</summary>
    private readonly StripedLongCounter _hitCounter = new();

    /// <summary>The monitor that serializes queue maintenance. Within capacity, writers contend with TryEnter and losers skip the cycle; over capacity, writers block on it to apply back-pressure.</summary>
    private readonly object _maintenanceLock = new();

    /// <summary>The counter of misses taken on the lock-free read path; striped to avoid reader contention.</summary>
    private readonly StripedLongCounter _missCounter = new();

    /// <summary>The capacity slice of the cold queue, whose unaccessed entries are the eviction victims.</summary>
    private readonly int _coldCapacity;

    /// <summary>The queue of entries one unaccessed pass away from eviction.</summary>
    private readonly ConcurrentQueue<Node> _coldQueue = new();

    /// <summary>The capacity slice of the warm queue, holding entries with proven reuse.</summary>
    private readonly int _warmCapacity;

    /// <summary>The queue of entries that were accessed while hot or cold and have earned protection.</summary>
    private readonly ConcurrentQueue<Node> _warmQueue = new();

    /// <summary>The occupancy counter of the cold queue, including not-yet-drained dead nodes.</summary>
    private int _coldCount;

    /// <summary>The cumulative number of entries evicted by capacity pressure, updated under the effectively single-writer eviction dispatch.</summary>
    private long _evictionCount;

    /// <summary>The occupancy counter of the hot queue, including not-yet-drained dead nodes.</summary>
    private int _hotCount;

    /// <summary>The lock-free count of live entries, updated on every successful add, removal, and eviction. Drives the maintenance cycle's hard capacity enforcement and <see cref="ApproximateCount" />.</summary>
    private int _liveCount;

    /// <summary>The occupancy counter of the warm queue, including not-yet-drained dead nodes.</summary>
    private int _warmCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentLruCache{TKey, TValue}" /> class with the default
    /// capacity and key comparer.
    /// </summary>
    public ConcurrentLruCache()
        : this(DefaultCapacity, (IEqualityComparer<TKey>?)null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentLruCache{TKey, TValue}" /> class with the specified
    /// capacity and the default key comparer.
    /// </summary>
    /// <param name="capacity">The maximum number of entries the cache aims to retain. Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    public ConcurrentLruCache(int capacity)
        : this(capacity, (IEqualityComparer<TKey>?)null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentLruCache{TKey, TValue}" /> class with the specified
    /// capacity and key comparer.
    /// </summary>
    /// <param name="capacity">The maximum number of entries the cache aims to retain. Must be positive.</param>
    /// <param name="comparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// The capacity is partitioned into hot, warm, and cold queue slices of roughly one third each; every slice the
    /// partition can afford is at least one entry, and the slices sum to <paramref name="capacity" /> exactly.
    /// </remarks>
    public ConcurrentLruCache(int capacity, IEqualityComparer<TKey>? comparer)
    {
        ThrowHelper.ThrowIfZeroOrNegative(capacity);

        Capacity = capacity;
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
        _dictionary = new ConcurrentDictionary<TKey, Node>(_comparer);

        (_hotCapacity, _warmCapacity, _coldCapacity) = ComputePartition(capacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentLruCache{TKey, TValue}" /> class with the specified
    /// capacity, seeded with the entries of the specified sequence, using the default key comparer.
    /// </summary>
    /// <param name="capacity">The maximum number of entries the cache aims to retain. Must be positive.</param>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    public ConcurrentLruCache(int capacity, IEnumerable<KeyValuePair<TKey, TValue>> source)
        : this(capacity, source, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentLruCache{TKey, TValue}" /> class with the specified
    /// capacity and key comparer, seeded with the entries of the specified sequence.
    /// </summary>
    /// <param name="capacity">The maximum number of entries the cache aims to retain. Must be positive.</param>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <param name="comparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// If more elements are provided than the capacity allows, earlier entries are evicted per the pseudo-LRU policy as
    /// the copy proceeds.
    /// </remarks>
    public ConcurrentLruCache(int capacity, IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? comparer)
        : this(capacity, comparer)
    {
        ThrowHelper.ThrowIfNull(source);

        foreach (KeyValuePair<TKey, TValue> kvp in source)
            Add(kvp.Key, kvp.Value);
    }

    /// <summary>
    /// Occurs immediately <b>after</b> an entry has been evicted by capacity pressure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Handlers run on the thread whose write triggered the maintenance cycle, <em>after</em> the maintenance lock has
    /// been released, so a handler can safely call back into the cache. The key and value provided are no longer
    /// present by the time the handler observes them.
    /// </para>
    /// <para>
    /// Each subscriber is invoked independently, and ordinary handler exceptions are caught and suppressed — only
    /// <see cref="OutOfMemoryException" /> propagates. There is no pre-removal event: a committed concurrent eviction
    /// cannot be unwound.
    /// </para>
    /// <para>
    /// Explicit removals via <see cref="TryRemove" />, replacements via <see cref="Add(TKey, TValue)" /> or the indexer
    /// setter, and bulk resets via <see cref="Clear" /> are not evictions and do not raise this event.
    /// </para>
    /// </remarks>
    public event Action<TKey, TValue>? ItemEvicted;

    /// <summary>
    /// Gets an approximate entry count without touching the backing dictionary.
    /// </summary>
    /// <value>
    /// A lock-free counter of live entries, updated on every successful add, removal, and eviction. Under concurrent
    /// mutation the value may momentarily differ from <see cref="Count" /> by the number of in-flight operations; the
    /// two agree once mutation quiesces.
    /// </value>
    /// <remarks>
    /// Use this property for fast size estimates on hot paths; prefer <see cref="Count" /> for the exact number of live
    /// entries at a coherent instant.
    /// </remarks>
    public int ApproximateCount =>
        Volatile.Read(ref _liveCount);

    /// <summary>
    /// Gets the number of entries the cache aims to retain.
    /// </summary>
    /// <value>
    /// The configured capacity. <see cref="Count" /> may transiently exceed this value by at most the number of
    /// concurrently in-flight writers; maintenance converges the count back under the bound.
    /// </value>
    public int Capacity { get; }

    /// <summary>
    /// Gets the equality comparer used for key identity and hash-table lookup.
    /// </summary>
    /// <value>The active equality comparer.</value>
    public IEqualityComparer<TKey> Comparer => _comparer;

    /// <summary>
    /// Gets the number of live entries currently stored in the cache.
    /// </summary>
    /// <value>The exact count of entries in the backing dictionary at the moment of the call.</value>
    /// <remarks>
    /// Reading this property takes the backing dictionary's internal locks briefly. The value may transiently exceed
    /// <see cref="Capacity" /> by at most the number of concurrently in-flight writers.
    /// </remarks>
    public int Count => _dictionary.Count;

    /// <summary>
    /// Gets the cumulative number of entries evicted by capacity pressure since creation or the last
    /// <see cref="Clear" />.
    /// </summary>
    /// <value>The eviction count. Explicit removals and replacements are not evictions.</value>
    public long EvictionCount => Interlocked.Read(ref _evictionCount);

    /// <summary>
    /// Gets the cumulative number of successful lookups since creation or the last <see cref="Clear" />.
    /// </summary>
    /// <value>The hit count, aggregated from striped per-processor counters; exact once lookups have quiesced.</value>
    public long HitCount => _hitCounter.Sum();

    /// <summary>
    /// Gets the fraction of lookups that were hits.
    /// </summary>
    /// <value><c>HitCount / (HitCount + MissCount)</c>, or <c>0.0</c> when no lookups have been recorded.</value>
    /// <remarks>
    /// Lookups are <see cref="TryGetValue" />, the indexer getter, and <see cref="GetOrAdd(TKey, TValue)" /> /
    /// <see cref="GetOrAdd(TKey, Func{TKey, TValue})" />. <see cref="ContainsKey" /> is a pure probe and is not
    /// counted.
    /// </remarks>
    public double HitRatio
    {
        get
        {
            long hits = HitCount;
            long total = hits + MissCount;

            return total == 0 ? 0.0 : (double)hits / total;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the cache currently stores no live entries.
    /// </summary>
    /// <value><see langword="true" /> if the backing dictionary is empty; otherwise, <see langword="false" />.</value>
    public bool IsEmpty => _dictionary.IsEmpty;

    /// <summary>
    /// Gets the cumulative number of failed lookups since creation or the last <see cref="Clear" />.
    /// </summary>
    /// <value>The miss count, aggregated from striped per-processor counters; exact once lookups have quiesced.</value>
    public long MissCount => _missCounter.Sum();

    /// <summary>
    /// Gets the capacity slice of the cold queue. Exposed to the test assembly so partition invariants can be asserted
    /// directly.
    /// </summary>
    internal int ColdCapacity => _coldCapacity;

    /// <summary>
    /// Gets the current occupancy of the cold queue, including not-yet-drained dead nodes. Exposed to the test
    /// assembly.
    /// </summary>
    internal int ColdCount => Volatile.Read(ref _coldCount);

    /// <summary>
    /// Gets the capacity slice of the hot queue. Exposed to the test assembly so partition invariants can be asserted
    /// directly.
    /// </summary>
    internal int HotCapacity => _hotCapacity;

    /// <summary>
    /// Gets the current occupancy of the hot queue, including not-yet-drained dead nodes. Exposed to the test assembly.
    /// </summary>
    internal int HotCount => Volatile.Read(ref _hotCount);

    /// <summary>
    /// Gets the capacity slice of the warm queue. Exposed to the test assembly so partition invariants can be asserted
    /// directly.
    /// </summary>
    internal int WarmCapacity => _warmCapacity;

    /// <summary>
    /// Gets the current occupancy of the warm queue, including not-yet-drained dead nodes. Exposed to the test
    /// assembly.
    /// </summary>
    internal int WarmCount => Volatile.Read(ref _warmCount);

    /// <summary>
    /// Adds the specified key and value to the cache, or replaces the value if the key already exists.
    /// </summary>
    /// <param name="key">The key of the element to add or update.</param>
    /// <param name="value">The value to associate with <paramref name="key" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// Replacing an existing key installs a fresh entry: the previous node is retired and the new entry starts its
    /// recency life in the hot queue. This differs from <see cref="ConcurrentEvictingDictionary{TKey, TValue}" />,
    /// which preserves eviction metadata on in-place replacement, and is a consequence of values being immutable on a
    /// lock-free read path. A replacement is not an eviction and does not raise <see cref="ItemEvicted" />.
    /// </para>
    /// <para>
    /// Adding a new key may push a queue over its slice; the triggered maintenance cycle then evicts from the cold end,
    /// raising <see cref="ItemEvicted" /> for each evicted entry.
    /// </para>
    /// </remarks>
    public void Add(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        while (true)
        {
            if (_dictionary.TryGetValue(key, out Node? existing))
            {
                var replacement = new Node(key, value, Node.StateHot);
                if (_dictionary.TryUpdate(key, replacement, existing))
                {
                    // Retire the old node; its stale queue slot drains on a later maintenance pass.
                    existing.MarkRemoved();
                    Enqueue(_hotQueue, ref _hotCount, replacement);
                    Maintain();
                    return;
                }

                continue;
            }

            var created = new Node(key, value, Node.StateHot);
            if (_dictionary.TryAdd(key, created))
            {
                Interlocked.Increment(ref _liveCount);
                Enqueue(_hotQueue, ref _hotCount, created);
                Maintain();
                return;
            }
        }
    }

    /// <summary>
    /// Removes all entries present at the time of the call and resets the telemetry counters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sweep runs under the maintenance lock: every entry currently in the backing dictionary is removed with a
    /// node-conditional remove, and the recency queues are drained of the resulting dead nodes. Entries added
    /// concurrently with the sweep may survive it — unlike
    /// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Clear" />, this cache has no global write lock to make the
    /// reset atomic against writers.
    /// </para>
    /// <para>
    /// A clear is a bulk reset, not an eviction — <see cref="ItemEvicted" /> is not raised for the removed entries.
    /// <see cref="HitCount" />, <see cref="MissCount" />, and <see cref="EvictionCount" /> are reset to zero.
    /// </para>
    /// </remarks>
    public void Clear()
    {
        lock (_maintenanceLock)
        {
            foreach (KeyValuePair<TKey, Node> pair in _dictionary)
            {
                if (_dictionary.TryRemove(pair))
                {
                    Interlocked.Decrement(ref _liveCount);
                    pair.Value.MarkRemoved();
                }
            }

            DrainDeadNodes(_hotQueue, ref _hotCount);
            DrainDeadNodes(_warmQueue, ref _warmCount);
            DrainDeadNodes(_coldQueue, ref _coldCount);

            _hitCounter.Reset();
            _missCounter.Reset();
            Interlocked.Exchange(ref _evictionCount, 0);
        }
    }

    /// <summary>
    /// Determines whether the cache contains an entry for the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>
    /// <see langword="true" /> if an entry exists for <paramref name="key" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This is a pure, lock-free probe: it does not set the entry's accessed flag and does not contribute to
    /// <see cref="HitCount" /> or <see cref="MissCount" />.
    /// </remarks>
    public bool ContainsKey(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        return _dictionary.ContainsKey(key);
    }

    /// <summary>
    /// Adds a key/value pair to the cache if the key does not already exist, and returns either the existing or the
    /// newly added value.
    /// </summary>
    /// <param name="key">The key of the element to get or add.</param>
    /// <param name="value">The value to add when the key is absent.</param>
    /// <returns>
    /// The existing value when an entry for <paramref name="key" /> is present; otherwise <paramref name="value" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// A hit is lock-free, sets the entry's accessed flag, and counts toward <see cref="HitCount" />; a successful add
    /// counts toward <see cref="MissCount" /> and may trigger evictions via the maintenance cycle.
    /// </remarks>
    public TValue GetOrAdd(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        while (true)
        {
            if (_dictionary.TryGetValue(key, out Node? node))
            {
                if (!node.WasAccessed)
                    node.WasAccessed = true;

                _hitCounter.Increment();
                return node.Value;
            }

            var created = new Node(key, value, Node.StateHot);
            if (_dictionary.TryAdd(key, created))
            {
                Interlocked.Increment(ref _liveCount);
                _missCounter.Increment();
                Enqueue(_hotQueue, ref _hotCount, created);
                Maintain();
                return value;
            }
        }
    }

    /// <summary>
    /// Adds a key/value pair produced by the specified value factory if the key does not already exist, and returns
    /// either the existing or the newly created value.
    /// </summary>
    /// <param name="key">The key of the element to get or add.</param>
    /// <param name="valueFactory">The function used to generate a value for the key when it is absent.</param>
    /// <returns>
    /// The existing value when an entry for <paramref name="key" /> is present; otherwise the value produced by
    /// <paramref name="valueFactory" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="valueFactory" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The factory runs outside all internal coordination, so racing callers missing on the same key may each invoke
    /// it; exactly one produced value is stored, and every caller returns the stored value. Values produced by losing
    /// invocations are discarded. This matches
    /// <see cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})" /> and keeps the read path
    /// free of per-hit indirection; when the factory must run at most once per key, use
    /// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})" />, whose factory is
    /// single-flight.
    /// </para>
    /// <para>
    /// If the factory throws, nothing is added and the exception propagates.
    /// </para>
    /// </remarks>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(valueFactory);

        while (true)
        {
            if (_dictionary.TryGetValue(key, out Node? node))
            {
                if (!node.WasAccessed)
                    node.WasAccessed = true;

                _hitCounter.Increment();
                return node.Value;
            }

            var created = new Node(key, valueFactory(key), Node.StateHot);
            if (_dictionary.TryAdd(key, created))
            {
                Interlocked.Increment(ref _liveCount);
                _missCounter.Increment();
                Enqueue(_hotQueue, ref _hotCount, created);
                Maintain();
                return created.Value;
            }
        }
    }

    /// <summary>
    /// Copies the cache's entries into a new array.
    /// </summary>
    /// <returns>
    /// A new array containing a coherent point-in-time snapshot of the cache's entries, or an empty array if the cache
    /// is empty. The order of entries is unspecified.
    /// </returns>
    /// <remarks>
    /// The snapshot is taken by the backing dictionary under its internal locks and is unaffected by concurrent
    /// modifications made afterward. Taking a snapshot does not set accessed flags and does not contribute to the
    /// telemetry counters.
    /// </remarks>
    public KeyValuePair<TKey, TValue>[] ToArray()
    {
        KeyValuePair<TKey, Node>[] snapshot = _dictionary.ToArray();
        if (snapshot.Length == 0)
            return [];

        var result = new KeyValuePair<TKey, TValue>[snapshot.Length];
        for (int i = 0; i < snapshot.Length; i++)
            result[i] = new KeyValuePair<TKey, TValue>(snapshot[i].Key, snapshot[i].Value.Value);

        return result;
    }

    /// <summary>
    /// Attempts to add the specified key and value to the cache, without replacing an existing entry.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value to associate with <paramref name="key" />.</param>
    /// <returns>
    /// <see langword="true" /> if the entry was added; <see langword="false" /> if an entry for <paramref name="key" />
    /// already exists.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// A rejected add does not set the existing entry's accessed flag and does not contribute to the telemetry
    /// counters. A successful add may trigger evictions via the maintenance cycle.
    /// </remarks>
    public bool TryAdd(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        var created = new Node(key, value, Node.StateHot);
        if (_dictionary.TryAdd(key, created))
        {
            Interlocked.Increment(ref _liveCount);
            Enqueue(_hotQueue, ref _hotCount, created);
            Maintain();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with the specified key, if the key is found; otherwise,
    /// the default value for the type of the value parameter.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the cache contains an entry for the specified key; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// The lookup is entirely lock-free: a hit performs the dictionary probe and one volatile write of the entry's
    /// accessed flag (skipped when already set, so repeated hot-key reads do not write at all). A hit counts toward
    /// <see cref="HitCount" />, a miss toward <see cref="MissCount" />.
    /// </remarks>
    public bool TryGetValue(TKey key, out TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        if (_dictionary.TryGetValue(key, out Node? node))
        {
            // Check-first keeps repeated reads of a hot key from re-dirtying the flag's cache line.
            if (!node.WasAccessed)
                node.WasAccessed = true;

            value = node.Value;
            _hitCounter.Increment();
            return true;
        }

        value = default!;
        _missCounter.Increment();
        return false;
    }

    /// <summary>
    /// Attempts to remove the entry with the specified key, returning the removed value.
    /// </summary>
    /// <param name="key">The key of the entry to remove.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the value of the removed entry; otherwise, the default value
    /// for the type.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if an entry was found and removed; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// The key is reusable immediately, but the removed entry's dead queue node keeps its occupancy slot until a later
    /// maintenance pass drains it. An explicit removal is not an eviction — it does not raise
    /// <see cref="ItemEvicted" /> and does not increment <see cref="EvictionCount" />.
    /// </remarks>
    public bool TryRemove(TKey key, out TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        if (_dictionary.TryRemove(key, out Node? node))
        {
            Interlocked.Decrement(ref _liveCount);
            node.MarkRemoved();
            value = node.Value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Computes the hot/warm/cold capacity partition for a total capacity: roughly one third each, with the remainder
    /// assigned to warm, degenerating gracefully for tiny capacities.
    /// </summary>
    /// <param name="capacity">The total capacity. Must be positive.</param>
    /// <returns>
    /// The hot, warm, and cold slices; each is at least the minimum the total can afford, and they sum to
    /// <paramref name="capacity" /> exactly.
    /// </returns>
    internal static (int Hot, int Warm, int Cold) ComputePartition(int capacity)
    {
        if (capacity == 1)
            return (1, 0, 0);

        if (capacity == 2)
            return (1, 0, 1);

        int hot = capacity / 3;
        int cold = capacity / 3;

        return (hot, capacity - hot - cold, cold);
    }

    /// <summary>
    /// Dequeues up to the currently observed number of nodes from a queue, dropping dead ones and re-enqueuing
    /// survivors. Used by <see cref="Clear" /> to release the occupancy held by dead nodes; the bound keeps the drain
    /// from spinning against concurrent writers.
    /// </summary>
    /// <param name="queue">The queue to drain.</param>
    /// <param name="count">The queue's occupancy counter field.</param>
    private static void DrainDeadNodes(ConcurrentQueue<Node> queue, ref int count)
    {
        int limit = Volatile.Read(ref count);
        for (int i = 0; i < limit; i++)
        {
            if (!TryDequeue(queue, ref count, out Node? node))
                break;

            if (node.State != Node.StateRemoved)
                Enqueue(queue, ref count, node);
        }
    }

    /// <summary>
    /// Dispatches post-commit eviction notifications for the entries in the buffer and adds them to
    /// <see cref="EvictionCount" />. Must be called after the maintenance lock has been released.
    /// </summary>
    /// <param name="evicted">
    /// The eviction buffer, or <see langword="null" /> when the maintenance run evicted nothing.
    /// </param>
    private void OnEvicted(List<KeyValuePair<TKey, TValue>>? evicted)
    {
        if (evicted is null || evicted.Count == 0)
            return;

        Interlocked.Add(ref _evictionCount, evicted.Count);

        Action<TKey, TValue>? onEvicted = ItemEvicted;
        if (onEvicted is null)
            return;

        // Each handler is guarded independently so a throwing subscriber cannot prevent subsequent subscribers from
        // receiving the notification; the eviction is already committed and cannot be undone.
        Delegate[] handlers = onEvicted.GetInvocationList();
        foreach (KeyValuePair<TKey, TValue> pair in evicted)
        {
            foreach (Action<TKey, TValue> handler in handlers)
            {
                try
                {
                    handler(pair.Key, pair.Value);
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                { /* isolate ordinary handler failures; let process-fatal exceptions propagate */
                }
            }
        }
    }
}
