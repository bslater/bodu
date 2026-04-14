// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.LastDateOfIsoWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the last date (Sunday) of the specified ISO 8601 week and year.
    /// </summary>
    /// <param name="isoYear">
    /// The ISO 8601 year, which corresponds to the calendar year containing the Thursday of the first ISO week (e.g., 2024).
    /// </param>
    /// <param name="isoWeek">The ISO 8601 week number within the specified year. Valid values range from 1 to 53, depending on the year.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the Sunday that ends the specified ISO 8601 week.</returns>
    /// <remarks>
    /// <para>According to the ISO 8601 standard:</para>
    /// <list type="bullet">
    /// <item>
    /// <description>Weeks begin on Monday and end on Sunday.</description>
    /// </item>
    /// <item>
    /// <description>Week 1 is the first week with at least four days in the new year.</description>
    /// </item>
    /// <item>
    /// <description>January 4th is always part of ISO week 1.</description>
    /// </item>
    /// </list>
    /// <para>
    /// This method calculates the target Sunday by anchoring on January 4th, backtracking to the Monday of ISO week 1, advancing by the
    /// specified number of weeks, and adding six days to reach the Sunday of that week.
    /// </para>
    /// <note type="tip">For the corresponding start of the week, use <see cref="GetFirstDateOfIsoWeek"/>.</note>
    /// <para>The result has its time component normalized to midnight (00:00:00), and the original <see cref="DateTime.Kind"/> is retained.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="isoYear"/> is outside the valid range, or if <paramref name="isoWeek"/> is less than 1 or greater than
    /// the number of ISO weeks in the year.
    /// </exception>
    public static DateTime LastDateOfIsoWeek(int isoYear, int isoWeek)
    {
        ThrowHelper.ThrowIfOutOfRange(isoYear, DateTime.MinValue.Year, DateTime.MaxValue.Year);
        ThrowHelper.ThrowIfOutOfRange(isoWeek, 1, GetIsoWeeksInYear(isoYear));

        long ticks = GetDateTicks(isoYear, 1, 4);
        ticks += (
            1 - ((((int)GetDayOfWeekFromTicks(ticks) + 6) % 7) + 1) + // Backtrack to Monday
            ((isoWeek - 1) * 7) + // Advance to target week
            6) // Advance to Sunday
            * TicksPerDay;

        return new DateTime(ticks, DateTimeKind.Unspecified);
    }
}
