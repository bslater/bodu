// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionary{T,T}.Expiration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Collections.Generic;

public partial class EvictingDictionary<TKey, TValue>
{
    /// <summary>Indicates whether the configured expiration kind is sliding. <see langword="false" /> when expiration is disabled or absolute.</summary>
    private readonly bool _slidingExpiration;

    /// <summary>The time source driving expiration clock reads, or <see langword="null" /> when expiration is disabled. Cached from <see cref="Expiration" /> so hot-path guards are a single null check.</summary>
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
    /// restarts the lifetime if the key already exists. If the dictionary has reached its capacity and the key is new,
    /// expired entries are purged first and, when none exist, an entry is evicted according to the configured
    /// <see cref="EvictingDictionaryPolicy" />.
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
    /// No <see cref="Expiration" /> configuration was supplied at construction, or the method is invoked from within an
    /// <see cref="ItemEvicting" /> or <see cref="ItemEvicted" /> event handler.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The per-entry <paramref name="timeToLive" /> applies to this write only: a later update through
    /// <see cref="Add(TKey, TValue)" /> or the indexer setter discards the override and re-applies the dictionary
    /// default. Under <see cref="EvictingDictionaryExpirationKind.Sliding" />, successful read accesses refresh the
    /// deadline using this entry's own time-to-live.
    /// </para>
    /// <para>
    /// If an entry for <paramref name="key" /> exists but has expired, it is lazily removed (raising the eviction
    /// events) and the new entry is added with fresh eviction metadata.
    /// </para>
    /// </remarks>
    public void Add(TKey key, TValue value, TimeSpan timeToLive)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfZeroOrNegative(timeToLive);
        ThrowIfEvicting();
        ThrowIfExpirationNotConfigured();

        AddCore(key, value, timeToLive);
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
    /// No <see cref="Expiration" /> configuration was supplied at construction, or the method is invoked from within an
    /// <see cref="ItemEvicting" /> or <see cref="ItemEvicted" /> event handler.
    /// </exception>
    /// <remarks>
    /// An expired-but-unpurged entry for <paramref name="key" /> does not block the add: it is lazily removed (raising
    /// the eviction events) and the new entry is added, returning <see langword="true" />. If the dictionary is at
    /// capacity, expired entries are purged first and, when none exist, an entry is evicted according to the configured
    /// <see cref="EvictingDictionaryPolicy" />.
    /// </remarks>
    public bool TryAdd(TKey key, TValue value, TimeSpan timeToLive)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfZeroOrNegative(timeToLive);
        ThrowIfEvicting();
        ThrowIfExpirationNotConfigured();

        if (TryGetLiveItem(key, slide: false, out _))
            return false;

