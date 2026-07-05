// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTree{T}.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public sealed partial class IntervalTree<T>
{
    /// <inheritdoc />
    IEnumerator<(T Low, T High)> IEnumerable<(T Low, T High)>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Enumerates the intervals of an <see cref="IntervalTree{T}" /> in ascending (low, high) order without allocating,
    /// repeating duplicate intervals once per stored occurrence.
    /// </summary>
    /// <remarks>
    /// The enumerator captures the tree's structural version on construction and advances through parent-pointer
    /// successor steps. Any structural mutation invalidates the enumerator and causes <see cref="MoveNext" /> or
    /// <see cref="Reset" /> to throw <see cref="InvalidOperationException" />.
    /// </remarks>
    [Serializable]
    public struct Enumerator
        : IEnumerator<(T Low, T High)>
    {
        /// <summary>The tree being enumerated.</summary>
        private readonly IntervalTree<T> _tree;

        /// <summary>The version captured from <see cref="_tree" /> at construction.</summary>
        private readonly int _version;

        /// <summary>The next node to yield, or <see langword="null" /> when the walk is exhausted.</summary>
        private Node? _next;

        /// <summary>The number of further repetitions of the current interval still owed by its multiplicity.</summary>
        private int _pending;

        /// <summary>The interval returned by <see cref="Current" /> for the current position.</summary>
        private (T Low, T High) _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct over the specified tree.
        /// </summary>
        /// <param name="owner">The tree to enumerate.</param>
        internal Enumerator(IntervalTree<T> owner)
        {
            _tree = owner;
            _version = owner._version;
            _next = owner._root == null ? null : MinimumNode(owner._root);
            _pending = 0;
            _current = default!;
        }

        /// <inheritdoc />
        public readonly (T Low, T High) Current => _current;

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _tree._version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            if (_pending > 0)
            {
                // Repeat the current interval for its remaining duplicate occurrences.
                _pending--;
                return true;
            }

            if (_next == null)
                return false;

            _current = (_next.Low, _next.High);
            _pending = _next.Multiplicity - 1;
            _next = SuccessorNode(_next);
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (_version != _tree._version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            _next = _tree._root == null ? null : MinimumNode(_tree._root);
            _pending = 0;
            _current = default!;
        }

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }
    }
}
