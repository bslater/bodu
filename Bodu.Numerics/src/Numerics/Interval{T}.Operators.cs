// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Interval{T}.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct Interval<T>
{
    /// <summary>
    /// Returns the intersection of two intervals — the values shared by both — as an operator alias for
    /// <see cref="Intersect(Interval{T})" />.
    /// </summary>
    /// <param name="left">The first interval.</param>
    /// <param name="right">The second interval.</param>
    /// <returns>The intersection interval, or <see cref="Empty" /> when the two share no values.</returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var shared = Interval<int>.Closed(1, 5) & Interval<int>.Closed(3, 7);   // [3, 5]
    ///]]>
    /// </code>
    /// </example>
    public static Interval<T> operator &(Interval<T> left, Interval<T> right) =>
        left.Intersect(right);

    /// <summary>
    /// Returns the contiguous union of two intervals as an operator alias for <see cref="TryUnion(Interval{T}, out Interval{T})" />,
    /// for operands whose union is a single interval.
    /// </summary>
    /// <param name="left">The first interval.</param>
    /// <param name="right">The second interval.</param>
    /// <returns>The single contiguous interval covering both operands.</returns>
    /// <exception cref="InvalidOperationException">
    /// The operands are disjoint and non-adjacent, so their union is not a single contiguous interval. Use
    /// <see cref="Difference(Interval{T})" /> or accumulate the pieces when a multi-interval result is possible.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var merged = Interval<int>.ClosedOpen(1, 5) | Interval<int>.Closed(5, 10);   // [1, 10]
    ///]]>
    /// </code>
    /// </example>
    public static Interval<T> operator |(Interval<T> left, Interval<T> right) =>
        left.TryUnion(right, out Interval<T> union)
            ? union
            : throw new InvalidOperationException(NumericsResourceStrings.Op_Invalid_IntervalNonContiguousUnion);
}
