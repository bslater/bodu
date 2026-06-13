// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPattern.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics;
using System.Numerics;

namespace Bodu;

/// <summary>
/// Represents an immutable set of selected days in a standard seven-day week.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="WeekPattern" /> provides a compact bitmask representation of selected days, supporting non-destructive
/// composition, parsing, formatting, comparison, and enumeration.
/// </para>
/// <para>
/// Because <see cref="WeekPattern" /> is a value type, all operations that change the selection return a new instance
/// rather than modifying the receiver. Use <see cref="With" /> and <see cref="Without" /> to build patterns
/// incrementally, and the bitwise operators to combine or intersect them.
/// </para>
/// <code language="csharp">
///<![CDATA[
/// Build a pattern for Monday, Wednesday, and Friday
/// var pattern = WeekPattern.Empty
///     .With(DayOfWeek.Monday)
///     .With(DayOfWeek.Wednesday)
///     .With(DayOfWeek.Friday);
///
/// Check membership
/// bool hasFriday = pattern.Contains(DayOfWeek.Friday); // true
///
/// Count selected days
/// int count = pattern.Count; // 3
///
/// Enumerate selected days
/// foreach (DayOfWeek day in pattern)
///     Console.WriteLine(day);
///]]>
/// </code>
/// </remarks>
[Serializable]
[DebuggerDisplay("{ToString(),nq} ({Count} selected)")]
public readonly partial struct WeekPattern
    : IEnumerable<DayOfWeek>
{
#pragma warning disable IDE1006

    /// <summary>
    /// Represents a <see cref="WeekPattern" /> with no days selected.
    /// </summary>
    public static readonly WeekPattern Empty;

    /// <summary>
    /// Represents a <see cref="WeekPattern" /> with Monday through Friday selected.
    /// </summary>
    public static readonly WeekPattern Weekdays = new(
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);

    /// <summary>
    /// Represents a <see cref="WeekPattern" /> with Saturday and Sunday selected.
    /// </summary>
    public static readonly WeekPattern Weekend = new(DayOfWeek.Saturday, DayOfWeek.Sunday);

#pragma warning restore IDE1006

    private const int MaxValue = 0b1111111;
    private const int MinValue = 0b0000000;
    private const byte ShiftValue = 0x01;

#pragma warning disable IDE1006
    private static readonly char[] WeekdaySymbols = ['S', 'M', 'T', 'W', 'T', 'F', 'S'];
#pragma warning restore IDE1006

    private readonly byte _selectedDays;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeekPattern" /> struct with the specified selected days.
    /// </summary>
    /// <param name="daysOfWeek">An array of <see cref="DayOfWeek" /> values to mark as selected.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if any value in <paramref name="daysOfWeek" /> is outside the valid <see cref="DayOfWeek" /> range.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
    /// Console.WriteLine(pattern.Count); // 3
    ///]]>
    /// </code>
    /// </example>
    public WeekPattern(params DayOfWeek[] daysOfWeek)
    {
        _selectedDays = 0;
        foreach (DayOfWeek day in daysOfWeek ?? [])
        {
            ThrowHelper.ThrowIfOutOfRange((int)day, 0, 6);
            _selectedDays |= (byte)(ShiftValue << (6 - (int)day));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WeekPattern" /> struct by parsing the specified string.
    /// </summary>
    /// <param name="input">The input string representing selected days. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="input" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown if <paramref name="input" /> is not exactly seven characters long or contains invalid characters.
    /// </exception>
    /// <remarks>
    /// This constructor behaves identically to <see cref="Parse(string)" />, automatically inferring the format from
    /// the input string. Use <see cref="ParseExact(string, string)" /> when the format must be specified explicitly.
    /// </remarks>
    public WeekPattern(string input)
        : this(Parse(input))
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="WeekPattern" /> struct from a raw day bitmask.
    /// </summary>
    /// <param name="value">The day bitmask, where each of the seven low-order bits selects a day of the week.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="value" /> is less than <see cref="MinValue" /> or greater than <see cref="MaxValue" />.
    /// </exception>
    private WeekPattern(int value)
    {
        ThrowHelper.ThrowIfOutOfRange(value, MinValue, MaxValue);
        _selectedDays = (byte)value;
    }

    /// <summary>
    /// Gets the number of days currently selected.
    /// </summary>
    /// <value>An <see cref="int" /> in the range [0, 7] representing the total number of selected days.</value>
    public readonly int Count =>
        BitOperations.PopCount(_selectedDays);

    /// <summary>
    /// Gets whether the specified <see cref="DayOfWeek" /> is selected.
    /// </summary>
    /// <param name="day">The day to query.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="day" /> is selected; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="day" /> is outside the valid <see cref="DayOfWeek" /> range.
    /// </exception>
    public readonly bool this[DayOfWeek day]
    {
        get
        {
            ThrowHelper.ThrowIfOutOfRange((int)day, 0, 6);
            return (_selectedDays & (ShiftValue << (6 - (int)day))) != 0;
        }
    }

    /// <summary>
    /// Determines whether the specified <see cref="DayOfWeek" /> is selected in this pattern.
    /// </summary>
    /// <param name="day">The day to test. Must be a valid <see cref="DayOfWeek" /> value.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="day" /> is selected; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="day" /> is outside the valid <see cref="DayOfWeek" /> range.
    /// </exception>
    public bool Contains(DayOfWeek day) => this[day];

    /// <summary>
    /// Returns a new <see cref="WeekPattern" /> that includes the specified day in addition to those already selected.
    /// If <paramref name="day" /> is already selected the returned instance is equal to the current one.
    /// </summary>
    /// <param name="day">The <see cref="DayOfWeek" /> to add.</param>
    /// <returns>
    /// A new <see cref="WeekPattern" /> with <paramref name="day" /> selected alongside all previously selected days.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="day" /> is outside the valid <see cref="DayOfWeek" /> range.
    /// </exception>
    public readonly WeekPattern With(DayOfWeek day)
    {
        ThrowHelper.ThrowIfOutOfRange((int)day, 0, 6);
        return new WeekPattern(_selectedDays | (byte)(ShiftValue << (6 - (int)day)));
    }

    /// <summary>
    /// Returns a new <see cref="WeekPattern" /> that excludes the specified day. If <paramref name="day" /> is not
    /// selected the returned instance is equal to the current one.
    /// </summary>
    /// <param name="day">The <see cref="DayOfWeek" /> to remove.</param>
    /// <returns>
    /// A new <see cref="WeekPattern" /> with <paramref name="day" /> unselected and all other previously selected days
    /// retained.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="day" /> is outside the valid <see cref="DayOfWeek" /> range.
    /// </exception>
    public readonly WeekPattern Without(DayOfWeek day)
    {
        ThrowHelper.ThrowIfOutOfRange((int)day, 0, 6);
        return new WeekPattern(_selectedDays & (byte)~(ShiftValue << (6 - (int)day)));
    }

    /// <summary>
    /// Returns a <see cref="byte" /> representation of the selected days.
    /// </summary>
    /// <returns>A <see cref="byte" /> whose bits correspond to the selected days in Sunday-first order.</returns>
    public readonly byte ToByte() => _selectedDays;

    /// <summary>
    /// Returns an <see cref="int" /> representation of the selected days, for contexts where an integer type is
    /// required.
    /// </summary>
    /// <returns>An <see cref="int" /> whose value equals the underlying bitmask.</returns>
    public readonly int ToInt32() => _selectedDays;

    /// <summary>
    /// Creates a <see cref="WeekPattern" /> from the specified bitmask byte value.
    /// </summary>
    /// <param name="value">
    /// A <see cref="byte" /> in the range [0, 127] where each bit represents a day of the week in Sunday-first order.
    /// </param>
    /// <returns>A <see cref="WeekPattern" /> corresponding to the supplied bitmask.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="value" /> exceeds 127.</exception>
    public static WeekPattern FromByte(byte value) => new((int)value);

    /// <summary>
    /// Returns a struct enumerator that yields each selected <see cref="DayOfWeek" /> in Sunday-first order without
    /// allocating.
    /// </summary>
    /// <returns>
    /// A <see cref="Enumerator" /> that iterates the selected days. Returning the struct directly avoids the heap
    /// allocation that a yield-state-machine enumerator would incur, and the C# <c>foreach</c> pattern binds to it via
    /// pattern matching without going through <see cref="IEnumerable{T}" />.
    /// </returns>
    public Enumerator GetEnumerator() => new(_selectedDays);

    /// <inheritdoc />
    IEnumerator<DayOfWeek> IEnumerable<DayOfWeek>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
