// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteInterval{T}.Factories.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct DiscreteInterval<T>
{
    /// <summary>
    /// Gets the canonical empty interval — the interval that contains no integer. Equal to every other empty
    /// <see cref="DiscreteInterval{T}" /> and to the default value.
    /// </summary>
    /// <value>An interval whose <see cref="IsEmpty" /> is <see langword="true" />.</value>
    public static DiscreteInterval<T> Empty =>
        default;

    /// <summary>
    /// Gets the unbounded interval <c>(-&#x221E;, +&#x221E;)</c> — every integer of <typeparamref name="T" />.
    /// </summary>
    /// <value>An interval unbounded on both sides.</value>
    public static DiscreteInterval<T> All =>
        new(T.Zero, T.Zero, (byte)(LowerUnboundedFlag | UpperUnboundedFlag));

    /// <summary>
    /// Creates the closed interval <c>[lower, upper]</c> — every integer from <paramref name="lower" /> to
    /// <paramref name="upper" /> inclusive.
    /// </summary>
    /// <param name="lower">The inclusive lower bound.</param>
    /// <param name="upper">The inclusive upper bound.</param>
    /// <returns>
    /// The interval, or <see cref="Empty" /> when <paramref name="lower" /> exceeds <paramref name="upper" />.
    /// </returns>
    public static DiscreteInterval<T> Closed(T lower, T upper) =>
        FromInclusive(lower, upper);

    /// <summary>
    /// Creates the open interval <c>(lower, upper)</c> — every integer strictly between the bounds. Adjacent integer
    /// bounds (for example <c>(1, 2)</c>) admit no integer and yield <see cref="Empty" />.
    /// </summary>
    /// <param name="lower">The exclusive lower bound.</param>
    /// <param name="upper">The exclusive upper bound.</param>
    /// <returns>The canonicalized interval.</returns>
    public static DiscreteInterval<T> Open(T lower, T upper) =>
        FromInclusive(lower + T.One, upper - T.One);

    /// <summary>
    /// Creates the closed-open interval <c>[lower, upper)</c> — every integer from <paramref name="lower" /> inclusive
    /// up to but excluding <paramref name="upper" />.
    /// </summary>
    /// <param name="lower">The inclusive lower bound.</param>
    /// <param name="upper">The exclusive upper bound.</param>
    /// <returns>The canonicalized interval.</returns>
    public static DiscreteInterval<T> ClosedOpen(T lower, T upper) =>
        FromInclusive(lower, upper - T.One);

    /// <summary>
    /// Creates the open-closed interval <c>(lower, upper]</c> — every integer above <paramref name="lower" /> up to and
    /// including <paramref name="upper" />.
    /// </summary>
    /// <param name="lower">The exclusive lower bound.</param>
    /// <param name="upper">The inclusive upper bound.</param>
    /// <returns>The canonicalized interval.</returns>
    public static DiscreteInterval<T> OpenClosed(T lower, T upper) =>
        FromInclusive(lower + T.One, upper);

    /// <summary>
    /// Creates the single-integer interval <c>[value, value]</c>.
    /// </summary>
    /// <param name="value">The single integer the interval contains.</param>
    /// <returns>A one-element interval.</returns>
    public static DiscreteInterval<T> Singleton(T value) =>
        new(value, value, PopulatedFlag);

    /// <summary>
    /// Creates the lower-bounded interval <c>[lower, +&#x221E;)</c> — every integer greater than or equal to
    /// <paramref name="lower" />.
    /// </summary>
    /// <param name="lower">The inclusive lower bound.</param>
    /// <returns>An upper-unbounded interval.</returns>
    public static DiscreteInterval<T> AtLeast(T lower) =>
        new(lower, T.Zero, UpperUnboundedFlag);

    /// <summary>
    /// Creates the lower-bounded interval <c>(lower, +&#x221E;)</c> — every integer greater than
    /// <paramref name="lower" />.
    /// </summary>
    /// <param name="lower">The exclusive lower bound.</param>
    /// <returns>An upper-unbounded interval.</returns>
    public static DiscreteInterval<T> GreaterThan(T lower) =>
        new(lower + T.One, T.Zero, UpperUnboundedFlag);

    /// <summary>
    /// Creates the upper-bounded interval <c>(-&#x221E;, upper]</c> — every integer less than or equal to
    /// <paramref name="upper" />.
    /// </summary>
    /// <param name="upper">The inclusive upper bound.</param>
    /// <returns>A lower-unbounded interval.</returns>
    public static DiscreteInterval<T> AtMost(T upper) =>
        new(T.Zero, upper, LowerUnboundedFlag);

    /// <summary>
    /// Creates the upper-bounded interval <c>(-&#x221E;, upper)</c> — every integer less than <paramref name="upper" />.
    /// </summary>
    /// <param name="upper">The exclusive upper bound.</param>
    /// <returns>A lower-unbounded interval.</returns>
    public static DiscreteInterval<T> LessThan(T upper) =>
        new(T.Zero, upper - T.One, LowerUnboundedFlag);
}
