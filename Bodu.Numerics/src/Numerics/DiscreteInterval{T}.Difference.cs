// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteInterval{T}.Difference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct DiscreteInterval<T>
{
    /// <summary>
    /// Returns this interval with the integers of <paramref name="other" /> removed — the set difference
    /// <c>this \ other</c> — as zero, one, or two disjoint intervals.
    /// </summary>
    /// <param name="other">The interval whose integers are removed from this one.</param>
    /// <returns>The remaining integers as a <see cref="DiscreteIntervalPair{T}" />.</returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// DiscreteInterval<int>.Closed(0, 10).Difference(DiscreteInterval<int>.Closed(3, 5));   // [0, 2] ∪ [6, 10]
    ///]]>
    /// </code>
    /// </example>
    public DiscreteIntervalPair<T> Difference(DiscreteInterval<T> other)
    {
        if (IsEmpty)
            return DiscreteIntervalPair<T>.Empty;

        if (other.IsEmpty || !Overlaps(other))
            return DiscreteIntervalPair<T>.Create(this, Empty);

        // Left remainder: the integers of this interval below other's first integer (which is finite here). The cut sits
        // one integer below other's first.
        DiscreteInterval<T> left = Empty;
        if (ExtendsLeftOf(other))
        {
            left = LowerUnbounded
                ? AtMost(other._first - T.One)
                : FromInclusive(_first, other._first - T.One);
        }

        // Right remainder: the integers of this interval above other's last integer.
        DiscreteInterval<T> right = Empty;
        if (ExtendsRightOf(other))
        {
            right = UpperUnbounded
                ? AtLeast(other._last + T.One)
                : FromInclusive(other._last + T.One, _last);
        }

        return DiscreteIntervalPair<T>.Create(left, right);
    }

    /// <summary>
    /// Returns the symmetric difference of this interval and <paramref name="other" /> — the integers in exactly one of
    /// the two — as zero, one, or two disjoint intervals.
    /// </summary>
    /// <param name="other">The interval to symmetric-difference with.</param>
    /// <returns>The integers covered by exactly one operand as a <see cref="DiscreteIntervalPair{T}" />.</returns>
    public DiscreteIntervalPair<T> SymmetricDifference(DiscreteInterval<T> other)
    {
        DiscreteIntervalPair<T> thisMinusOther = Difference(other);
        DiscreteIntervalPair<T> otherMinusThis = other.Difference(this);

        if (otherMinusThis.Count == 0)
            return thisMinusOther;
        if (thisMinusOther.Count == 0)
            return otherMinusThis;

        DiscreteInterval<T> p = thisMinusOther.First;
        DiscreteInterval<T> q = otherMinusThis.First;
        return p.CompareStartTo(q) <= 0
            ? DiscreteIntervalPair<T>.Create(p, q)
            : DiscreteIntervalPair<T>.Create(q, p);
    }

    /// <summary>
    /// Determines whether this interval extends below <paramref name="other" />'s lower bound (has integers to its
    /// left).
    /// </summary>
    /// <param name="other">The interval to compare against.</param>
    /// <returns><see langword="true" /> when this interval starts strictly below <paramref name="other" />.</returns>
    private bool ExtendsLeftOf(DiscreteInterval<T> other) =>
        (LowerUnbounded && !other.LowerUnbounded)
        || (!LowerUnbounded && !other.LowerUnbounded && _first < other._first);

    /// <summary>
    /// Determines whether this interval extends above <paramref name="other" />'s upper bound (has integers to its
    /// right).
    /// </summary>
    /// <param name="other">The interval to compare against.</param>
    /// <returns><see langword="true" /> when this interval ends strictly above <paramref name="other" />.</returns>
    private bool ExtendsRightOf(DiscreteInterval<T> other) =>
        (UpperUnbounded && !other.UpperUnbounded)
        || (!UpperUnbounded && !other.UpperUnbounded && _last > other._last);

    /// <summary>
    /// Compares the starting position of this interval with <paramref name="other" /> for ordering disjoint pieces.
    /// </summary>
    /// <param name="other">The interval to compare against.</param>
    /// <returns>A negative value when this interval starts earlier, zero when equal, otherwise positive.</returns>
    private int CompareStartTo(DiscreteInterval<T> other) =>
        LowerUnbounded ? (other.LowerUnbounded ? 0 : -1)
        : other.LowerUnbounded ? 1
        : _first.CompareTo(other._first);
}
