// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.GetIsoWeekOfYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the ISO 8601 week number for the specified <see cref="DateTime"/>.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> value to evaluate.</param>
    /// <returns>The ISO 8601 week number of the year that contains <paramref name="dateTime"/>, ranging from 1 to 53.</returns>
    /// <remarks>
    /// <para>This method uses the ISO 8601 standard for week numbering, where:</para>
    /// <list type="bullet">
    /// <item>
    /// <description>Weeks begin on Monday.</description>
    /// </item>
    /// <item>
    /// <description>Week 1 is the first week that contains at least four days in the new year.</description>
    /// </item>
    /// </list>
    /// <para>
    /// The result is computed using <see cref="CalendarWeekRule.FirstFourDayWeek"/> and <see cref="DayOfWeek.Monday"/> against the
    /// date portion of <paramref name="dateTime"/>. Any time-of-day component is discarded before the calculation.
    /// </para>
    /// </remarks>
    public static int GetIsoWeekOfYear(this DateTime dateTime) => 
        GetWeekOfYear(TruncateToDateTicks(dateTime), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
}
