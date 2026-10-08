// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SecondSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics;
using System.Numerics;

namespace Bodu;

/// <summary>
/// Represents an immutable set of seconds of the minute, 0 to 59.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="SecondSet" /> holds one bit per second, so it never allocates, and testing, adding or removing a
/// second, or combining two sets with <c>|</c>, <c>&amp;</c>, <c>^</c> and <c>~</c>, is a single bitwise operation.
/// <see cref="Contains(int)" /> answers <see langword="false" /> for any value outside 0 to 59 rather than throwing,
/// which keeps a membership test against <see cref="DateTime.Second" /> to a comparison and a bit test.
/// </para>
/// <para>
/// A leap second (60) is not a value: <see cref="DateTime" /> cannot represent one.
/// </para>
/// <para>
/// The text form lists the seconds in ascending order, separated by commas, each run of two or more consecutive seconds
/// written as an inclusive range, so <c>"0,30"</c> selects on the minute and the half minute. The empty set is the
/// empty string. <see cref="Parse(string)" /> reads any list of values and ranges in that form, in any order, and
/// <see cref="ToString()" /> writes the canonical one. <see cref="ToString(string)" /> also writes every second without
/// ranges, or a binary form with one character per second, and <see cref="Parse(string)" /> and
/// <see cref="ParseExact(string, string)" /> read them back.
/// <see cref="TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider)" /> writes the same text into a span
/// of characters, its other overload into a span of UTF-8 bytes, and the span and UTF-8 overloads of <c>Parse</c>,
/// <c>TryParse</c>, <c>ParseExact</c> and <c>TryParseExact</c> read it.
/// </para>
/// <para>
/// <see cref="ToUInt64" /> and <see cref="FromUInt64(ulong)" /> expose the bits directly: bit <c>n</c> selects second
/// <c>n</c>.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var selected = new SecondSet(0, 30);
/// SecondSet parsed = SecondSet.Parse("0,30");
///
/// bool due = selected.Contains(DateTime.Now.Second);
/// bool same = selected == parsed; // true
///]]>
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq} ({Count} selected)")]
public readonly partial struct SecondSet
    : IEquatable<SecondSet>,
      IEqualityOperators<SecondSet, SecondSet, bool>,
      IBitwiseOperators<SecondSet, SecondSet, SecondSet>,
      IParsable<SecondSet>,
      ISpanParsable<SecondSet>,
      IUtf8SpanParsable<SecondSet>,
      IFormattable,
      ISpanFormattable,
      IUtf8SpanFormattable,
      IEnumerable<int>,
      ICalendarValueSet<SecondSet, int>
{
    /// <summary>The smallest second in the set's domain.</summary>
    private const int MinimumValue = 0;

    /// <summary>The largest second in the set's domain.</summary>
    private const int MaximumValue = 59;

    /// <summary>The bits that select every second in the domain.</summary>
    private const ulong AllBits = (1UL << 60) - 1;

    /// <summary>The selected seconds, bit <c>n</c> selecting second <c>n</c>.</summary>
    private readonly ulong _bits;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecondSet" /> struct that selects the specified seconds.
    /// </summary>
    /// <param name="seconds">The seconds to select, or <see langword="null" /> for none.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a value in <paramref name="seconds" /> is less than 0 or greater than 59.
    /// </exception>
    /// <remarks>
    /// A second may appear more than once, and the order is immaterial.
    /// </remarks>
    public SecondSet(params int[]? seconds)
    {
        _bits = CalendarValueSet.FromValues(seconds, MinimumValue, MaximumValue, nameof(seconds));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecondSet" /> struct from bits already known to lie in the domain.
    /// </summary>
    /// <param name="bits">The bits, with nothing set above bit 59.</param>
    private SecondSet(ulong bits)
    {
        _bits = bits;
    }

    /// <summary>
    /// Gets the set that selects no second.
    /// </summary>
    public static SecondSet Empty { get; }

    /// <summary>
    /// Gets the set that selects every second from 0 to 59.
    /// </summary>
    public static SecondSet All { get; } = new(AllBits);

    /// <summary>
    /// Gets the number of seconds the set selects.
    /// </summary>
    /// <value>A number from 0 to 60.</value>
    public int Count =>
        BitOperations.PopCount(_bits);

    /// <summary>
    /// Creates a set from its bits, bit <c>n</c> selecting second <c>n</c>.
    /// </summary>
    /// <param name="bits">The bits.</param>
    /// <returns>The set that <paramref name="bits" /> describe.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="bits" /> sets a bit above bit 59, which selects no second.
    /// </exception>
    public static SecondSet FromUInt64(ulong bits)
    {
        ThrowHelper.ThrowIfBitsOutsideMask(bits, AllBits);

        return new SecondSet(bits);
    }

    /// <summary>
    /// Determines whether the set selects the specified second.
    /// </summary>
    /// <param name="second">The second to test.</param>
    /// <returns>
    /// <see langword="true" /> when the set selects <paramref name="second" />; <see langword="false" /> when it does
    /// not, including when <paramref name="second" /> is outside 0 to 59.
    /// </returns>
    public bool Contains(int second) =>
        (uint)(second - MinimumValue) <= MaximumValue - MinimumValue && (_bits & (1UL << (second - MinimumValue))) != 0;

    /// <summary>
    /// Returns an enumerator that yields the selected seconds in ascending order without allocating.
    /// </summary>
    /// <returns>An enumerator over the selected seconds.</returns>
    public Enumerator GetEnumerator() =>
        new(_bits);

    /// <inheritdoc />
    IEnumerator<int> IEnumerable<int>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Returns the set's bits, bit <c>n</c> selecting second <c>n</c>.
    /// </summary>
    /// <returns>The bits, with nothing set above bit 59.</returns>
    public ulong ToUInt64() =>
        _bits;

    /// <summary>
    /// Returns a set that also selects the specified second.
    /// </summary>
    /// <param name="second">The second to add.</param>
    /// <returns>A set that selects <paramref name="second" /> and every second this set selects.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="second" /> is less than 0 or greater than 59.
    /// </exception>
    public SecondSet With(int second)
    {
        ThrowHelper.ThrowIfOutOfRange(second, MinimumValue, MaximumValue);

        return new SecondSet(_bits | (1UL << (second - MinimumValue)));
    }

    /// <summary>
    /// Returns a set that no longer selects the specified second.
    /// </summary>
    /// <param name="second">The second to remove.</param>
    /// <returns>A set that selects every second this set selects except <paramref name="second" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="second" /> is less than 0 or greater than 59.
    /// </exception>
    public SecondSet Without(int second)
    {
        ThrowHelper.ThrowIfOutOfRange(second, MinimumValue, MaximumValue);

        return new SecondSet(_bits & ~(1UL << (second - MinimumValue)));
    }
}
