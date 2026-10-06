// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfMonthSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics;
using System.Numerics;

namespace Bodu;

/// <summary>
/// Represents an immutable set of days of the month, 1 to 31.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="DayOfMonthSet" /> holds one bit per day, so it never allocates, and testing, adding or removing a day,
/// or combining two sets with <c>|</c>, <c>&amp;</c>, <c>^</c> and <c>~</c>, is a single bitwise operation.
/// <see cref="Contains(int)" /> answers <see langword="false" /> for any value outside 1 to 31 rather than throwing,
/// which keeps a membership test against <see cref="DateTime.Day" /> to a comparison and a bit test.
/// </para>
/// <para>
/// The values are day numbers and do not depend on the length of a month: a set that contains 31 contains it whatever
/// the month, so a date in a 30-day month never matches it. Days counted back from the end of a month are a separate
/// question, which a caller answers by testing <c>DateTime.DaysInMonth(year, month) - day + 1</c> against a second set.
/// </para>
/// <para>
/// The text form lists the days in ascending order, separated by commas, each run of two or more consecutive days
/// written as an inclusive range, so <c>"1,15"</c> selects the 1st and the 15th. The empty set is the empty string.
/// <see cref="Parse(string)" /> reads any list of values and ranges in that form, in any order, and
/// <see cref="ToString()" /> writes the canonical one. <see cref="ToString(string)" /> also writes every day without
/// ranges, or a binary form with one character per day, and <see cref="Parse(string)" /> and
/// <see cref="ParseExact(string, string)" /> read them back.
/// </para>
/// <para>
/// <see cref="ToUInt64" /> and <see cref="FromUInt64(ulong)" /> expose the bits directly: bit <c>n</c> selects day
/// <c>1 + n</c>.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var selected = new DayOfMonthSet(1, 15);
/// DayOfMonthSet parsed = DayOfMonthSet.Parse("1,15");
///
/// bool due = selected.Contains(DateTime.Today.Day);
/// bool same = selected == parsed; // true
///]]>
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq} ({Count} selected)")]
public readonly partial struct DayOfMonthSet
    : IEquatable<DayOfMonthSet>,
      IEqualityOperators<DayOfMonthSet, DayOfMonthSet, bool>,
      IBitwiseOperators<DayOfMonthSet, DayOfMonthSet, DayOfMonthSet>,
      IParsable<DayOfMonthSet>,
      IFormattable,
      IEnumerable<int>
{
    /// <summary>The smallest day in the set's domain.</summary>
    private const int MinimumValue = 1;

    /// <summary>The largest day in the set's domain.</summary>
    private const int MaximumValue = 31;

    /// <summary>The bits that select every day in the domain.</summary>
    private const ulong AllBits = (1UL << 31) - 1;

    /// <summary>The selected days, bit <c>n</c> selecting day <c>1 + n</c>.</summary>
    private readonly ulong _bits;

    /// <summary>
    /// Initializes a new instance of the <see cref="DayOfMonthSet" /> struct that selects the specified days.
    /// </summary>
    /// <param name="days">The days to select, or <see langword="null" /> for none.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a value in <paramref name="days" /> is less than 1 or greater than 31.
    /// </exception>
    /// <remarks>
    /// A day may appear more than once, and the order is immaterial.
    /// </remarks>
    public DayOfMonthSet(params int[]? days)
    {
        _bits = CalendarValueSet.FromValues(days, MinimumValue, MaximumValue, nameof(days));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DayOfMonthSet" /> struct from bits already known to lie in the
    /// domain.
    /// </summary>
    /// <param name="bits">The bits, with nothing set above bit 30.</param>
    private DayOfMonthSet(ulong bits)
    {
        _bits = bits;
    }

    /// <summary>
    /// Gets the set that selects no day.
    /// </summary>
    public static DayOfMonthSet Empty { get; }

    /// <summary>
    /// Gets the set that selects every day from 1 to 31.
    /// </summary>
    public static DayOfMonthSet All { get; } = new(AllBits);

    /// <summary>
    /// Gets the number of days the set selects.
    /// </summary>
    /// <value>A number from 0 to 31.</value>
    public int Count =>
        BitOperations.PopCount(_bits);

    /// <summary>
    /// Creates a set from its bits, bit <c>n</c> selecting day <c>1 + n</c>.
    /// </summary>
    /// <param name="bits">The bits.</param>
    /// <returns>The set that <paramref name="bits" /> describe.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="bits" /> sets a bit above bit 30, which selects no day.
    /// </exception>
    public static DayOfMonthSet FromUInt64(ulong bits)
    {
        ThrowHelper.ThrowIfBitsOutsideMask(bits, AllBits);

        return new DayOfMonthSet(bits);
    }

    /// <summary>
    /// Determines whether the set selects the specified day.
    /// </summary>
    /// <param name="day">The day to test.</param>
    /// <returns>
    /// <see langword="true" /> when the set selects <paramref name="day" />; <see langword="false" /> when it does not,
    /// including when <paramref name="day" /> is outside 1 to 31.
    /// </returns>
    public bool Contains(int day) =>
        (uint)(day - MinimumValue) <= MaximumValue - MinimumValue && (_bits & (1UL << (day - MinimumValue))) != 0;

    /// <summary>
    /// Returns an enumerator that yields the selected days in ascending order without allocating.
    /// </summary>
    /// <returns>An enumerator over the selected days.</returns>
    public Enumerator GetEnumerator() =>
        new(_bits);

    /// <inheritdoc />
    IEnumerator<int> IEnumerable<int>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Returns the set's bits, bit <c>n</c> selecting day <c>1 + n</c>.
    /// </summary>
    /// <returns>The bits, with nothing set above bit 30.</returns>
    public ulong ToUInt64() =>
        _bits;

    /// <summary>
    /// Returns a set that also selects the specified day.
    /// </summary>
    /// <param name="day">The day to add.</param>
    /// <returns>A set that selects <paramref name="day" /> and every day this set selects.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="day" /> is less than 1 or greater than 31.
    /// </exception>
    public DayOfMonthSet With(int day)
    {
        ThrowHelper.ThrowIfOutOfRange(day, MinimumValue, MaximumValue);

        return new DayOfMonthSet(_bits | (1UL << (day - MinimumValue)));
    }

    /// <summary>
    /// Returns a set that no longer selects the specified day.
    /// </summary>
    /// <param name="day">The day to remove.</param>
    /// <returns>A set that selects every day this set selects except <paramref name="day" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="day" /> is less than 1 or greater than 31.
    /// </exception>
    public DayOfMonthSet Without(int day)
    {
        ThrowHelper.ThrowIfOutOfRange(day, MinimumValue, MaximumValue);

        return new DayOfMonthSet(_bits & ~(1UL << (day - MinimumValue)));
    }
}
