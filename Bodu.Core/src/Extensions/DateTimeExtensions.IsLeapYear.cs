// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsLeapYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Determines whether the year of the specified <see cref="DateTime" /> is a leap year, according to the proleptic
    /// Gregorian calendar.
    /// </summary>
    /// <param name="dateTime">The date and time value whose <see cref="DateTime.Year" /> is evaluated.</param>
    /// <returns>
    /// <see langword="true" /> if the year contains February 29; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method applies the Gregorian leap-year rules:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// Years divisible by 4 are leap years,
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// except years divisible by 100,
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// unless also divisible by 400.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// For example, the years 2000 and 2024 are leap years, while 1900 and 2100 are not.
    /// </para>
    /// <para>
    /// This method does not consider culture-specific calendars; it always evaluates leap years using the Gregorian
    /// calendar.
    /// </para>
    /// </remarks>
    public static bool IsLeapYear(this DateTime dateTime) => DateTime.IsLeapYear(dateTime.Year);
}
