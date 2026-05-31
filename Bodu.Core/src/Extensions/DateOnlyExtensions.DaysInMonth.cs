// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.DaysInMonth.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the number of days in the calendar month of the specified <see cref="DateOnly" />, using the proleptic
    /// Gregorian calendar.
    /// </summary>
    /// <param name="date">The date value whose year and month are used to determine the result.</param>
    /// <returns>
    /// The total number of days in the specified month and year of <paramref name="date" />, based on the
    /// <see cref="GregorianCalendar" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload always evaluates the result using the proleptic Gregorian calendar, regardless of the current
    /// culture or calendar settings. For culture-specific results, use the
    /// <see cref="DaysInMonth(DateOnly, CultureInfo)" /> or <see cref="DaysInMonth(DateOnly, Calendar)" /> overload.
    /// </para>
    /// </remarks>
    public static int DaysInMonth(this DateOnly date) => DateTime.DaysInMonth(date.Year, date.Month);

    /// <summary>
    /// Returns the number of days in the calendar month of the specified <see cref="DateOnly" />, using the calendar
    /// associated with the supplied culture.
    /// </summary>
    /// <param name="date">The date value whose year and month are used to determine the result.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo" /> that supplies the calendar. If <see langword="null" />,
    /// <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </param>
    /// <returns>
    /// The total number of days in the specified month and year of <paramref name="date" />, based on the calendar of
    /// <paramref name="culture" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload retrieves the <see cref="Calendar" /> from the culture's
    /// <see cref="DateTimeFormatInfo.Calendar" /> property and returns the number of days for the month of the supplied
    /// <paramref name="date" />.
    /// </para>
    /// <para>
    /// This is useful when working with cultures that use non-Gregorian calendars such as <see cref="HebrewCalendar" />
    /// or <see cref="HijriCalendar" />. If the calendar supports leap months or eras, this method does not account for
    /// them explicitly. For precise control, use the overload that accepts a <see cref="Calendar" /> directly.
    /// </para>
    /// </remarks>
    public static int DaysInMonth(this DateOnly date, CultureInfo? culture) => (culture ?? CultureInfo.CurrentCulture).DateTimeFormat.Calendar.GetDaysInMonth(date.Year, date.Month);

    /// <summary>
    /// Returns the number of days in the calendar month of the specified <see cref="DateOnly" />, using the supplied or
    /// current culture's calendar.
    /// </summary>
    /// <param name="date">The date value whose year and month are used to determine the result.</param>
    /// <param name="calendar">
    /// An optional <see cref="Calendar" /> instance used to evaluate the result. If <see langword="null" />, the
    /// calendar of <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </param>
    /// <returns>
    /// The total number of days in the specified month and year of <paramref name="date" />, based on the rules of the
    /// supplied or current calendar.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload supports calendar-aware computations for systems such as <see cref="HebrewCalendar" />,
    /// <see cref="HijriCalendar" />, <see cref="JapaneseCalendar" />, and others supported by .NET. The result is
    /// equivalent to <c>calendar.GetDaysInMonth(date.Year, date.Month)</c>, or uses the current culture's calendar if
    /// <paramref name="calendar" /> is <see langword="null" />.
    /// </para>
    /// <para>
    /// This method does not account for leap months. For calendars that support leap months or multiple eras, consider
    /// using <c>GetDaysInMonth(year, month, era)</c> instead.
    /// </para>
    /// </remarks>
    public static int DaysInMonth(this DateOnly date, Calendar? calendar) => (calendar ?? CultureInfo.CurrentCulture.Calendar).GetDaysInMonth(date.Year, date.Month);
}
