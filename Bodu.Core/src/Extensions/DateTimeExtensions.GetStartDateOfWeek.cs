// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.GetStartDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the first day of the specified culture-defined week number in
    /// the given calendar year.
    /// </summary>
    /// <param name="year">
    /// The calendar year to evaluate. Must be between the <c>Year</c> property values of
    /// <see cref="DateTime.MinValue" /> and <see cref="DateTime.MaxValue" />, inclusive.
    /// </param>
    /// <param name="week">
    /// The culture-defined week number to evaluate, starting at 1. The maximum valid value depends on the
    /// <see cref="CalendarWeekRule" /> and <see cref="DayOfWeek" /> used by the supplied <paramref name="culture" />.
    /// </param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo" /> used to determine the <see cref="CalendarWeekRule" /> and starting
    /// <see cref="DayOfWeek" />. If <see langword="null" />, <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first date of the specified week in the specified
    /// year, using <see cref="DateTimeKind.Unspecified" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method uses the culture-defined week numbering system. The start of week 1 depends on the culture's
    /// <see cref="CalendarWeekRule" />: under <see cref="CalendarWeekRule.FirstDay" /> the (possibly partial) first
    /// week begins on January 1 itself; under <see cref="CalendarWeekRule.FirstFullWeek" /> it begins at the first
    /// occurrence of the culture's <see cref="DateTimeFormatInfo.FirstDayOfWeek" /> on or after January 1; and under
    /// <see cref="CalendarWeekRule.FirstFourDayWeek" /> it begins at the week boundary of the week containing January 1
    /// when at least four days of that week fall in the new year (which may place the start in the previous December),
    /// otherwise one week later. Subsequent weeks advance in 7-day intervals from the week-boundary alignment.
    /// </para>
    /// <para>
    /// The result is validated by recalculating the week number for the computed date using the internal week-of-year
    /// calculation and comparing it to <paramref name="week" />. Dates that fall in the previous calendar year (such as
    /// the start of ISO week 1 in late December) are handled correctly.
    /// </para>
    /// <para>
    /// The returned value is normalized to midnight (00:00:00) and uses <see cref="DateTimeKind.Unspecified" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" /> is less than the <c>Year</c> of <see cref="DateTime.MinValue" /> or greater
    /// than that of <see cref="DateTime.MaxValue" />, -or- <paramref name="week" /> does not correspond to a valid week
    /// number for <paramref name="year" /> under the rules of the supplied or current <paramref name="culture" />.
    /// </exception>
    public static DateTime GetStartDateOfWeek(int year, int week, CultureInfo? culture = null)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateTime.MinValue.Year, DateTime.MaxValue.Year);

        DateTimeFormatInfo dfi = (culture ?? CultureInfo.CurrentCulture).DateTimeFormat;

        long jan1 = GetDateTicks(year, 1, 1);

        // Days from the week boundary back to January 1 (0 when January 1 is itself the week start).
        long offsetTicks = GetTicksSincePreviousOrSameDayOfWeek(jan1, dfi.FirstDayOfWeek);

        // Start of week 1 under the culture's rule. FirstDay: the (possibly partial) first week begins on January 1;
        // weeks 2+ are aligned to the week boundary of the week containing January 1. FirstFullWeek: week 1 begins at
        // the first week boundary on or after January 1. FirstFourDayWeek: week 1 begins at the boundary of the week
        // containing January 1 when that week keeps at least four days in the new year (possibly in the previous
        // December), otherwise one week later.
        long ticks = dfi.CalendarWeekRule switch
        {
            CalendarWeekRule.FirstDay =>
                week == 1 ? jan1 : jan1 - offsetTicks + ((week - 1) * 7L * TimeSpan.TicksPerDay),
            CalendarWeekRule.FirstFullWeek =>
                (offsetTicks == 0 ? jan1 : jan1 + ((7L * TimeSpan.TicksPerDay) - offsetTicks)) + ((week - 1) * 7L * TimeSpan.TicksPerDay),
            CalendarWeekRule.FirstFourDayWeek =>
                (offsetTicks <= 3L * TimeSpan.TicksPerDay ? jan1 - offsetTicks : jan1 + ((7L * TimeSpan.TicksPerDay) - offsetTicks)) + ((week - 1) * 7L * TimeSpan.TicksPerDay),
            _ => throw new ArgumentOutOfRangeException(
                nameof(culture),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_EnumValue, nameof(CalendarWeekRule), dfi.CalendarWeekRule)),
        };

        // Validate week number: an out-of-range or nonexistent week lands on a date whose recomputed week differs.
        if ((ulong)ticks > (ulong)DateTime.MaxValue.Ticks
            || GetWeekOfYear(ticks, dfi.CalendarWeekRule, dfi.FirstDayOfWeek) != week)
        {
            throw new ArgumentOutOfRangeException(
                nameof(week),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_WeekNotValidForYearAndCulture, week, year, (culture ?? CultureInfo.CurrentCulture).Name));
        }

        return new DateTime(ticks, DateTimeKind.Unspecified);
    }
}
