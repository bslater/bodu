// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictionPolicyCore{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Provides the shared eviction-policy engine consumed by <see cref="EvictingDictionary{TKey, TValue}" /> and the
/// segments of <c>ConcurrentEvictingDictionary&lt;TKey, TValue&gt;</c>: candidate selection for every
/// <see cref="EvictingDictionaryPolicy" />, recency-list and frequency-bucket bookkeeping, the Second-Chance clock
/// sweep, and touch re-linking.
/// </summary>
/// <typeparam name="TKey">The type of keys tracked by the owning cache.</typeparam>
/// <typeparam name="TValue">The type of values stored by the owning cache.</typeparam>
/// <remarks>
/// <para>
/// The engine owns the backing store and the policy tracking structures and centralizes only the policy bookkeeping.
/// Everything the two consumers deliberately do differently — locking, eviction events, enumeration versioning,
/// time-based expiration, and count publication — stays in the consumer, which sequences calls into this engine from
/// its own mutation paths.
/// </para>
/// <para>
/// The engine performs no synchronization of its own. The non-concurrent dictionary calls it single-threaded; the
/// concurrent dictionary guards every call with the owning segment's stripe lock.
/// </para>
/// </remarks>
internal sealed class EvictionPolicyCore<TKey, TValue>
    where TKey : notnull
{
    /// <summary>The eviction policy driving candidate selection and bookkeeping.</summary>
    private readonly EvictingDictionaryPolicy _policy;

    /// <summary>The backing store mapping each key to its cached value and bookkeeping metadata.</summary>
    private readonly Dictionary<TKey, EvictionEntry<TKey, TValue>> _store;

    /// <summary>Tracks key ordering, used by the FIFO, LRU, MRU, and SecondChance policies; <see langword="null" /> otherwise.</summary>
    private readonly LinkedList<TKey>? _order;

    /// <summary>Maps access frequency to the keys observed at that frequency, used by the least-frequently-used policy; <see langword="null" /> otherwise.</summary>
    private readonly SortedDictionary<int, LinkedList<TKey>>? _frequencyList;

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictionPolicyCore{TKey, TValue}" /> class with the specified
    /// policy and key comparer, creating the tracking structure the policy requires: FIFO, LRU, MRU, and SecondChance
    /// use a linked list; LFU uses a sorted dictionary of frequency lists; RandomReplacement needs no tracking.
    /// </summary>
    /// <param name="policy">The eviction policy used when capacity is exceeded.</param>
    /// <param name="comparer">The equality comparer used for key identity and hash-table lookup.</param>
    internal EvictionPolicyCore(EvictingDictionaryPolicy policy, IEqualityComparer<TKey> comparer)
    {
        _policy = policy;
        _store = new Dictionary<TKey, EvictionEntry<TKey, TValue>>(comparer);

        switch (policy)
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
    /// Gets the backing store mapping each key to its cached value and bookkeeping metadata. Consumers read it directly
    /// for lookups, counting, expiry sweeps, and enumeration.
    /// </summary>
    internal Dictionary<TKey, EvictionEntry<TKey, TValue>> Store => _store;

    /// <summary>
    /// Gets the key ordering list used by the FIFO, LRU, MRU, and SecondChance policies, or <see langword="null" />
    /// when the policy does not track order. Consumers read it directly for policy-ordered enumeration.
    /// </summary>
    internal LinkedList<TKey>? Order => _order;

    /// <summary>
    /// Gets the frequency-bucket map used by the least-frequently-used policy, or <see langword="null" /> when the
    /// policy does not track frequency. Consumers read it directly for policy-ordered enumeration.
    /// </summary>
    internal SortedDictionary<int, LinkedList<TKey>>? FrequencyList => _frequencyList;

    /// <summary>
    /// Records a newly created entry in the policy tracking structures and the store: recency-tracked policies append
    /// the key to the order list, LeastFrequentlyUsed places it in the bucket for its initial frequency, and the entry
    /// is stored under its key.
    /// </summary>
    /// <param name="key">The key of the new entry.</param>
    /// <param name="entry">
    /// The entry to record; its <see cref="EvictionEntry{TKey, TValue}.Node" /> is assigned.
    /// </param>
    internal void AddNewEntry(TKey key, EvictionEntry<TKey, TValue> entry)
    {
        if (_order is not null)
            entry.Node = _order.AddLast(key);

        if (_policy == EvictingDictionaryPolicy.LeastFrequentlyUsed)
            entry.Node = AddToFrequencyList(entry.Frequency, key);

        _store[key] = entry;
    }

    /// <summary>
    /// Unlinks the entry from every policy tracking structure and removes it from the store.
    /// </summary>
    /// <param name="key">The key of the entry to remove.</param>
    /// <param name="entry">The entry's cache item.</param>
    internal void RemoveEntry(TKey key, EvictionEntry<TKey, TValue> entry)
    {
        if (_order is not null && entry.Node is not null)
            _order.Remove(entry.Node);

        if (_policy == EvictingDictionaryPolicy.LeastFrequentlyUsed)
            RemoveFromFrequencyList(entry);

        _store.Remove(key);
    }

    /// <summary>
    /// Removes all entries from the store and resets the policy tracking structures.
    /// </summary>
    internal void Clear()
    {
        _store.Clear();
        _order?.Clear();
        _frequencyList?.Clear();
    }

    /// <summary>
    /// Records an access against the policy for the specified entry: recency-tracked policies re-link the entry's node
    /// to the most-recently-used position, LeastFrequentlyUsed moves it to the next frequency bucket, and SecondChance
    /// sets its reference flag.
    /// </summary>
    /// <param name="key">The key that was accessed.</param>
    /// <param name="entry">The associated cache entry for the key.</param>
    /// <returns>
    /// <see langword="true" /> if the touch structurally repositioned the entry (LeastRecentlyUsed, MostRecentlyUsed,
    /// or LeastFrequentlyUsed), so consumers that version their enumeration can invalidate in-flight enumerators;
    /// <see langword="false" /> when only the SecondChance flag was set or the policy ignores touches.
    /// </returns>
    /// <remarks>
    /// Re-links the existing node instead of allocating a fresh one per touch — this is the consumers' hottest path
    /// (every read under a recency policy, held under the stripe lock in the concurrent dictionary).
    /// </remarks>
    internal bool Touch(TKey key, EvictionEntry<TKey, TValue> entry)
    {
        switch (_policy)
        {
            case EvictingDictionaryPolicy.LeastRecentlyUsed:
            case EvictingDictionaryPolicy.MostRecentlyUsed:
                if (_order is not null)
                {
                    if (entry.Node is not null)
                    {
                        _order.Remove(entry.Node);
                        _order.AddLast(entry.Node);
                    }
                    else
                    {
                        entry.Node = _order.AddLast(key);
                    }

                    return true;
                }

                break;

            case EvictingDictionaryPolicy.LeastFrequentlyUsed:
                MoveToNextFrequencyBucket(entry, key);
                return true;

            case EvictingDictionaryPolicy.SecondChance:
                entry.SecondChance = true;
                break;
        }

        return false;
    }

    /// <summary>
    /// Selects the next eviction victim according to the policy, without removing it.
    /// </summary>
    /// <param name="keyToRemove">
    /// When this method returns <see langword="true" />, the key selected for eviction.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the policy produced a candidate; <see langword="false" /> when the tracking
    /// structures hold no candidate (the consumer decides whether that is an at-capacity invariant violation).
    /// </returns>
    /// <remarks>
    /// Success is reported explicitly rather than via a null sentinel: relying on <c>keyToRemove is not null</c> is
    /// unreliable when <typeparamref name="TKey" /> is a value type because <c>default(TKey)</c> (e.g. <c>0</c>) may
    /// itself be a valid key. The SecondChance selection mutates the tracking state (clears reference bits and cycles
    /// spared nodes to the tail) per the clock algorithm.
    /// </remarks>
    internal bool TrySelectEvictionCandidate(out TKey keyToRemove)
    {
        keyToRemove = default!;

        switch (_policy)
        {
            case EvictingDictionaryPolicy.FirstInFirstOut:
            case EvictingDictionaryPolicy.LeastRecentlyUsed:
                if (_order?.First is { } firstNode)
                {
                    keyToRemove = firstNode.Value;
                    return true;
                }

                break;

            case EvictingDictionaryPolicy.MostRecentlyUsed:
                if (_order?.Last is { } lastNode)
                {
                    keyToRemove = lastNode.Value;
                    return true;
                }

                break;

            case EvictingDictionaryPolicy.LeastFrequentlyUsed:
                if (PeekLeastFrequentNode() is LinkedListNode<TKey> lfuNode)
                {
                    keyToRemove = lfuNode.Value;
                    return true;
                }

                break;

            case EvictingDictionaryPolicy.RandomReplacement:
                if (_store.Count > 0)
                {
                    // Walk the key collection's struct enumerator to the drawn index rather than routing
                    // through LINQ ElementAt — same O(n) worst case and the same selected element for a
                    // given draw, but no enumerator boxing or LINQ dispatch layers.
                    int skip = Random.Shared.Next(_store.Count);
                    foreach (TKey key in _store.Keys)
                    {
                        if (skip-- == 0)
                        {
                            keyToRemove = key;
                            break;
                        }
                    }

                    return true;
                }

                break;

            case EvictingDictionaryPolicy.SecondChance:
                if (_order?.Count > 0)
                {
                    keyToRemove = GetSecondChanceCandidate();
                    return true;
                }

                break;
        }

        return false;
    }

    /// <summary>
    /// Returns the key that would be evicted next based on the policy and current tracking state, as a pure read that
    /// mutates nothing.
    /// </summary>
    /// <returns>The key next in line for eviction, or <see langword="default" /> when the store is empty.</returns>
    internal TKey? PeekEvictionCandidate()
    {
        return _policy switch
        {
            EvictingDictionaryPolicy.FirstInFirstOut or EvictingDictionaryPolicy.LeastRecentlyUsed
                when _order?.First is not null => _order.First.Value,

            EvictingDictionaryPolicy.MostRecentlyUsed
                when _order?.Last is not null => _order.Last.Value,

            EvictingDictionaryPolicy.LeastFrequentlyUsed
                when PeekLeastFrequentNode() is { } lfuCandidate
                => lfuCandidate.Value,

            EvictingDictionaryPolicy.RandomReplacement
                when _store.Count > 0 => PeekFirstStoreKey(),

            EvictingDictionaryPolicy.SecondChance when _order is not null => PeekSecondChanceCandidate(),

            _ => default
        };
    }

    /// <summary>
    /// Returns the head node of the lowest-frequency LeastFrequentlyUsed bucket, or <see langword="null" /> when the
    /// frequency list is absent, empty, or its first bucket holds no keys.
    /// </summary>
    /// <returns>
    /// The least-frequently-used key's bucket node, or <see langword="null" /> when there is no candidate.
    /// </returns>
    /// <remarks>
    /// <see cref="SortedDictionary{TKey, TValue}" /> enumerates in ascending key order, so only the first bucket is
    /// inspected — a single <c>MoveNext</c> rather than a LINQ <c>First()</c> chain.
    /// </remarks>
    private LinkedListNode<TKey>? PeekLeastFrequentNode()
    {
        if (_frequencyList is null)
            return null;

        foreach (KeyValuePair<int, LinkedList<TKey>> bucket in _frequencyList)
            return bucket.Value?.First;

        return null;
    }

    /// <summary>
    /// Returns the first key produced by the store's enumeration order, used as the RandomReplacement peek candidate.
    /// </summary>
    /// <returns>The first key in the store's enumeration order.</returns>
    /// <remarks>
    /// Callers must ensure the store is non-empty; the fallback value is never observed.
    /// </remarks>
    private TKey PeekFirstStoreKey()
    {
        foreach (TKey key in _store.Keys)
            return key;

        return default!;
    }

    /// <summary>
    /// Returns the key that would be selected next by the Second-Chance algorithm, without modifying any state.
    /// </summary>
    /// <returns>
    /// The candidate key for eviction, or the oldest key if all items have their second-chance flag set.
    /// </returns>
    private TKey? PeekSecondChanceCandidate()
    {
        foreach (TKey key in _order!)
        {
            if (_store.TryGetValue(key, out EvictionEntry<TKey, TValue>? entry) && !entry.SecondChance)
                return key;
        }

        return _order.First is not null ? _order.First.Value : default;
    }

    /// <summary>
    /// Finds the next candidate for eviction using the Second-Chance algorithm. Items with their second-chance flag set
    /// are moved to the end of the list and cleared. If no eligible item is found, the oldest item is returned.
    /// </summary>
    /// <returns>The key to evict according to the Second-Chance strategy.</returns>
    /// <remarks>
    /// Called only from <see cref="TrySelectEvictionCandidate" /> after that method has already verified the order list
    /// is non-empty, so no defensive empty-list checks are performed here. The clock sweep preserves the total node
    /// count by removing and re-appending each cycled item, so <c>_order.First</c> is guaranteed to be non-null after
    /// the loop completes.
    /// </remarks>
    private TKey GetSecondChanceCandidate()
    {
        LinkedListNode<TKey>? node = _order!.First;
        while (node is not null)
        {
            TKey key = node.Value;
            LinkedListNode<TKey> current = node;
            node = node.Next;

            if (!_store.TryGetValue(key, out EvictionEntry<TKey, TValue>? entry))
                continue;

            if (!entry.SecondChance)
                return key;

            // Clock algorithm: clear the reference bit and cycle the item to the tail, giving it one extra pass before
            // eviction. Re-link the existing node rather than allocating a replacement.
            entry.SecondChance = false;
            _order.Remove(current);
            _order.AddLast(current);
        }

        return _order.First!.Value;
    }

    /// <summary>
    /// Adds the specified key to the LeastFrequentlyUsed frequency bucket for the given frequency.
    /// </summary>
    /// <param name="frequency">The new frequency count.</param>
    /// <param name="key">The key to add.</param>
    /// <returns>The bucket node representing the key, stored on the entry for O(1) comparer-agnostic removal.</returns>
    private LinkedListNode<TKey> AddToFrequencyList(int frequency, TKey key)
    {
        if (!_frequencyList!.TryGetValue(frequency, out LinkedList<TKey>? list))
        {
            list = new LinkedList<TKey>();
            _frequencyList[frequency] = list;
        }

        return list.AddLast(key);
    }

    /// <summary>
    /// Increments the entry's access frequency and moves its node from the previous LeastFrequentlyUsed bucket to the
    /// bucket for the new frequency, re-linking the existing node so a touch allocates nothing.
    /// </summary>
    /// <param name="entry">The cache entry being touched.</param>
    /// <param name="key">The key of the entry, used only when the entry has no node yet.</param>
    private void MoveToNextFrequencyBucket(EvictionEntry<TKey, TValue> entry, TKey key)
    {
        LinkedListNode<TKey>? node = entry.Node;
        if (node is { List: { } previousBucket })
        {
            previousBucket.Remove(node);

            if (previousBucket.Count == 0)
                _frequencyList!.Remove(entry.Frequency);
        }

        entry.Frequency++;

        if (!_frequencyList!.TryGetValue(entry.Frequency, out LinkedList<TKey>? bucket))
        {
            bucket = new LinkedList<TKey>();
            _frequencyList[entry.Frequency] = bucket;
        }

        if (node is not null)
            bucket.AddLast(node);
        else
            entry.Node = bucket.AddLast(key);
    }

    /// <summary>
    /// Removes the entry's node from its LeastFrequentlyUsed frequency bucket. Cleans up the bucket if it becomes
    /// empty.
    /// </summary>
    /// <param name="entry">The cache entry whose bucket node is being removed.</param>
    /// <remarks>
    /// Operates on the stored <see cref="EvictionEntry{TKey, TValue}.Node" /> rather than searching the bucket by key:
    /// a by-key <see cref="LinkedList{T}.Remove(T)" /> compares with <see cref="EqualityComparer{T}.Default" /> and
    /// misses keys stored under a custom comparer, leaving ghost entries that corrupt eviction order and eventually
    /// starve candidate selection. The node form is also O(1) instead of an O(bucket) scan.
    /// </remarks>
    private void RemoveFromFrequencyList(EvictionEntry<TKey, TValue> entry)
    {
        if (entry.Node is { List: { } list })
        {
            list.Remove(entry.Node);

            if (list.Count == 0)
                _frequencyList!.Remove(entry.Frequency);
        }
    }
}
