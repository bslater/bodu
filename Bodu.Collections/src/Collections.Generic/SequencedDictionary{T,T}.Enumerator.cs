// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionary{T,T}.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class SequencedDictionary<TKey, TValue>
{
    /// <summary>
    /// Returns an enumerator that iterates through the dictionary in iteration order.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> over the dictionary's entries.</returns>
    /// <remarks>
    /// Entries are produced in insertion order, or access order when access ordering is enabled. Any mutation of the
    /// dictionary — including reads that reposition entries in access-order mode — invalidates the enumerator.
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
    /// Enumerates the entries of a <see cref="SequencedDictionary{TKey, TValue}" /> in iteration order without
    /// allocating.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use the <see langword="foreach" /> statement to enumerate the dictionary rather than using this struct directly.
    /// </para>
    /// <para>
    /// The enumerator captures the dictionary's version on construction. Any mutation of the dictionary (including
    /// reads that reposition entries in access-order mode) invalidates the enumerator and causes
    /// <see cref="MoveNext" /> or <see cref="Reset" /> to throw <see cref="InvalidOperationException" />.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct Enumerator
        : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        /// <summary>Sentinel state value: enumeration has not started; <see cref="MoveNext" /> begins the walk.</summary>
        private const int StateNotStarted = 0;

        /// <summary>Sentinel state value: the enumerator is positioned on an entry exposed by <see cref="Current" />.</summary>
        private const int StateActive = 1;

        /// <summary>Sentinel state value: enumeration has completed; further <see cref="MoveNext" /> calls return <see langword="false" />.</summary>
        private const int StateEnded = 2;

        /// <summary>The dictionary whose entries this enumerator iterates over.</summary>
        private readonly SequencedDictionary<TKey, TValue> _dictionary;

        /// <summary>The dictionary version captured at construction, used to detect concurrent modification.</summary>
        private readonly int _version;

        /// <summary>The ordering node for the current entry, or <see langword="null" /> when not positioned on one.</summary>
        private LinkedListNode<TKey>? _node;

        /// <summary>The entry returned by <see cref="Current" /> for the current position.</summary>
        private KeyValuePair<TKey, TValue> _current;

        /// <summary>The enumeration state: one of <see cref="StateNotStarted" />, <see cref="StateActive" />, or <see cref="StateEnded" />.</summary>
        private int _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct over the specified dictionary, capturing
        /// its version.
        /// </summary>
        /// <param name="dictionary">The dictionary to enumerate.</param>
        internal Enumerator(SequencedDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
            _version = dictionary._version;
            _node = null;
            _current = default;
            _state = StateNotStarted;
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

            // Fail fast before touching the node links: a mutation may have detached the current node, whose Next
            // link would otherwise end the walk silently instead of throwing.
            if (_version != _dictionary._version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            LinkedListNode<TKey>? node = _state == StateNotStarted ? _dictionary._order.First : _node!.Next;
            if (node is null)
            {
                _node = null;
                _current = default;
                _state = StateEnded;
                return false;
            }

            _node = node;
            _current = new KeyValuePair<TKey, TValue>(node.Value, _dictionary._store[node.Value].Value);
            _state = StateActive;
            return true;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Restarts the walk from the beginning against the version captured at construction.
        /// </remarks>
        public void Reset()
        {
            if (_version != _dictionary._version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            _node = null;
            _current = default;
            _state = StateNotStarted;
        }
    }
}
