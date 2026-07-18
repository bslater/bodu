// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.FirstDateOfWeekInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the first occurrence of the specified
    /// <see cref="DayOfWeek" /> within the same calendar month and year as the specified <paramref name="date" />.
    /// </summary>
    /// <param name="date">The date value whose month and year are used to determine the result.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate within the month. For example, <see cref="DayOfWeek.Monday" /> returns
    /// the first Monday.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the first occurrence of <paramref name="dayOfWeek" /> within the same
    /// calendar month and year as <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The search begins on the first day of the month and proceeds forward to locate the first matching weekday.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    public static DateOnly FirstDateOfWeekInMonth(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        int baseDayNumber = DateTimeExtensions.GetDayNumberUnchecked(date.Year, date.Month, 1);
        return DateOnly.FromDayNumber(baseDayNumber + (((int)dayOfWeek - (int)GetDayOfWeekFromDayNumber(baseDayNumber) + 7) % 7));
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the first occurrence of the specified
    /// <see cref="DayOfWeek" /> in the given <paramref name="month" /> and <paramref name="year" />.
    /// </summary>
    /// <param name="year">
    /// The calendar year of the result. Must be between the <c>Year</c> property values of
    /// <see cref="DateOnly.MinValue" /> and <see cref="DateOnly.MaxValue" />, inclusive.
    /// </param>
    /// <param name="month">The calendar month of the result, from 1 through 12.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate within the month. For example, <see cref="DayOfWeek.Monday" /> returns
    /// the first Monday.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the first occurrence of <paramref name="dayOfWeek" /> within the
    /// specified month and year.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The search begins on the first day of the month and proceeds forward to locate the first matching weekday.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" /> is less than the <c>Year</c> of <see cref="DateOnly.MinValue" /> or greater
    /// than that of <see cref="DateOnly.MaxValue" />, -or- <paramref name="month" /> is less than 1 or greater than 12,
    /// -or- <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    public static DateOnly GetFirstDateOfWeekInMonth(int year, int month, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateTimeExtensions.MinYear, DateTimeExtensions.MaxYear);
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return DateOnly.FromDayNumber(GetFirstDateOfWeekInMonthDayNumber(year, month, dayOfWeek));
    }
}
