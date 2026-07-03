// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalSet{T}.SetOperations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct IntervalSet<T> : IEquatable<IntervalSet<T>>
{
    /// <summary>
    /// Returns the union of this set with <paramref name="interval" />.
    /// </summary>
    /// <param name="interval">The interval to add.</param>
    /// <returns>The normalized union.</returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// IntervalSet<int>.Of(Interval<int>.Closed(1, 3), Interval<int>.Closed(8, 9))
    ///     .Union(Interval<int>.Closed(2, 5));   // [1, 5] ∪ [8, 9]
    ///]]>
    /// </code>
    /// </example>
    public IntervalSet<T> Union(Interval<T> interval)
    {
        if (interval.IsEmpty)
            return this;

        if (_intervals is null)
            return Of(interval);

        var pieces = new List<Interval<T>>(_intervals) { interval };
        return From(pieces);
    }

    /// <summary>
    /// Returns the union of this set with <paramref name="other" />.
    /// </summary>
    /// <param name="other">The set to add.</param>
    /// <returns>The normalized union.</returns>
    public IntervalSet<T> Union(IntervalSet<T> other)
    {
        if (other._intervals is null)
            return this;

        if (_intervals is null)
            return other;

        var pieces = new List<Interval<T>>(_intervals);
        pieces.AddRange(other._intervals);
        return From(pieces);
    }

    /// <summary>
    /// Returns the intersection of this set with <paramref name="interval" /> — the members shared with it.
    /// </summary>
    /// <param name="interval">The interval to intersect with.</param>
    /// <returns>The normalized intersection.</returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// IntervalSet<int>.Of(Interval<int>.Closed(1, 5), Interval<int>.Closed(8, 12))
    ///     .Intersect(Interval<int>.Closed(4, 10));   // [4, 5] ∪ [8, 10]
    ///]]>
    /// </code>
    /// </example>
    public IntervalSet<T> Intersect(Interval<T> interval)
    {
        if (_intervals is null)
            return Empty;

        var pieces = new List<Interval<T>>();
        foreach (Interval<T> piece in _intervals)
        {
            Interval<T> overlap = piece.Intersect(interval);
            if (!overlap.IsEmpty)
                pieces.Add(overlap);
        }

        return From(pieces);
    }

    /// <summary>
    /// Returns the intersection of this set with <paramref name="other" /> — the members in both.
    /// </summary>
    /// <param name="other">The set to intersect with.</param>
    /// <returns>The normalized intersection.</returns>
    public IntervalSet<T> Intersect(IntervalSet<T> other)
    {
        if (_intervals is null || other._intervals is null)
            return Empty;

        var pieces = new List<Interval<T>>();
        foreach (Interval<T> a in _intervals)
        {
            foreach (Interval<T> b in other._intervals)
            {
                Interval<T> overlap = a.Intersect(b);
                if (!overlap.IsEmpty)
                    pieces.Add(overlap);
            }
        }

        return From(pieces);
    }

    /// <summary>
    /// Returns this set with the members of <paramref name="interval" /> removed — the set difference.
    /// </summary>
    /// <param name="interval">The interval to subtract.</param>
    /// <returns>The normalized difference.</returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// IntervalSet<int>.Of(Interval<int>.Closed(1, 10))
    ///     .Except(Interval<int>.Closed(3, 5));   // [1, 3) ∪ (5, 10]
    ///]]>
    /// </code>
    /// </example>
    public IntervalSet<T> Except(Interval<T> interval)
    {
        if (_intervals is null || interval.IsEmpty)
            return this;

        var pieces = new List<Interval<T>>();
        foreach (Interval<T> piece in _intervals)
        {
            IntervalPair<T> remainder = piece.Difference(interval);
            for (int i = 0; i < remainder.Count; i++)
                pieces.Add(remainder[i]);
        }

        return From(pieces);
    }

    /// <summary>
    /// Returns this set with the members of <paramref name="other" /> removed — the set difference.
    /// </summary>
    /// <param name="other">The set to subtract.</param>
    /// <returns>The normalized difference.</returns>
    public IntervalSet<T> Except(IntervalSet<T> other)
    {
        if (_intervals is null || other._intervals is null)
            return this;

        IntervalSet<T> result = this;
        foreach (Interval<T> piece in other._intervals)
            result = result.Except(piece);

        return result;
    }

    /// <summary>
    /// Returns the complement of this set — every value not in the set, over the whole line
    /// <c>(-&#x221E;, +&#x221E;)</c>.
    /// </summary>
    /// <returns>The complement set.</returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// IntervalSet<int>.Of(Interval<int>.Closed(1, 5)).Complement();   // (-∞, 1) ∪ (5, +∞)
    ///]]>
    /// </code>
    /// </example>
    public IntervalSet<T> Complement()
    {
        IntervalSet<T> result = Of(Interval<T>.All);
        if (_intervals is null)
            return result;

        foreach (Interval<T> piece in _intervals)
            result = result.Except(piece);

        return result;
    }

    /// <summary>
    /// Determines whether this set equals <paramref name="other" /> — the same normalized pieces in the same order.
    /// </summary>
    /// <param name="other">The set to compare against.</param>
    /// <returns>
    /// <see langword="true" /> when the two describe the same set of values; otherwise <see langword="false" />.
    /// </returns>
    public bool Equals(IntervalSet<T> other)
    {
        int count = Count;
        if (count != other.Count)
            return false;

        for (int i = 0; i < count; i++)
        {
            if (!_intervals![i].Equals(other._intervals![i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether this set equals the boxed <paramref name="obj" />.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="obj" /> is an equal <see cref="IntervalSet{T}" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is IntervalSet<T> other && Equals(other);

    /// <summary>
    /// Returns a hash code consistent with <see cref="Equals(IntervalSet{T})" />.
    /// </summary>
    /// <returns>A 32-bit hash code.</returns>
    public override int GetHashCode()
    {
        if (_intervals is null)
            return 0;

        var hash = default(HashCode);
        foreach (Interval<T> piece in _intervals)
            hash.Add(piece);

        return hash.ToHashCode();
    }

    /// <summary>
    /// Determines whether two sets are equal.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns><see langword="true" /> when equal; otherwise <see langword="false" />.</returns>
    public static bool operator ==(IntervalSet<T> left, IntervalSet<T> right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two sets are not equal.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns><see langword="true" /> when they differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(IntervalSet<T> left, IntervalSet<T> right) =>
        !left.Equals(right);
}
