// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Interval{T}.SetOperations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct Interval<T>
{
    /// <summary>
    /// Determines whether <paramref name="value" /> lies within the interval, honoring the inclusivity of each
    /// endpoint.
    /// </summary>
    /// <param name="value">The value to test for membership.</param>
    /// <returns>
    /// <see langword="true" /> when the interval is non-empty and <paramref name="value" /> falls between the endpoints
    /// under the configured inclusivity rules; otherwise <see langword="false" />. An empty interval contains no value,
    /// including itself.
    /// </returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var window = Interval<int>.ClosedOpen(1, 5);   // [1, 5)
    /// window.Contains(1);                            // True  — closed lower
    /// window.Contains(4);                            // True  — interior
    /// window.Contains(5);                            // False — open upper
    /// window.Contains(0);                            // False — outside
    ///
    /// Interval<int>.Empty.Contains(0);               // False — the empty interval contains nothing
    ///]]>
    /// </code>
    /// </example>
    public bool Contains(T value)
    {
        if (IsEmpty)
            return false;

        bool lowerOk = LowerUnbounded || (LowerInclusive ? value >= _lower : value > _lower);
        bool upperOk = UpperUnbounded || (UpperInclusive ? value <= _upper : value < _upper);
        return lowerOk && upperOk;
    }

    /// <summary>
    /// Determines whether this interval fully contains <paramref name="other" /> — every value of
    /// <paramref name="other" /> is also a value of this interval.
    /// </summary>
    /// <param name="other">The interval to test for containment.</param>
    /// <returns>
    /// <see langword="true" /> when this interval is a superset of <paramref name="other" />; otherwise
    /// <see langword="false" />. The empty interval is a subset of every interval, so any interval contains the empty
    /// interval; only the empty interval contains the empty interval as a non-empty member.
    /// </returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var outer = Interval<int>.Closed(0, 10);
    ///
    /// outer.Contains(Interval<int>.Closed(2, 8));        // True — strict subset
    /// outer.Contains(Interval<int>.Closed(0, 10));       // True — equal sets
    /// outer.Contains(Interval<int>.Closed(2, 11));       // False — exceeds upper
    /// outer.Contains(Interval<int>.Empty);               // True — ∅ ⊆ every set
    ///
    /// // Endpoint inclusivity is honored: an open lower fits inside a closed lower at the same value.
    /// var closed = Interval<int>.Closed(0, 10);          // [0, 10]
    /// var open   = Interval<int>.Open(0, 10);            // (0, 10)
    /// closed.Contains(open);                             // True
    /// open.Contains(closed);                             // False — closed includes 0 and 10
    ///]]>
    /// </code>
    /// </example>
    public bool Contains(Interval<T> other)
    {
        if (other.IsEmpty)
            return true;

        if (IsEmpty)
            return false;

        bool lowerOk = CompareLowerWith(other) <= 0;
        bool upperOk = CompareUpperWith(other) >= 0;
        return lowerOk && upperOk;
    }

    /// <summary>
    /// Determines whether this interval shares any values with <paramref name="other" />.
    /// </summary>
    /// <param name="other">The interval to test for overlap.</param>
    /// <returns>
    /// <see langword="true" /> when the two intervals share at least one value; otherwise <see langword="false" />. An
    /// empty interval shares no values with any interval and is therefore never overlapping.
    /// </returns>
    /// <remarks>
    /// Two intervals that touch but do not share any value — for example <c>[1, 2)</c> and <c>[2, 3]</c> — do not
    /// overlap, because no single value belongs to both. To test whether they are adjacent (touching), inspect the
    /// endpoints directly.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var a = Interval<int>.Closed(1, 5);
    /// var b = Interval<int>.Closed(3, 7);
    /// a.Overlaps(b);                                                     // True — share [3, 5]
    ///
    /// // Touching at a boundary but not both including it: NOT overlapping.
    /// Interval<int>.ClosedOpen(1, 5).Overlaps(Interval<int>.Closed(5, 10));   // False — neither holds 5 jointly
    /// Interval<int>.OpenClosed(1, 5).Overlaps(Interval<int>.Closed(5, 10));   // True  — both include 5
    ///
    /// Interval<int>.Closed(1, 2).Overlaps(Interval<int>.Closed(5, 6));   // False — disjoint
    ///]]>
    /// </code>
    /// </example>
    public bool Overlaps(Interval<T> other)
    {
        if (IsEmpty || other.IsEmpty)
            return false;

        // Two non-empty intervals overlap iff each one's lower endpoint lies below the other's upper endpoint
        // (respecting inclusivity), where an unbounded side is always below/above any finite endpoint.
        return LowerBelowUpper(this, other) && LowerBelowUpper(other, this);
    }

    /// <summary>
    /// Determines whether the lower endpoint of <paramref name="a" /> lies below the upper endpoint of
    /// <paramref name="b" />, treating an unbounded lower as below every value and an unbounded upper as above every
    /// value. Equal finite endpoints count as below only when both include the shared value.
    /// </summary>
    /// <param name="a">The interval contributing the lower endpoint.</param>
    /// <param name="b">The interval contributing the upper endpoint.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="a" />'s lower is below <paramref name="b" />'s upper.
    /// </returns>
    private static bool LowerBelowUpper(Interval<T> a, Interval<T> b)
    {
        if (a.LowerUnbounded || b.UpperUnbounded)
            return true;

        int cmp = a._lower.CompareTo(b._upper);
        return cmp < 0 || (cmp == 0 && a.LowerInclusive && b.UpperInclusive);
    }

    /// <summary>
    /// Returns the intersection of this interval with <paramref name="other" /> — the interval of values shared by
    /// both.
    /// </summary>
    /// <param name="other">The interval to intersect with.</param>
    /// <returns>The intersection interval, or <see cref="Empty" /> when the two intervals share no values.</returns>
    /// <remarks>
    /// <para>
    /// On endpoint ties, the <b>stricter</b> (open) inclusivity wins so that the result is a true subset of both
    /// operands. For example, <c>[1, 5]</c> intersected with <c>(1, 5)</c> yields <c>(1, 5)</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// Interval<int>.Closed(1, 5).Intersect(Interval<int>.Closed(3, 7));   // [3, 5]
    /// Interval<int>.Closed(1, 3).Intersect(Interval<int>.Closed(5, 7));   // ∅ — disjoint
    ///
    /// // On ties, the stricter (open) inclusivity wins.
    /// var closed = Interval<int>.Closed(1, 5);   // [1, 5]
    /// var open   = Interval<int>.Open(1, 5);     // (1, 5)
    /// closed.Intersect(open);                    // (1, 5)
    ///]]>
    /// </code>
    /// </example>
    public Interval<T> Intersect(Interval<T> other)
    {
        if (IsEmpty || other.IsEmpty || !Overlaps(other))
            return Empty;

        // The intersection takes the tighter endpoint on each side: the greater lower and the smaller upper. On a value
        // tie the stricter (open) inclusivity wins, which the endpoint comparisons already encode.
        Interval<T> lowerSource = CompareLowerWith(other) >= 0 ? this : other;
        Interval<T> upperSource = CompareUpperWith(other) <= 0 ? this : other;

        (T lowerValue, byte lowerFlag) = lowerSource.LowerPart();
        (T upperValue, byte upperFlag) = upperSource.UpperPart();

        var result = new Interval<T>(lowerValue, upperValue, (byte)(lowerFlag | upperFlag));
        return result.IsEmpty ? Empty : result;
    }

    /// <summary>
    /// Returns this interval with the values of <paramref name="other" /> removed — the set difference
    /// <c>this \ other</c> — as zero, one, or two disjoint intervals.
    /// </summary>
    /// <param name="other">The interval whose values are removed from this one.</param>
    /// <returns>
    /// An <see cref="IntervalPair{T}" /> holding the remainder: empty when this interval is empty or wholly covered by
    /// <paramref name="other" />; a single interval when <paramref name="other" /> is disjoint or trims one side; and
    /// two disjoint intervals when <paramref name="other" /> lies strictly inside this interval.
    /// </returns>
    /// <remarks>
    /// The endpoint at each cut flips inclusivity: removing a closed bound leaves an open bound on the remainder, and
    /// vice versa. Unbounded operands are handled naturally — for example the difference of <see cref="All" /> and a
    /// finite interval yields the two half-lines around it.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// Interval<int>.Closed(0, 10).Difference(Interval<int>.Closed(3, 5));   // [0, 3) ∪ (5, 10]
    /// Interval<int>.Closed(0, 10).Difference(Interval<int>.Closed(8, 20));  // [0, 8)
    /// Interval<double>.All.Difference(Interval<double>.Closed(3, 5));       // (-∞, 3) ∪ (5, +∞)
    ///]]>
    /// </code>
    /// </example>
    public IntervalPair<T> Difference(Interval<T> other)
    {
        if (IsEmpty)
            return IntervalPair<T>.Empty;

        if (other.IsEmpty || !Overlaps(other))
            return IntervalPair<T>.Create(this, Interval<T>.Empty);

        // Left remainder: the part of this interval strictly below other's lower endpoint (which is finite here, since
        // this extends left of it). The cut bound at other.Lower is open when other includes it, closed when it excludes it.
        Interval<T> left = Interval<T>.Empty;
        if (CompareLowerWith(other) < 0)
        {
            (T lowerValue, byte lowerFlag) = LowerPart();
            byte cutUpperFlag = other.LowerInclusive ? (byte)0 : UpperInclusiveFlag;
            var candidate = new Interval<T>(lowerValue, other._lower, (byte)(lowerFlag | cutUpperFlag));
            if (!candidate.IsEmpty)
                left = candidate;
        }

        // Right remainder: the part of this interval strictly above other's upper endpoint.
        Interval<T> right = Interval<T>.Empty;
        if (CompareUpperWith(other) > 0)
        {
            (T upperValue, byte upperFlag) = UpperPart();
            byte cutLowerFlag = other.UpperInclusive ? (byte)0 : LowerInclusiveFlag;
            var candidate = new Interval<T>(other._upper, upperValue, (byte)(cutLowerFlag | upperFlag));
            if (!candidate.IsEmpty)
                right = candidate;
        }

        return IntervalPair<T>.Create(left, right);
    }

    /// <summary>
    /// Returns the symmetric difference of this interval and <paramref name="other" /> — the values in exactly one of
    /// the two, <c>(this \ other) &#x222A; (other \ this)</c> — as zero, one, or two disjoint intervals.
    /// </summary>
    /// <param name="other">The interval to symmetric-difference with.</param>
    /// <returns>An <see cref="IntervalPair{T}" /> holding the values covered by exactly one operand.</returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// Interval<int>.Closed(0, 5).SymmetricDifference(Interval<int>.Closed(3, 8));   // [0, 3) ∪ (5, 8]
    /// Interval<int>.Closed(0, 5).SymmetricDifference(Interval<int>.Closed(0, 5));   // ∅ (equal sets)
    ///]]>
    /// </code>
    /// </example>
    public IntervalPair<T> SymmetricDifference(Interval<T> other)
    {
        IntervalPair<T> thisMinusOther = Difference(other);
        IntervalPair<T> otherMinusThis = other.Difference(this);

        // When one operand lies inside the other, that side contributes two pieces and the other contributes none;
        // otherwise each side contributes at most one, and the two are ordered lowest-first.
        if (otherMinusThis.Count == 0)
            return thisMinusOther;
        if (thisMinusOther.Count == 0)
            return otherMinusThis;

        Interval<T> p = thisMinusOther.First;
        Interval<T> q = otherMinusThis.First;
        return p.CompareStartTo(q) <= 0
            ? IntervalPair<T>.Create(p, q)
            : IntervalPair<T>.Create(q, p);
    }

    /// <summary>
    /// Attempts to compute the union of this interval with <paramref name="other" /> as a single contiguous interval.
    /// </summary>
    /// <param name="other">The interval to union with.</param>
    /// <param name="result">
    /// When the method returns <see langword="true" />, contains the contiguous union of the two intervals; otherwise
    /// <see cref="Empty" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the two intervals are either overlapping or adjacent (their union is a single
    /// contiguous interval); <see langword="false" /> when the intervals are disjoint and their union would require two
    /// separate intervals to represent.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Two intervals are adjacent when the upper endpoint of one equals the lower endpoint of the other and at least
    /// one of those endpoints is inclusive. For example, <c>[1, 2)</c> and <c>[2, 3]</c> are adjacent and union to
    /// <c>[1, 3]</c>; <c>[1, 2)</c> and <c>(2, 3]</c> are disjoint because the value <c>2</c> is in neither interval
    /// and the result would not be contiguous.
    /// </para>
    /// <para>
    /// On endpoint ties, the <b>looser</b> (closed) inclusivity wins so that the result is a superset of either
    /// operand. Union with the empty interval is always defined: an empty operand leaves the other operand unchanged.
    /// </para>
    /// <para>
    /// When the two intervals are disjoint with a true gap between them, the union would require two pieces to
    /// represent and this method returns <see langword="false" /> rather than synthesise a non-contiguous result.
    /// Callers that need a multi-piece result should accumulate the operands into a higher-level collection of
    /// intervals.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Adjacent — [1, 5) ∪ [5, 10] → [1, 10]
    /// Interval<int>.ClosedOpen(1, 5).TryUnion(Interval<int>.Closed(5, 10), out var contiguous);
    /// // contiguous == [1, 10], result == true
    ///
    /// // Overlapping — [1, 5] ∪ [3, 7] → [1, 7]
    /// Interval<int>.Closed(1, 5).TryUnion(Interval<int>.Closed(3, 7), out var merged);
    /// // merged == [1, 7], result == true
    ///
    /// // Disjoint — [1, 5) ∪ (5, 10] is not contiguous (no operand contains 5).
    /// bool ok = Interval<int>.ClosedOpen(1, 5).TryUnion(Interval<int>.OpenClosed(5, 10), out _);
    /// // ok == false
    ///
    /// // Empty operand acts as identity.
    /// Interval<int>.Empty.TryUnion(Interval<int>.Closed(1, 5), out var same);
    /// // same == [1, 5], result == true
    ///]]>
    /// </code>
    /// </example>
    public bool TryUnion(Interval<T> other, out Interval<T> result)
    {
        if (IsEmpty)
        {
            result = other;
            return true;
        }

        if (other.IsEmpty)
        {
            result = this;
            return true;
        }

        // The two intervals must overlap or be adjacent for the union to be a single contiguous interval.
        if (!Overlaps(other) && !IsAdjacentTo(other))
        {
            result = Empty;
            return false;
        }

        // The contiguous union takes the looser endpoint on each side: the smaller lower and the greater upper. On a
        // value tie the looser (closed) inclusivity wins, which the endpoint comparisons already encode.
        Interval<T> lowerSource = CompareLowerWith(other) <= 0 ? this : other;
        Interval<T> upperSource = CompareUpperWith(other) >= 0 ? this : other;

        (T lowerValue, byte lowerFlag) = lowerSource.LowerPart();
        (T upperValue, byte upperFlag) = upperSource.UpperPart();

        result = new Interval<T>(lowerValue, upperValue, (byte)(lowerFlag | upperFlag));
        return true;
    }

    /// <summary>
    /// Determines whether the upper endpoint of one interval equals the lower endpoint of the other and at least one of
    /// those endpoints is inclusive — the condition under which the two intervals' values would form a single
    /// contiguous run.
    /// </summary>
    /// <param name="other">The interval to test for adjacency.</param>
    /// <returns><see langword="true" /> when the intervals are adjacent; otherwise <see langword="false" />.</returns>
    private bool IsAdjacentTo(Interval<T> other) =>
        (!UpperUnbounded && !other.LowerUnbounded && _upper == other._lower && (UpperInclusive || other.LowerInclusive))
        || (!other.UpperUnbounded && !LowerUnbounded && other._upper == _lower && (other.UpperInclusive || LowerInclusive));

    /// <summary>
    /// Compares this interval's lower endpoint with <paramref name="other" />'s, ordering an unbounded lower below
    /// every finite lower and, on a value tie, an inclusive (closed) lower below an open one — so a smaller result
    /// denotes the endpoint that admits the leftmost values.
    /// </summary>
    /// <param name="other">The interval whose lower endpoint is compared.</param>
    /// <returns>A negative value when this lower is further left, zero when equivalent, otherwise positive.</returns>
    private int CompareLowerWith(Interval<T> other)
    {
        if (LowerUnbounded)
            return other.LowerUnbounded ? 0 : -1;
        if (other.LowerUnbounded)
            return 1;

        int cmp = _lower.CompareTo(other._lower);
        return cmp != 0 ? cmp : LowerInclusive == other.LowerInclusive ? 0 : LowerInclusive ? -1 : 1;
    }

    /// <summary>
    /// Compares the starting (lower) position of this interval with <paramref name="other" /> for ordering disjoint
    /// pieces lowest-first.
    /// </summary>
    /// <param name="other">The interval to compare against.</param>
    /// <returns>A negative value when this interval starts earlier, zero when equal, otherwise positive.</returns>
    internal int CompareStartTo(Interval<T> other) =>
        CompareLowerWith(other);

    /// <summary>
    /// Compares this interval's upper endpoint with <paramref name="other" />'s, ordering an unbounded upper above
    /// every finite upper and, on a value tie, an inclusive (closed) upper above an open one — so a larger result
    /// denotes the endpoint that admits the rightmost values.
    /// </summary>
    /// <param name="other">The interval whose upper endpoint is compared.</param>
    /// <returns>A negative value when this upper is further left, zero when equivalent, otherwise positive.</returns>
    private int CompareUpperWith(Interval<T> other)
    {
        if (UpperUnbounded)
            return other.UpperUnbounded ? 0 : 1;
        if (other.UpperUnbounded)
            return -1;

        int cmp = _upper.CompareTo(other._upper);
        return cmp != 0 ? cmp : UpperInclusive == other.UpperInclusive ? 0 : UpperInclusive ? 1 : -1;
    }

    /// <summary>
    /// Returns the lower half of this interval as an endpoint value and its packed lower flag, for reassembling a new
    /// interval from selected endpoints.
    /// </summary>
    /// <returns>
    /// The lower endpoint value (<see cref="INumberBase{TSelf}.Zero" /> when unbounded) and its flag bits.
    /// </returns>
    private (T Value, byte Flag) LowerPart() =>
        LowerUnbounded ? (T.Zero, LowerUnboundedFlag) : (_lower, (byte)(LowerInclusive ? LowerInclusiveFlag : 0));

    /// <summary>
    /// Returns the upper half of this interval as an endpoint value and its packed upper flag, for reassembling a new
    /// interval from selected endpoints.
    /// </summary>
    /// <returns>
    /// The upper endpoint value (<see cref="INumberBase{TSelf}.Zero" /> when unbounded) and its flag bits.
    /// </returns>
    private (T Value, byte Flag) UpperPart() =>
        UpperUnbounded ? (T.Zero, UpperUnboundedFlag) : (_upper, (byte)(UpperInclusive ? UpperInclusiveFlag : 0));
}
