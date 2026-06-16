// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.GetLastDateOfIsoWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the last day (Sunday) of the specified ISO 8601 week and
    /// year.
    /// </summary>
    /// <param name="isoYear">
    /// The ISO 8601 year, defined as the year containing the Thursday of the first ISO week. Must be between the
    /// <c>Year</c> property values of <see cref="DateTime.MinValue" /> and <see cref="DateTime.MaxValue" />, inclusive.
    /// </param>
    /// <param name="isoWeek">
    /// The ISO 8601 week number to evaluate, ranging from 1 to the number of ISO weeks in the supplied year.
    /// </param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the Sunday that ends the specified ISO 8601 week, using
    /// <see cref="DateTimeKind.Unspecified" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method computes the last day of a given ISO 8601 week by anchoring on January 4 (which always falls in ISO
    /// week 1), backtracking to the preceding Monday, advancing by the supplied number of weeks, and adding six days to
    /// reach the Sunday of that week.
    /// </para>
    /// <para>
    /// The ISO 8601 calendar follows these rules:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// weeks begin on Monday and end on Sunday;
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// week 1 is the first week containing at least four days of the new year;
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// years contain either 52 or 53 weeks.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// The returned value is normalized to midnight (00:00:00) and uses <see cref="DateTimeKind.Unspecified" />. For
    /// the corresponding start of the week, use <see cref="GetFirstDateOfIsoWeek(int, int)" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="isoYear" /> is less than the <c>Year</c> of <see cref="DateTime.MinValue" /> or
    /// greater than that of <see cref="DateTime.MaxValue" />, -or- <paramref name="isoWeek" /> is less than 1 or
    /// greater than the number of ISO weeks in <paramref name="isoYear" />.
    /// </exception>
    public static DateTime GetLastDateOfIsoWeek(int isoYear, int isoWeek)
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
