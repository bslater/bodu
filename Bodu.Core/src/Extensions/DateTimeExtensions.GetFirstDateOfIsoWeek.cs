// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.GetFirstDateOfIsoWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the first day (Monday) of the specified ISO 8601 week and
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
    /// An object whose value is set to midnight (00:00:00) on the Monday that begins the specified ISO 8601 week, using
    /// <see cref="DateTimeKind.Unspecified" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method computes the first day of a given ISO 8601 week by anchoring on January 4 (which always falls in ISO
    /// week 1), then backtracking to the preceding Monday and advancing by the supplied number of weeks.
    /// </para>
    /// <para>
    /// The ISO 8601 calendar follows these rules:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// weeks begin on Monday;
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
    /// The returned value is normalized to midnight (00:00:00) and uses <see cref="DateTimeKind.Unspecified" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="isoYear" /> is less than the <c>Year</c> of <see cref="DateTime.MinValue" /> or
    /// greater than that of <see cref="DateTime.MaxValue" />, -or- <paramref name="isoWeek" /> is less than 1 or
    /// greater than the number of ISO weeks in <paramref name="isoYear" />.
    /// </exception>
    public static DateTime GetFirstDateOfIsoWeek(int isoYear, int isoWeek)
    {
        ThrowHelper.ThrowIfOutOfRange(isoYear, DateTime.MinValue.Year, DateTime.MaxValue.Year);
        ThrowHelper.ThrowIfOutOfRange(isoWeek, 1, GetIsoWeeksInYear(isoYear));

        var ticks = GetDateTicks(isoYear, 1, 4);
        ticks += (
            1 - ((((int)GetDayOfWeekFromTicks(ticks) + 6) % 7) + 1) + // Backtrack to Monday
            ((isoWeek - 1) * 7)) // Advance to target week
        * TicksPerDay;

        return new DateTime(ticks, DateTimeKind.Unspecified);
    }
}
