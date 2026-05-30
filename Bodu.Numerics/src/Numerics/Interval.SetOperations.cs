// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Interval.SetOperations.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// <see langword="true" /> when the interval is non-empty and <paramref name="value" /> falls between the
    /// endpoints under the configured inclusivity rules; otherwise <see langword="false" />. An empty interval
    /// contains no value, including itself.
    /// </returns>
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
    /// <see langword="false" />. The empty interval is a subset of every interval, so any interval contains the
    /// empty interval; only the empty interval contains the empty interval as a non-empty member.
    /// </returns>
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
    /// <see langword="true" /> when the two intervals share at least one value; otherwise <see langword="false" />.
    /// An empty interval shares no values with any interval and is therefore never overlapping.
    /// </returns>
    /// <remarks>
    /// Two intervals that touch but do not share any value — for example <c>[1, 2)</c> and <c>[2, 3]</c> —
    /// do not overlap, because no single value belongs to both. To test whether they are adjacent (touching),
    /// inspect the endpoints directly.
    /// </remarks>
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
    /// Returns the intersection of this interval with <paramref name="other" /> — the interval of values shared
    /// by both.
    /// </summary>
    /// <param name="other">The interval to intersect with.</param>
    /// <returns>The intersection interval, or <see cref="Empty" /> when the two intervals share no values.</returns>
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
    /// Attempts to compute the union of this interval with <paramref name="other" /> as a single contiguous
    /// interval.
    /// </summary>
    /// <param name="other">The interval to union with.</param>
    /// <param name="result">
    /// When the method returns <see langword="true" />, contains the contiguous union of the two intervals;
    /// otherwise <see cref="Empty" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the two intervals are either overlapping or adjacent (their union is a
    /// single contiguous interval); <see langword="false" /> when the intervals are disjoint and their union
    /// would require two separate intervals to represent.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Two intervals are adjacent when the upper endpoint of one equals the lower endpoint of the other and at
    /// least one of those endpoints is inclusive. For example, <c>[1, 2)</c> and <c>[2, 3]</c> are adjacent and
    /// union to <c>[1, 3]</c>; <c>[1, 2)</c> and <c>(2, 3]</c> are disjoint because the value <c>2</c> is in
    /// neither interval and the result would not be contiguous.
    /// </para>
    /// <para>
    /// Union with the empty interval is always defined: an empty operand leaves the other operand unchanged.
    /// </para>
    /// </remarks>
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
    /// Determines whether the upper endpoint of one interval equals the lower endpoint of the other and at least
    /// one of those endpoints is inclusive — the condition under which the two intervals' values would form a
    /// single contiguous run.
    /// </summary>
    /// <param name="other">The interval to test for adjacency.</param>
    /// <returns><see langword="true" /> when the intervals are adjacent; otherwise <see langword="false" />.</returns>
    private bool IsAdjacentTo(Interval<T> other)
    {
        if (_upper == other._lower && (UpperInclusive || other.LowerInclusive))
            return true;

        if (other._upper == _lower && (other.UpperInclusive || LowerInclusive))
            return true;

        return false;
    }

    /// <summary>
    /// Compares two lower endpoints, treating an inclusive lower endpoint as less than an open one at the same
    /// value (because an inclusive lower endpoint admits the value itself).
    /// </summary>
    /// <param name="aLower">The first lower endpoint.</param>
    /// <param name="aInclusive">Whether the first lower endpoint is inclusive.</param>
    /// <param name="bLower">The second lower endpoint.</param>
    /// <param name="bInclusive">Whether the second lower endpoint is inclusive.</param>
    /// <returns>A negative value if the first endpoint admits a strictly smaller set, zero if equivalent, otherwise positive.</returns>
    private static int CompareLowerEndpoint(T aLower, bool aInclusive, T bLower, bool bInclusive)
    {
        int cmp = aLower.CompareTo(bLower);
        if (cmp != 0)
            return cmp;

        if (aInclusive == bInclusive)
            return 0;

        return aInclusive ? -1 : 1;
    }

    /// <summary>
    /// Compares two upper endpoints, treating an inclusive upper endpoint as greater than an open one at the same
    /// value (because an inclusive upper endpoint admits the value itself).
    /// </summary>
    /// <param name="aUpper">The first upper endpoint.</param>
    /// <param name="aInclusive">Whether the first upper endpoint is inclusive.</param>
    /// <param name="bUpper">The second upper endpoint.</param>
    /// <param name="bInclusive">Whether the second upper endpoint is inclusive.</param>
    /// <returns>A negative value if the first endpoint admits a strictly smaller set, zero if equivalent, otherwise positive.</returns>
    private static int CompareUpperEndpoint(T aUpper, bool aInclusive, T bUpper, bool bInclusive)
    {
        int cmp = aUpper.CompareTo(bUpper);
        if (cmp != 0)
            return cmp;

        if (aInclusive == bInclusive)
            return 0;

        return aInclusive ? 1 : -1;
    }
}
