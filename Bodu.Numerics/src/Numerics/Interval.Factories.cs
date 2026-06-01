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
    public static Interval<T> Empty =>
        new(T.Zero, T.Zero, lowerInclusive: false, upperInclusive: false);

    /// <summary>
    /// Creates a closed-closed interval — <c>[lower, upper]</c> — that includes both endpoints.
    /// </summary>
    /// <param name="lower">The lower endpoint.</param>
    /// <param name="upper">The upper endpoint.</param>
    /// <returns>A closed-closed interval over the supplied bounds.</returns>
    /// <remarks>
    /// When <paramref name="lower" /> is greater than <paramref name="upper" />, the returned interval is empty. When
    /// the two endpoints are equal, the returned interval is a degenerate single-point interval (see
    /// <see cref="Singleton(T)" />).
    /// </remarks>
    public static Interval<T> Closed(T lower, T upper) =>
        new(lower, upper, lowerInclusive: true, upperInclusive: true);

    /// <summary>
    /// Creates an open-open interval — <c>(lower, upper)</c> — that excludes both endpoints.
    /// </summary>
    /// <param name="lower">The lower endpoint.</param>
    /// <param name="upper">The upper endpoint.</param>
    /// <returns>An open-open interval over the supplied bounds.</returns>
    /// <remarks>
    /// When <paramref name="lower" /> is greater than or equal to <paramref name="upper" />, the returned interval is
    /// empty.
    /// </remarks>
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
    /// The closed-open shape is the most common in programming contexts: a range starting at an inclusive lower bound
    /// and ending before an exclusive upper bound matches the conventions of <c>System.Range</c>, LINQ's
    /// <c>Enumerable.Range</c>, and most iterator protocols.
    /// </remarks>
    public static Interval<T> ClosedOpen(T lower, T upper) =>
        new(lower, upper, lowerInclusive: true, upperInclusive: false);

    /// <summary>
    /// Creates an open-closed interval — <c>(lower, upper]</c> — that excludes the lower endpoint and includes the
    /// upper endpoint.
    /// </summary>
    /// <param name="lower">The lower endpoint (excluded).</param>
    /// <param name="upper">The upper endpoint (included).</param>
    /// <returns>An open-closed interval over the supplied bounds.</returns>
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
    public static Interval<T> Singleton(T value) =>
        new(value, value, lowerInclusive: true, upperInclusive: true);
}
