// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTree{T,T}.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public sealed partial class IntervalTree<TKey, TValue>
{
    /// <inheritdoc />
    IEnumerator<(TKey Low, TKey High, TValue Value)> IEnumerable<(TKey Low, TKey High, TValue Value)>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Enumerates the entries of an <see cref="IntervalTree{TKey, TValue}" /> in ascending (low, high) order without
    /// allocating, yielding an interval's values in insertion order.
    /// </summary>
    /// <remarks>
    /// The enumerator captures the tree's structural version on construction and advances through parent-pointer
    /// successor steps. Any structural mutation invalidates the enumerator and causes <see cref="MoveNext" /> or
    /// <see cref="Reset" /> to throw <see cref="InvalidOperationException" />.
    /// </remarks>
    [Serializable]
    public struct Enumerator
        : IEnumerator<(TKey Low, TKey High, TValue Value)>
    {
        /// <summary>The tree being enumerated.</summary>
        private readonly IntervalTree<TKey, TValue> _tree;

        /// <summary>The version captured from <see cref="_tree" /> at construction.</summary>
        private readonly int _version;

        /// <summary>The node currently being drained, or <see langword="null" /> before the first advance.</summary>
        private Node? _node;

        /// <summary>The next node to drain after <see cref="_node" />, or <see langword="null" /> when exhausted.</summary>
        private Node? _next;

        /// <summary>The index of the current entry within <see cref="_node" />'s value list.</summary>
        private int _valueIndex;

        /// <summary>The entry returned by <see cref="Current" /> for the current position.</summary>
        private (TKey Low, TKey High, TValue Value) _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct over the specified tree.
        /// </summary>
        /// <param name="owner">The tree to enumerate.</param>
        internal Enumerator(IntervalTree<TKey, TValue> owner)
        {
            _tree = owner;
            _version = owner._version;
            _node = null;
            _next = owner._root == null ? null : MinimumNode(owner._root);
            _valueIndex = 0;
            _current = default!;
        }

        /// <inheritdoc />
        public readonly (TKey Low, TKey High, TValue Value) Current => _current;

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _tree._version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            // Drain the current node's remaining values before stepping to the successor.
            if (_node != null && _valueIndex + 1 < _node.Values.Count)
            {
                _valueIndex++;
                _current = (_node.Low, _node.High, _node.Values[_valueIndex]);
                return true;
            }

            if (_next == null)
            {
                _node = null;
                return false;
            }

            _node = _next;
            _valueIndex = 0;
            _current = (_node.Low, _node.High, _node.Values[0]);
            _next = SuccessorNode(_node);
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (_version != _tree._version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            _node = null;
            _next = _tree._root == null ? null : MinimumNode(_tree._root);
            _valueIndex = 0;
            _current = default!;
        }

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }
    }
}
