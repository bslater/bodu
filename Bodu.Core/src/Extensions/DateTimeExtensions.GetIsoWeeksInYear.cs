// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.GetIsoWeeksInYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the number of ISO 8601 weeks in the specified <paramref name="year" />.
    /// </summary>
    /// <param name="year">
    /// The ISO 8601 year to evaluate. Must be between the <c>Year</c> property values of
    /// <see cref="DateTime.MinValue" /> and <see cref="DateTime.MaxValue" />, inclusive.
    /// </param>
    /// <returns>The number of ISO 8601 weeks in the supplied year — either 52 or 53.</returns>
    /// <remarks>
    /// <para>
    /// According to ISO 8601, a year contains 53 weeks if either of the following is true:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// January 1 of the supplied year falls on a Thursday;
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// December 31 of the supplied year falls on a Thursday (equivalent to January 1 of the following year falling on a
    /// Friday).
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// All other years contain exactly 52 weeks. The implementation evaluates these conditions by computing the weekday
    /// of January 1 for the supplied year and the following year, using <see cref="GetDayOfWeekForJanuary1(int)" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" /> is less than the <c>Year</c> of <see cref="DateTime.MinValue" /> or greater
    /// than that of <see cref="DateTime.MaxValue" />.
    /// </exception>
    public static int GetIsoWeeksInYear(int year)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateTime.MinValue.Year, DateTime.MaxValue.Year);

        // A year has 53 ISO weeks if Jan 1 is Thursday, or if Dec 31 is Thursday.
        // Dec 31 of 'year' is Thursday iff Jan 1 of 'year + 1' is Friday.
        return (GetDayOfWeekForJanuary1(year) == DayOfWeek.Thursday || GetDayOfWeekForJanuary1(year + 1) == DayOfWeek.Friday) ? 53 : 52;
    }
}
