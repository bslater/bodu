// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Interval{T}.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct Interval<T>
{
    /// <summary>
    /// Gets the lower endpoint of the interval.
    /// </summary>
    /// <value>The lower endpoint passed to the constructor or factory method.</value>
    public T Lower =>
        _lower;

    /// <summary>
    /// Gets the upper endpoint of the interval.
    /// </summary>
    /// <value>The upper endpoint passed to the constructor or factory method.</value>
    public T Upper =>
        _upper;

    /// <summary>
    /// Gets a value indicating whether the lower endpoint is part of the interval.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when the interval is closed on the lower side (i.e. <c>[Lower, ...</c>);
    /// <see langword="false" /> when open (i.e. <c>(Lower, ...</c>).
    /// </value>
    public bool LowerInclusive =>
        (_flags & 1) != 0;

    /// <summary>
    /// Gets a value indicating whether the upper endpoint is part of the interval.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when the interval is closed on the upper side (i.e. <c>..., Upper]</c>);
    /// <see langword="false" /> when open (i.e. <c>..., Upper)</c>).
    /// </value>
    public bool UpperInclusive =>
        (_flags & UpperInclusiveFlag) != 0;

    /// <summary>
    /// Gets a value indicating whether the lower side is unbounded — the interval extends to <c>-&#x221E;</c> with no
    /// finite lower limit.
    /// </summary>
    /// <value><see langword="true" /> when the interval is lower-unbounded (i.e. <c>(-&#x221E;, ...</c>); otherwise <see langword="false" />.</value>
    public bool LowerUnbounded =>
        (_flags & LowerUnboundedFlag) != 0;

    /// <summary>
    /// Gets a value indicating whether the upper side is unbounded — the interval extends to <c>+&#x221E;</c> with no
    /// finite upper limit.
    /// </summary>
    /// <value><see langword="true" /> when the interval is upper-unbounded (i.e. <c>..., +&#x221E;)</c>); otherwise <see langword="false" />.</value>
    public bool UpperUnbounded =>
        (_flags & UpperUnboundedFlag) != 0;

    /// <summary>
    /// Gets a value indicating whether both endpoints are finite — the interval has a concrete lower and upper limit.
    /// </summary>
    /// <value><see langword="true" /> when neither side is unbounded; otherwise <see langword="false" />.</value>
    public bool IsBounded =>
        (_flags & (LowerUnboundedFlag | UpperUnboundedFlag)) == 0;

    /// <summary>
    /// Gets a value indicating whether the interval contains no values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An interval is empty when its bounds cannot admit any value. Two cases produce an empty interval:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <see cref="Lower" /> is strictly greater than <see cref="Upper" /> — the bounds are inverted.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="Lower" /> equals <see cref="Upper" /> and at least one endpoint is open — for example <c>(5, 5]</c>,
    /// <c>[5, 5)</c>, and <c>(5, 5)</c> are all empty because no value of <typeparamref name="T" /> can satisfy both
    /// endpoint constraints. <c>[5, 5]</c> is non-empty and represents the single point <c>5</c>; see
    /// <see cref="IsDegenerate" />.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <value><see langword="true" /> when the interval is empty; otherwise <see langword="false" />.</value>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// Interval<int>.Closed(1, 5).IsEmpty;     // False — [1, 5] holds values
    /// Interval<int>.Open(5, 5).IsEmpty;       // True  — (5, 5) admits no value
    /// Interval<int>.Closed(5, 1).IsEmpty;     // True  — inverted bounds
    /// Interval<int>.Closed(5, 5).IsEmpty;     // False — the single point 5
    ///]]>
    /// </code>
    /// </example>
    public bool IsEmpty =>
        IsBounded && (_lower > _upper || (_lower == _upper && (!LowerInclusive || !UpperInclusive)));

    /// <summary>
    /// Gets a value indicating whether the interval represents a single point — a closed-closed interval whose lower
    /// and upper endpoints are equal.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when <see cref="Lower" /> equals <see cref="Upper" /> and both
    /// <see cref="LowerInclusive" /> and <see cref="UpperInclusive" /> are <see langword="true" />; otherwise
    /// <see langword="false" />.
    /// </value>
    public bool IsDegenerate =>
        IsBounded && _lower == _upper && LowerInclusive && UpperInclusive;

    /// <summary>
    /// Gets the algebraic length of the interval — the difference between its upper and lower endpoints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For non-empty intervals, the length is computed as <c>Upper - Lower</c> regardless of endpoint inclusion. This
    /// matches the Lebesgue measure for continuous numeric types (<see cref="double" />, <see cref="decimal" />): the
    /// measure of <c>[1, 2]</c>, <c>(1, 2)</c>, <c>[1, 2)</c>, and <c>(1, 2]</c> is the same value <c>1</c>.
    /// </para>
    /// <para>
    /// For integer types, callers wanting the count of integers contained in the interval should compute it directly
    /// from <see cref="Lower" />, <see cref="Upper" />, <see cref="LowerInclusive" />, and
    /// <see cref="UpperInclusive" /> — endpoint inclusion matters for that semantic, and this property does not model
    /// it.
    /// </para>
    /// <para>
    /// For empty intervals, the length is <see cref="INumberBase{TSelf}.Zero" />. For unbounded intervals the length is
    /// infinite and not representable in <typeparamref name="T" />, so this property throws.
    /// </para>
    /// </remarks>
    /// <value>The non-negative length of the interval, or <see cref="INumberBase{TSelf}.Zero" /> when empty.</value>
    /// <exception cref="InvalidOperationException">The interval is unbounded (<see cref="IsBounded" /> is <see langword="false" />).</exception>
    public T Length =>
        IsEmpty ? T.Zero
        : IsBounded ? _upper - _lower
        : throw new InvalidOperationException(NumericsResourceStrings.Op_Invalid_IntervalUnboundedLength);
}
