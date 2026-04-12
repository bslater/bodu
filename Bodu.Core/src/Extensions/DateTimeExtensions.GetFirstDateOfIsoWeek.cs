// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.GetFirstDateOfIsoWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the first day (Monday) of the specified ISO 8601 week and year.
    /// </summary>
    /// <param name="isoYear">
    /// The ISO 8601 year, defined as the year containing the Thursday of the first ISO week. This may differ from the calendar year for
    /// dates near the beginning or end of the year.
    /// </param>
    /// <param name="isoWeek">The ISO 8601 week number to evaluate, ranging from 1 to 53 depending on the year.</param>
    /// <returns>The date of the Monday that begins the specified ISO 8601 week, at midnight (00:00:00).</returns>
    /// <remarks>
    /// <para>
    /// This method computes the first day of a given ISO 8601 week by anchoring on January 4 (which always falls in ISO week 1), then
    /// backtracking to the preceding Monday and advancing by the specified number of weeks.
    /// </para>
    /// <para>The ISO 8601 calendar follows these rules:</para>
    /// <list type="bullet">
    /// <item>
    /// <description>Weeks begin on Monday.</description>
    /// </item>
    /// <item>
    /// <description>Week 1 is the first week that contains a Thursday.</description>
    /// </item>
    /// <item>
    /// <description>Years may contain either 52 or 53 weeks.</description>
    /// </item>
    /// </list>
    /// <para>The <see cref="DateTime.Kind" /> property of the returned instance is <see cref="DateTimeKind.Unspecified" />.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="isoYear" /> is less than 1 or greater than 9999,
    /// -or- <paramref name="isoWeek" /> is less than 1 or greater than the number of ISO weeks in the given year.
    /// </exception>
    public static DateTime GetFirstDateOfIsoWeek(int isoYear, int isoWeek)
    {
        ThrowHelper.ThrowIfOutOfRange(isoYear, DateTime.MinValue.Year, DateTime.MaxValue.Year);
        ThrowHelper.ThrowIfOutOfRange(isoWeek, 1, GetIsoWeeksInYear(isoYear));

        long ticks = GetDateTicks(isoYear, 1, 4);
        ticks += (
            1 - ((((int)GetDayOfWeekFromTicks(ticks) + 6) % 7) + 1) + // Backtrack to Monday
            ((isoWeek - 1) * 7)) // Advance to target week
        * TicksPerDay;

        return new DateTime(ticks, DateTimeKind.Unspecified);
    }
}
