// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics;
using System.Numerics;

namespace Bodu;

/// <summary>
/// Represents an immutable set of months of the year, 1 (January) to 12 (December).
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="MonthSet" /> holds one bit per month, so it never allocates, and testing, adding or removing a month,
/// or combining two sets with <c>|</c>, <c>&amp;</c>, <c>^</c> and <c>~</c>, is a single bitwise operation.
/// <see cref="Contains(int)" /> answers <see langword="false" /> for any value outside 1 to 12 rather than throwing,
/// which keeps a membership test against <see cref="DateTime.Month" /> to a comparison and a bit test.
/// </para>
/// <para>
/// The text form lists the months in ascending order, separated by commas, each run of two or more consecutive months
/// written as an inclusive range, so <c>"1,4,7,10"</c> selects the first month of each quarter. The empty set is the
/// empty string. <see cref="Parse(string)" /> reads any list of values and ranges in that form, in any order, and
/// <see cref="ToString()" /> writes the canonical one. <see cref="ToString(string)" /> also writes every month without
/// ranges, a binary form with one character per month, or a letter mask of the months' initials, such as
/// <c>"JFM________D"</c>, and <see cref="Parse(string)" /> and <see cref="ParseExact(string, string)" /> read them
/// back. <see cref="TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider)" /> writes the same text into a
/// span of characters, its other overload into a span of UTF-8 bytes, and the span and UTF-8 overloads of <c>Parse</c>,
/// <c>TryParse</c>, <c>ParseExact</c> and <c>TryParseExact</c> read it.
/// </para>
/// <para>
/// <see cref="ToUInt64" /> and <see cref="FromUInt64(ulong)" /> expose the bits directly: bit <c>n</c> selects month
/// <c>1 + n</c>.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var selected = new MonthSet(1, 4, 7, 10);
/// MonthSet parsed = MonthSet.Parse("1,4,7,10");
///
/// bool due = selected.Contains(DateTime.Today.Month);
/// bool same = selected == parsed; // true
///]]>
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq} ({Count} selected)")]
public readonly partial struct MonthSet
    : IEquatable<MonthSet>,
      IEqualityOperators<MonthSet, MonthSet, bool>,
      IBitwiseOperators<MonthSet, MonthSet, MonthSet>,
      IParsable<MonthSet>,
      ISpanParsable<MonthSet>,
      IUtf8SpanParsable<MonthSet>,
      IFormattable,
      ISpanFormattable,
      IUtf8SpanFormattable,
      IEnumerable<int>,
      ICalendarValueSet<MonthSet, int>
{
    /// <summary>The smallest month in the set's domain.</summary>
    private const int MinimumValue = 1;

    /// <summary>The largest month in the set's domain.</summary>
    private const int MaximumValue = 12;

    /// <summary>The bits that select every month in the domain.</summary>
    private const ulong AllBits = (1UL << 12) - 1;

    /// <summary>The selected months, bit <c>n</c> selecting month <c>1 + n</c>.</summary>
    private readonly ulong _bits;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonthSet" /> struct that selects the specified months.
    /// </summary>
    /// <param name="months">The months to select, or <see langword="null" /> for none.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a value in <paramref name="months" /> is less than 1 or greater than 12.
    /// </exception>
    /// <remarks>
    /// A month may appear more than once, and the order is immaterial.
    /// </remarks>
    public MonthSet(params int[]? months)
    {
        _bits = CalendarValueSet.FromValues(months, MinimumValue, MaximumValue, nameof(months));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MonthSet" /> struct from bits already known to lie in the domain.
    /// </summary>
    /// <param name="bits">The bits, with nothing set above bit 11.</param>
    private MonthSet(ulong bits)
    {
        _bits = bits;
    }

    /// <summary>
    /// Gets the set that selects no month.
    /// </summary>
    public static MonthSet Empty { get; }

    /// <summary>
    /// Gets the set that selects every month from 1 to 12.
    /// </summary>
    public static MonthSet All { get; } = new(AllBits);

    /// <summary>
    /// Gets the number of months the set selects.
    /// </summary>
    /// <value>A number from 0 to 12.</value>
    public int Count =>
        BitOperations.PopCount(_bits);

    /// <summary>
    /// Creates a set from its bits, bit <c>n</c> selecting month <c>1 + n</c>.
    /// </summary>
    /// <param name="bits">The bits.</param>
    /// <returns>The set that <paramref name="bits" /> describe.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="bits" /> sets a bit above bit 11, which selects no month.
    /// </exception>
    public static MonthSet FromUInt64(ulong bits)
    {
        ThrowHelper.ThrowIfBitsOutsideMask(bits, AllBits);

        return new MonthSet(bits);
    }

    /// <summary>
    /// Determines whether the set selects the specified month.
    /// </summary>
    /// <param name="month">The month to test.</param>
    /// <returns>
    /// <see langword="true" /> when the set selects <paramref name="month" />; <see langword="false" /> when it does
    /// not, including when <paramref name="month" /> is outside 1 to 12.
    /// </returns>
    public bool Contains(int month) =>
        (uint)(month - MinimumValue) <= MaximumValue - MinimumValue && (_bits & (1UL << (month - MinimumValue))) != 0;

    /// <summary>
    /// Returns an enumerator that yields the selected months in ascending order without allocating.
    /// </summary>
    /// <returns>An enumerator over the selected months.</returns>
    public Enumerator GetEnumerator() =>
        new(_bits);

    /// <inheritdoc />
    IEnumerator<int> IEnumerable<int>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Returns the set's bits, bit <c>n</c> selecting month <c>1 + n</c>.
    /// </summary>
    /// <returns>The bits, with nothing set above bit 11.</returns>
    public ulong ToUInt64() =>
        _bits;

    /// <summary>
    /// Returns a set that also selects the specified month.
    /// </summary>
    /// <param name="month">The month to add.</param>
    /// <returns>A set that selects <paramref name="month" /> and every month this set selects.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="month" /> is less than 1 or greater than 12.
    /// </exception>
    public MonthSet With(int month)
    {
        ThrowHelper.ThrowIfOutOfRange(month, MinimumValue, MaximumValue);

        return new MonthSet(_bits | (1UL << (month - MinimumValue)));
    }

    /// <summary>
    /// Returns a set that no longer selects the specified month.
    /// </summary>
    /// <param name="month">The month to remove.</param>
    /// <returns>A set that selects every month this set selects except <paramref name="month" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="month" /> is less than 1 or greater than 12.
    /// </exception>
    public MonthSet Without(int month)
    {
        ThrowHelper.ThrowIfOutOfRange(month, MinimumValue, MaximumValue);

        return new MonthSet(_bits & ~(1UL << (month - MinimumValue)));
    }
}
