// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.NthDateOfWeekInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the specified ordinal occurrence of a
    /// <see cref="DayOfWeek" /> within the same calendar month and year as the specified <paramref name="date" />.
    /// </summary>
    /// <param name="date">
    /// The date value whose month and year are used to determine the result. The day component is ignored.
    /// </param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate within the month. For example, <see cref="DayOfWeek.Monday" /> returns
    /// the nth Monday.
    /// </param>
    /// <param name="ordinal">
    /// The ordinal occurrence to return. Valid values are <see cref="WeekOrdinal.First" />,
    /// <see cref="WeekOrdinal.Second" />, <see cref="WeekOrdinal.Third" />, <see cref="WeekOrdinal.Fourth" />,
    /// <see cref="WeekOrdinal.Fifth" />, and <see cref="WeekOrdinal.Last" />. <see cref="WeekOrdinal.Fifth" /> is valid
    /// only in months where five matching weekdays occur.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the requested occurrence of <paramref name="dayOfWeek" /> within the same
    /// calendar month and year as <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// For <see cref="WeekOrdinal.Last" />, the method returns the final matching <paramref name="dayOfWeek" /> in the
    /// month. For other ordinal values, the method locates the first matching weekday and offsets by a multiple of
    /// seven days to reach the desired ordinal.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration, -or-
    /// <paramref name="ordinal" /> is not a defined value of the <see cref="WeekOrdinal" /> enumeration, -or- the
    /// requested <paramref name="ordinal" /> does not occur within the month (for example, a fifth Thursday in
    /// February).
    /// </exception>
    public static DateOnly NthDateOfWeekInMonth(this DateOnly date, DayOfWeek dayOfWeek, WeekOrdinal ordinal)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(ordinal);

        switch (ordinal)
        {
            case Extensions.WeekOrdinal.First:
                return date.FirstDateOfWeekInMonth(dayOfWeek);

            case Extensions.WeekOrdinal.Last:
                return date.LastDateOfWeekInMonth(dayOfWeek);

            default:
                int dayNumber = DateTimeExtensions.GetDayNumberUnchecked(date.Year, date.Month, 1);
                var result = DateOnly.FromDayNumber(
                    dayNumber + (((int)dayOfWeek - (int)GetDayOfWeekFromDayNumber(dayNumber) + 7) % 7) + (((int)ordinal - 1) * 7));

                if (result.Month != date.Month) throw new ArgumentOutOfRangeException(nameof(ordinal), string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_OrdinalDoesNotExistForMonth, ordinal, dayOfWeek, date.ToString("MMMM yyyy", CultureInfo.CurrentCulture)));

                return result;
        }
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the specified ordinal occurrence of a
    /// <see cref="DayOfWeek" /> within the given calendar <paramref name="month" /> and <paramref name="year" />.
    /// </summary>
    /// <param name="year">
    /// The calendar year of the result. Must be between the <c>Year</c> property values of
    /// <see cref="DateOnly.MinValue" /> and <see cref="DateOnly.MaxValue" />, inclusive.
    /// </param>
    /// <param name="month">
    /// The calendar month of the result. Must be between 1 and 12, inclusive, where 1 represents January and 12
    /// represents December.
    /// </param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate within the month. For example, <see cref="DayOfWeek.Tuesday" /> returns
    /// the nth Tuesday.
    /// </param>
    /// <param name="ordinal">
    /// The ordinal occurrence to return. Valid values are <see cref="WeekOrdinal.First" />,
    /// <see cref="WeekOrdinal.Second" />, <see cref="WeekOrdinal.Third" />, <see cref="WeekOrdinal.Fourth" />,
    /// <see cref="WeekOrdinal.Fifth" />, and <see cref="WeekOrdinal.Last" />. <see cref="WeekOrdinal.Fifth" /> is valid
    /// only in months where five matching weekdays occur.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the requested occurrence of <paramref name="dayOfWeek" /> within the
    /// specified <paramref name="year" /> and <paramref name="month" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// For <see cref="WeekOrdinal.Last" />, the method returns the final matching <paramref name="dayOfWeek" /> in the
    /// month. For other ordinal values, the method locates the first matching weekday and offsets by a multiple of
    /// seven days to reach the desired ordinal.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" /> is less than the <c>Year</c> of <see cref="DateOnly.MinValue" /> or greater
    /// than that of <see cref="DateOnly.MaxValue" />, -or- <paramref name="month" /> is less than 1 or greater than 12,
    /// -or- <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration, -or-
    /// <paramref name="ordinal" /> is not a defined value of the <see cref="WeekOrdinal" /> enumeration, -or- the
    /// requested <paramref name="ordinal" /> does not occur within the month (for example, a fifth Thursday in
    /// February).
    /// </exception>
    public static DateOnly GetNthDateOfWeekInMonth(int year, int month, DayOfWeek dayOfWeek, WeekOrdinal ordinal)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateOnly.MinValue.Year, DateOnly.MaxValue.Year);
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(ordinal);

        switch (ordinal)
        {
            case Extensions.WeekOrdinal.First:
                return DateOnly.FromDayNumber(DateOnlyExtensions.GetFirstDateOfWeekInMonthDayNumber(year, month, dayOfWeek));

            case Extensions.WeekOrdinal.Last:
                return DateOnly.FromDayNumber(DateOnlyExtensions.GetLastDateOfWeekInMonthDayNumber(year, month, dayOfWeek));

            default:
                var result = DateOnly.FromDayNumber(DateOnlyExtensions.GetFirstDateOfWeekInMonthDayNumber(year, month, dayOfWeek) + (((int)ordinal - 1) * 7));

                if (result.Month != month) throw new ArgumentOutOfRangeException(nameof(ordinal), string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_OrdinalDoesNotExistForMonth, ordinal, dayOfWeek, new DateOnly(year, month, 1).ToString("MMMM yyyy", CultureInfo.CurrentCulture)));

                return result;
        }
    }

    /// <summary>
    /// Attempts to find the specified ordinal occurrence of a <see cref="DayOfWeek" /> within a calendar month,
    /// reporting an occurrence the month does not have instead of throwing.
    /// </summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month, where 1 represents January and 12 represents December.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek" /> to locate within the month.</param>
    /// <param name="ordinal">The ordinal occurrence to find.</param>
    /// <param name="result">The occurrence when the month has it; otherwise the default value.</param>
    /// <returns>
    /// <see langword="true" /> when the month has the occurrence; <see langword="false" /> when
    /// <paramref name="ordinal" /> names a fifth occurrence the month lacks, or is not a value
    /// <see cref="WeekOrdinal" /> defines.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the one definition of the nth weekday of a month that callers needing a non-throwing answer share, such
    /// as the notable-date strategies. It validates nothing: <paramref name="year" /> must lie between 1 and 9999,
    /// <paramref name="month" /> between 1 and 12, and <paramref name="dayOfWeek" /> must be defined.
    /// </para>
    /// <para>
    /// A missing occurrence is never built as a date, so the method neither throws nor leaves the range of
    /// <see cref="DateOnly" />, even for a fifth occurrence December 9999 does not have.
    /// </para>
    /// </remarks>
    internal static bool TryGetNthDateOfWeekInMonth(int year, int month, DayOfWeek dayOfWeek, WeekOrdinal ordinal, out DateOnly result)
    {
        if (ordinal == Extensions.WeekOrdinal.Last)
        {
            result = DateOnly.FromDayNumber(GetLastDateOfWeekInMonthDayNumber(year, month, dayOfWeek));
            return true;
        }

        if (ordinal is < Extensions.WeekOrdinal.First or > Extensions.WeekOrdinal.Fifth)
        {
            result = default;
            return false;
        }

        // Only a fifth occurrence can miss a month of 28 to 31 days. Comparing day numbers with the month's last day
        // keeps a missing one from ever becoming a date, so December 9999 needs no special case.
        int dayNumber = GetFirstDateOfWeekInMonthDayNumber(year, month, dayOfWeek) + (((int)ordinal - 1) * 7);
        if (dayNumber > DateTimeExtensions.GetDayNumberUnchecked(year, month, DateTime.DaysInMonth(year, month)))
        {
            result = default;
            return false;
        }

        result = DateOnly.FromDayNumber(dayNumber);
        return true;
    }
}
