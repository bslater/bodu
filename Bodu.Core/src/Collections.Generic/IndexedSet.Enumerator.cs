// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSet.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;

namespace Bodu.Collections.Generic;

public partial class IndexedSet<T>
{
    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Enumerates the elements of an <see cref="IndexedSet{T}" /> in insertion order without allocating.
    /// </summary>
    /// <remarks>
    /// The enumerator captures the set's version on construction. Any structural mutation invalidates the
    /// enumerator and causes <see cref="MoveNext" /> or <see cref="Reset" /> to throw
    /// <see cref="InvalidOperationException" />.
    /// </remarks>
    [Serializable]
    public struct Enumerator : IEnumerator<T>
    {
        /// <summary>
        /// The set being enumerated.
        /// </summary>
        private readonly IndexedSet<T> _owner;

        /// <summary>
        /// The version captured from <see cref="_owner" /> at construction.
        /// </summary>
        private readonly int _version;

        /// <summary>
        /// The index of the next item to yield.
        /// </summary>
        private int _index;

        /// <summary>
        /// The item returned by <see cref="Current" /> for the current position.
        /// </summary>
        private T _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct over the specified set.
        /// </summary>
        /// <param name="owner">The set to enumerate.</param>
        internal Enumerator(IndexedSet<T> owner)
        {
            _owner = owner;
            _version = owner._version;
            _index = 0;
            _current = default!;
        }

        /// <inheritdoc />
        public T Current => _current;

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _owner._version)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionModified);

            if (_index >= _owner._count)
                return false;

            _current = _owner._items[_index];
            _index++;
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (_version != _owner._version)
                throw new InvalidOperationException(ResourceStrings.InvalidOperation_CollectionModified);

            _index = 0;
            _current = default!;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
