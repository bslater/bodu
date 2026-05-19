// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Range.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a half-open range, where <see cref="StartInclusive" /> is included and <see cref="EndExclusive" /> is
/// excluded.
/// </summary>
/// <typeparam name="T">The comparable endpoint type.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Range{T}" /> is an immutable value type used as the element of <see cref="RangeSet{T}" /> and as a shared
/// building block for the <see cref="RangeDictionary{TKey, TValue}" /> family. The constructor validates that the start
/// endpoint is strictly less than the end endpoint and rejects degenerate or inverted ranges with
/// <see cref="ArgumentException" />.
/// </para>
/// <para>
/// Half-open semantics — <c>[start, end)</c> — match the convention used by .NET span slicing,
/// <see cref="System.Range" />, and most database <c>BETWEEN</c> alternatives. They allow adjacent ranges (
/// <c>[0, 5)</c> followed by <c>[5, 10)</c>) to abut without overlapping, which is the property that keeps
/// <see cref="RangeSet{T}" /> and <see cref="RangeDictionary{TKey, TValue}" /> internally consistent.
/// </para>
/// <para>
/// Equality is structural: two <see cref="Range{T}" /> values are equal when both endpoints compare equal under the
/// default endpoint comparer. <see cref="Range{T}" /> is suitable as a dictionary key when <typeparamref name="T" /> is
/// itself a well-behaved value type.
/// </para>
/// </remarks>
[DebuggerDisplay("[{StartInclusive}, {EndExclusive})")]
public readonly struct Range<T>
    : IEquatable<Range<T>>
    where T : IComparable<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Range{T}" /> struct.
    /// </summary>
    /// <param name="startInclusive">The inclusive start of the range.</param>
    /// <param name="endExclusive">The exclusive end of the range.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="startInclusive" /> or <paramref name="endExclusive" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="startInclusive" /> is greater than or equal to <paramref name="endExclusive" />.
    /// </exception>
    public Range(T startInclusive, T endExclusive)
    {
        ValidateRange(startInclusive, endExclusive, Comparer<T>.Default);

        StartInclusive = startInclusive;
        EndExclusive = endExclusive;
    }

    /// <summary>
    /// Gets the inclusive start of the range.
    /// </summary>
    /// <returns>The inclusive lower bound of the range.</returns>
    public T StartInclusive { get; }

    /// <summary>
    /// Gets the exclusive end of the range.
    /// </summary>
    /// <returns>The exclusive upper bound of the range.</returns>
    public T EndExclusive { get; }

    /// <summary>
    /// Determines whether the specified value falls inside this range.
    /// </summary>
    /// <param name="value">The value to test. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="value" /> is inside the range; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public bool Contains(T value)
    {
        ThrowHelper.ThrowIfNull(value);

        IComparer<T> comparer = Comparer<T>.Default;
        return comparer.Compare(StartInclusive, value) <= 0 &&
               comparer.Compare(value, EndExclusive) < 0;
    }

    /// <summary>
    /// Determines whether two ranges have identical endpoints.
    /// </summary>
    /// <param name="other">The range to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if the ranges share the same start and end; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(Range<T> other) =>
        Comparer<T>.Default.Compare(StartInclusive, other.StartInclusive) == 0 &&
        Comparer<T>.Default.Compare(EndExclusive, other.EndExclusive) == 0;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is Range<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(StartInclusive, EndExclusive);

    /// <inheritdoc />
    public override string ToString() =>
        $"[{StartInclusive}, {EndExclusive})";

    /// <summary>
    /// Returns a value indicating whether two ranges have identical endpoints.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true" /> if the operands are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(Range<T> left, Range<T> right) => left.Equals(right);

    /// <summary>
    /// Returns a value indicating whether two ranges have differing endpoints.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true" /> if the operands are not equal; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(Range<T> left, Range<T> right) => !left.Equals(right);

    /// <summary>
    /// Validates that the specified endpoints describe a non-empty half-open range under <paramref name="comparer" />.
    /// </summary>
    /// <param name="startInclusive">The inclusive start.</param>
    /// <param name="endExclusive">The exclusive end.</param>
    /// <param name="comparer">The comparer used to order endpoints. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="startInclusive" /> or <paramref name="endExclusive" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="startInclusive" /> is greater than or equal to <paramref name="endExclusive" /> per
    /// <paramref name="comparer" />.
    /// </exception>
    internal static void ValidateRange(T startInclusive, T endExclusive, IComparer<T> comparer)
    {
        ThrowHelper.ThrowIfNull(startInclusive);
        ThrowHelper.ThrowIfNull(endExclusive);

        if (comparer.Compare(startInclusive, endExclusive) >= 0) throw new ArgumentException("The range start must be less than the range end.", nameof(startInclusive)); //TODO: define a BCL-style exception message in the resx file and remove inline static text
    }
}
