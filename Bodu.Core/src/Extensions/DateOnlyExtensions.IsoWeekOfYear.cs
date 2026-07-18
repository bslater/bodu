// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.IsoWeekOfYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
#if BODU_EXTENSION_MEMBERS

    extension(DateOnly date)
    {
        /// <summary>
        /// Gets the ISO 8601 week number for this date.
        /// </summary>
        /// <value>An integer in the range 1 – 53 representing the ISO 8601 week number that contains this date.</value>
        /// <remarks>
        /// <para>
        /// This follows the ISO 8601 standard for week numbering, where:
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// <description>weeks begin on Monday;</description>
        /// </item>
        /// <item>
        /// <description>week 1 is the first week containing at least four days of the new year.</description>
        /// </item>
        /// </list>
        /// <para>
        /// The result is computed using <see cref="CalendarWeekRule.FirstFourDayWeek" /> and
        /// <see cref="DayOfWeek.Monday" />, and is identical to the value produced by the <see cref="DateTime" /> twin
        /// for the same calendar date.
        /// </para>
        /// </remarks>
        public int IsoWeekOfYear =>
            DateTimeExtensions.GetWeekOfYear(date.DayNumber * DateTimeExtensions.TicksPerDay, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

#else

    /// <summary>
    /// Returns the ISO 8601 week number for the specified <paramref name="date" />.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <returns>
    /// An integer in the range 1 – 53 representing the ISO 8601 week number that contains <paramref name="date" />.
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
    /// <see cref="DayOfWeek.Monday" />, and is identical to the value produced by the <see cref="DateTime" /> twin for
    /// the same calendar date.
    /// </para>
    /// </remarks>
    public static int IsoWeekOfYear(this DateOnly date) =>
        DateTimeExtensions.GetWeekOfYear(date.DayNumber * DateTimeExtensions.TicksPerDay, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

#endif
}
