// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Interval.SetOperations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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

        bool lowerOk = LowerInclusive ? value >= _lower : value > _lower;
        bool upperOk = UpperInclusive ? value <= _upper : value < _upper;
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

        bool lowerOk = CompareLowerEndpoint(_lower, LowerInclusive, other._lower, other.LowerInclusive) <= 0;
        bool upperOk = CompareUpperEndpoint(_upper, UpperInclusive, other._upper, other.UpperInclusive) >= 0;
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

        // Two non-empty intervals overlap iff this.Lower < other.Upper (respecting inclusivity) and
        // other.Lower < this.Upper. We collapse the four cases into endpoint comparisons.
        bool aBelowB = _lower < other._upper || (_lower == other._upper && LowerInclusive && other.UpperInclusive);
        bool bBelowA = other._lower < _upper || (other._lower == _upper && other.LowerInclusive && UpperInclusive);
        return aBelowB && bBelowA;
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

        // Pick the larger of the two lower endpoints (and the stricter inclusivity when the values tie).
        T newLower;
        bool newLowerInclusive;
        int lowerCmp = _lower.CompareTo(other._lower);
        if (lowerCmp > 0)
        {
            newLower = _lower;
            newLowerInclusive = LowerInclusive;
        }
        else if (lowerCmp < 0)
        {
            newLower = other._lower;
            newLowerInclusive = other.LowerInclusive;
        }
        else
        {
            newLower = _lower;
            newLowerInclusive = LowerInclusive && other.LowerInclusive;
        }

        // Pick the smaller of the two upper endpoints (and the stricter inclusivity when the values tie).
        T newUpper;
        bool newUpperInclusive;
        int upperCmp = _upper.CompareTo(other._upper);
        if (upperCmp < 0)
        {
            newUpper = _upper;
            newUpperInclusive = UpperInclusive;
        }
        else if (upperCmp > 0)
        {
            newUpper = other._upper;
            newUpperInclusive = other.UpperInclusive;
        }
        else
        {
            newUpper = _upper;
            newUpperInclusive = UpperInclusive && other.UpperInclusive;
        }

        return new Interval<T>(newLower, newUpper, newLowerInclusive, newUpperInclusive);
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

        T newLower;
        bool newLowerInclusive;
        int lowerCmp = _lower.CompareTo(other._lower);
        if (lowerCmp < 0)
        {
            newLower = _lower;
            newLowerInclusive = LowerInclusive;
        }
        else if (lowerCmp > 0)
        {
            newLower = other._lower;
            newLowerInclusive = other.LowerInclusive;
        }
        else
        {
            newLower = _lower;
            newLowerInclusive = LowerInclusive || other.LowerInclusive;
        }

        T newUpper;
        bool newUpperInclusive;
        int upperCmp = _upper.CompareTo(other._upper);
        if (upperCmp > 0)
        {
            newUpper = _upper;
            newUpperInclusive = UpperInclusive;
        }
        else if (upperCmp < 0)
        {
            newUpper = other._upper;
            newUpperInclusive = other.UpperInclusive;
        }
        else
        {
            newUpper = _upper;
            newUpperInclusive = UpperInclusive || other.UpperInclusive;
        }

        result = new Interval<T>(newLower, newUpper, newLowerInclusive, newUpperInclusive);
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
        (_upper == other._lower && (UpperInclusive || other.LowerInclusive))
        || (other._upper == _lower && (other.UpperInclusive || LowerInclusive));

    /// <summary>
    /// Compares two lower endpoints, treating an inclusive lower endpoint as less than an open one at the same value
    /// (because an inclusive lower endpoint admits the value itself).
    /// </summary>
    /// <param name="aLower">The first lower endpoint.</param>
    /// <param name="aInclusive">Whether the first lower endpoint is inclusive.</param>
    /// <param name="bLower">The second lower endpoint.</param>
    /// <param name="bInclusive">Whether the second lower endpoint is inclusive.</param>
    /// <returns>
    /// A negative value if the first endpoint admits a strictly smaller set, zero if equivalent, otherwise positive.
    /// </returns>
    private static int CompareLowerEndpoint(T aLower, bool aInclusive, T bLower, bool bInclusive)
    {
        int cmp = aLower.CompareTo(bLower);
        return cmp != 0 ? cmp : aInclusive == bInclusive ? 0 : aInclusive ? -1 : 1;
    }

    /// <summary>
    /// Compares two upper endpoints, treating an inclusive upper endpoint as greater than an open one at the same value
    /// (because an inclusive upper endpoint admits the value itself).
    /// </summary>
    /// <param name="aUpper">The first upper endpoint.</param>
    /// <param name="aInclusive">Whether the first upper endpoint is inclusive.</param>
    /// <param name="bUpper">The second upper endpoint.</param>
    /// <param name="bInclusive">Whether the second upper endpoint is inclusive.</param>
    /// <returns>
    /// A negative value if the first endpoint admits a strictly smaller set, zero if equivalent, otherwise positive.
    /// </returns>
    private static int CompareUpperEndpoint(T aUpper, bool aInclusive, T bUpper, bool bInclusive)
    {
        int cmp = aUpper.CompareTo(bUpper);
        return cmp != 0 ? cmp : aInclusive == bInclusive ? 0 : aInclusive ? 1 : -1;
    }
}
