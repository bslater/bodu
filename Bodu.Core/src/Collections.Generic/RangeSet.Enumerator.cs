// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeSet.Enumerator.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public sealed partial class RangeSet<T>
{
    /// <inheritdoc />
    IEnumerator<Range<T>> IEnumerable<Range<T>>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Enumerates the ranges of a <see cref="RangeSet{T}" /> in ascending start order without allocating.
    /// </summary>
    /// <remarks>
    /// The enumerator captures the set's version on construction. Any structural mutation invalidates the enumerator
    /// and causes <see cref="MoveNext" /> or <see cref="Reset" /> to throw <see cref="InvalidOperationException" />.
    /// </remarks>
    [Serializable]
    public struct Enumerator : IEnumerator<Range<T>>
    {
        /// <summary>
        /// The set being enumerated.
        /// </summary>
        private readonly RangeSet<T> _owner;

        /// <summary>
        /// The version captured from <see cref="_owner" /> at construction.
        /// </summary>
        private readonly int _version;

        /// <summary>
        /// The index of the next range to yield.
        /// </summary>
        private int _index;

        /// <summary>
        /// The range returned by <see cref="Current" /> for the current position.
        /// </summary>
        private Range<T> _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct over the specified set.
        /// </summary>
        /// <param name="owner">The set to enumerate.</param>
        internal Enumerator(RangeSet<T> owner)
        {
            _owner = owner;
            _version = owner._version;
            _index = 0;
            _current = default;
        }

        /// <inheritdoc />
        public readonly Range<T> Current => _current;

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _owner._version)
                throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionModified);

            if (_index >= _owner._count)
                return false;

            _current = new Range<T>(_owner._starts[_index], _owner._ends[_index]);
            _index++;
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (_version != _owner._version)
                throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionModified);

            _index = 0;
            _current = default;
        }

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }
    }
}
