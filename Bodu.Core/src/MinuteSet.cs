// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MinuteSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics;
using System.Numerics;

namespace Bodu;

/// <summary>
/// Represents an immutable set of minutes of the hour, 0 to 59.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="MinuteSet" /> holds one bit per minute, so it never allocates, and testing, adding or removing a
/// minute, or combining two sets with <c>|</c>, <c>&amp;</c>, <c>^</c> and <c>~</c>, is a single bitwise operation.
/// <see cref="Contains(int)" /> answers <see langword="false" /> for any value outside 0 to 59 rather than throwing,
/// which keeps a membership test against <see cref="DateTime.Minute" /> to a comparison and a bit test.
/// </para>
/// <para>
/// The text form lists the minutes in ascending order, separated by commas, each run of two or more consecutive minutes
/// written as an inclusive range, so <c>"0,15,30,45"</c> selects every quarter hour. The empty set is the empty string.
/// <see cref="Parse(string)" /> reads any list of values and ranges in that form, in any order, and
/// <see cref="ToString()" /> writes the canonical one. <see cref="ToString(string)" /> also writes every minute without
/// ranges, or a binary form with one character per minute, and <see cref="Parse(string)" /> and
/// <see cref="ParseExact(string, string)" /> read them back.
/// </para>
/// <para>
/// <see cref="ToUInt64" /> and <see cref="FromUInt64(ulong)" /> expose the bits directly: bit <c>n</c> selects minute
/// <c>n</c>.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var selected = new MinuteSet(0, 15, 30, 45);
/// MinuteSet parsed = MinuteSet.Parse("0,15,30,45");
///
/// bool due = selected.Contains(DateTime.Now.Minute);
/// bool same = selected == parsed; // true
///]]>
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq} ({Count} selected)")]
public readonly partial struct MinuteSet
    : IEquatable<MinuteSet>,
      IEqualityOperators<MinuteSet, MinuteSet, bool>,
      IBitwiseOperators<MinuteSet, MinuteSet, MinuteSet>,
      IParsable<MinuteSet>,
      IFormattable,
      IEnumerable<int>,
      ICalendarValueSet<MinuteSet, int>
{
    /// <summary>The smallest minute in the set's domain.</summary>
    private const int MinimumValue = 0;

    /// <summary>The largest minute in the set's domain.</summary>
    private const int MaximumValue = 59;

    /// <summary>The bits that select every minute in the domain.</summary>
    private const ulong AllBits = (1UL << 60) - 1;

    /// <summary>The selected minutes, bit <c>n</c> selecting minute <c>n</c>.</summary>
    private readonly ulong _bits;

    /// <summary>
    /// Initializes a new instance of the <see cref="MinuteSet" /> struct that selects the specified minutes.
    /// </summary>
    /// <param name="minutes">The minutes to select, or <see langword="null" /> for none.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a value in <paramref name="minutes" /> is less than 0 or greater than 59.
    /// </exception>
    /// <remarks>
    /// A minute may appear more than once, and the order is immaterial.
    /// </remarks>
    public MinuteSet(params int[]? minutes)
    {
        _bits = CalendarValueSet.FromValues(minutes, MinimumValue, MaximumValue, nameof(minutes));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MinuteSet" /> struct from bits already known to lie in the domain.
    /// </summary>
    /// <param name="bits">The bits, with nothing set above bit 59.</param>
    private MinuteSet(ulong bits)
    {
        _bits = bits;
    }

    /// <summary>
    /// Gets the set that selects no minute.
    /// </summary>
    public static MinuteSet Empty { get; }

    /// <summary>
    /// Gets the set that selects every minute from 0 to 59.
    /// </summary>
    public static MinuteSet All { get; } = new(AllBits);

    /// <summary>
    /// Gets the number of minutes the set selects.
    /// </summary>
    /// <value>A number from 0 to 60.</value>
    public int Count =>
        BitOperations.PopCount(_bits);

    /// <summary>
    /// Creates a set from its bits, bit <c>n</c> selecting minute <c>n</c>.
    /// </summary>
    /// <param name="bits">The bits.</param>
    /// <returns>The set that <paramref name="bits" /> describe.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="bits" /> sets a bit above bit 59, which selects no minute.
    /// </exception>
    public static MinuteSet FromUInt64(ulong bits)
    {
        ThrowHelper.ThrowIfBitsOutsideMask(bits, AllBits);

        return new MinuteSet(bits);
    }

    /// <summary>
    /// Determines whether the set selects the specified minute.
    /// </summary>
    /// <param name="minute">The minute to test.</param>
    /// <returns>
    /// <see langword="true" /> when the set selects <paramref name="minute" />; <see langword="false" /> when it does
    /// not, including when <paramref name="minute" /> is outside 0 to 59.
    /// </returns>
    public bool Contains(int minute) =>
        (uint)(minute - MinimumValue) <= MaximumValue - MinimumValue && (_bits & (1UL << (minute - MinimumValue))) != 0;

    /// <summary>
    /// Returns an enumerator that yields the selected minutes in ascending order without allocating.
    /// </summary>
    /// <returns>An enumerator over the selected minutes.</returns>
    public Enumerator GetEnumerator() =>
        new(_bits);

    /// <inheritdoc />
    IEnumerator<int> IEnumerable<int>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Returns the set's bits, bit <c>n</c> selecting minute <c>n</c>.
    /// </summary>
    /// <returns>The bits, with nothing set above bit 59.</returns>
    public ulong ToUInt64() =>
        _bits;

    /// <summary>
    /// Returns a set that also selects the specified minute.
    /// </summary>
    /// <param name="minute">The minute to add.</param>
    /// <returns>A set that selects <paramref name="minute" /> and every minute this set selects.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minute" /> is less than 0 or greater than 59.
    /// </exception>
    public MinuteSet With(int minute)
    {
        ThrowHelper.ThrowIfOutOfRange(minute, MinimumValue, MaximumValue);

        return new MinuteSet(_bits | (1UL << (minute - MinimumValue)));
    }

    /// <summary>
    /// Returns a set that no longer selects the specified minute.
    /// </summary>
    /// <param name="minute">The minute to remove.</param>
    /// <returns>A set that selects every minute this set selects except <paramref name="minute" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minute" /> is less than 0 or greater than 59.
    /// </exception>
    public MinuteSet Without(int minute)
    {
        ThrowHelper.ThrowIfOutOfRange(minute, MinimumValue, MaximumValue);

        return new MinuteSet(_bits & ~(1UL << (minute - MinimumValue)));
    }
}
