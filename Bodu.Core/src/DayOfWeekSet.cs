// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics;
using System.Numerics;

namespace Bodu;

/// <summary>
/// Represents an immutable set of days of the week.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="DayOfWeekSet" /> holds one bit per day, so it never allocates, and testing, adding or removing a day,
/// or combining two sets with <c>|</c>, <c>&amp;</c>, <c>^</c> and <c>~</c>, is a single bitwise operation.
/// <see cref="Contains(DayOfWeek)" /> answers <see langword="false" /> for a value outside the seven days rather than
/// throwing, which keeps a membership test against <see cref="DateTime.DayOfWeek" /> to a comparison and a bit test.
/// </para>
/// <para>
/// The text form is a seven-character mask with one character per day, the day's letter when it is selected and
/// <c>_</c> when it is not, Sunday first: <c>"_MTWTF_"</c> selects Monday to Friday. <see cref="ToString(string)" />
/// can also write the mask Monday first, with another placeholder, or in binary (<c>"0111110"</c>), and
/// <see cref="Parse(string)" /> reads any of those forms.
/// </para>
/// <para>
/// <see cref="ToUInt64" /> and <see cref="FromUInt64(ulong)" /> expose the bits directly: bit <c>n</c> selects
/// <c>(DayOfWeek)n</c>, so Sunday is bit 0 and Saturday bit 6.
/// </para>
/// <para>
/// The presets name the common working weeks: <see cref="Weekdays" />, <see cref="Weekend" />, and one for each named
/// <see cref="WorkingDaysOfWeek" /> value.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// DayOfWeekSet fourDayWeek = DayOfWeekSet.Weekdays.Without(DayOfWeek.Friday);
///
/// bool working = fourDayWeek.Contains(DateTime.Today.DayOfWeek);
/// string text = fourDayWeek.ToString(); // "_MTWT__"
/// bool same = DayOfWeekSet.Parse(text) == fourDayWeek; // true
///]]>
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq} ({Count} selected)")]
public readonly partial struct DayOfWeekSet
    : IEquatable<DayOfWeekSet>,
      IEqualityOperators<DayOfWeekSet, DayOfWeekSet, bool>,
      IBitwiseOperators<DayOfWeekSet, DayOfWeekSet, DayOfWeekSet>,
      IParsable<DayOfWeekSet>,
      IFormattable,
      IEnumerable<DayOfWeek>
{
    /// <summary>The bits that select every day of the week.</summary>
    private const ulong AllBits = (1UL << 7) - 1;

    /// <summary>The selected days, bit <c>n</c> selecting <c>(DayOfWeek)n</c>.</summary>
    private readonly ulong _bits;

    /// <summary>
    /// Initializes a new instance of the <see cref="DayOfWeekSet" /> struct that selects the specified days.
    /// </summary>
    /// <param name="days">The days to select, or <see langword="null" /> for none.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a value in <paramref name="days" /> is not one of the seven <see cref="DayOfWeek" /> values.
    /// </exception>
    /// <remarks>
    /// A day may appear more than once, and the order is immaterial.
    /// </remarks>
    public DayOfWeekSet(params DayOfWeek[]? days)
    {
        ulong bits = 0;
        if (days is not null)
        {
            foreach (DayOfWeek day in days)
            {
                ThrowHelper.ThrowIfOutOfRange((int)day, (int)DayOfWeek.Sunday, (int)DayOfWeek.Saturday, paramName: nameof(days));
                bits |= 1UL << (int)day;
            }
        }

        _bits = bits;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DayOfWeekSet" /> struct from bits already known to lie in the week.
    /// </summary>
    /// <param name="bits">The bits, with nothing set above bit 6.</param>
    private DayOfWeekSet(ulong bits)
    {
        _bits = bits;
    }

    /// <summary>
    /// Gets the set that selects no day.
    /// </summary>
    public static DayOfWeekSet Empty { get; }

    /// <summary>
    /// Gets the set that selects every day of the week.
    /// </summary>
    /// <remarks>
    /// Corresponds to <see cref="WorkingDaysOfWeek.AllDays" />.
    /// </remarks>
    public static DayOfWeekSet All { get; } = new(AllBits);

    /// <summary>
    /// Gets the number of days the set selects.
    /// </summary>
    /// <value>A number from 0 to 7.</value>
    public int Count =>
        BitOperations.PopCount(_bits);

    /// <summary>
    /// Creates a set from its bits, bit <c>n</c> selecting <c>(DayOfWeek)n</c>.
    /// </summary>
    /// <param name="bits">The bits.</param>
    /// <returns>The set that <paramref name="bits" /> describe.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="bits" /> sets a bit above bit 6, which selects no day.
    /// </exception>
    public static DayOfWeekSet FromUInt64(ulong bits)
    {
        ThrowHelper.ThrowIfBitsOutsideMask(bits, AllBits);

        return new DayOfWeekSet(bits);
    }

    /// <summary>
    /// Determines whether the set selects the specified day.
    /// </summary>
    /// <param name="day">The day to test.</param>
    /// <returns>
    /// <see langword="true" /> when the set selects <paramref name="day" />; <see langword="false" /> when it does not,
    /// including when <paramref name="day" /> is not one of the seven <see cref="DayOfWeek" /> values.
    /// </returns>
    public bool Contains(DayOfWeek day) =>
        (uint)day <= (uint)DayOfWeek.Saturday && (_bits & (1UL << (int)day)) != 0;

    /// <summary>
    /// Returns an enumerator that yields the selected days in <see cref="DayOfWeek" /> order, Sunday first, without
    /// allocating.
    /// </summary>
    /// <returns>An enumerator over the selected days.</returns>
    public Enumerator GetEnumerator() =>
        new(_bits);

    /// <inheritdoc />
    IEnumerator<DayOfWeek> IEnumerable<DayOfWeek>.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Returns the set's bits, bit <c>n</c> selecting <c>(DayOfWeek)n</c>.
    /// </summary>
    /// <returns>The bits, with nothing set above bit 6.</returns>
    public ulong ToUInt64() =>
        _bits;

    /// <summary>
    /// Returns a set that also selects the specified day.
    /// </summary>
    /// <param name="day">The day to add.</param>
    /// <returns>A set that selects <paramref name="day" /> and every day this set selects.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="day" /> is not one of the seven <see cref="DayOfWeek" /> values.
    /// </exception>
    public DayOfWeekSet With(DayOfWeek day)
    {
        ThrowHelper.ThrowIfOutOfRange((int)day, (int)DayOfWeek.Sunday, (int)DayOfWeek.Saturday, paramName: nameof(day));

        return new DayOfWeekSet(_bits | (1UL << (int)day));
    }

    /// <summary>
    /// Returns a set that no longer selects the specified day.
    /// </summary>
    /// <param name="day">The day to remove.</param>
    /// <returns>A set that selects every day this set selects except <paramref name="day" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="day" /> is not one of the seven <see cref="DayOfWeek" /> values.
    /// </exception>
    public DayOfWeekSet Without(DayOfWeek day)
    {
        ThrowHelper.ThrowIfOutOfRange((int)day, (int)DayOfWeek.Sunday, (int)DayOfWeek.Saturday, paramName: nameof(day));

        return new DayOfWeekSet(_bits & ~(1UL << (int)day));
    }
}
