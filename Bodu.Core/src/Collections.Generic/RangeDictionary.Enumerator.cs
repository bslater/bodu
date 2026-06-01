// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionary.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public sealed partial class RangeDictionary<TKey, TValue>
{
    /// <inheritdoc />
    IEnumerator<ValueRange<TKey, TValue>> IEnumerable<ValueRange<TKey, TValue>>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Enumerates the entries of a <see cref="RangeDictionary{TKey, TValue}" /> in ascending start order without
    /// allocating.
    /// </summary>
    /// <remarks>
    /// The enumerator captures the dictionary's version on construction. Any structural mutation invalidates the
    /// enumerator and causes <see cref="MoveNext" /> or <see cref="Reset" /> to throw
    /// <see cref="InvalidOperationException" />.
    /// </remarks>
    [Serializable]
    public struct Enumerator : IEnumerator<ValueRange<TKey, TValue>>
    {
        /// <summary>
        /// The dictionary being enumerated.
        /// </summary>
        private readonly RangeDictionary<TKey, TValue> _owner;

        /// <summary>
        /// The version captured from <c>owner</c> at construction.
        /// </summary>
        private readonly int _version;

        /// <summary>
        /// The index of the next entry to yield.
        /// </summary>
        private int _index;

        /// <summary>
        /// The entry returned by <see cref="Current" /> for the current position.
        /// </summary>
        private ValueRange<TKey, TValue> _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct over the specified dictionary.
        /// </summary>
        /// <param name="owner">The dictionary to enumerate.</param>
        internal Enumerator(RangeDictionary<TKey, TValue> owner)
        {
            _owner = owner;
            _version = owner._version;
            _index = 0;
            _current = default;
        }

        /// <inheritdoc />
        public readonly ValueRange<TKey, TValue> Current => _current;

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _owner._version)
                throw new InvalidOperationException(ResourceStrings.Op_Invalid_CollectionModified);

            if (_index >= _owner._count)
                return false;

            _current = new ValueRange<TKey, TValue>(
                _owner._starts[_index],
                _owner._ends[_index],
                _owner._values[_index],
                skipValidation: true);

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
