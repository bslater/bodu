// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPattern.Enumerator.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu;

public readonly partial struct WeekPattern
{
    /// <summary>
    /// Enumerates the selected days of a <see cref="WeekPattern" /> in Sunday-first order without allocating.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returned by <see cref="WeekPattern.GetEnumerator" /> and bound directly by the C# <c>foreach</c> pattern,
    /// so iterating a <see cref="WeekPattern" /> requires no heap allocation. Accessing the enumerator through
    /// <see cref="IEnumerator{T}" /> or <see cref="IEnumerator" /> boxes this struct as a one-off cost.
    /// </para>
    /// <para>
    /// The enumerator captures the underlying bitmask at construction; the source <see cref="WeekPattern" /> is
    /// a value type and immutable, so the snapshot semantics are exact.
    /// </para>
    /// </remarks>
    public struct Enumerator : IEnumerator<DayOfWeek>
    {
        private readonly byte _bits;
        private int _index;
        private DayOfWeek _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct over the supplied bitmask.
        /// </summary>
        /// <param name="bits">The packed selected-day bitmask sourced from the originating pattern.</param>
        internal Enumerator(byte bits)
        {
            _bits = bits;
            _index = -1;
            _current = default;
        }

        /// <summary>
        /// Gets the <see cref="DayOfWeek" /> at the current position of the enumerator.
        /// </summary>
        /// <returns>The day yielded by the most recent successful <see cref="MoveNext" /> call.</returns>
        public readonly DayOfWeek Current => _current;

        /// <inheritdoc />
        readonly object IEnumerator.Current => _current;

        /// <summary>
        /// Advances the enumerator to the next selected day.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> when a further selected day is available and <see cref="Current" /> has been
        /// updated; <see langword="false" /> when the enumerator has passed the end of the pattern.
        /// </returns>
        public bool MoveNext()
        {
            while (++_index < 7)
            {
                if ((_bits & (ShiftValue << (6 - _index))) != 0)
                {
                    _current = (DayOfWeek)_index;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Resets the enumerator to its initial position, before the first selected day.
        /// </summary>
        public void Reset()
        {
            _index = -1;
            _current = default;
        }

        /// <inheritdoc />
        public readonly void Dispose()
        {
            // Nothing to release; the enumerator owns no managed or unmanaged resources.
        }
    }
}
