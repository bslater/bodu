// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionary{T,T}.Expiration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentEvictingDictionary<TKey, TValue>
{
    /// <summary>The time source driving expiration clock reads, or <see langword="null" /> when expiration is disabled. Cached from <see cref="Expiration" /> so snapshot guards are a single null check.</summary>
    private readonly TimeProvider? _timeProvider;

    /// <summary>
    /// Gets the time-based expiration configuration for this dictionary, or <see langword="null" /> when expiration is
    /// disabled.
    /// </summary>
    /// <value>
    /// The <see cref="EvictingDictionaryExpiration" /> supplied at construction, or <see langword="null" /> when the
    /// dictionary was constructed without one. When <see langword="null" /> the dictionary performs no clock reads and
    /// behaves as a capacity-only cache.
    /// </value>
    public EvictingDictionaryExpiration? Expiration { get; }

    /// <summary>
    /// Adds the specified key and value to the dictionary with an explicit time-to-live, or replaces the value and
    /// restarts the lifetime if the key already exists.
    /// </summary>
    /// <param name="key">The key of the element to add or update.</param>
    /// <param name="value">The value to associate with <paramref name="key" />.</param>
    /// <param name="timeToLive">
    /// The lifetime for this entry, overriding the dictionary's default
    /// <see cref="EvictingDictionaryExpiration.TimeToLive" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="timeToLive" /> is less than or equal to <see cref="TimeSpan.Zero" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No <see cref="Expiration" /> configuration was supplied at construction.
    /// </exception>
    /// <remarks>
    /// The per-entry <paramref name="timeToLive" /> applies to this write only: a later update through
    /// <see cref="Add(TKey, TValue)" /> or the indexer setter discards the override and re-applies the dictionary
    /// default. Under <see cref="EvictingDictionaryExpirationKind.Sliding" />, successful read accesses refresh the
    /// deadline using this entry's own time-to-live. The operation is atomic and locks only the segment that owns
    /// <paramref name="key" />.
    /// </remarks>
    public void Add(TKey key, TValue value, TimeSpan timeToLive)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfZeroOrNegative(timeToLive);
        ThrowIfExpirationNotConfigured();

        List<KeyValuePair<TKey, TValue>>? evicted = null;
        int index = GetSegmentIndex(key);
        lock (_locks[index])
        {
            _segments[index].AddOrReplace(key, value, timeToLive, ref evicted);
        }

        OnEvicted(evicted);
    }

    /// <summary>
    /// Adds a key/value pair with an explicit time-to-live if the key does not already exist, and returns either the
    /// existing or the newly added value.
    /// </summary>
    /// <param name="key">The key of the element to get or add.</param>
    /// <param name="value">The value to add when the key is absent.</param>
    /// <param name="timeToLive">
    /// The lifetime applied when the entry is added, overriding the dictionary's default
    /// <see cref="EvictingDictionaryExpiration.TimeToLive" />.
    /// </param>
    /// <returns>
    /// The existing value when a live entry for <paramref name="key" /> is present; otherwise <paramref name="value" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="timeToLive" /> is less than or equal to <see cref="TimeSpan.Zero" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No <see cref="Expiration" /> configuration was supplied at construction.
    /// </exception>
    /// <remarks>
    /// A hit counts as an access for the configured policy and leaves the existing entry's lifetime rules unchanged
    /// (sliding refresh applies as for <see cref="TryGetValue" />); the <paramref name="timeToLive" /> is applied only
    /// when the entry is added. The whole operation is atomic and locks only the segment that owns
    /// <paramref name="key" />.
    /// </remarks>
    public TValue GetOrAdd(TKey key, TValue value, TimeSpan timeToLive)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfZeroOrNegative(timeToLive);
        ThrowIfExpirationNotConfigured();

        return GetOrAddCore(key, value, valueFactory: null, timeToLive);
    }

    /// <summary>
    /// Adds a key/value pair produced by the specified value factory with an explicit time-to-live if the key does not
    /// already exist, and returns either the existing or the newly created value.
    /// </summary>
    /// <param name="key">The key of the element to get or add.</param>
    /// <param name="valueFactory">The function used to generate a value for the key when it is absent.</param>
    /// <param name="timeToLive">
    /// The lifetime applied when the entry is added, overriding the dictionary's default
    /// <see cref="EvictingDictionaryExpiration.TimeToLive" />.
    /// </param>
    /// <returns>
    /// The existing value when a live entry for <paramref name="key" /> is present; otherwise the value produced by
    /// <paramref name="valueFactory" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="valueFactory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="timeToLive" /> is less than or equal to <see cref="TimeSpan.Zero" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No <see cref="Expiration" /> configuration was supplied at construction.
    /// </exception>
    /// <remarks>
    /// The factory is invoked inside the owning segment's lock — at most once per key even under concurrent misses. See
    /// <see cref="GetOrAdd(TKey, Func{TKey, TValue})" /> for the factory caveats; the <paramref name="timeToLive" /> is
    /// applied only when the entry is added.
    /// </remarks>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory, TimeSpan timeToLive)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(valueFactory);
        ThrowHelper.ThrowIfZeroOrNegative(timeToLive);
        ThrowIfExpirationNotConfigured();

        return GetOrAddCore(key, default!, valueFactory, timeToLive);
    }

    /// <summary>
    /// Removes every expired entry from the dictionary and returns the number of entries removed.
    /// </summary>
    /// <returns>
    /// The number of expired entries that were removed; <c>0</c> when nothing had expired or when no
    /// <see cref="Expiration" /> configuration was supplied at construction.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Each removal counts as an eviction: it raises <see cref="ItemEvicted" /> (after the segment locks are released)
    /// and increments <see cref="EvictionCount" />, exactly as a capacity-triggered eviction does.
    /// </para>
    /// <para>
    /// Segments are purged one at a time, each under its own lock, so the sweep is not atomic across the whole
    /// dictionary: an entry added to an already purged segment during the call is not examined, and entries may expire
    /// in later segments while earlier ones are being processed. Each segment's purge is individually atomic.
    /// </para>
    /// <para>
    /// The dictionary never removes expired entries on a background timer; besides the lazy removal performed when an
    /// access touches an expired key, this method is the way to reconcile <see cref="Count" /> with the set of live
    /// entries. Call it periodically for caches that can sit idle for long periods.
    /// </para>
    /// </remarks>
    public int RemoveExpired()
    {
        List<KeyValuePair<TKey, TValue>>? evicted = null;
        int removed = 0;

        for (int i = 0; i < _segments.Length; i++)
        {
            lock (_locks[i])
            {
                removed += _segments[i].PurgeExpired(ref evicted);
            }
        }

        OnEvicted(evicted);
        return removed;
    }

    /// <summary>
    /// Attempts to add the specified key and value to the dictionary with an explicit time-to-live, without replacing
    /// an existing live entry.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value to associate with <paramref name="key" />.</param>
    /// <param name="timeToLive">
    /// The lifetime for this entry, overriding the dictionary's default
    /// <see cref="EvictingDictionaryExpiration.TimeToLive" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the entry was added; <see langword="false" /> if a live (non-expired) entry for
    /// <paramref name="key" /> already exists.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="timeToLive" /> is less than or equal to <see cref="TimeSpan.Zero" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No <see cref="Expiration" /> configuration was supplied at construction.
    /// </exception>
    /// <remarks>
    /// An expired-but-unpurged entry for <paramref name="key" /> does not block the add: it is lazily removed as an
    /// eviction and the new entry is added, returning <see langword="true" />. The operation is atomic and locks only
    /// the segment that owns <paramref name="key" />.
    /// </remarks>
    public bool TryAdd(TKey key, TValue value, TimeSpan timeToLive)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfZeroOrNegative(timeToLive);
        ThrowIfExpirationNotConfigured();

        List<KeyValuePair<TKey, TValue>>? evicted = null;
        int index = GetSegmentIndex(key);
        bool added;
        lock (_locks[index])
        {
            added = _segments[index].TryAdd(key, value, timeToLive, ref evicted);
        }

        OnEvicted(evicted);
        return added;
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if no <see cref="Expiration" /> configuration was supplied at
    /// construction. Guards the per-entry time-to-live overloads, which require a time provider.
    /// </summary>
    private void ThrowIfExpirationNotConfigured()
    {
        if (Expiration is null) throw new InvalidOperationException(ConcurrentCollectionsResourceStrings.Op_Invalid_ExpirationNotConfigured);
    }
}
