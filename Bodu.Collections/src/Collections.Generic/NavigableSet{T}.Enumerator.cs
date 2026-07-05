// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSet{T}.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public sealed partial class NavigableSet<T>
{
    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Enumerates the elements of a <see cref="NavigableSet{T}" /> in ascending comparer order without allocating.
    /// </summary>
    /// <remarks>
    /// The enumerator captures the set's structural version on construction and advances through parent-pointer
    /// successor steps. Any structural mutation invalidates the enumerator and causes <see cref="MoveNext" /> or
    /// <see cref="Reset" /> to throw <see cref="InvalidOperationException" />.
    /// </remarks>
    [Serializable]
    public struct Enumerator
        : IEnumerator<T>
    {
        /// <summary>The set being enumerated.</summary>
        private readonly NavigableSet<T> _set;

        /// <summary>The version captured from <see cref="_set" /> at construction.</summary>
        private readonly int _version;

        /// <summary>The next node to yield, or <see langword="null" /> when the walk is exhausted.</summary>
        private Node? _next;

        /// <summary>The item returned by <see cref="Current" /> for the current position.</summary>
        private T _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct over the specified set.
        /// </summary>
        /// <param name="owner">The set to enumerate.</param>
        internal Enumerator(NavigableSet<T> owner)
        {
            _set = owner;
            _version = owner._version;
            _next = owner._root == null ? null : MinimumNode(owner._root);
            _current = default!;
        }

        /// <inheritdoc />
        public readonly T Current => _current;

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _set._version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            if (_next == null)
                return false;

            _current = _next.Item;
            _next = SuccessorNode(_next);
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (_version != _set._version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            _next = _set._root == null ? null : MinimumNode(_set._root);
            _current = default!;
        }

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }
    }
}
