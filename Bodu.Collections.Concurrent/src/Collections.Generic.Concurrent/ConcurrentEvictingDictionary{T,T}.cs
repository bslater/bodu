// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionary{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Concurrent;

/// <summary>
/// Provides a thread-safe, fixed-capacity dictionary that automatically removes entries based on a chosen eviction
/// policy, such as First-In-First-Out (FirstInFirstOut), Least Recently Used (LeastRecentlyUsed), or Least Frequently
/// Used (LeastFrequentlyUsed).
/// </summary>
/// <typeparam name="TKey">
/// Specifies the type of keys in the dictionary (constraint: <c>where TKey : notnull</c>).
/// </typeparam>
/// <typeparam name="TValue">Specifies the type of values in the dictionary.</typeparam>
/// <remarks>
/// <para>
/// <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> is the thread-safe variant of
/// <see cref="EvictingDictionary{TKey, TValue}" />. It partitions its capacity across a fixed number of internal <em>segments</em>,
/// each guarded by its own monitor: the key comparer's hash routes every key to exactly one segment, so operations on
/// keys owned by different segments proceed in parallel. Reads are writes in an evicting cache — a lookup repositions
/// the key for recency-tracked policies — so even <see cref="TryGetValue" /> takes its segment's lock; the striping
/// keeps that contention local.
/// </para>
/// <para>
/// Eviction order is <em>exact within a segment and approximate globally</em>: each segment runs the configured
/// <see cref="EvictingDictionaryPolicy" /> over its own slice of the capacity, so a heavily used segment may evict
/// while other segments still have free slots. The sum of the segment slices equals <see cref="Capacity" /> exactly, so
/// the dictionary never stores more than <see cref="Capacity" /> entries. Use the internal concurrency-level
/// constructor (or a capacity of 1) to obtain a single segment whose eviction sequence matches the non-concurrent type
/// exactly.
/// </para>
/// <para>
/// The single-key operations — <see cref="Add(TKey, TValue)" />, <see cref="TryAdd(TKey, TValue)" />,
/// <see cref="TryGetValue" />, <see cref="ContainsKey" />, <see cref="Touch" />, <see cref="TryRemove" />, the indexer,
/// and <see cref="GetOrAdd(TKey, TValue)" /> — are each individually atomic and lock only the owning segment.
/// <see cref="Count" />, <see cref="IsEmpty" />, <see cref="Clear" />, <see cref="ToArray" />, and the snapshot
/// properties acquire every segment lock and therefore observe a coherent point-in-time state;
/// <see cref="ApproximateCount" /> is lock-free.
/// </para>
/// <para>
/// Enumeration iterates over a true snapshot captured when the enumerator is created (via <see cref="ToArray" />) and
/// does not reflect subsequent changes. Unlike enumerators on non-concurrent collections, it never throws
/// <see cref="InvalidOperationException" /> because of concurrent modification. The order of enumerated entries is
/// unspecified.
/// </para>
/// <para>
/// Supplying an <see cref="EvictingDictionaryExpiration" /> at construction adds time-based expiry orthogonal to the
/// capacity policy, with the same lazy-purge model as the non-concurrent type: expired entries are invisible to lookups
/// and enumeration before they are physically removed, capacity pressure purges a segment's expired entries ahead of a
/// policy eviction, and <see cref="Count" /> reports the raw stored count <em>including</em> expired-but-unpurged
/// entries — call <see cref="RemoveExpired" /> to reconcile. Without an expiration configuration the dictionary
/// performs no clock reads.
/// </para>
/// <para>
/// Unlike <see cref="EvictingDictionary{TKey, TValue}" />, this type exposes only the post-commit
/// <see cref="ItemEvicted" /> event: under concurrency an eviction has already been committed by the time a handler
/// could observe it, so a pre-removal event could not be honored. Handlers run after the segment lock has been released
/// and their exceptions are suppressed (except <see cref="OutOfMemoryException" />) — see the event's documentation.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var cache = new ConcurrentEvictingDictionary<string, byte[]>(
///     capacity: 1024, EvictingDictionaryPolicy.LeastRecentlyUsed);
///
/// Parallel.ForEach(requests, request =>
/// {
///     byte[] payload = cache.GetOrAdd(request.Key, key => LoadPayload(key));
///     Serve(request, payload);
/// });
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Count ≈ {ApproximateCount}, Capacity = {Capacity}, Policy = {Policy}")]
[DebuggerTypeProxy(typeof(ConcurrentEvictingDictionaryDebugView<,>))]
public sealed partial class ConcurrentEvictingDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <summary>The upper bound applied to <see cref="DefaultConcurrencyLevel" /> so that the segment array stays small on high-core machines where the typical cache is also small. The internal constructor can request a higher level.</summary>
    internal const int MaxDefaultConcurrencyLevel = 32;

    /// <summary>The capacity used when the dictionary is constructed without an explicit capacity.</summary>
    private const int DefaultCapacity = 16;

    /// <summary>The eviction policy used when the dictionary is constructed without an explicit policy.</summary>
    private const EvictingDictionaryPolicy DefaultPolicy = EvictingDictionaryPolicy.LeastRecentlyUsed;

    /// <summary>The equality comparer used for key identity, hash-table lookup, and segment routing. Never <see langword="null" />.</summary>
    private readonly IEqualityComparer<TKey> _comparer;

    /// <summary>The fixed array of monitor objects guarding the segments. Segment <c>i</c> is guarded by <c>_locks[i]</c>. Allocated once at construction and never replaced or resized.</summary>
    private readonly object[] _locks;

    /// <summary>The lock-striped policy caches. Each segment owns a fixed slice of the total capacity; the slices sum to <see cref="Capacity" /> exactly.</summary>
    private readonly Segment[] _segments;

    /// <summary>The cumulative number of entries evicted by capacity pressure or expiry, updated with interlocked operations.</summary>
    private long _evictionCount;

    /// <summary>The cumulative number of successful accesses and touches, updated with interlocked operations.</summary>
    private long _totalTouches;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> class, with the
    /// default capacity and eviction policy.
    /// </summary>
    /// <remarks>
    /// Creates an empty dictionary with a capacity of <see cref="DefaultCapacity" /> items, using
    /// <see cref="DefaultPolicy" /> for eviction when capacity is exceeded, and the default key comparer (
    /// <see cref="EqualityComparer{TKey}.Default" />).
    /// </remarks>
    public ConcurrentEvictingDictionary()
        : this(DefaultConcurrencyLevel, DefaultCapacity, DefaultPolicy, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> class, with the
    /// specified capacity and the default eviction policy.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    public ConcurrentEvictingDictionary(int capacity)
        : this(DefaultConcurrencyLevel, capacity, DefaultPolicy, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> class, with the
    /// specified capacity and eviction policy.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero, or <paramref name="policy" /> is not a defined
    /// <see cref="EvictingDictionaryPolicy" /> value.
    /// </exception>
    public ConcurrentEvictingDictionary(int capacity, EvictingDictionaryPolicy policy)
        : this(DefaultConcurrencyLevel, capacity, policy, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> class, with the
    /// specified capacity, using the default eviction policy and the specified key comparer.
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
    public ConcurrentEvictingDictionary(int capacity, IEqualityComparer<TKey>? comparer)
        : this(DefaultConcurrencyLevel, capacity, DefaultPolicy, comparer, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> class, with the
    /// specified capacity, eviction policy, and key comparer.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <param name="comparer">
    /// The equality comparer to use for keys, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero, or <paramref name="policy" /> is not a defined
    /// <see cref="EvictingDictionaryPolicy" /> value.
    /// </exception>
    public ConcurrentEvictingDictionary(int capacity, EvictingDictionaryPolicy policy, IEqualityComparer<TKey>? comparer)
        : this(DefaultConcurrencyLevel, capacity, policy, comparer, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> class, with the
    /// specified capacity and time-based expiration, using the default eviction policy and key comparer.
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
    public ConcurrentEvictingDictionary(int capacity, EvictingDictionaryExpiration? expiration)
        : this(DefaultConcurrencyLevel, capacity, DefaultPolicy, null, expiration) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> class, with the
    /// specified capacity, eviction policy, and time-based expiration, using the default key comparer.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <param name="expiration">
    /// The time-based expiration configuration, or <see langword="null" /> to disable expiry.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero, or <paramref name="policy" /> is not a defined
    /// <see cref="EvictingDictionaryPolicy" /> value.
    /// </exception>
    public ConcurrentEvictingDictionary(int capacity, EvictingDictionaryPolicy policy, EvictingDictionaryExpiration? expiration)
        : this(DefaultConcurrencyLevel, capacity, policy, null, expiration) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> class, with the
    /// specified capacity, eviction policy, key comparer, and time-based expiration.
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
    /// <paramref name="capacity" /> is less than or equal to zero, or <paramref name="policy" /> is not a defined
    /// <see cref="EvictingDictionaryPolicy" /> value.
    /// </exception>
    public ConcurrentEvictingDictionary(int capacity, EvictingDictionaryPolicy policy, IEqualityComparer<TKey>? comparer, EvictingDictionaryExpiration? expiration)
        : this(DefaultConcurrencyLevel, capacity, policy, comparer, expiration) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> class, with elements
    /// copied from the specified sequence, using the default capacity and eviction policy.
    /// </summary>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// If more elements are provided than the capacity allows, entries are evicted according to the policy.
    /// </remarks>
    public ConcurrentEvictingDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source)
        : this(DefaultCapacity, source, DefaultPolicy, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> class, with elements
    /// copied from the specified sequence, using the specified capacity and the default eviction policy.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero.
    /// </exception>
    public ConcurrentEvictingDictionary(int capacity, IEnumerable<KeyValuePair<TKey, TValue>> source)
        : this(capacity, source, DefaultPolicy, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> class, with elements
    /// copied from the specified sequence, using the default capacity and the specified eviction policy.
    /// </summary>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="EvictingDictionaryPolicy" /> value.
    /// </exception>
    public ConcurrentEvictingDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source, EvictingDictionaryPolicy policy)
        : this(DefaultCapacity, source, policy, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> class, with elements
    /// copied from the specified sequence, using the specified capacity and eviction policy.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="source">The sequence of key/value pairs to copy. Must not be <see langword="null" />.</param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero, or <paramref name="policy" /> is not a defined
    /// <see cref="EvictingDictionaryPolicy" /> value.
    /// </exception>
    public ConcurrentEvictingDictionary(int capacity, IEnumerable<KeyValuePair<TKey, TValue>> source, EvictingDictionaryPolicy policy)
        : this(capacity, source, policy, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> class, with elements
    /// copied from the specified sequence, using the specified capacity, eviction policy, and key comparer.
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
    /// <paramref name="capacity" /> is less than or equal to zero, or <paramref name="policy" /> is not a defined
    /// <see cref="EvictingDictionaryPolicy" /> value.
    /// </exception>
    public ConcurrentEvictingDictionary(int capacity, IEnumerable<KeyValuePair<TKey, TValue>> source, EvictingDictionaryPolicy policy, IEqualityComparer<TKey>? comparer)
        : this(capacity, source, policy, comparer, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> class, with elements
    /// copied from the specified sequence, using the specified capacity, eviction policy, key comparer, and time-based
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
    /// <paramref name="capacity" /> is less than or equal to zero, or <paramref name="policy" /> is not a defined
    /// <see cref="EvictingDictionaryPolicy" /> value.
    /// </exception>
    /// <remarks>
    /// Copied entries receive the default <see cref="EvictingDictionaryExpiration.TimeToLive" /> (when configured); if
    /// more elements are provided than the capacity allows, entries are evicted according to the policy.
    /// </remarks>
    public ConcurrentEvictingDictionary(int capacity, IEnumerable<KeyValuePair<TKey, TValue>> source, EvictingDictionaryPolicy policy, IEqualityComparer<TKey>? comparer, EvictingDictionaryExpiration? expiration)
        : this(DefaultConcurrencyLevel, capacity, policy, comparer, expiration)
    {
        ThrowHelper.ThrowIfNull(source);

        foreach (KeyValuePair<TKey, TValue> kvp in source)
            Add(kvp.Key, kvp.Value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentEvictingDictionary{TKey, TValue}" /> class with the
    /// specified lock-striping level, capacity, eviction policy, key comparer, and time-based expiration.
    /// </summary>
    /// <param name="concurrencyLevel">The maximum number of segments to allocate. Must be greater than zero.</param>
    /// <param name="capacity">
    /// The maximum number of key/value pairs the dictionary can contain. Must be positive.
    /// </param>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <param name="comparer">The key comparer, or <see langword="null" /> to use the default comparer.</param>
    /// <param name="expiration">
    /// The time-based expiration configuration, or <see langword="null" /> to disable expiry.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than or equal to zero, <paramref name="concurrencyLevel" /> is less than or
    /// equal to zero, or <paramref name="policy" /> is not a defined <see cref="EvictingDictionaryPolicy" /> value.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the single shared initializer that every public constructor delegates to. It is also visible to the test
    /// assembly so instances can be constructed with an explicit segment count — a concurrency level of 1 yields a
    /// single segment whose eviction sequence matches the non-concurrent
    /// <see cref="EvictingDictionary{TKey, TValue}" /> exactly.
    /// </para>
    /// <para>
    /// The segment count is <c>min(concurrencyLevel, capacity)</c> so every segment owns at least one slot. The
    /// capacity is divided as evenly as possible: the first <c>capacity % segmentCount</c> segments receive one extra
    /// slot, and the slices sum to <paramref name="capacity" /> exactly.
    /// </para>
    /// </remarks>
    internal ConcurrentEvictingDictionary(int concurrencyLevel, int capacity, EvictingDictionaryPolicy policy, IEqualityComparer<TKey>? comparer, EvictingDictionaryExpiration? expiration)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(concurrencyLevel, 0);
        ThrowHelper.ThrowIfZeroOrNegative(capacity);
        ThrowHelper.ThrowIfEnumValueIsUndefined(policy);

        Capacity = capacity;
        Policy = policy;
        Expiration = expiration;
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
        _timeProvider = expiration?.TimeProvider;

        int segmentCount = Math.Min(concurrencyLevel, capacity);
        int baseCapacity = capacity / segmentCount;
        int remainder = capacity % segmentCount;

        var segments = new Segment[segmentCount];
        var locks = new object[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            segments[i] = new Segment(baseCapacity + (i < remainder ? 1 : 0), policy, _comparer, expiration);
            locks[i] = new object();
        }

        _segments = segments;
        _locks = locks;
    }

    /// <summary>
    /// Occurs immediately <b>after</b> an entry has been evicted because of capacity pressure or time-based expiry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Handlers run on the thread whose operation caused the eviction, <em>after</em> the owning segment's lock has
    /// been released, so a handler can safely call back into the dictionary without deadlocking. The key and value
    /// provided are no longer present in the dictionary by the time the handler observes them.
    /// </para>
    /// <para>
    /// Each subscriber is invoked independently, and ordinary handler exceptions are caught and suppressed — only
    /// <see cref="OutOfMemoryException" /> propagates. This differs from the non-concurrent
    /// <see cref="EvictingDictionary{TKey, TValue}.ItemEvicted" />, which propagates handler exceptions: under
    /// concurrency the eviction has already been committed and observed by other threads, so propagating a handler
    /// exception could not undo it. For the same reason there is no pre-removal <c>ItemEvicting</c> event on this type.
    /// </para>
    /// <para>
    /// Explicit removals via <see cref="TryRemove" /> and bulk resets via <see cref="Clear" /> are not evictions and do
    /// not raise this event.
    /// </para>
    /// </remarks>
    public event Action<TKey, TValue>? ItemEvicted;

    /// <summary>
    /// Gets an approximate entry count without acquiring any segment lock.
    /// </summary>
    /// <value>
    /// The sum of the per-segment entry counters at the moment of the call. The result is correct when no other thread
    /// is concurrently mutating the dictionary; under concurrent writes the returned value may not reflect any single
    /// coherent point-in-time state. Like <see cref="Count" />, it includes expired-but-unpurged entries.
    /// </value>
    /// <remarks>
    /// Use this property when callers need a fast size estimate — for capacity hints, telemetry, or display — but can
    /// tolerate values that lag active writers. Prefer <see cref="Count" /> when an exact snapshot is required.
    /// </remarks>
    public int ApproximateCount
    {
        get
        {
            int count = 0;
            foreach (Segment segment in _segments)
            {
                checked
                {
                    count += segment.CountApproximate;
                }
            }

            return count;
        }
    }

    /// <summary>
    /// Gets the maximum number of items that can be stored in the dictionary before eviction occurs.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// Gets the equality comparer used for key identity, hash-table lookup, and segment routing.
    /// </summary>
    /// <value>The active equality comparer.</value>
    public IEqualityComparer<TKey> Comparer => _comparer;

    /// <summary>
    /// Gets the number of key/value pairs physically stored in the dictionary.
    /// </summary>
    /// <value>The raw stored count, including expired entries that have not yet been purged.</value>
    /// <remarks>
    /// <para>
    /// Reading this property acquires every segment lock, yielding an exact point-in-time count. Under concurrency the
    /// returned value may already be stale by the time the caller inspects it; prefer <see cref="ApproximateCount" />
    /// for a lock-free estimate.
    /// </para>
    /// <para>
    /// When time-based expiration is configured, this property deliberately reports the raw stored count — <em>including</em>
    /// expired-but-unpurged entries — matching <see cref="EvictingDictionary{TKey, TValue}" />. Call
    /// <see cref="RemoveExpired" /> to purge expired entries and reconcile <see cref="Count" /> with the live set.
    /// </para>
    /// </remarks>
    public int Count
    {
        get
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                return GetRawCountNoLocks();
            }
            finally
            {
                ReleaseLocks(locksAcquired);
            }
        }
    }

    /// <summary>
    /// Gets the total number of items evicted from the dictionary since creation, whether by capacity pressure or
    /// time-based expiry.
    /// </summary>
    /// <value>The cumulative eviction count. Reset to zero by <see cref="Clear" />.</value>
    public long EvictionCount => Interlocked.Read(ref _evictionCount);

    /// <summary>
    /// Gets a value indicating whether the dictionary is empty, observed as an exact point-in-time snapshot.
    /// </summary>
    /// <value><see langword="true" /> if the dictionary stores no entries; otherwise, <see langword="false" />.</value>
    /// <remarks>
    /// Reading this property acquires every segment lock, producing a coherent point-in-time view. Like
    /// <see cref="Count" />, expired-but-unpurged entries count as stored.
    /// </remarks>
    public bool IsEmpty
    {
        get
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);

                foreach (Segment segment in _segments)
                {
                    if (segment.CountUnsafe != 0)
                        return false;
                }

                return true;
            }
            finally
            {
                ReleaseLocks(locksAcquired);
            }
        }
    }

    /// <summary>
    /// Gets the eviction policy configured for this dictionary.
    /// </summary>
    public EvictingDictionaryPolicy Policy { get; }

    /// <summary>
    /// Gets the total number of times any key has been successfully accessed or touched.
    /// </summary>
    /// <value>The cumulative touch count. Reset to zero by <see cref="Clear" />.</value>
    public long TotalTouches => Interlocked.Read(ref _totalTouches);

    /// <summary>
    /// Gets the number of internal segments the capacity is partitioned across. Exposed to the test assembly so
    /// partitioning invariants can be asserted directly.
    /// </summary>
    internal int SegmentCount => _segments.Length;

    /// <summary>
    /// Gets the default number of segments used by the public constructors. Reads
    /// <see cref="Environment.ProcessorCount" /> and routes through <see cref="ClampDefaultConcurrencyLevel(int)" /> so
    /// the clamp logic can be exercised in isolation.
    /// </summary>
    /// <value>The clamped default lock striping level for newly created instances.</value>
    private static int DefaultConcurrencyLevel =>
        ClampDefaultConcurrencyLevel(Environment.ProcessorCount);

    /// <summary>
    /// Adds the specified key and value to the dictionary, or replaces the value if the key already exists. If the
    /// owning segment has reached its capacity slice and the key is new, an existing entry is evicted according to the
    /// configured <see cref="EvictingDictionaryPolicy" />.
    /// </summary>
    /// <param name="key">The key of the element to add or update.</param>
    /// <param name="value">The value to associate with <paramref name="key" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// When an entry for <paramref name="key" /> already exists its value is updated in place without disturbing
    /// eviction metadata: position in the order list, LFU frequency, and the SecondChance reference flag are all
    /// preserved. This differs from <see cref="Dictionary{TKey, TValue}.Add(TKey, TValue)" />, which throws on
    /// duplicate keys.
    /// </para>
    /// <para>
    /// When time-based expiration is configured, each write is a fresh lease: the entry's lifetime restarts using the
    /// dictionary default <see cref="EvictingDictionaryExpiration.TimeToLive" /> (any per-entry override from a
    /// previous <see cref="Add(TKey, TValue, TimeSpan)" /> is discarded).
    /// </para>
    /// <para>
    /// This operation is atomic and locks only the segment that owns <paramref name="key" />. Any eviction it causes
    /// raises <see cref="ItemEvicted" /> after the segment lock has been released.
    /// </para>
    /// </remarks>
    public void Add(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        List<KeyValuePair<TKey, TValue>>? evicted = null;
        int index = GetSegmentIndex(key);
        lock (_locks[index])
        {
            _segments[index].AddOrReplace(key, value, null, ref evicted);
        }

        OnEvicted(evicted);
    }

    /// <summary>
    /// Attempts to add the specified key and value to the dictionary, without replacing an existing live entry.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value to associate with <paramref name="key" />.</param>
    /// <returns>
    /// <see langword="true" /> if the entry was added; <see langword="false" /> if a live entry for
    /// <paramref name="key" /> already exists.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// When time-based expiration is configured, an expired-but-unpurged entry for <paramref name="key" /> does not
    /// block the add: it is lazily removed as an eviction and the new entry is added, returning <see langword="true" />.
    /// This operation is atomic and locks only the segment that owns <paramref name="key" />.
    /// </remarks>
    public bool TryAdd(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        List<KeyValuePair<TKey, TValue>>? evicted = null;
        int index = GetSegmentIndex(key);
        bool added;
        lock (_locks[index])
        {
            added = _segments[index].TryAdd(key, value, null, ref evicted);
        }

        OnEvicted(evicted);
        return added;
    }

    /// <summary>
    /// Removes all entries from the dictionary and resets the <see cref="TotalTouches" /> and
    /// <see cref="EvictionCount" /> counters.
    /// </summary>
    /// <remarks>
    /// This operation acquires every segment lock, so it is atomic with respect to all other operations. It is a bulk
    /// reset, not an eviction — <see cref="ItemEvicted" /> is not raised for the removed entries, matching
    /// <see cref="ConcurrentCircularBuffer{T}.Clear" />.
    /// </remarks>
    public void Clear()
    {
        int locksAcquired = 0;
        try
        {
            AcquireAllLocks(ref locksAcquired);

            foreach (Segment segment in _segments)
                segment.Clear();

            Interlocked.Exchange(ref _evictionCount, 0);
            Interlocked.Exchange(ref _totalTouches, 0);
        }
        finally
        {
            ReleaseLocks(locksAcquired);
        }
    }

    /// <summary>
    /// Determines whether the dictionary contains a live entry for the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>
    /// <see langword="true" /> if a live entry exists for <paramref name="key" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This is a pure read with respect to the capacity policy — it does not update recency or frequency metadata and
    /// does not increment <see cref="TotalTouches" />. When time-based expiration is configured, an expired entry
    /// counts as absent (it is lazily removed as an eviction and <see langword="false" /> is returned), and a hit
    /// refreshes the deadline under <see cref="EvictingDictionaryExpirationKind.Sliding" />. The operation locks only
    /// the segment that owns <paramref name="key" />.
    /// </remarks>
    public bool ContainsKey(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        List<KeyValuePair<TKey, TValue>>? evicted = null;
        int index = GetSegmentIndex(key);
        bool found;
        lock (_locks[index])
        {
            found = _segments[index].Contains(key, ref evicted);
        }

        OnEvicted(evicted);
        return found;
    }

    /// <summary>
    /// Adds a key/value pair to the dictionary if the key does not already exist, and returns either the existing or
    /// the newly added value.
    /// </summary>
    /// <param name="key">The key of the element to get or add.</param>
    /// <param name="value">The value to add when the key is absent.</param>
    /// <returns>
    /// The existing value when a live entry for <paramref name="key" /> is present; otherwise <paramref name="value" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// A hit counts as an access for the configured policy (identical to <see cref="TryGetValue" />) and increments
    /// <see cref="TotalTouches" />; a miss adds the entry, evicting per the policy when the owning segment is full. The
    /// whole operation is atomic and locks only the segment that owns <paramref name="key" />.
    /// </remarks>
    public TValue GetOrAdd(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        return GetOrAddCore(key, value, valueFactory: null, ttlOverride: null);
    }

    /// <summary>
    /// Adds a key/value pair to the dictionary by using the specified value factory if the key does not already exist,
    /// and returns either the existing or the newly created value.
    /// </summary>
    /// <param name="key">The key of the element to get or add.</param>
    /// <param name="valueFactory">The function used to generate a value for the key when it is absent.</param>
    /// <returns>
    /// The existing value when a live entry for <paramref name="key" /> is present; otherwise the value produced by
    /// <paramref name="valueFactory" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="valueFactory" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Unlike
    /// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})" />,
    /// the factory is invoked <em>inside</em> the owning segment's lock, so it runs at most once per key even under
    /// concurrent misses — the single-flight behavior that prevents cache stampedes.
    /// </para>
    /// <para>
    /// The cost of that guarantee is that the factory blocks every other operation on the same segment while it runs:
    /// keep factories short, and never call back into this dictionary from a factory — doing so from the same thread
    /// re-enters the segment monitor and can corrupt eviction bookkeeping mid-add. If the factory throws, nothing is
    /// added and the exception propagates.
    /// </para>
    /// </remarks>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(valueFactory);

        return GetOrAddCore(key, default!, valueFactory, ttlOverride: null);
    }

    /// <summary>
    /// Copies the dictionary's live entries into a new array.
    /// </summary>
    /// <returns>
    /// A new array containing a coherent point-in-time snapshot of the dictionary's live entries, or an empty array if
    /// the dictionary is empty. The order of entries is unspecified.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method acquires every segment lock for the duration of the copy, so the returned array reflects the
    /// dictionary's contents at a single instant and is unaffected by concurrent modifications made afterward.
    /// </para>
    /// <para>
    /// When time-based expiration is configured, the clock is read once and expired entries are filtered out of the
    /// snapshot without being removed or having their deadlines refreshed, so the array length may be less than
    /// <see cref="Count" /> until <see cref="RemoveExpired" /> reconciles the raw store.
    /// </para>
    /// </remarks>
    public KeyValuePair<TKey, TValue>[] ToArray()
    {
        int locksAcquired = 0;
        try
        {
            AcquireAllLocks(ref locksAcquired);
            return SnapshotNoLocks();
        }
        finally
        {
            ReleaseLocks(locksAcquired);
        }
    }

    /// <summary>
    /// Marks the specified key as recently accessed without retrieving its value. If the eviction policy involves usage
    /// tracking, this updates the internal usage metadata.
    /// </summary>
    /// <param name="key">The key to touch.</param>
    /// <returns>
    /// <see langword="true" /> if a live entry exists and was marked as accessed; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// When time-based expiration is configured, an expired entry counts as absent: it is lazily removed as an eviction
    /// and <see langword="false" /> is returned. <see cref="Touch" /> affects only the capacity-policy metadata — it
    /// does not refresh a sliding expiration deadline; use a read access ( <see cref="TryGetValue" />, the indexer
    /// getter, or <see cref="ContainsKey" />) to slide. The operation locks only the segment that owns
    /// <paramref name="key" />.
    /// </remarks>
    public bool Touch(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        List<KeyValuePair<TKey, TValue>>? evicted = null;
        int index = GetSegmentIndex(key);
        bool touched;
        lock (_locks[index])
        {
            touched = _segments[index].Touch(key, ref evicted);
        }

        if (touched)
            Interlocked.Increment(ref _totalTouches);

        OnEvicted(evicted);
        return touched;
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
    /// <see langword="true" /> if the dictionary contains a live entry for the specified key; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// A successful read counts as an access for the configured <see cref="EvictingDictionaryPolicy" />:
    /// recency-tracked policies reposition the key, <see cref="EvictingDictionaryPolicy.LeastFrequentlyUsed" />
    /// increments its frequency, and <see cref="EvictingDictionaryPolicy.SecondChance" /> sets its reference flag.
    /// <see cref="TotalTouches" /> is incremented on every successful lookup.
    /// </para>
    /// <para>
    /// When time-based expiration is configured, an expired entry counts as absent (it is lazily removed as an eviction
    /// and <see langword="false" /> is returned), and a hit refreshes the deadline under
    /// <see cref="EvictingDictionaryExpirationKind.Sliding" />. The operation locks only the segment that owns
    /// <paramref name="key" />.
    /// </para>
    /// </remarks>
    public bool TryGetValue(TKey key, out TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        List<KeyValuePair<TKey, TValue>>? evicted = null;
        int index = GetSegmentIndex(key);
        bool found;
        lock (_locks[index])
        {
            found = _segments[index].TryGet(key, ref evicted, out value);
        }

        if (found)
            Interlocked.Increment(ref _totalTouches);

        OnEvicted(evicted);
        return found;
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
    /// <see cref="TryRemove" /> operates on physically stored entries: when time-based expiration is configured it also
    /// removes an expired-but-unpurged entry and returns <see langword="true" />. An explicit removal is not an
    /// eviction — it does not raise <see cref="ItemEvicted" /> and does not increment <see cref="EvictionCount" />. The
    /// operation is atomic and locks only the segment that owns <paramref name="key" />.
    /// </remarks>
    public bool TryRemove(TKey key, out TValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        int index = GetSegmentIndex(key);
        lock (_locks[index])
        {
            return _segments[index].TryRemove(key, out value);
        }
    }

    /// <summary>
    /// Clamps a raw processor count to the range used by <see cref="DefaultConcurrencyLevel" />, guaranteeing at least
    /// one segment and at most <see cref="MaxDefaultConcurrencyLevel" /> segments regardless of the host machine.
    /// </summary>
    /// <param name="processorCount">The raw processor count to clamp.</param>
    /// <returns>A value in the range <c>[1, MaxDefaultConcurrencyLevel]</c>.</returns>
    /// <remarks>
    /// Extracted as a static helper so unit tests can exercise the clamp envelope with synthetic processor counts
    /// rather than relying on the machine's actual <see cref="Environment.ProcessorCount" />.
    /// </remarks>
    internal static int ClampDefaultConcurrencyLevel(int processorCount) =>
        Math.Clamp(processorCount, 1, MaxDefaultConcurrencyLevel);

    /// <summary>
    /// Returns the capacity slice owned by the segment at the specified index. Exposed to the test assembly so
    /// partitioning invariants can be asserted directly.
    /// </summary>
    /// <param name="index">The zero-based segment index.</param>
    /// <returns>The maximum number of entries the segment may hold.</returns>
    internal int GetSegmentCapacity(int index) =>
        _segments[index].Capacity;

    /// <summary>
    /// Acquires every segment lock in ascending index order.
    /// </summary>
    /// <param name="locksAcquired">
    /// A running count of locks successfully entered, incremented as each lock is taken so the caller's
    /// <see langword="finally" /> block can release exactly the locks that were acquired.
    /// </param>
    private void AcquireAllLocks(ref int locksAcquired)
    {
        for (int i = 0; i < _locks.Length; i++)
        {
            bool lockTaken = false;
            try
            {
                Monitor.Enter(_locks[i], ref lockTaken);
            }
            finally
            {
                if (lockTaken)
                    locksAcquired++;
            }
        }
    }

    /// <summary>
    /// Implements the shared lookup-or-insert path for the <c>GetOrAdd</c> overloads. The factory, when supplied, runs
    /// inside the owning segment's lock so it is invoked at most once per key.
    /// </summary>
    /// <param name="key">The key of the element to get or add.</param>
    /// <param name="value">The value to add when <paramref name="valueFactory" /> is <see langword="null" />.</param>
    /// <param name="valueFactory">
    /// The value factory, or <see langword="null" /> to use <paramref name="value" />.
    /// </param>
    /// <param name="ttlOverride">
    /// The per-entry time-to-live, or <see langword="null" /> to apply the dictionary default.
    /// </param>
    /// <returns>The existing value on a hit; otherwise the added value.</returns>
    private TValue GetOrAddCore(TKey key, TValue value, Func<TKey, TValue>? valueFactory, TimeSpan? ttlOverride)
    {
        List<KeyValuePair<TKey, TValue>>? evicted = null;
        int index = GetSegmentIndex(key);
        bool hit;
        TValue result;

        try
        {
            lock (_locks[index])
            {
                Segment segment = _segments[index];
                if (segment.TryGet(key, ref evicted, out TValue existing))
                {
                    hit = true;
                    result = existing;
                }
                else
                {
                    hit = false;
                    result = valueFactory is null ? value : valueFactory(key);
                    segment.AddOrReplace(key, result, ttlOverride, ref evicted);
                }
            }
        }
        finally
        {
            // A throwing factory must not swallow evictions already committed by the lookup (an expired entry may
            // have been lazily removed before the factory ran).
            OnEvicted(evicted);
        }

        if (hit)
            Interlocked.Increment(ref _totalTouches);

        return result;
    }

    /// <summary>
    /// Returns the total raw entry count by summing the per-segment counts. The caller must already hold every lock.
    /// </summary>
    /// <returns>
    /// The number of entries currently stored across all segments, including expired-but-unpurged entries.
    /// </returns>
    private int GetRawCountNoLocks()
    {
        int count = 0;
        foreach (Segment segment in _segments)
        {
            checked
            {
                count += segment.CountUnsafe;
            }
        }

        return count;
    }

    /// <summary>
    /// Maps a key to the index of the segment that owns it, using the comparer's hash code.
    /// </summary>
    /// <param name="key">The key to route.</param>
    /// <returns>The zero-based segment index.</returns>
    private int GetSegmentIndex(TKey key) =>
        (int)((uint)_comparer.GetHashCode(key) % (uint)_segments.Length);

    /// <summary>
    /// Dispatches post-commit eviction notifications for the entries in the buffer and adds them to
    /// <see cref="EvictionCount" />. Must be called after every segment lock touched by the operation has been
    /// released.
    /// </summary>
    /// <param name="evicted">
    /// The eviction buffer, or <see langword="null" /> when the operation evicted nothing.
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

    /// <summary>
    /// Releases the first <paramref name="locksAcquired" /> segment locks.
    /// </summary>
    /// <param name="locksAcquired">The number of segment locks, starting at index zero, to release.</param>
    private void ReleaseLocks(int locksAcquired)
    {
        for (int i = 0; i < locksAcquired; i++)
            Monitor.Exit(_locks[i]);
    }

    /// <summary>
    /// Builds a snapshot of the dictionary's live entries. The caller must already hold every lock.
    /// </summary>
    /// <returns>
    /// The live entries across all segments; expired entries are filtered against a single clock read.
    /// </returns>
    private KeyValuePair<TKey, TValue>[] SnapshotNoLocks()
    {
        bool checkExpiry = _timeProvider is not null;
        long nowTicks = checkExpiry ? _timeProvider!.GetUtcNow().UtcTicks : 0L;

        var result = new List<KeyValuePair<TKey, TValue>>(GetRawCountNoLocks());
        foreach (Segment segment in _segments)
            segment.AppendSnapshot(result, nowTicks, checkExpiry);

        return result.ToArray();
    }
}
