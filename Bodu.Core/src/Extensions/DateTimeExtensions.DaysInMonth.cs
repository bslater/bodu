// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.DaysInMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the number of days in the month of the specified <see cref="DateTime"/>, using the proleptic Gregorian calendar.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> whose year and month are used to determine the number of days.</param>
    /// <returns>The total number of days in the specified month and year of <paramref name="dateTime"/>, based on the <see cref="GregorianCalendar"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method always calculates the result using the proleptic Gregorian calendar (i.e., the calendar used internally by
    /// <see cref="DateTime"/>), regardless of the current culture or calendar settings.
    /// </para>
    /// <para>
    /// For culture-specific results, use the <see cref="DaysInMonth(DateTime, CultureInfo)"/> or
    /// <see cref="DaysInMonth(DateTime, Calendar)"/> overloads.
    /// </para>
    /// </remarks>
    public static int DaysInMonth(this DateTime dateTime)
    {
        return DateTime.DaysInMonth(dateTime.Year, dateTime.Month);
    }

    /// <summary>
    /// Returns the number of days in the month of the specified <see cref="DateTime"/>, using the calendar associated with the specified culture.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> whose year and month are used to determine the number of days.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo"/> used to determine the calendar. If <c>null</c>, <see cref="CultureInfo.CurrentCulture"/> is used.
    /// </param>
    /// <returns>
    /// The total number of days in the specified month and year of <paramref name="dateTime"/>, based on the calendar used by <paramref name="culture"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method retrieves the <see cref="Calendar"/> from the culture’s <see cref="DateTimeFormatInfo.Calendar"/> property and returns
    /// the number of days for the month of the given <paramref name="dateTime"/>.
    /// </para>
    /// <para>This is useful when working with cultures that use non-Gregorian calendars such as <see cref="HebrewCalendar"/> or <see cref="HijriCalendar"/>.</para>
    /// <note>If the calendar supports leap months or eras, this method does not account for them explicitly. For precise control, use the
    /// overload that accepts a <see cref="Calendar"/> directly.</note>
    /// </remarks>
    public static int DaysInMonth(this DateTime dateTime, CultureInfo? culture)
    {
        return (culture ?? CultureInfo.CurrentCulture).DateTimeFormat.Calendar.GetDaysInMonth(dateTime.Year, dateTime.Month);
    }

    /// <summary>
    /// Returns the number of days in the month of the specified <see cref="DateTime"/>, using the specified or current culture’s calendar.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> whose year and month are used to determine the number of days.</param>
    /// <param name="calendar">
    /// An optional <see cref="Calendar"/> instance used to evaluate the number of days. If <c>null</c>, the calendar of
    /// <see cref="CultureInfo.CurrentCulture"/> is used.
    /// </param>
    /// <returns>
    /// The total number of days in the specified month and year of <paramref name="dateTime"/>, based on the rules of the given or current calendar.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method supports calendar-aware computations for systems such as <see cref="HebrewCalendar"/>, <see cref="HijriCalendar"/>,
    /// <see cref="JapaneseCalendar"/>, and others supported by .NET.
    /// </para>
    /// <para>
    /// The result is equivalent to <c>calendar.GetDaysInMonth(dateTime.Year, dateTime.Month)</c>, or uses the current culture's calendar if
    /// <paramref name="calendar"/> is <see langword="null"/>.
    /// </para>
    /// <note>This method does not account for leap months. For calendars that support leap months or multiple eras, consider using
    /// <c>GetDaysInMonth(year, month, era)</c> instead.</note>
    /// </remarks>
    public static int DaysInMonth(this DateTime dateTime, Calendar? calendar)
    {
        return (calendar ?? CultureInfo.CurrentCulture.Calendar).GetDaysInMonth(dateTime.Year, dateTime.Month);
    }
}
