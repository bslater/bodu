// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalSet{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// Represents an immutable set of values as a normalized collection of disjoint, non-adjacent
/// <see cref="Interval{T}" /> pieces in ascending order — the general result of interval algebra that can produce a
/// disconnected range.
/// </summary>
/// <typeparam name="T">The numeric type used for the intervals' endpoints.</typeparam>
/// <remarks>
/// <para>
/// Where <see cref="Interval{T}" /> models a single connected range and <see cref="IntervalPair{T}" /> the at-most-two
/// pieces of a binary difference, <see cref="IntervalSet{T}" /> models an arbitrary union of ranges. Overlapping and
/// adjacent inputs are coalesced at construction, so the pieces are always disjoint, non-adjacent, sorted, and free of
/// empty entries — a canonical form in which set equality is piecewise equality.
/// </para>
/// <para>
/// The type is the home for the operations a single interval cannot express as one value: N-ary
/// <see cref="Union(Interval{T})" />, <see cref="Except(Interval{T})" />, and <see cref="Complement" /> (the values not
/// in the set, over the whole line). It composes with <see cref="Interval{T}" /> throughout.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var set = IntervalSet<int>.Of(Interval<int>.Closed(1, 3), Interval<int>.Closed(2, 5), Interval<int>.Closed(8, 9));
/// set.Count;                 // 2 — [1, 5] and [8, 9] (the first two coalesced)
/// set.Contains(4);           // True
/// set.Complement();          // (-∞, 1) ∪ (5, 8) ∪ (9, +∞)
///]]>
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq}")]
public readonly partial struct IntervalSet<T>
    where T : INumber<T>
{
    /// <summary>The normalized, disjoint, sorted pieces, or <see langword="null" /> for the empty set.</summary>
    private readonly Interval<T>[]? _intervals;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalSet{T}" /> struct from an already-normalized array.
    /// </summary>
    /// <param name="intervals">
    /// The normalized, disjoint, sorted pieces, or <see langword="null" /> for the empty set.
    /// </param>
    private IntervalSet(Interval<T>[]? intervals)
    {
        _intervals = intervals is { Length: > 0 } ? intervals : null;
    }

    /// <summary>
    /// Gets the empty set — the set containing no values.
    /// </summary>
    /// <value>An <see cref="IntervalSet{T}" /> whose <see cref="Count" /> is zero.</value>
    public static IntervalSet<T> Empty =>
        default;

    /// <summary>
    /// Gets the number of disjoint pieces in the set.
    /// </summary>
    /// <value>The count of normalized intervals.</value>
    public int Count =>
        _intervals?.Length ?? 0;

    /// <summary>
    /// Gets a value indicating whether the set contains no values.
    /// </summary>
    /// <value><see langword="true" /> when the set has no pieces; otherwise <see langword="false" />.</value>
    public bool IsEmpty =>
        _intervals is null;

    /// <summary>
    /// Gets the piece at <paramref name="index" />, ordered lowest-first.
    /// </summary>
    /// <param name="index">The zero-based index, less than <see cref="Count" />.</param>
    /// <returns>The interval at the requested position.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is out of range.</exception>
    public Interval<T> this[int index] =>
        _intervals is not null && (uint)index < (uint)_intervals.Length
            ? _intervals[index]
            : throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>
    /// Creates a set from the supplied intervals, coalescing overlapping and adjacent pieces into a normalized form.
    /// </summary>
    /// <param name="intervals">The intervals to include; empty intervals are ignored.</param>
    /// <returns>The normalized set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="intervals" /> is <see langword="null" />.</exception>
    public static IntervalSet<T> Of(params Interval<T>[] intervals)
    {
        ThrowHelper.ThrowIfNull(intervals);

        return new IntervalSet<T>(Normalize(intervals));
    }

    /// <summary>
    /// Creates a set from a sequence of intervals, coalescing overlapping and adjacent pieces into a normalized form.
    /// </summary>
    /// <param name="intervals">The intervals to include; empty intervals are ignored.</param>
    /// <returns>The normalized set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="intervals" /> is <see langword="null" />.</exception>
    public static IntervalSet<T> From(IEnumerable<Interval<T>> intervals)
    {
        ThrowHelper.ThrowIfNull(intervals);

        return new IntervalSet<T>(Normalize(intervals));
    }

    /// <summary>
    /// Determines whether <paramref name="value" /> is a member of any piece of the set.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// <see langword="true" /> when the set contains <paramref name="value" />; otherwise <see langword="false" />.
    /// </returns>
    public bool Contains(T value)
    {
        if (_intervals is null)
            return false;

        foreach (Interval<T> piece in _intervals)
        {
            if (piece.Contains(value))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether every value of <paramref name="interval" /> is a member of the set.
    /// </summary>
    /// <param name="interval">The interval to test for enclosure.</param>
    /// <returns>
    /// <see langword="true" /> when the set is a superset of <paramref name="interval" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Because the pieces are disjoint and non-adjacent, an enclosed interval must lie entirely within one piece.
    /// </remarks>
    public bool Encloses(Interval<T> interval)
    {
        if (interval.IsEmpty)
            return true;

        if (_intervals is null)
            return false;

        foreach (Interval<T> piece in _intervals)
        {
            if (piece.Contains(interval))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns an enumerator over the disjoint pieces in ascending order.
    /// </summary>
    /// <returns>A struct enumerator.</returns>
    public Enumerator GetEnumerator() =>
        new(_intervals);

    /// <summary>
    /// Returns a set-notation string representation — the pieces joined by the union symbol, or the empty-set glyph.
    /// </summary>
    /// <returns>The formatted set.</returns>
    public override string ToString() =>
        _intervals is null ? "∅" : string.Join(" ∪ ", _intervals);

    /// <summary>
    /// Coalesces a sequence of intervals into a sorted array of disjoint, non-adjacent, non-empty pieces.
    /// </summary>
    /// <param name="source">The intervals to normalize.</param>
    /// <returns>The normalized array, or <see langword="null" /> when the result is empty.</returns>
    private static Interval<T>[]? Normalize(IEnumerable<Interval<T>> source)
    {
        var items = new List<Interval<T>>();
        foreach (Interval<T> interval in source)
        {
            if (!interval.IsEmpty)
                items.Add(interval);
        }

        if (items.Count == 0)
            return null;

        items.Sort(static (x, y) => x.CompareStartTo(y));

        var merged = new List<Interval<T>>();
        Interval<T> current = items[0];
        for (int i = 1; i < items.Count; i++)
        {
            if (current.TryUnion(items[i], out Interval<T> union))
            {
                current = union;
            }
            else
            {
                merged.Add(current);
                current = items[i];
            }
        }

        merged.Add(current);
        return merged.ToArray();
    }

    /// <summary>
    /// Enumerates the pieces of an <see cref="IntervalSet{T}" /> without allocating.
    /// </summary>
    public struct Enumerator
    {
        /// <summary>The pieces being enumerated, or <see langword="null" /> for the empty set.</summary>
        private readonly Interval<T>[]? _intervals;

        /// <summary>The zero-based index of the current piece, or -1 before <see cref="MoveNext" />.</summary>
        private int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator" /> struct positioned before the first piece.
        /// </summary>
        /// <param name="intervals">The pieces to enumerate.</param>
        internal Enumerator(Interval<T>[]? intervals)
        {
            _intervals = intervals;
            _index = -1;
        }

        /// <summary>
        /// Gets the piece at the current position.
        /// </summary>
        /// <value>The current interval.</value>
        public readonly Interval<T> Current =>
            _intervals![_index];

        /// <summary>
        /// Advances to the next piece.
        /// </summary>
        /// <returns><see langword="true" /> while a piece remains; otherwise <see langword="false" />.</returns>
        public bool MoveNext() =>
            _intervals is not null && ++_index < _intervals.Length;
    }
}