        AddCore(key, value, timeToLive);
        return true;
    }

    /// <summary>
    /// Removes every expired entry from the dictionary and returns the number of entries removed.
    /// </summary>
    /// <returns>
    /// The number of expired entries that were removed; <c>0</c> when nothing had expired or when no
    /// <see cref="Expiration" /> configuration was supplied at construction.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if invoked from within an <see cref="ItemEvicting" /> or <see cref="ItemEvicted" /> event handler.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Each removal raises the <see cref="ItemEvicting" /> and <see cref="ItemEvicted" /> events and increments
    /// <see cref="EvictionCount" />, exactly as a capacity-triggered eviction does.
    /// </para>
    /// <para>
    /// The dictionary never removes expired entries on a background timer; besides the lazy removal performed when an
    /// access touches an expired key, this method is the way to reconcile <see cref="Count" /> with the set of live
    /// entries. Call it periodically for caches that can sit idle for long periods.
    /// </para>
    /// </remarks>
    public int RemoveExpired()
    {
        ThrowIfEvicting();

        return PurgeExpired();
    }

    /// <summary>
    /// Computes the absolute expiration deadline for an entry, saturating at <see cref="long.MaxValue" /> instead of
    /// overflowing for very large time-to-live values.
    /// </summary>
    /// <param name="nowTicks">The current time in UTC ticks.</param>
    /// <param name="ttlTicks">The entry's time-to-live in ticks; must be positive.</param>
    /// <returns>The deadline in UTC ticks at or after which the entry counts as expired.</returns>
    private static long ComputeDeadline(long nowTicks, long ttlTicks) =>
        nowTicks > long.MaxValue - ttlTicks ? long.MaxValue : nowTicks + ttlTicks;

    /// <summary>
    /// Reads the current time from the configured time provider, in UTC ticks. Must only be called when expiration is
    /// enabled.
    /// </summary>
    /// <returns>The current UTC time in ticks.</returns>
    private long GetNowTicks() =>
        _timeProvider!.GetUtcNow().UtcTicks;

    /// <summary>
    /// Removes every entry whose deadline has passed, raising the eviction events per entry, and returns the number
    /// removed. Returns <c>0</c> without reading the clock when expiration is disabled.
    /// </summary>
    /// <returns>The number of expired entries removed.</returns>
    private int PurgeExpired()
    {
        if (_timeProvider is null || _store.Count == 0)
            return 0;

        long nowTicks = GetNowTicks();
        List<KeyValuePair<TKey, CacheItem>>? expired = null;

        foreach (KeyValuePair<TKey, CacheItem> pair in _store)
        {
            if (pair.Value.ExpiresAtTicks <= nowTicks)
                (expired ??= new List<KeyValuePair<TKey, CacheItem>>()).Add(pair);
        }

        if (expired is null)
            return 0;

        int removed = 0;
        foreach (KeyValuePair<TKey, CacheItem> pair in expired)
        {
            if (RemoveExpiredEntry(pair.Key, pair.Value))
                removed++;
        }

        return removed;
    }

    /// <summary>
    /// Removes a single expired entry, raising the <see cref="ItemEvicting" /> and <see cref="ItemEvicted" /> events
    /// and incrementing <see cref="EvictionCount" /> exactly as the capacity-eviction path does.
    /// </summary>
    /// <param name="key">The key of the expired entry.</param>
    /// <param name="item">The expired cache item.</param>
    /// <returns>
    /// <see langword="true" /> if the entry was removed; <see langword="false" /> when called while an eviction event
    /// is being dispatched, in which case the entry is left for a later purge (it remains invisible to reads).
    /// </returns>
    private bool RemoveExpiredEntry(TKey key, CacheItem item)
    {
        // Mid-event reads must not mutate the dictionary; the caller still reports the key absent.
        if (_isEvicting)
            return false;

        try
        {
            _isEvicting = true;
            ItemEvicting?.Invoke(key, item.Value);
        }
        finally
        {
            _isEvicting = false;
        }

        EvictionCount++;
        Remove(key);

        try
        {
            _isEvicting = true;
            ItemEvicted?.Invoke(key, item.Value);
        }
        finally
        {
            _isEvicting = false;
        }

        return true;
    }

    /// <summary>
    /// Stamps an entry's expiration metadata from the per-entry override or the dictionary default. No-op when
    /// expiration is disabled.
    /// </summary>
    /// <param name="item">The cache item to stamp.</param>
    /// <param name="ttlOverride">
    /// The per-entry time-to-live, or <see langword="null" /> to apply the dictionary default.
    /// </param>
    private void SetExpiration(CacheItem item, TimeSpan? ttlOverride)
    {
        if (_timeProvider is null)
            return;

        long ttlTicks = (ttlOverride ?? Expiration!.TimeToLive)?.Ticks ?? 0L;

        item.TtlTicks = ttlTicks;
        item.ExpiresAtTicks = ttlTicks > 0 ? ComputeDeadline(GetNowTicks(), ttlTicks) : long.MaxValue;
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if no <see cref="Expiration" /> configuration was supplied at
    /// construction. Guards the per-entry time-to-live overloads, which require a time provider.
    /// </summary>
    private void ThrowIfExpirationNotConfigured()
    {
        if (Expiration is null) throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_ExpirationNotConfigured);
    }

    /// <summary>
    /// Attempts to retrieve the live (non-expired) cache item for the specified key. An expired entry is lazily removed
    /// (raising the eviction events) and reported as absent.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="slide">
    /// <see langword="true" /> to refresh the entry's deadline on a hit when the expiration kind is sliding;
    /// <see langword="false" /> to leave the deadline untouched.
    /// </param>
    /// <param name="item">When this method returns <see langword="true" />, the live cache item.</param>
    /// <returns>
    /// <see langword="true" /> if a live entry exists for <paramref name="key" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// When expiration is disabled this reduces to a plain store lookup with no clock reads, preserving the
    /// capacity-only hot path.
    /// </remarks>
    private bool TryGetLiveItem(TKey key, bool slide, [NotNullWhen(true)] out CacheItem? item)
    {
        if (!_store.TryGetValue(key, out item))
            return false;

        if (_timeProvider is null)
            return true;

        long nowTicks = GetNowTicks();
        if (item.ExpiresAtTicks <= nowTicks)
        {
            RemoveExpiredEntry(key, item);
            item = null;
            return false;
        }

        if (slide && _slidingExpiration && item.TtlTicks > 0)
            item.ExpiresAtTicks = ComputeDeadline(nowTicks, item.TtlTicks);

        return true;
    }
}
