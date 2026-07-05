// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSet.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public sealed partial class BitSet
{
    /// <summary>
    /// Returns a non-boxing enumerator that yields the indices of the set bits in ascending order.
    /// </summary>
    /// <returns>An <see cref="Enumerator" /> positioned before the first set bit.</returns>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <inheritdoc />
    IEnumerator<int> IEnumerable<int>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Enumerates the set-bit indices of a <see cref="BitSet" /> in ascending order without allocating.
    /// </summary>
    /// <remarks>
    /// The enumerator captures the set's version on construction. Any mutation invalidates the enumerator and causes
    /// <see cref="MoveNext" /> or <see cref="Reset" /> to throw <see cref="InvalidOperationException" />.
    /// </remarks>
    [Serializable]
    public struct Enumerator
        : IEnumerator<int>
    {
        /// <summary>The set being enumerated.</summary>
        private readonly BitSet _bitSet;

        /// <summary>The version captured from <see cref="_bitSet" /> at construction.</summary>
        private readonly int _version;

        /// <summary>The lowest bit index the next <see cref="MoveNext" /> call may yield.</summary>
        private int _next;

        /// <summary>The index returned by <see cref="Current" /> for the current position.</summary>
        private int _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct over the specified set.
        /// </summary>
        /// <param name="bitSet">The set to enumerate.</param>
        internal Enumerator(BitSet bitSet)
        {
            _bitSet = bitSet;
            _version = bitSet._version;
            _next = 0;
            _current = default;
        }

        /// <inheritdoc />
        public readonly int Current => _current;

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_version != _bitSet._version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            var index = _bitSet.NextSetBitCore(_next);
            if (index < 0)
                return false;

            _current = index;
            _next = index + 1;
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            if (_version != _bitSet._version)
                throw new InvalidOperationException(CollectionsResourceStrings.Op_Invalid_CollectionModified);

            _next = 0;
            _current = default;
        }

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }
    }
}
