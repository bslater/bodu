// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteInterval{T}.SetOperations.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct DiscreteInterval<T>
{
    /// <summary>
    /// Determines whether <paramref name="value" /> is an integer of this interval.
    /// </summary>
    /// <param name="value">The integer to test.</param>
    /// <returns><see langword="true" /> when the interval contains <paramref name="value" />; otherwise <see langword="false" />.</returns>
    public bool Contains(T value)
    {
        if (IsEmpty)
            return false;

        bool lowerOk = LowerUnbounded || value >= _first;
        bool upperOk = UpperUnbounded || value <= _last;
        return lowerOk && upperOk;
    }

    /// <summary>
    /// Determines whether this interval shares any integer with <paramref name="other" />.
    /// </summary>
    /// <param name="other">The interval to test for overlap.</param>
    /// <returns><see langword="true" /> when the two share at least one integer; otherwise <see langword="false" />.</returns>
    public bool Overlaps(DiscreteInterval<T> other)
    {
        if (IsEmpty || other.IsEmpty)
            return false;

        return FirstAtMostLast(this, other) && FirstAtMostLast(other, this);
    }

    /// <summary>
    /// Returns the intersection of this interval with <paramref name="other" /> — the integers in both.
    /// </summary>
    /// <param name="other">The interval to intersect with.</param>
    /// <returns>The intersection, or <see cref="Empty" /> when the two share no integer.</returns>
    public DiscreteInterval<T> Intersect(DiscreteInterval<T> other)
    {
        if (IsEmpty || other.IsEmpty)
            return Empty;

        // Lower side: the greater (rightmost) lower bound; an unbounded lower loses to any finite lower.
        bool lowerUnbounded = LowerUnbounded && other.LowerUnbounded;
        T first = LowerUnbounded ? other._first : other.LowerUnbounded ? _first : Max(_first, other._first);

        // Upper side: the smaller (leftmost) upper bound; an unbounded upper loses to any finite upper.
        bool upperUnbounded = UpperUnbounded && other.UpperUnbounded;
        T last = UpperUnbounded ? other._last : other.UpperUnbounded ? _last : Min(_last, other._last);

        if (!lowerUnbounded && !upperUnbounded)
            return FromInclusive(first, last);

        // At least one side stays unbounded, so the result is infinite on that side and cannot be empty.
        byte flags = (byte)((lowerUnbounded ? LowerUnboundedFlag : 0) | (upperUnbounded ? UpperUnboundedFlag : 0));
        return new DiscreteInterval<T>(lowerUnbounded ? T.Zero : first, upperUnbounded ? T.Zero : last, flags);
    }

    /// <summary>
    /// Attempts to compute the union of this interval with <paramref name="other" /> as a single contiguous run of
    /// integers, treating successor-adjacent intervals (no integer between them) as contiguous.
    /// </summary>
    /// <param name="other">The interval to union with.</param>
    /// <param name="result">When this method returns <see langword="true" />, the contiguous union; otherwise <see cref="Empty" />.</param>
    /// <returns>
    /// <see langword="true" /> when the two intervals overlap or are successor-adjacent; <see langword="false" /> when an
    /// integer gap separates them so their union needs two runs.
    /// </returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// DiscreteInterval<int>.Closed(1, 2).TryUnion(DiscreteInterval<int>.Closed(3, 4), out var run);
    /// // run == [1, 4], result == true (no integer lies between 2 and 3)
    ///]]>
    /// </code>
    /// </example>
    public bool TryUnion(DiscreteInterval<T> other, out DiscreteInterval<T> result)
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

        if (!Overlaps(other) && !IsSuccessorAdjacent(other))
        {
            result = Empty;
            return false;
        }

        bool lowerUnbounded = LowerUnbounded || other.LowerUnbounded;
        T first = lowerUnbounded ? T.Zero : Min(_first, other._first);

        bool upperUnbounded = UpperUnbounded || other.UpperUnbounded;
        T last = upperUnbounded ? T.Zero : Max(_last, other._last);

        result = !lowerUnbounded && !upperUnbounded
            ? FromInclusive(first, last)
            : new DiscreteInterval<T>(first, last, (byte)((lowerUnbounded ? LowerUnboundedFlag : 0) | (upperUnbounded ? UpperUnboundedFlag : 0)));
        return true;
    }

    /// <summary>
    /// Determines whether <paramref name="a" />'s lower bound is at or below <paramref name="b" />'s upper bound, with
    /// unbounded sides ordered outside every finite bound.
    /// </summary>
    /// <param name="a">The interval contributing the lower bound.</param>
    /// <param name="b">The interval contributing the upper bound.</param>
    /// <returns><see langword="true" /> when <paramref name="a" />'s lower is at or below <paramref name="b" />'s upper.</returns>
    private static bool FirstAtMostLast(DiscreteInterval<T> a, DiscreteInterval<T> b) =>
        a.LowerUnbounded || b.UpperUnbounded || a._first <= b._last;

    /// <summary>
    /// Determines whether this interval and <paramref name="other" /> are successor-adjacent — the successor of one's last
    /// integer is the other's first integer, leaving no integer between them.
    /// </summary>
    /// <param name="other">The interval to test.</param>
    /// <returns><see langword="true" /> when the intervals are successor-adjacent; otherwise <see langword="false" />.</returns>
    private bool IsSuccessorAdjacent(DiscreteInterval<T> other) =>
        (!UpperUnbounded && !other.LowerUnbounded && _last + T.One == other._first)
        || (!other.UpperUnbounded && !LowerUnbounded && other._last + T.One == _first);

    /// <summary>Returns the greater of two integers.</summary>
    /// <param name="a">The first integer.</param>
    /// <param name="b">The second integer.</param>
    /// <returns>The greater value.</returns>
    private static T Max(T a, T b) =>
        a >= b ? a : b;

    /// <summary>Returns the lesser of two integers.</summary>
    /// <param name="a">The first integer.</param>
    /// <param name="b">The second integer.</param>
    /// <returns>The lesser value.</returns>
    private static T Min(T a, T b) =>
        a <= b ? a : b;
}
