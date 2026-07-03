// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteIntervalPair{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// Represents the result of a binary <see cref="DiscreteInterval{T}" /> set operation as zero, one, or two disjoint,
/// non-adjacent intervals in ascending order — the discrete counterpart to <see cref="IntervalPair{T}" />.
/// </summary>
/// <typeparam name="T">The integer type used for the intervals' endpoints.</typeparam>
/// <remarks>
/// Returned by <see cref="DiscreteInterval{T}.Difference(DiscreteInterval{T})" /> and
/// <see cref="DiscreteInterval{T}.SymmetricDifference(DiscreteInterval{T})" />. Because subtracting one integer
/// interval from another leaves at most a left and a right remainder, the result never needs more than two pieces, so
/// this allocation-free value type stores them inline.
/// </remarks>
public readonly struct DiscreteIntervalPair<T>
    where T : IBinaryInteger<T>
{
    /// <summary>
    /// The first (lower) run; only meaningful when <see cref="_count" /> is at least one.
    /// </summary>
    private readonly DiscreteInterval<T> _first;

    /// <summary>
    /// The second (upper) run; only meaningful when <see cref="_count" /> is two.
    /// </summary>
    private readonly DiscreteInterval<T> _second;

    /// <summary>
    /// The number of non-empty disjoint runs this pair holds (0, 1, or 2).
    /// </summary>
    private readonly int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscreteIntervalPair{T}" /> struct from already-packed, ordered
    /// pieces.
    /// </summary>
    /// <param name="first">The first (lower) piece.</param>
    /// <param name="second">The second (upper) piece.</param>
    /// <param name="count">The number of non-empty pieces.</param>
    private DiscreteIntervalPair(DiscreteInterval<T> first, DiscreteInterval<T> second, int count)
    {
        _first = first;
        _second = second;
        _count = count;
    }

    /// <summary>
    /// Gets the empty result — zero pieces.
    /// </summary>
    /// <value>A <see cref="DiscreteIntervalPair{T}" /> whose <see cref="Count" /> is zero.</value>
    public static DiscreteIntervalPair<T> Empty =>
        default;

    /// <summary>
    /// Gets the number of disjoint intervals in the result (0, 1, or 2).
    /// </summary>
    /// <value>The count of non-empty pieces.</value>
    public int Count =>
        _count;

    /// <summary>
    /// Gets a value indicating whether the result contains no intervals.
    /// </summary>
    /// <value><see langword="true" /> when <see cref="Count" /> is zero; otherwise <see langword="false" />.</value>
    public bool IsEmpty =>
        _count == 0;

    /// <summary>
    /// Gets the first (lower) piece, or <see cref="DiscreteInterval{T}.Empty" /> when the result is empty.
    /// </summary>
    /// <value>The lowest interval in the result.</value>
    public DiscreteInterval<T> First =>
        _first;

    /// <summary>
    /// Gets the second (upper) piece, or <see cref="DiscreteInterval{T}.Empty" /> when the result has fewer than two
    /// pieces.
    /// </summary>
    /// <value>The higher interval in the result.</value>
    public DiscreteInterval<T> Second =>
        _second;

    /// <summary>
    /// Gets the piece at <paramref name="index" />, ordered lowest-first.
    /// </summary>
    /// <param name="index">The zero-based index, less than <see cref="Count" />.</param>
    /// <returns>The interval at the requested position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or not less than <see cref="Count" />.
    /// </exception>
    public DiscreteInterval<T> this[int index] =>
        index switch
        {
            0 when _count > 0 => _first,
            1 when _count > 1 => _second,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };

    /// <summary>
    /// Creates a result from a lower and an upper candidate piece, dropping any that are empty and packing the
    /// remainder lowest-first. The caller must supply the pieces in ascending order.
    /// </summary>
    /// <param name="first">The lower candidate piece (may be empty).</param>
    /// <param name="second">The upper candidate piece (may be empty).</param>
    /// <returns>The packed result.</returns>
    internal static DiscreteIntervalPair<T> Create(DiscreteInterval<T> first, DiscreteInterval<T> second)
    {
        if (second.IsEmpty)
            return first.IsEmpty ? default : new DiscreteIntervalPair<T>(first, DiscreteInterval<T>.Empty, 1);

        return first.IsEmpty
            ? new DiscreteIntervalPair<T>(second, DiscreteInterval<T>.Empty, 1)
            : new DiscreteIntervalPair<T>(first, second, 2);
    }

    /// <summary>
    /// Returns an enumerator that iterates the non-empty pieces in ascending order.
    /// </summary>
    /// <returns>A struct enumerator over the pieces.</returns>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <summary>
    /// Returns a set-notation string representation of the result.
    /// </summary>
    /// <returns>The formatted result.</returns>
    public override string ToString() =>
        _count switch
        {
            0 => "∅",
            1 => _first.ToString(),
            _ => $"{_first} ∪ {_second}",
        };

    /// <summary>
    /// Enumerates the non-empty pieces of a <see cref="DiscreteIntervalPair{T}" /> without allocating.
    /// </summary>
    public struct Enumerator
    {
        /// <summary>
        /// The pair being enumerated.
        /// </summary>
        private readonly DiscreteIntervalPair<T> _pair;

        /// <summary>
        /// The zero-based index of the current run, or -1 before the first <see cref="MoveNext" />.
        /// </summary>
        private int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct positioned before the first piece.
        /// </summary>
        /// <param name="pair">The result to enumerate.</param>
        internal Enumerator(DiscreteIntervalPair<T> pair)
        {
            _pair = pair;
            _index = -1;
        }

        /// <summary>
        /// Gets the piece at the current position.
        /// </summary>
        /// <value>The current interval.</value>
        public readonly DiscreteInterval<T> Current =>
            _index == 0 ? _pair._first : _pair._second;

        /// <summary>
        /// Advances to the next piece.
        /// </summary>
        /// <returns><see langword="true" /> while a piece remains; otherwise <see langword="false" />.</returns>
        public bool MoveNext() =>
            ++_index < _pair._count;
    }
}
