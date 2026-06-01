// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Interval.Factories.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct Interval<T>
{
    /// <summary>
    /// Gets the canonical empty interval — the interval that contains no values. Equal by
    /// <see cref="IEquatable{T}.Equals(T)" /> to every other empty <see cref="Interval{T}" /> over the same
    /// <typeparamref name="T" />.
    /// </summary>
    /// <returns>
    /// An <see cref="Interval{T}" /> whose <see cref="IsEmpty" /> property is <see langword="true" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use <see cref="Empty" /> as the neutral element of the interval set algebra: <see cref="Intersect" /> returns it
    /// when two operands share no values, and <see cref="TryUnion" /> treats it as the identity. The
    /// default-constructed <c>Interval&lt;T&gt;</c> compares equal to <see cref="Empty" />, so a field or local that
    /// was never assigned reads as the empty interval rather than as a malformed value.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var none = Interval<int>.Empty;
    /// none.IsEmpty;                    // True
    /// none.ToString();                 // "∅"
    ///
    /// // Any inverted-bounds or equal-bounds-with-open-endpoint interval compares equal to Empty.
    /// var inverted = new Interval<int>(5, 1, true, true);
    /// var collapsed = new Interval<int>(0, 0, false, false);
    /// none == inverted;                // True
    /// none == collapsed;               // True
    ///]]>
    /// </code>
    /// </example>
    public static Interval<T> Empty =>
        new(T.Zero, T.Zero, lowerInclusive: false, upperInclusive: false);

    /// <summary>
    /// Creates a closed-closed interval — <c>[lower, upper]</c> — that includes both endpoints.
    /// </summary>
    /// <param name="lower">The lower endpoint.</param>
    /// <param name="upper">The upper endpoint.</param>
    /// <returns>A closed-closed interval over the supplied bounds.</returns>
    /// <remarks>
    /// <para>
    /// The closed-closed shape — also called <i>inclusive</i> — is the natural choice for ranges where both boundary
    /// values are valid members of the set: a percentage in <c>[0, 100]</c>, a die roll in <c>[1, 6]</c>, a thermometer
    /// reading in <c>[-273.15, +∞)</c>. Prefer <see cref="ClosedOpen(T, T)" /> for spans, slices, and scheduling
    /// windows where adjacent ranges should partition cleanly.
    /// </para>
    /// <para>
    /// When <paramref name="lower" /> is greater than <paramref name="upper" />, the returned interval is empty. When
    /// the two endpoints are equal, the returned interval is a degenerate single-point interval (see
    /// <see cref="Singleton(T)" />).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var percentage = Interval<int>.Closed(0, 100);   // [0, 100]
    /// percentage.Contains(0);                          // True — closed lower
    /// percentage.Contains(100);                        // True — closed upper
    /// percentage.ToString();                           // "[0, 100]"
    ///
    /// // Inverted bounds collapse to Empty.
    /// Interval<int>.Closed(10, 5).IsEmpty;             // True
    ///]]>
    /// </code>
    /// </example>
    public static Interval<T> Closed(T lower, T upper) =>
        new(lower, upper, lowerInclusive: true, upperInclusive: true);

    /// <summary>
    /// Creates an open-open interval — <c>(lower, upper)</c> — that excludes both endpoints.
    /// </summary>
    /// <param name="lower">The lower endpoint.</param>
    /// <param name="upper">The upper endpoint.</param>
    /// <returns>An open-open interval over the supplied bounds.</returns>
    /// <remarks>
    /// <para>
    /// The open-open shape — also called <i>exclusive</i> — is the natural choice for strict inequalities such as
    /// <c>0 &lt; rate &lt; 1</c> or "between but not at" semantics where neither boundary value is itself a member.
    /// </para>
    /// <para>
    /// When <paramref name="lower" /> is greater than or equal to <paramref name="upper" />, the returned interval is
    /// empty — there is no value strictly between two equal or inverted bounds.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var strictlyPositive = Interval<double>.Open(0.0, double.PositiveInfinity);  // (0, +∞)
    /// strictlyPositive.Contains(0.0);                                              // False — open lower
    /// strictlyPositive.Contains(1e-300);                                           // True
    ///
    /// // Equal bounds with both endpoints open are empty.
    /// Interval<int>.Open(5, 5).IsEmpty;                                            // True
    ///]]>
    /// </code>
    /// </example>
    public static Interval<T> Open(T lower, T upper) =>
        new(lower, upper, lowerInclusive: false, upperInclusive: false);

    /// <summary>
    /// Creates a closed-open interval — <c>[lower, upper)</c> — that includes the lower endpoint and excludes the upper
    /// endpoint.
    /// </summary>
    /// <param name="lower">The lower endpoint (included).</param>
    /// <param name="upper">The upper endpoint (excluded).</param>
    /// <returns>A closed-open interval over the supplied bounds.</returns>
    /// <remarks>
    /// <para>
    /// The closed-open shape is the most common in programming contexts: a range starting at an inclusive lower bound
    /// and ending before an exclusive upper bound matches the conventions of <c>System.Range</c>, LINQ's
    /// <c>Enumerable.Range</c>, and most iterator protocols. Adjacent closed-open intervals partition a span cleanly —
    /// <c>[0, 90)</c> and <c>[90, 181)</c> together cover exactly <c>[0, 181)</c> with no overlap and no gap — so this
    /// shape is the default choice for scheduling windows, bucket boundaries, and time slots.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var window = Interval<int>.ClosedOpen(0, 100);   // [0, 100)
    /// window.Contains(0);                              // True  — closed lower
    /// window.Contains(99);                             // True
    /// window.Contains(100);                            // False — open upper
    ///
    /// // Adjacent half-open windows merge with no overlap and no gap.
    /// var q1 = Interval<int>.ClosedOpen(0, 90);
    /// var q2 = Interval<int>.ClosedOpen(90, 181);
    /// q1.TryUnion(q2, out var firstHalf);              // firstHalf = [0, 181)
    ///]]>
    /// </code>
    /// </example>
    public static Interval<T> ClosedOpen(T lower, T upper) =>
        new(lower, upper, lowerInclusive: true, upperInclusive: false);

    /// <summary>
    /// Creates an open-closed interval — <c>(lower, upper]</c> — that excludes the lower endpoint and includes the
    /// upper endpoint.
    /// </summary>
    /// <param name="lower">The lower endpoint (excluded).</param>
    /// <param name="upper">The upper endpoint (included).</param>
    /// <returns>An open-closed interval over the supplied bounds.</returns>
    /// <remarks>
    /// <para>
    /// The open-closed shape is the mirror image of <see cref="ClosedOpen(T, T)" /> and the natural choice for ranges
    /// expressed as "strictly greater than X, up to and including Y" — a billing tier above a threshold, a histogram
    /// bin that owns its upper edge, or a tax bracket that exits at one boundary and enters at the next.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Billing tier: anything above $1,000 up to and including $10,000.
    /// var tier = Interval<decimal>.OpenClosed(1000m, 10_000m);   // (1000, 10000]
    /// tier.Contains(1000m);                                       // False — open lower
    /// tier.Contains(10_000m);                                     // True  — closed upper
    /// tier.ToString();                                            // "(1000, 10000]"
    ///]]>
    /// </code>
    /// </example>
    public static Interval<T> OpenClosed(T lower, T upper) =>
        new(lower, upper, lowerInclusive: false, upperInclusive: true);

    /// <summary>
    /// Creates a degenerate interval that contains the single value <paramref name="value" /> — equivalent to
    /// <c>[value, value]</c>.
    /// </summary>
    /// <param name="value">The single value the interval contains.</param>
    /// <returns>
    /// A closed-closed interval whose lower and upper endpoints both equal <paramref name="value" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A degenerate interval has algebraic <see cref="Length" /> of zero but is not empty: it contains exactly the one
    /// value its endpoints share. Use it as the identity for an "any-of" intersection sweep, as a single-value filter
    /// that composes uniformly with multi-value filters, or as the seed of a union that accumulates a span.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var single = Interval<int>.Singleton(42);
    /// single.Contains(42);                              // True
    /// single.IsDegenerate;                              // True
    /// single.IsEmpty;                                   // False
    /// single.Length;                                    // 0
    /// single.ToString();                                // "[42, 42]"
    ///]]>
    /// </code>
    /// </example>
    public static Interval<T> Singleton(T value) =>
        new(value, value, lowerInclusive: true, upperInclusive: true);
}
