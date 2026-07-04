// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalPair{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// Represents the result of a binary interval set operation as zero, one, or two disjoint intervals in ascending order
/// — the maximum number of pieces that subtracting or symmetric-differencing two intervals can produce.
/// </summary>
/// <typeparam name="T">The numeric type used for the intervals' endpoints.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Interval{T}.Difference(Interval{T})" /> and <see cref="Interval{T}.SymmetricDifference(Interval{T})" />
/// return this type. Because removing one interval from another leaves at most a left and a right remainder, the result
/// never needs more than two pieces, so this allocation-free value type stores them inline rather than in a heap
/// collection. When fewer than two pieces are present the surplus slots are the empty interval and are not enumerated.
/// </para>
/// <para>
/// The pieces are ordered lowest-first and are guaranteed disjoint and non-adjacent. Enumerate with a <c>foreach</c>
/// loop, index them via <see cref="this[int]" /> (0-based, bounded by <see cref="Count" />), or read
/// <see cref="First" /> and <see cref="Second" /> directly.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// IntervalPair<int> diff = Interval<int>.Closed(0, 10).Difference(Interval<int>.Closed(3, 5));
/// diff.Count;          // 2
/// foreach (var piece in diff)
///     Console.WriteLine(piece);   // "[0, 3)" then "(5, 10]"
///]]>
/// </code>
/// </example>
public readonly struct IntervalPair<T>
    where T : INumber<T>
{
    /// <summary>The first (lower) piece; only meaningful when <see cref="_count" /> is at least one.</summary>
    private readonly Interval<T> _first;

    /// <summary>The second (upper) piece; only meaningful when <see cref="_count" /> is two.</summary>
    private readonly Interval<T> _second;

    /// <summary>The number of non-empty disjoint pieces this pair holds (0, 1, or 2).</summary>
    private readonly int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalPair{T}" /> struct from already-packed, ordered pieces.
    /// </summary>
    /// <param name="first">The first (lower) piece.</param>
    /// <param name="second">The second (upper) piece.</param>
    /// <param name="count">The number of non-empty pieces.</param>
    private IntervalPair(Interval<T> first, Interval<T> second, int count)
    {
        _first = first;
        _second = second;
        _count = count;
    }

    /// <summary>
    /// Gets the empty result — zero pieces.
    /// </summary>
    /// <value>An <see cref="IntervalPair{T}" /> whose <see cref="Count" /> is zero.</value>
    public static IntervalPair<T> Empty =>
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
    /// Gets the first (lower) piece, or <see cref="Interval{T}.Empty" /> when the result is empty.
    /// </summary>
    /// <value>The lowest interval in the result.</value>
    public Interval<T> First =>
        _first;

    /// <summary>
    /// Gets the second (upper) piece, or <see cref="Interval{T}.Empty" /> when the result has fewer than two pieces.
    /// </summary>
    /// <value>The higher interval in the result.</value>
    public Interval<T> Second =>
        _second;

    /// <summary>
    /// Gets the piece at <paramref name="index" />, ordered lowest-first.
    /// </summary>
    /// <param name="index">The zero-based index, less than <see cref="Count" />.</param>
    /// <returns>The interval at the requested position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is negative or not less than <see cref="Count" />.
    /// </exception>
    public Interval<T> this[int index] =>
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
    internal static IntervalPair<T> Create(Interval<T> first, Interval<T> second)
    {
        if (second.IsEmpty)
            return first.IsEmpty ? default : new IntervalPair<T>(first, Interval<T>.Empty, 1);

        return first.IsEmpty
            ? new IntervalPair<T>(second, Interval<T>.Empty, 1)
            : new IntervalPair<T>(first, second, 2);
    }

    /// <summary>
    /// Returns an <see cref="IntervalSet{T}" /> containing the pieces of this result.
    /// </summary>
    /// <returns>The equivalent normalized set — empty, or the one or two disjoint pieces this pair holds.</returns>
    /// <remarks>
    /// <see cref="IntervalPair{T}" /> is the allocation-free result of a binary <see cref="Interval{T}.Difference" />
    /// or <see cref="Interval{T}.SymmetricDifference" /> and holds at most two pieces; <see cref="IntervalSet{T}" /> is
    /// the general disconnected-range model. Lift a transient pair into a set when the result must compose with further
    /// N-ary set algebra or be persisted. Empty slots are dropped, so the result has <see cref="Count" /> pieces.
    /// </remarks>
    public IntervalSet<T> ToIntervalSet() =>
        IntervalSet<T>.Of(_first, _second);

    /// <summary>
    /// Returns an enumerator that iterates the non-empty pieces in ascending order.
    /// </summary>
    /// <returns>A struct enumerator over the pieces.</returns>
    public Enumerator GetEnumerator() =>
        new(this);

    /// <summary>
    /// Returns a set-notation string representation of the result — the empty-set glyph when empty, otherwise the
    /// pieces joined by the union symbol.
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
    /// Enumerates the non-empty pieces of an <see cref="IntervalPair{T}" /> without allocating.
    /// </summary>
    public struct Enumerator
    {
        /// <summary>The pair being enumerated.</summary>
        private readonly IntervalPair<T> _pair;

        /// <summary>The zero-based index of the current piece, or -1 before <see cref="MoveNext" />.</summary>
        private int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct positioned before the first piece.
        /// </summary>
        /// <param name="pair">The result to enumerate.</param>
        internal Enumerator(IntervalPair<T> pair)
        {
            _pair = pair;
            _index = -1;
        }

        /// <summary>
        /// Gets the piece at the current position.
        /// </summary>
        /// <value>The current interval.</value>
        public readonly Interval<T> Current =>
            _index == 0 ? _pair._first : _pair._second;

        /// <summary>
        /// Advances to the next piece.
        /// </summary>
        /// <returns><see langword="true" /> while a piece remains; otherwise <see langword="false" />.</returns>
        public bool MoveNext() =>
            ++_index < _pair._count;
    }
}
