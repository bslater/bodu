// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionary{T,T}.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Globalization;

using Bodu.Collections.Generic.Internal;

namespace Bodu.Collections.Generic;

public partial class EvictingDictionary<TKey, TValue>
{
    /// <summary>
    /// Returns an enumerator that iterates through the dictionary in the order defined by the current
    /// <see cref="EvictingDictionaryPolicy" />.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> over the dictionary's live entries.</returns>
    /// <remarks>
    /// When time-based expiration is configured, the clock is read once when the enumerator is created and expired
    /// entries are filtered out (never removed) against that snapshot, so a single enumeration observes a stable set
    /// and never refreshes sliding deadlines.
    /// </remarks>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <inheritdoc />
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Enumerates the live entries of an <see cref="EvictingDictionary{TKey, TValue}" /> in policy order without
    /// allocating.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use the <see langword="foreach" /> statement to enumerate the dictionary rather than using this struct directly.
    /// </para>
    /// <para>
    /// The enumerator captures the dictionary's version — and, when time-based expiration is configured, a single clock
    /// snapshot — on construction. Any mutation of the dictionary (including reads that reposition entries under the
    /// LeastRecentlyUsed, MostRecentlyUsed, or LeastFrequentlyUsed policies) invalidates the enumerator and causes
    /// <see cref="MoveNext" /> or <see cref="Reset" /> to throw <see cref="InvalidOperationException" />. Expired
    /// entries are filtered against the captured clock snapshot and are never removed by enumeration.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct Enumerator
        : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        /// <summary>Sentinel state value: enumeration has not started; <see cref="MoveNext" /> begins the walk.</summary>
        private const int StateNotStarted = 0;

        /// <summary>Sentinel state value: the enumerator is positioned on a live entry exposed by <see cref="Current" />.</summary>
        private const int StateActive = 1;

        /// <summary>Sentinel state value: enumeration has completed; further <see cref="MoveNext" /> calls return <see langword="false" />.</summary>
        private const int StateEnded = 2;

        /// <summary>The dictionary whose entries this enumerator iterates over.</summary>
        private readonly EvictingDictionary<TKey, TValue> _dictionary;

        /// <summary>The dictionary version captured at construction, used to detect concurrent modification.</summary>
        private readonly int _version;

        /// <summary>Indicates whether time-based expiration filtering applies to this enumeration.</summary>
        private readonly bool _checkExpiry;

        /// <summary>The clock snapshot, in UTC ticks, that expired entries are filtered against; <c>0</c> when expiration is disabled.</summary>
        private readonly long _nowTicks;

        /// <summary>The order-list or frequency-bucket node for the current entry under the node-walking policies.</summary>
        private LinkedListNode<TKey>? _node;

        /// <summary>The frequency-bucket cursor used by the LeastFrequentlyUsed policy.</summary>
        private SortedDictionary<int, LinkedList<TKey>>.Enumerator _frequencyBuckets;

        /// <summary>The store cursor used by the RandomReplacement policy.</summary>
        private Dictionary<TKey, EvictionEntry<TKey, TValue>>.Enumerator _storeEntries;

        /// <summary>The entry returned by <see cref="Current" /> for the current position.</summary>
        private KeyValuePair<TKey, TValue> _current;

        /// <summary>The enumeration state: one of <see cref="StateNotStarted" />, <see cref="StateActive" />, or <see cref="StateEnded" />.</summary>
        private int _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct over the specified dictionary, capturing
        /// its version and, when expiration is configured, a single clock snapshot.
        /// </summary>
        /// <param name="dictionary">The dictionary to enumerate.</param>
        internal Enumerator(EvictingDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
            _version = dictionary._version;
            _checkExpiry = dictionary._timeProvider is not null;
            _nowTicks = _checkExpiry ? dictionary.GetNowTicks() : 0L;
            _node = null;
            _frequencyBuckets = default;
            _storeEntries = default;
            _current = default;
            _state = StateNotStarted;

            // Only the cursor the policy walks is created; the remaining cursors stay default and are never touched.
            if (dictionary.Policy == EvictingDictionaryPolicy.LeastFrequentlyUsed && dictionary._frequencyList is not null)
                _frequencyBuckets = dictionary._frequencyList.GetEnumerator();
            else if (dictionary.Policy == EvictingDictionaryPolicy.RandomReplacement)
                _storeEntries = dictionary._store.GetEnumerator();
        }

