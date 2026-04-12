// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.DaysInYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the number of days in the calendar year of the specified <see cref="DateTime" />, using the current culture's calendar.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> for which to calculate the number of days in its calendar year.</param>
    /// <returns>
    /// The total number of days in the year corresponding to <paramref name="dateTime" />, as defined by the current culture's <see cref="Calendar" />.
    /// </returns>
    /// <remarks>
    /// This method uses the calendar defined by <see cref="CultureInfo.CurrentCulture" />. The result may vary depending on the calendar
    /// system (e.g., Gregorian, Hebrew, Hijri).
    /// </remarks>
    public static int DaysInYear(this DateTime dateTime)
    {
        return dateTime.DaysInYear(null!);
    }

    /// <summary>
    /// Returns the number of days in the calendar year of the specified <see cref="DateTime" />, using the specified <see cref="Calendar" />.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> whose year is evaluated.</param>
    /// <param name="calendar">
    /// The <see cref="Calendar" /> used to determine the number of days in the year. If <c>null</c>, the current culture's calendar is used.
    /// </param>
    /// <returns>The number of days in the year of <paramref name="dateTime" />, based on the provided or fallback calendar.</returns>
    /// <remarks>
    /// Use this method when you want to explicitly calculate based on a specific calendar system (e.g., <see cref="GregorianCalendar" />,
    /// <see cref="HebrewCalendar" />). If <paramref name="calendar" /> is <see langword="null" />, the calendar from
    /// <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </remarks>
    public static int DaysInYear(this DateTime dateTime, Calendar calendar)
    {
        return (calendar ?? CultureInfo.CurrentCulture.Calendar).GetDaysInYear(dateTime.Year);
    }
}
