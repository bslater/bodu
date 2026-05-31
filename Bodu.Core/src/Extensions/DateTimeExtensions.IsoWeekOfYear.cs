// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsoWeekOfYear.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the ISO 8601 week number for the specified <paramref name="dateTime" />.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <returns>
    /// An integer in the range 1 – 53 representing the ISO 8601 week number that contains <paramref name="dateTime" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method follows the ISO 8601 standard for week numbering, where:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// weeks begin on Monday;
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// week 1 is the first week containing at least four days of the new year.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// The result is computed using <see cref="CalendarWeekRule.FirstFourDayWeek" /> and
    /// <see cref="DayOfWeek.Monday" /> against the date portion of <paramref name="dateTime" />. Any time-of-day
    /// component is discarded before the calculation.
    /// </para>
    /// </remarks>
    public static int IsoWeekOfYear(this DateTime dateTime) =>
        GetWeekOfYear(TruncateToDateTicks(dateTime), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
}