        /// <inheritdoc />
        public readonly KeyValuePair<TKey, TValue> Current =>
            _state == StateActive
                ? _current
                : throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_EnumeratorNotOnElement);

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc />
        public readonly void Dispose()
        {
            // No unmanaged resources; method provided for interface completeness.
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_state == StateEnded)
                return false;

            // Fail fast before touching any cursor state: a mutation may have detached the current node, whose links
            // would otherwise end the walk silently instead of throwing.
            if (_version != _dictionary._version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            switch (_dictionary.Policy)
            {
                case EvictingDictionaryPolicy.FirstInFirstOut:
                case EvictingDictionaryPolicy.LeastRecentlyUsed:
                case EvictingDictionaryPolicy.SecondChance:
                {
                    LinkedListNode<TKey>? node = _state == StateNotStarted ? _dictionary._order?.First : _node!.Next;
                    while (node is not null)
                    {
                        if (TrySetCurrent(node.Value))
                        {
                            _node = node;
                            return true;
                        }

                        node = node.Next;
                    }

                    break;
                }

                case EvictingDictionaryPolicy.MostRecentlyUsed:
                {
                    LinkedListNode<TKey>? node = _state == StateNotStarted ? _dictionary._order?.Last : _node!.Previous;
                    while (node is not null)
                    {
                        if (TrySetCurrent(node.Value))
                        {
                            _node = node;
                            return true;
                        }

                        node = node.Previous;
                    }

                    break;
                }

                case EvictingDictionaryPolicy.LeastFrequentlyUsed:
                {
                    if (_dictionary._frequencyList is null)
                        break;

                    LinkedListNode<TKey>? node = _state == StateActive ? _node!.Next : null;
                    while (true)
                    {
                        if (node is null)
                        {
                            // The current bucket is exhausted (or the walk has not started): advance to the next
                            // frequency bucket, which SortedDictionary yields in ascending frequency order.
                            if (!_frequencyBuckets.MoveNext())
                                break;

                            node = _frequencyBuckets.Current.Value.First;
                            continue;
                        }

                        if (TrySetCurrent(node.Value))
                        {
                            _node = node;
                            return true;
                        }

                        node = node.Next;
                    }

                    break;
                }

                case EvictingDictionaryPolicy.RandomReplacement:
                {
                    while (_storeEntries.MoveNext())
                    {
                        KeyValuePair<TKey, EvictionEntry<TKey, TValue>> pair = _storeEntries.Current;
                        if (!(_checkExpiry && pair.Value.ExpiresAtTicks <= _nowTicks))
                        {
                            _current = new KeyValuePair<TKey, TValue>(pair.Key, pair.Value.Value);
                            _state = StateActive;
                            return true;
                        }
                    }

                    break;
                }

                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.Op_Invalid_UnknownEvictionPolicy, _dictionary.Policy));
            }

            _node = null;
            _current = default;
            _state = StateEnded;
            return false;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Restarts the walk from the beginning against the same version and clock snapshot captured at construction.
        /// </remarks>
        public void Reset()
        {
            if (_version != _dictionary._version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            _node = null;
            _current = default;
            _state = StateNotStarted;

            if (_dictionary.Policy == EvictingDictionaryPolicy.LeastFrequentlyUsed && _dictionary._frequencyList is not null)
                _frequencyBuckets = _dictionary._frequencyList.GetEnumerator();
            else if (_dictionary.Policy == EvictingDictionaryPolicy.RandomReplacement)
                _storeEntries = _dictionary._store.GetEnumerator();
        }

        /// <summary>
        /// Loads the live entry for the specified key into <see cref="Current" />, skipping keys that are missing from
        /// the store or expired against the captured clock snapshot.
        /// </summary>
        /// <param name="key">The key produced by the policy walk.</param>
        /// <returns>
        /// <see langword="true" /> if the key resolved to a live entry and the enumerator is now positioned on it;
        /// otherwise, <see langword="false" />.
        /// </returns>
        private bool TrySetCurrent(TKey key)
        {
            if (_dictionary._store.TryGetValue(key, out EvictionEntry<TKey, TValue>? entry) && !(_checkExpiry && entry.ExpiresAtTicks <= _nowTicks))
            {
                _current = new KeyValuePair<TKey, TValue>(key, entry.Value);
                _state = StateActive;
                return true;
            }

            return false;
        }
    }
}
