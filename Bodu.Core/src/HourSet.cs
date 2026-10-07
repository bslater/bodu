// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HourSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics;
using System.Numerics;

namespace Bodu;

/// <summary>
/// Represents an immutable set of hours of the day, 0 to 23.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="HourSet" /> holds one bit per hour, so it never allocates, and testing, adding or removing an hour, or
/// combining two sets with <c>|</c>, <c>&amp;</c>, <c>^</c> and <c>~</c>, is a single bitwise operation.
/// <see cref="Contains(int)" /> answers <see langword="false" /> for any value outside 0 to 23 rather than throwing,
/// which keeps a membership test against <see cref="DateTime.Hour" /> to a comparison and a bit test.
/// </para>
/// <para>
/// The text form lists the hours in ascending order, separated by commas, each run of two or more consecutive hours
/// written as an inclusive range, so <c>"9-17"</c> selects nine to five. The empty set is the empty string.
/// <see cref="Parse(string)" /> reads any list of values and ranges in that form, in any order, and
/// <see cref="ToString()" /> writes the canonical one. <see cref="ToString(string)" /> also writes every hour without
/// ranges, or a binary form with one character per hour, and <see cref="Parse(string)" /> and
/// <see cref="ParseExact(string, string)" /> read them back.
/// </para>
/// <para>
/// <see cref="ToUInt64" /> and <see cref="FromUInt64(ulong)" /> expose the bits directly: bit <c>n</c> selects hour
/// <c>n</c>.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var selected = new HourSet(9, 10, 11, 12, 13, 14, 15, 16, 17);
/// HourSet parsed = HourSet.Parse("9-17");
///
/// bool due = selected.Contains(DateTime.Now.Hour);
/// bool same = selected == parsed; // true
///]]>
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq} ({Count} selected)")]
public readonly partial struct HourSet
    : IEquatable<HourSet>,
      IEqualityOperators<HourSet, HourSet, bool>,
      IBitwiseOperators<HourSet, HourSet, HourSet>,
      IParsable<HourSet>,
      IFormattable,
      IEnumerable<int>,
      ICalendarValueSet<HourSet, int>
{
    /// <summary>The smallest hour in the set's domain.</summary>
    private const int MinimumValue = 0;

    /// <summary>The largest hour in the set's domain.</summary>
    private const int MaximumValue = 23;

    /// <summary>The bits that select every hour in the domain.</summary>
    private const ulong AllBits = (1UL << 24) - 1;

    /// <summary>The selected hours, bit <c>n</c> selecting hour <c>n</c>.</summary>
    private readonly ulong _bits;

    /// <summary>
    /// Initializes a new instance of the <see cref="HourSet" /> struct that selects the specified hours.
    /// </summary>
    /// <param name="hours">The hours to select, or <see langword="null" /> for none.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a value in <paramref name="hours" /> is less than 0 or greater than 23.
    /// </exception>
    /// <remarks>
    /// An hour may appear more than once, and the order is immaterial.
    /// </remarks>
    public HourSet(params int[]? hours)
    {
        _bits = CalendarValueSet.FromValues(hours, MinimumValue, MaximumValue, nameof(hours));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HourSet" /> struct from bits already known to lie in the domain.
    /// </summary>
    /// <param name="bits">The bits, with nothing set above bit 23.</param>
    private HourSet(ulong bits)
    {
        _bits = bits;
    }

    /// <summary>
    /// Gets the set that selects no hour.
    /// </summary>
    public static HourSet Empty { get; }

    /// <summary>
    /// Gets the set that selects every hour from 0 to 23.
    /// </summary>
    public static HourSet All { get; } = new(AllBits);

    /// <summary>
    /// Gets the number of hours the set selects.
    /// </summary>
    /// <value>A number from 0 to 24.</value>
    public int Count =>
        BitOperations.PopCount(_bits);

    /// <summary>
    /// Creates a set from its bits, bit <c>n</c> selecting hour <c>n</c>.
    /// </summary>
    /// <param name="bits">The bits.</param>
    /// <returns>The set that <paramref name="bits" /> describe.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="bits" /> sets a bit above bit 23, which selects no hour.
    /// </exception>
    public static HourSet FromUInt64(ulong bits)
    {
        ThrowHelper.ThrowIfBitsOutsideMask(bits, AllBits);

        return new HourSet(bits);
    }

    /// <summary>
    /// Determines whether the set selects the specified hour.
    /// </summary>
    /// <param name="hour">The hour to test.</param>
    /// <returns>
    /// <see langword="true" /> when the set selects <paramref name="hour" />; <see langword="false" /> when it does
    /// not, including when <paramref name="hour" /> is outside 0 to 23.
    /// </returns>
    public bool Contains(int hour) =>
        (uint)(hour - MinimumValue) <= MaximumValue - MinimumValue && (_bits & (1UL << (hour - MinimumValue))) != 0;

    /// <summary>
    /// Returns an enumerator that yields the selected hours in ascending order without allocating.
    /// </summary>
    /// <returns>An enumerator over the selected hours.</returns>
    public Enumerator GetEnumerator() =>
        new(_bits);

    /// <inheritdoc />
    IEnumerator<int> IEnumerable<int>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Returns the set's bits, bit <c>n</c> selecting hour <c>n</c>.
    /// </summary>
    /// <returns>The bits, with nothing set above bit 23.</returns>
    public ulong ToUInt64() =>
        _bits;

    /// <summary>
    /// Returns a set that also selects the specified hour.
    /// </summary>
    /// <param name="hour">The hour to add.</param>
    /// <returns>A set that selects <paramref name="hour" /> and every hour this set selects.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="hour" /> is less than 0 or greater than 23.
    /// </exception>
    public HourSet With(int hour)
    {
        ThrowHelper.ThrowIfOutOfRange(hour, MinimumValue, MaximumValue);

        return new HourSet(_bits | (1UL << (hour - MinimumValue)));
    }

    /// <summary>
    /// Returns a set that no longer selects the specified hour.
    /// </summary>
    /// <param name="hour">The hour to remove.</param>
    /// <returns>A set that selects every hour this set selects except <paramref name="hour" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="hour" /> is less than 0 or greater than 23.
    /// </exception>
    public HourSet Without(int hour)
    {
        ThrowHelper.ThrowIfOutOfRange(hour, MinimumValue, MaximumValue);

        return new HourSet(_bits & ~(1UL << (hour - MinimumValue)));
    }
}
