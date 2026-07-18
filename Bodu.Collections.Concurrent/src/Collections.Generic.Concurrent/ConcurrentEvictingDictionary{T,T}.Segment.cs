// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionary{T,T}.Segment.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Bodu.Collections.Generic.Internal;

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentEvictingDictionary<TKey, TValue>
{
    /// <summary>
    /// Represents one lock stripe of the dictionary: a small, exact policy cache over a private slice of the total
    /// capacity. The parent guards every call into a segment with that segment's stripe lock; segments therefore run
    /// the same single-threaded eviction mechanics as <see cref="EvictingDictionary{TKey, TValue}" /> without any
    /// internal synchronization of their own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Removals caused by capacity pressure or expiry are never surfaced as events from inside the segment (a handler
    /// running under the stripe lock could block the stripe or deadlock). Instead, every mutating method takes a
    /// <c>ref</c> eviction buffer that the parent drains — raising <see cref="ItemEvicted" /> and updating
    /// <see cref="EvictionCount" /> — after the stripe lock has been released.
    /// </para>
    /// <para>
    /// The only member safe to call without the stripe lock is <see cref="CountApproximate" />, which reads a count
    /// published with release semantics after each mutation.
    /// </para>
    /// </remarks>
    private sealed class Segment
    {
        /// <summary>The maximum number of entries this segment may hold; a slice of the parent's total capacity.</summary>
        private readonly int _capacity;

        /// <summary>The eviction policy shared by every segment of the parent dictionary.</summary>
        private readonly EvictingDictionaryPolicy _policy;

        /// <summary>The shared eviction-policy engine that owns the store and policy tracking structures and implements candidate selection, touch re-linking, and frequency-bucket bookkeeping.</summary>
        private readonly EvictionPolicyCore<TKey, TValue> _core;

        /// <summary>Maps access frequency to the keys observed at that frequency, used by the least-frequently-used policy. Alias of the engine's structure for direct enumeration access.</summary>
        private readonly SortedDictionary<int, LinkedList<TKey>> _frequencyList;

        /// <summary>Tracks key ordering, used by the FIFO, LRU, MRU, and SecondChance policies. Alias of the engine's structure for direct enumeration access.</summary>
        private readonly LinkedList<TKey> _order;

        /// <summary>The backing store mapping each key to its cached value and bookkeeping metadata. Alias of the engine's store for direct lookup access.</summary>
        private readonly Dictionary<TKey, EvictionEntry<TKey, TValue>> _store;

        /// <summary>Indicates whether the configured expiration kind is sliding. <see langword="false" /> when expiration is disabled or absolute.</summary>
        private readonly bool _slidingExpiration;

        /// <summary>The default time-to-live in ticks applied to entries added without a per-entry override; zero when entries have no default lifetime.</summary>
        private readonly long _defaultTtlTicks;

        /// <summary>The time source driving expiration clock reads, or <see langword="null" /> when expiration is disabled.</summary>
        private readonly TimeProvider? _timeProvider;

        /// <summary>The entry count published with volatile semantics after every mutation so the parent's lock-free <see cref="ApproximateCount" /> can read it without taking the stripe lock.</summary>
        private int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="Segment" /> class with the specified capacity slice, policy,
        /// key comparer, and expiration configuration.
        /// </summary>
        /// <param name="capacity">The maximum number of entries this segment may hold. Always at least one.</param>
        /// <param name="policy">The eviction policy used when the segment's capacity is exceeded.</param>
        /// <param name="comparer">The key comparer shared with the parent dictionary.</param>
        /// <param name="expiration">
        /// The time-based expiration configuration, or <see langword="null" /> to disable expiry.
        /// </param>
        internal Segment(int capacity, EvictingDictionaryPolicy policy, IEqualityComparer<TKey> comparer, EvictingDictionaryExpiration? expiration)
        {
            _capacity = capacity;
            _policy = policy;

            _timeProvider = expiration?.TimeProvider;
            _slidingExpiration = expiration?.Kind == EvictingDictionaryExpirationKind.Sliding;
            _defaultTtlTicks = expiration?.TimeToLive?.Ticks ?? 0L;

            // The engine creates the tracking structures the policy requires; the aliases below give the
            // snapshot and lookup paths direct field access to the same objects.
            _core = new EvictionPolicyCore<TKey, TValue>(policy, comparer);
            _store = _core.Store;
            _order = _core.Order!;
            _frequencyList = _core.FrequencyList!;
        }

        /// <summary>
        /// Gets the segment's capacity slice. Exposed so the parent can assert partitioning invariants in tests.
        /// </summary>
        internal int Capacity => _capacity;

        /// <summary>
        /// Gets the segment's published entry count without requiring the stripe lock.
        /// </summary>
        internal int CountApproximate => Volatile.Read(ref _count);

        /// <summary>
        /// Gets the segment's raw entry count, including expired-but-unpurged entries. The caller must hold the stripe
        /// lock.
        /// </summary>
        internal int CountUnsafe => _store.Count;

        /// <summary>
        /// Adds the specified key and value, or replaces the value if a live entry for the key already exists. Evicts
        /// per the policy when the segment is full, preferring expired entries as victims. The caller must hold the
        /// stripe lock.
        /// </summary>
        /// <param name="key">The key of the element to add or update.</param>
        /// <param name="value">The value to associate with <paramref name="key" />.</param>
        /// <param name="ttlOverride">
        /// The per-entry time-to-live, or <see langword="null" /> to apply the dictionary default.
        /// </param>
        /// <param name="evicted">
        /// The eviction buffer that receives entries removed by expiry or capacity pressure.
        /// </param>
        internal void AddOrReplace(TKey key, TValue value, TimeSpan? ttlOverride, ref List<KeyValuePair<TKey, TValue>>? evicted)
        {
            if (_store.TryGetValue(key, out EvictionEntry<TKey, TValue>? existing))
            {
                if (_timeProvider is not null && existing.ExpiresAtTicks <= GetNowTicks())
                {
                    // The existing entry has expired: remove it as an expiry eviction and fall through to a fresh add
                    // so the new entry starts with fresh eviction metadata.
                    RemoveEntry(key, existing);
                    (evicted ??= new List<KeyValuePair<TKey, TValue>>()).Add(new KeyValuePair<TKey, TValue>(key, existing.Value));
                }
                else
                {
                    existing.Value = value;
                    SetExpiration(existing, ttlOverride);
                    _core.Touch(key, existing);
                    return;
                }
            }

            if (_store.Count >= _capacity)
            {
                // Expired entries are preferred victims: purge them before consulting the capacity policy.
                PurgeExpired(ref evicted);

                if (_store.Count >= _capacity)
                    EvictOne(ref evicted);
            }

            var item = new EvictionEntry<TKey, TValue>(value);
            SetExpiration(item, ttlOverride);

            _core.AddNewEntry(key, item);
            PublishCount();
        }

        /// <summary>
        /// Attempts to add the specified key and value without replacing an existing live entry. The caller must hold
        /// the stripe lock.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value to associate with <paramref name="key" />.</param>
        /// <param name="ttlOverride">
        /// The per-entry time-to-live, or <see langword="null" /> to apply the dictionary default.
        /// </param>
        /// <param name="evicted">
        /// The eviction buffer that receives entries removed by expiry or capacity pressure.
        /// </param>
        /// <returns>
        /// <see langword="true" /> if the entry was added; <see langword="false" /> if a live entry already exists.
        /// </returns>
        internal bool TryAdd(TKey key, TValue value, TimeSpan? ttlOverride, ref List<KeyValuePair<TKey, TValue>>? evicted)
        {
            if (TryGetLiveItem(key, slide: false, ref evicted, out _))
                return false;

            AddOrReplace(key, value, ttlOverride, ref evicted);
            return true;
        }

        /// <summary>
        /// Attempts to retrieve the value for the specified key, counting a hit as a policy access and sliding the
        /// expiration deadline. The caller must hold the stripe lock.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="evicted">The eviction buffer that receives a lazily removed expired entry.</param>
        /// <param name="value">When this method returns <see langword="true" />, the value for the key.</param>
        /// <returns><see langword="true" /> if a live entry exists; otherwise, <see langword="false" />.</returns>
        internal bool TryGet(TKey key, ref List<KeyValuePair<TKey, TValue>>? evicted, out TValue value)
        {
            if (TryGetLiveItem(key, slide: true, ref evicted, out EvictionEntry<TKey, TValue>? item))
            {
                value = item.Value;
                _core.Touch(key, item);
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Determines whether a live entry exists for the specified key, sliding the expiration deadline on a hit but
        /// leaving policy metadata untouched. The caller must hold the stripe lock.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <param name="evicted">The eviction buffer that receives a lazily removed expired entry.</param>
        /// <returns><see langword="true" /> if a live entry exists; otherwise, <see langword="false" />.</returns>
        internal bool Contains(TKey key, ref List<KeyValuePair<TKey, TValue>>? evicted) =>
            TryGetLiveItem(key, slide: true, ref evicted, out _);

        /// <summary>
        /// Marks the specified key as recently accessed for the capacity policy without sliding the expiration
        /// deadline. The caller must hold the stripe lock.
        /// </summary>
        /// <param name="key">The key to touch.</param>
        /// <param name="evicted">The eviction buffer that receives a lazily removed expired entry.</param>
        /// <returns>
        /// <see langword="true" /> if a live entry exists and was touched; otherwise, <see langword="false" />.
        /// </returns>
        internal bool Touch(TKey key, ref List<KeyValuePair<TKey, TValue>>? evicted)
        {
            if (TryGetLiveItem(key, slide: false, ref evicted, out EvictionEntry<TKey, TValue>? item))
            {
                _core.Touch(key, item);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the entry for the specified key, whether live or expired-but-unpurged, without treating the removal
        /// as an eviction. The caller must hold the stripe lock.
        /// </summary>
        /// <param name="key">The key of the entry to remove.</param>
        /// <param name="value">When this method returns <see langword="true" />, the removed entry's value.</param>
        /// <returns><see langword="true" /> if an entry was removed; otherwise, <see langword="false" />.</returns>
        internal bool TryRemove(TKey key, out TValue value)
        {
            if (_store.TryGetValue(key, out EvictionEntry<TKey, TValue>? item))
            {
                RemoveEntry(key, item);
                value = item.Value;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Reads the value of the live entry for the specified key without counting a policy access or sliding the
        /// expiration deadline, mirroring the pure-read semantics of <see cref="ContainsKey" />. The caller must hold
        /// the stripe lock.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="evicted">The eviction buffer that receives a lazily removed expired entry.</param>
        /// <param name="value">When this method returns <see langword="true" />, the live entry's value.</param>
        /// <returns><see langword="true" /> if a live entry exists; otherwise, <see langword="false" />.</returns>
        internal bool TryPeek(TKey key, ref List<KeyValuePair<TKey, TValue>>? evicted, out TValue value)
        {
            if (TryGetLiveItem(key, slide: false, ref evicted, out EvictionEntry<TKey, TValue>? item))
            {
                value = item.Value;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Atomically replaces the value of the live entry for the specified key with <paramref name="newValue" />, but
        /// only when its current value equals <paramref name="comparisonValue" /> under
        /// <see cref="EqualityComparer{TValue}.Default" />. A successful update counts as a policy access and starts a
        /// fresh expiration lease, matching the replace branch of <see cref="AddOrReplace" />. The caller must hold the
        /// stripe lock.
        /// </summary>
        /// <param name="key">The key whose value to update.</param>
        /// <param name="newValue">The value to store when the comparison succeeds.</param>
        /// <param name="comparisonValue">The value the current entry must equal for the update to occur.</param>
        /// <param name="evicted">The eviction buffer that receives a lazily removed expired entry.</param>
        /// <returns>
        /// <see langword="true" /> if a live entry existed and its value matched <paramref name="comparisonValue" />;
        /// otherwise, <see langword="false" />.
        /// </returns>
        internal bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue, ref List<KeyValuePair<TKey, TValue>>? evicted)
        {
            if (TryGetLiveItem(key, slide: false, ref evicted, out EvictionEntry<TKey, TValue>? item)
                && EqualityComparer<TValue>.Default.Equals(item.Value, comparisonValue))
            {
                item.Value = newValue;
                SetExpiration(item, null);
                _core.Touch(key, item);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Atomically updates the value of the live entry for the specified key using
        /// <paramref name="updateValueFactory" />, or adds a new entry from <paramref name="addValue" /> or
        /// <paramref name="addValueFactory" /> when no live entry exists. Both factories run under the stripe lock so
        /// each is invoked at most once. The caller must hold the stripe lock.
        /// </summary>
        /// <param name="key">The key of the element to add or update.</param>
        /// <param name="addValue">
        /// The value used when <paramref name="addValueFactory" /> is <see langword="null" /> and the key is absent.
        /// </param>
        /// <param name="addValueFactory">
        /// The factory that produces the value to add when the key is absent, or <see langword="null" /> to use
        /// <paramref name="addValue" />.
        /// </param>
        /// <param name="updateValueFactory">
        /// The factory that produces the replacement value from the existing value when the key is present.
        /// </param>
        /// <param name="evicted">
        /// The eviction buffer that receives entries removed by expiry or capacity pressure.
        /// </param>
        /// <returns>The new value stored for <paramref name="key" />.</returns>
        internal TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue>? addValueFactory, Func<TKey, TValue, TValue> updateValueFactory, ref List<KeyValuePair<TKey, TValue>>? evicted)
        {
            if (TryGetLiveItem(key, slide: false, ref evicted, out EvictionEntry<TKey, TValue>? existing))
            {
                TValue updated = updateValueFactory(key, existing.Value);
                existing.Value = updated;
                SetExpiration(existing, null);
                _core.Touch(key, existing);
                return updated;
            }

            TValue added = addValueFactory is null ? addValue : addValueFactory(key);
            AddOrReplace(key, added, null, ref evicted);
            return added;
        }

        /// <summary>
        /// Removes the entry for the specified key, but only when it is live and its value equals
        /// <paramref name="value" /> under <see cref="EqualityComparer{TValue}.Default" />. The removal is not treated
        /// as an eviction. The caller must hold the stripe lock.
        /// </summary>
        /// <param name="key">The key of the entry to remove.</param>
        /// <param name="value">The value the live entry must equal for the removal to occur.</param>
        /// <param name="evicted">The eviction buffer that receives a lazily removed expired entry.</param>
        /// <returns>
        /// <see langword="true" /> if a matching live entry was removed; otherwise, <see langword="false" />.
        /// </returns>
        internal bool TryRemovePair(TKey key, TValue value, ref List<KeyValuePair<TKey, TValue>>? evicted)
        {
            if (TryGetLiveItem(key, slide: false, ref evicted, out EvictionEntry<TKey, TValue>? item)
                && EqualityComparer<TValue>.Default.Equals(item.Value, value))
            {
                RemoveEntry(key, item);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes every expired entry from the segment, appending each to the eviction buffer, and returns the number
        /// removed. Returns <c>0</c> without reading the clock when expiration is disabled. The caller must hold the
        /// stripe lock.
        /// </summary>
        /// <param name="evicted">The eviction buffer that receives the removed entries.</param>
        /// <returns>The number of expired entries removed.</returns>
        internal int PurgeExpired(ref List<KeyValuePair<TKey, TValue>>? evicted)
        {
            if (_timeProvider is null || _store.Count == 0)
                return 0;

            long nowTicks = GetNowTicks();
            List<KeyValuePair<TKey, EvictionEntry<TKey, TValue>>>? expired = null;

            foreach (KeyValuePair<TKey, EvictionEntry<TKey, TValue>> pair in _store)
            {
                if (pair.Value.ExpiresAtTicks <= nowTicks)
                    (expired ??= new List<KeyValuePair<TKey, EvictionEntry<TKey, TValue>>>()).Add(pair);
            }

            if (expired is null)
                return 0;

            foreach (KeyValuePair<TKey, EvictionEntry<TKey, TValue>> pair in expired)
            {
                RemoveEntry(pair.Key, pair.Value);
                (evicted ??= new List<KeyValuePair<TKey, TValue>>()).Add(new KeyValuePair<TKey, TValue>(pair.Key, pair.Value.Value));
            }

            return expired.Count;
        }

        /// <summary>
        /// Removes all entries and resets the segment's tracking structures. The caller must hold the stripe lock.
        /// </summary>
        internal void Clear()
        {
            _core.Clear();
            PublishCount();
        }

        /// <summary>
        /// Appends a projection of the segment's live entries to <paramref name="target" /> in policy order, filtering
        /// expired entries against the supplied clock snapshot without removing them or sliding deadlines. The caller
        /// must hold the stripe lock.
        /// </summary>
        /// <typeparam name="TResult">The projected element type.</typeparam>
        /// <param name="target">
        /// The array receiving the projected entries; must have room for the segment's raw count from
        /// <paramref name="index" />.
        /// </param>
        /// <param name="index">
        /// The next write position in <paramref name="target" />, advanced per appended entry.
        /// </param>
        /// <param name="nowTicks">The clock snapshot used to filter expired entries.</param>
        /// <param name="checkExpiry">
        /// <see langword="true" /> when expiration is configured and filtering applies.
        /// </param>
        /// <param name="selector">The projection applied to each live key/value pair.</param>
        /// <exception cref="InvalidOperationException">The eviction policy is unrecognized.</exception>
        internal void AppendSnapshot<TResult>(TResult[] target, ref int index, long nowTicks, bool checkExpiry, Func<TKey, TValue, TResult> selector)
        {
            switch (_policy)
            {
                case EvictingDictionaryPolicy.FirstInFirstOut:
                case EvictingDictionaryPolicy.LeastRecentlyUsed:
                case EvictingDictionaryPolicy.SecondChance:
                    foreach (TKey key in _order)
                    {
                        if (_store.TryGetValue(key, out EvictionEntry<TKey, TValue>? item) && !(checkExpiry && item.ExpiresAtTicks <= nowTicks))
                            target[index++] = selector(key, item.Value);
                    }

                    break;

                case EvictingDictionaryPolicy.MostRecentlyUsed:
                    for (LinkedListNode<TKey>? node = _order.Last; node is not null; node = node.Previous)
                    {
                        if (_store.TryGetValue(node.Value, out EvictionEntry<TKey, TValue>? item) && !(checkExpiry && item.ExpiresAtTicks <= nowTicks))
                            target[index++] = selector(node.Value, item.Value);
                    }

                    break;

                case EvictingDictionaryPolicy.LeastFrequentlyUsed:
                    foreach (KeyValuePair<int, LinkedList<TKey>> freq in _frequencyList)
                    {
                        foreach (TKey key in freq.Value)
                        {
                            if (_store.TryGetValue(key, out EvictionEntry<TKey, TValue>? item) && !(checkExpiry && item.ExpiresAtTicks <= nowTicks))
                                target[index++] = selector(key, item.Value);
                        }
                    }

                    break;

                case EvictingDictionaryPolicy.RandomReplacement:
                    foreach ((TKey key, EvictionEntry<TKey, TValue> item) in _store)
                    {
                        if (!(checkExpiry && item.ExpiresAtTicks <= nowTicks))
                            target[index++] = selector(key, item.Value);
                    }

                    break;

                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ConcurrentCollectionsResourceStrings.Op_Invalid_UnknownEvictionPolicy, _policy));
            }
        }

        /// <summary>
        /// Computes the absolute expiration deadline for an entry, saturating at <see cref="long.MaxValue" /> instead
        /// of overflowing for very large time-to-live values.
        /// </summary>
        /// <param name="nowTicks">The current time in UTC ticks.</param>
        /// <param name="ttlTicks">The entry's time-to-live in ticks; must be positive.</param>
        /// <returns>The deadline in UTC ticks at or after which the entry counts as expired.</returns>
        private static long ComputeDeadline(long nowTicks, long ttlTicks) =>
            nowTicks > long.MaxValue - ttlTicks ? long.MaxValue : nowTicks + ttlTicks;

        /// <summary>
        /// Removes the next entry to be evicted based on the segment's policy, appending it to the eviction buffer.
        /// </summary>
        /// <param name="evicted">The eviction buffer that receives the removed entry.</param>
        /// <exception cref="InvalidOperationException">
        /// The configured eviction policy failed to produce a candidate while the segment is at or above its capacity,
        /// indicating the internal tracking structures have become desynchronized from the underlying store.
        /// </exception>
        private void EvictOne(ref List<KeyValuePair<TKey, TValue>>? evicted)
        {
            // The engine reports success explicitly rather than via a null sentinel because default(TKey) may itself
            // be a valid key when TKey is a value type.
            if (_core.TrySelectEvictionCandidate(out TKey keyToRemove) && _store.TryGetValue(keyToRemove, out EvictionEntry<TKey, TValue>? item))
            {
                RemoveEntry(keyToRemove, item);
                (evicted ??= new List<KeyValuePair<TKey, TValue>>()).Add(new KeyValuePair<TKey, TValue>(keyToRemove, item.Value));
                return;
            }

            // No candidate was produced. If the store is still at capacity the caller (AddOrReplace) would exceed the
            // limit, so fail loudly rather than silently corrupting the invariant.
            if (_store.Count >= _capacity) throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ConcurrentCollectionsResourceStrings.Op_Invalid_EvictionProducedNoCandidate, _policy));
        }

        /// <summary>
        /// Reads the current time from the configured time provider, in UTC ticks. Must only be called when expiration
        /// is enabled.
        /// </summary>
        /// <returns>The current UTC time in ticks.</returns>
        private long GetNowTicks() =>
            _timeProvider!.GetUtcNow().UtcTicks;

        /// <summary>
        /// Publishes the store's current count with volatile semantics for the parent's lock-free
        /// <see cref="CountApproximate" /> reads.
        /// </summary>
        private void PublishCount() =>
            Volatile.Write(ref _count, _store.Count);

        /// <summary>
        /// Unlinks and removes the entry for the specified key from the store and every policy tracking structure.
        /// </summary>
        /// <param name="key">The key of the entry to remove.</param>
        /// <param name="item">The entry's cache item.</param>
        private void RemoveEntry(TKey key, EvictionEntry<TKey, TValue> item)
        {
            _core.RemoveEntry(key, item);
            PublishCount();
        }

        /// <summary>
        /// Stamps an entry's expiration metadata from the per-entry override or the dictionary default. No-op when
        /// expiration is disabled.
        /// </summary>
        /// <param name="item">The cache item to stamp.</param>
        /// <param name="ttlOverride">
        /// The per-entry time-to-live, or <see langword="null" /> to apply the dictionary default.
        /// </param>
        private void SetExpiration(EvictionEntry<TKey, TValue> item, TimeSpan? ttlOverride)
        {
            if (_timeProvider is null)
                return;

            long ttlTicks = ttlOverride?.Ticks ?? _defaultTtlTicks;

            item.TtlTicks = ttlTicks;
            item.ExpiresAtTicks = ttlTicks > 0 ? ComputeDeadline(GetNowTicks(), ttlTicks) : long.MaxValue;
        }

        /// <summary>
        /// Attempts to retrieve the live (non-expired) cache item for the specified key. An expired entry is lazily
        /// removed, appended to the eviction buffer, and reported as absent.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="slide">
        /// <see langword="true" /> to refresh the entry's deadline on a hit when the expiration kind is sliding;
        /// <see langword="false" /> to leave the deadline untouched.
        /// </param>
        /// <param name="evicted">The eviction buffer that receives a lazily removed expired entry.</param>
        /// <param name="item">When this method returns <see langword="true" />, the live cache item.</param>
        /// <returns>
        /// <see langword="true" /> if a live entry exists for <paramref name="key" />; otherwise,
        /// <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// When expiration is disabled this reduces to a plain store lookup with no clock reads, preserving the
        /// capacity-only hot path.
        /// </remarks>
        private bool TryGetLiveItem(TKey key, bool slide, ref List<KeyValuePair<TKey, TValue>>? evicted, [NotNullWhen(true)] out EvictionEntry<TKey, TValue>? item)
        {
            if (!_store.TryGetValue(key, out item))
                return false;

            if (_timeProvider is null)
                return true;

            long nowTicks = GetNowTicks();
            if (item.ExpiresAtTicks <= nowTicks)
            {
                RemoveEntry(key, item);
                (evicted ??= new List<KeyValuePair<TKey, TValue>>()).Add(new KeyValuePair<TKey, TValue>(key, item.Value));
                item = null;
                return false;
            }

            if (slide && _slidingExpiration && item.TtlTicks > 0)
                item.ExpiresAtTicks = ComputeDeadline(nowTicks, item.TtlTicks);

            return true;
        }
    }
}
