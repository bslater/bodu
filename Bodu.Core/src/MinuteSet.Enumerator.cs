// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MinuteSet.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Numerics;

namespace Bodu;

public readonly partial struct MinuteSet
{
    /// <summary>
    /// Enumerates the minutes a <see cref="MinuteSet" /> selects, in ascending order, without allocating.
    /// </summary>
    /// <remarks>
    /// Returned by <see cref="MinuteSet.GetEnumerator" /> and bound directly by <c>foreach</c>. The enumerator captures
    /// the set's bits when it is created; a <see cref="MinuteSet" /> is immutable, so the snapshot is exact.
    /// </remarks>
    public struct Enumerator
        : IEnumerator<int>
    {
        /// <summary>The bits captured at construction.</summary>
        private readonly ulong _bits;

        /// <summary>The bits of the minutes not yet returned.</summary>
        private ulong _remaining;

        /// <summary>The minute most recently returned.</summary>
        private int _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct over the specified bits.
        /// </summary>
        /// <param name="bits">The bits of the set to enumerate.</param>
        internal Enumerator(ulong bits)
        {
            _bits = bits;
            _remaining = bits;
            _current = 0;
        }

        /// <summary>
        /// Gets the minute at the current position of the enumerator.
        /// </summary>
        /// <value>
        /// The current minute; undefined before the first call to <see cref="MoveNext" /> and after it returns
        /// <see langword="false" />.
        /// </value>
        public readonly int Current =>
            _current;

        /// <inheritdoc />
        readonly object IEnumerator.Current =>
            _current;

        /// <summary>
        /// Advances the enumerator to the next selected minute.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> when the enumerator moved to another minute; <see langword="false" /> when every
        /// selected minute has been returned.
        /// </returns>
        public bool MoveNext()
        {
            if (_remaining == 0)
                return false;

            _current = MinimumValue + BitOperations.TrailingZeroCount(_remaining);
            _remaining &= _remaining - 1;
            return true;
        }

        /// <summary>
        /// Returns the enumerator to its initial position, before the first selected minute.
        /// </summary>
        public void Reset()
        {
            _remaining = _bits;
            _current = 0;
        }

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }
    }
}
