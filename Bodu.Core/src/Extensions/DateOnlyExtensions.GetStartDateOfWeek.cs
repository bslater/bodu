// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.GetStartDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the first day of the specified culture-defined week number in
    /// the given calendar year.
    /// </summary>
    /// <param name="year">
    /// The calendar year to evaluate. Must be between the <c>Year</c> property values of
    /// <see cref="DateOnly.MinValue" /> and <see cref="DateOnly.MaxValue" />, inclusive.
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
    /// A <see cref="DateOnly" /> value set to the first date of the specified week in the specified year.
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
    /// The result is validated by recalculating the week number for the computed date and comparing it to
    /// <paramref name="week" />. Dates that fall in the previous calendar year (such as the start of ISO week 1 in late
    /// December) are handled correctly. This member delegates to
    /// <see cref="DateTimeExtensions.GetStartDateOfWeek(int, int, CultureInfo?)" /> — the twins share one
    /// implementation, so both surfaces always agree.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" /> is less than the <c>Year</c> of <see cref="DateOnly.MinValue" /> or greater
    /// than that of <see cref="DateOnly.MaxValue" />, -or- <paramref name="week" /> does not correspond to a valid week
    /// number for <paramref name="year" /> under the rules of the supplied or current <paramref name="culture" />.
    /// </exception>
    public static DateOnly GetStartDateOfWeek(int year, int week, CultureInfo? culture = null) =>
        DateTimeExtensions.GetStartDateOfWeek(year, week, culture).ToDateOnly();
}
