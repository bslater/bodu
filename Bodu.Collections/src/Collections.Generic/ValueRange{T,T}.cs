// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ValueRange{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Represents a half-open range mapped to a value — the entry projection produced when enumerating a
/// <see cref="RangeDictionary{TKey, TValue}" />.
/// </summary>
/// <typeparam name="TKey">The comparable endpoint type.</typeparam>
/// <typeparam name="TValue">The value type associated with the range.</typeparam>
/// <remarks>
/// <para>
/// <see cref="ValueRange{TKey, TValue}" /> pairs a half-open range — <c>[StartInclusive, EndExclusive)</c> — with an
/// associated value, in the spirit of <see cref="KeyValuePair{TKey, TValue}" /> for keyed dictionaries. It is the
/// element type returned by <see cref="RangeDictionary{TKey, TValue}" /> enumeration and the natural shape for any API
/// that projects keyed ranges.
/// </para>
/// <para>
/// The public constructor validates that the start endpoint is strictly less than the end endpoint under
/// <see cref="Comparer{T}.Default" /> and rejects degenerate or inverted ranges with <see cref="ArgumentException" />.
/// Owning collections that have already validated endpoints during insertion may use an internal unchecked overload to
/// avoid re-validating during projection.
/// </para>
/// </remarks>
[DebuggerDisplay("[{StartInclusive}, {EndExclusive}) = {Value}")]
public readonly struct ValueRange<TKey, TValue>
    : IEquatable<ValueRange<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueRange{TKey, TValue}" /> struct.
    /// </summary>
    /// <param name="startInclusive">The inclusive start of the range.</param>
    /// <param name="endExclusive">The exclusive end of the range.</param>
    /// <param name="value">The value mapped to the range.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="startInclusive" /> or <paramref name="endExclusive" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="startInclusive" /> is greater than or equal to <paramref name="endExclusive" />.
    /// </exception>
    public ValueRange(TKey startInclusive, TKey endExclusive, TValue value)
    {
        Range<TKey>.ValidateRange(startInclusive, endExclusive, Comparer<TKey>.Default);

        StartInclusive = startInclusive;
        EndExclusive = endExclusive;
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueRange{TKey, TValue}" /> struct without validating endpoints.
    /// </summary>
    /// <param name="startInclusive">The inclusive start of the range.</param>
    /// <param name="endExclusive">The exclusive end of the range.</param>
    /// <param name="value">The value mapped to the range.</param>
    /// <param name="skipValidation">Always <see langword="true" />; selects the unchecked overload.</param>
    /// <remarks>
    /// Used by <see cref="RangeDictionary{TKey, TValue}" /> when the endpoints have already been validated by the
    /// owning collection, to avoid revalidation overhead during enumeration and entry projection.
    /// </remarks>
    internal ValueRange(TKey startInclusive, TKey endExclusive, TValue value, bool skipValidation)
    {
        _ = skipValidation;

        StartInclusive = startInclusive;
        EndExclusive = endExclusive;
        Value = value;
    }

    /// <summary>
    /// Gets the inclusive start of the range.
    /// </summary>
    /// <value>The inclusive lower bound of the range.</value>
    public TKey StartInclusive { get; }

    /// <summary>
    /// Gets the exclusive end of the range.
    /// </summary>
    /// <value>The exclusive upper bound of the range.</value>
    public TKey EndExclusive { get; }

    /// <summary>
    /// Gets the value associated with the range.
    /// </summary>
    /// <value>The value mapped to this range.</value>
    public TValue Value { get; }

    /// <summary>
    /// Determines whether the specified key falls inside this range.
    /// </summary>
    /// <param name="key">The key to test. Must not be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="key" /> is inside the range; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool Contains(TKey key)
    {
        ThrowHelper.ThrowIfNull(key);

        Comparer<TKey> comparer = Comparer<TKey>.Default;
        return comparer.Compare(StartInclusive, key) <= 0 &&
               comparer.Compare(key, EndExclusive) < 0;
    }

    /// <summary>
    /// Determines whether two entries have identical endpoints and equal values.
    /// </summary>
    /// <param name="other">The entry to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if the entries share the same start and end and their values compare equal; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Endpoints are compared with <see cref="Comparer{T}.Default" /> and values with
    /// <see cref="EqualityComparer{T}.Default" />.
    /// </remarks>
    public bool Equals(ValueRange<TKey, TValue> other) =>
        Comparer<TKey>.Default.Compare(StartInclusive, other.StartInclusive) == 0 &&
        Comparer<TKey>.Default.Compare(EndExclusive, other.EndExclusive) == 0 &&
        EqualityComparer<TValue>.Default.Equals(Value, other.Value);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is ValueRange<TKey, TValue> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(StartInclusive, EndExclusive, Value);

    /// <inheritdoc />
    public override string ToString() =>
        $"[{StartInclusive}, {EndExclusive}) = {Value}";

    /// <summary>
    /// Returns a value indicating whether two entries have identical endpoints and equal values.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true" /> if the operands are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(ValueRange<TKey, TValue> left, ValueRange<TKey, TValue> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns a value indicating whether two entries differ in endpoints or value.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true" /> if the operands are not equal; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(ValueRange<TKey, TValue> left, ValueRange<TKey, TValue> right)
    {
        return !left.Equals(right);
    }
}
