// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSet.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Numerics;

namespace Bodu;

public readonly partial struct DayOfWeekSet
{
    /// <summary>
    /// Enumerates the days a <see cref="DayOfWeekSet" /> selects, in <see cref="DayOfWeek" /> order, Sunday first,
    /// without allocating.
    /// </summary>
    /// <remarks>
    /// Returned by <see cref="DayOfWeekSet.GetEnumerator" /> and bound directly by <c>foreach</c>. The enumerator
    /// captures the set's bits when it is created; a <see cref="DayOfWeekSet" /> is immutable, so the snapshot is
    /// exact.
    /// </remarks>
    public struct Enumerator
        : IEnumerator<DayOfWeek>
    {
        /// <summary>The bits captured at construction.</summary>
        private readonly ulong _bits;

        /// <summary>The bits of the days not yet returned.</summary>
        private ulong _remaining;

        /// <summary>The day most recently returned.</summary>
        private DayOfWeek _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct over the specified bits.
        /// </summary>
        /// <param name="bits">The bits of the set to enumerate.</param>
        internal Enumerator(ulong bits)
        {
            _bits = bits;
            _remaining = bits;
            _current = default;
        }

        /// <summary>
        /// Gets the day at the current position of the enumerator.
        /// </summary>
        /// <value>
        /// The current day; undefined before the first call to <see cref="MoveNext" /> and after it returns
        /// <see langword="false" />.
        /// </value>
        public readonly DayOfWeek Current =>
            _current;

        /// <inheritdoc />
        readonly object IEnumerator.Current =>
            _current;

        /// <summary>
        /// Advances the enumerator to the next selected day.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> when the enumerator moved to another day; <see langword="false" /> when every
        /// selected day has been returned.
        /// </returns>
        public bool MoveNext()
        {
            if (_remaining == 0)
                return false;

            _current = (DayOfWeek)BitOperations.TrailingZeroCount(_remaining);
            _remaining &= _remaining - 1;
            return true;
        }

        /// <summary>
        /// Returns the enumerator to its initial position, before the first selected day.
        /// </summary>
        public void Reset()
        {
            _remaining = _bits;
            _current = default;
        }

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }
    }
}
