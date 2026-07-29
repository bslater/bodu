// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteInterval{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// Represents an immutable interval over a discrete integer domain — a set of consecutive integers of type
/// <typeparamref name="T" /> — with successor/predecessor-aware emptiness and adjacency.
/// </summary>
/// <typeparam name="T">The integer type used for the interval's endpoints.</typeparam>
/// <remarks>
/// <para>
/// <see cref="DiscreteInterval{T}" /> is the discrete counterpart to the continuous <see cref="Interval{T}" />. Where
/// <see cref="Interval{T}" /> models a continuum of real coordinates, <see cref="DiscreteInterval{T}" /> models the set
/// of representable integers between its bounds, which changes two behaviours fundamentally:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <b>Emptiness reflects representable membership.</b> An open interval whose bounds are consecutive integers — for
/// example <c>(1, 2)</c> — contains no integer and is therefore empty, unlike the non-empty continuous interval over
/// the same bounds.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Adjacency is by successor.</b> Two intervals that leave no integer between them — for example <c>[1, 2]</c> and
/// <c>[3, 4]</c> — are adjacent and union to a single run <c>[1, 4]</c>, because no integer lies in the gap.
/// </description>
/// </item>
/// </list>
/// <para>
/// Every interval is canonicalized to inclusive <c>[First, Last]</c> integer bounds at construction (an open bound is
/// shifted inward by one), so all equal integer sets share one representation and the default value is the empty set.
/// Unbounded and half-bounded intervals (<c>[a, +&#x221E;)</c>, <c>(-&#x221E;, b]</c>, <c>(-&#x221E;, +&#x221E;)</c>)
/// are supported through the <see cref="AtLeast(T)" />, <see cref="AtMost(T)" />, and <see cref="All" /> factory
/// family.
/// </para>
/// <para>
/// <b>Domain scope.</b> This type is deliberately integer-only — its domain is <see cref="IBinaryInteger{TSelf}" />. It
/// is not a general discrete-domain abstraction: ranges over <see cref="System.DateOnly" />, <see cref="char" />, enum
/// values, or a custom successor domain are out of scope. For a disconnected result — the union of several runs — use
/// <see cref="IntervalSet{T}" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Numerics;
///
/// DiscreteInterval<int>.Open(1, 2).IsEmpty;                 // True — no integer strictly between 1 and 2
///
/// var a = DiscreteInterval<int>.Closed(1, 2);
/// var b = DiscreteInterval<int>.Closed(3, 4);
/// a.TryUnion(b, out var run);                               // run = [1, 4], result = true (successor-adjacent)
///
/// DiscreteInterval<int>.Closed(1, 10).Count;                // 10
///]]>
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq}")]
public readonly partial struct DiscreteInterval<T>
    where T : IBinaryInteger<T>
{
    /// <summary>Flag bit marking the lower side as unbounded (<c>-&#x221E;</c>).</summary>
    private const byte LowerUnboundedFlag = 0b001;

    /// <summary>Flag bit marking the upper side as unbounded (<c>+&#x221E;</c>).</summary>
    private const byte UpperUnboundedFlag = 0b010;

    /// <summary>Flag bit marking a non-empty bounded body (distinguishes the singleton <c>[0, 0]</c> from the empty default).</summary>
    private const byte PopulatedFlag = 0b100;

    /// <summary>Combined mask of every flag that denotes a non-empty interval.</summary>
    private const byte NonEmptyMask = LowerUnboundedFlag | UpperUnboundedFlag | PopulatedFlag;

    /// <summary>The inclusive lower bound (canonical), or <see cref="INumberBase{TSelf}.Zero" /> when lower-unbounded or empty.</summary>
    private readonly T _first;

    /// <summary>The inclusive upper bound (canonical), or <see cref="INumberBase{TSelf}.Zero" /> when upper-unbounded or empty.</summary>
    private readonly T _last;

    /// <summary>Packed flags. Bit 0 marks the lower side unbounded; bit 1 the upper side unbounded; bit 2 marks a populated bounded body. The empty interval has no bits set, so <c>default</c> is empty.</summary>
    private readonly byte _flags;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscreteInterval{T}" /> struct from canonical inclusive bounds and
    /// packed flags.
    /// </summary>
    /// <param name="first">The canonical inclusive lower bound.</param>
    /// <param name="last">The canonical inclusive upper bound.</param>
    /// <param name="flags">The packed flag byte.</param>
    private DiscreteInterval(T first, T last, byte flags)
    {
        _first = first;
        _last = last;
        _flags = flags;
    }

    /// <summary>
    /// Creates a bounded interval from inclusive integer bounds, collapsing to <see cref="Empty" /> when the bounds do
    /// not admit any integer.
    /// </summary>
    /// <param name="first">The inclusive lower bound.</param>
    /// <param name="last">The inclusive upper bound.</param>
    /// <returns>The canonical interval.</returns>
    private static DiscreteInterval<T> FromInclusive(T first, T last) =>
        first > last ? default : new DiscreteInterval<T>(first, last, PopulatedFlag);

    /// <summary>
    /// Attempts to compute the successor of <paramref name="value" />, detecting wrap-around at the domain maximum.
    /// </summary>
    /// <param name="value">The integer whose successor is computed.</param>
    /// <param name="successor">When this method returns <see langword="true" />, the value one above <paramref name="value" />.</param>
    /// <returns><see langword="false" /> when <paramref name="value" /> is the domain maximum and has no successor.</returns>
    /// <remarks>
    /// Fixed-width integer addition wraps silently, so <c>T.MaxValue + 1</c> lands on <c>T.MinValue</c>; the wrap is
    /// detected by ordering (<c>successor &lt; value</c>). Arbitrary-precision types never wrap, so this always
    /// succeeds for them.
    /// </remarks>
    private static bool TrySuccessor(T value, out T successor)
    {
        successor = value + T.One;
        return successor > value;
    }

    /// <summary>
    /// Attempts to compute the predecessor of <paramref name="value" />, detecting wrap-around at the domain minimum.
    /// </summary>
    /// <param name="value">The integer whose predecessor is computed.</param>
    /// <param name="predecessor">When this method returns <see langword="true" />, the value one below <paramref name="value" />.</param>
    /// <returns><see langword="false" /> when <paramref name="value" /> is the domain minimum and has no predecessor.</returns>
    private static bool TryPredecessor(T value, out T predecessor)
    {
        predecessor = value - T.One;
        return predecessor < value;
    }
}
