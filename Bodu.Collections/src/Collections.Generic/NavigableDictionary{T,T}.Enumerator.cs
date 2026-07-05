// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionary{T,T}.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public sealed partial class NavigableDictionary<TKey, TValue>
{
    /// <inheritdoc />
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Enumerates the entries of a <see cref="NavigableDictionary{TKey, TValue}" /> in ascending key order without
    /// allocating.
    /// </summary>
    /// <remarks>
    /// The enumerator captures the dictionary's structural version on construction and advances through parent-pointer
    /// successor steps. Any structural mutation invalidates the enumerator and causes <see cref="MoveNext" /> or
    /// <see cref="Reset" /> to throw <see cref="InvalidOperationException" />.
    /// </remarks>
    [Serializable]
    public struct Enumerator
        : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        /// <summary>The dictionary being enumerated.</summary>
        private readonly NavigableDictionary<TKey, TValue> _dictionary;

        /// <summary>The version captured from <see cref="_dictionary" /> at construction.</summary>
        private readonly int _version;

        /// <summary>The next node to yield, or <see langword="null" /> when the walk is exhausted.</summary>
        private Node? _next;

        /// <summary>The entry returned by <see cref="Current" /> for the current position.</summary>
        private KeyValuePair<TKey, TValue> _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct over the specified dictionary.
        /// </summary>
        /// <param name="owner">The dictionary to enumerate.</param>
        internal Enumerator(NavigableDictionary<TKey, TValue> owner)
        {
            _dictionary = owner;
            _version = owner._version;
            _next = owner._root == null ? null : MinimumNode(owner._root);
            _current = default;
        }

        /// <inheritdoc />
        public readonly KeyValuePair<TKey, TValue> Current => _current;

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _dictionary._version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            if (_next == null)
                return false;

            _current = _next.Entry;
            _next = SuccessorNode(_next);
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (_version != _dictionary._version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            _next = _dictionary._root == null ? null : MinimumNode(_dictionary._root);
            _current = default;
        }

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }
    }
}
