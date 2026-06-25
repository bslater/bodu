// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.WorkingDaysBetween.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Counts the working days in the inclusive range bounded by the two dates, regardless of their order.
    /// </summary>
    /// <param name="start">One end of the range.</param>
    /// <param name="end">The other end of the range.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The number of working days in the inclusive range.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// INotableDateService service = AmericasCalendarData.CreateService("US");
    ///
    /// // Billable working days across a project window, excluding weekends and holidays.
    /// int days = new DateOnly(2026, 7, 1).WorkingDaysBetween(new DateOnly(2026, 7, 31), service, "US");
    ///]]>
    /// </code>
    /// </example>
    public static int WorkingDaysBetween(this DateOnly start, DateOnly end, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        DateOnly lower = start <= end ? start : end;
        DateOnly upper = start <= end ? end : start;

        int count = 0;
        for (DateOnly cursor = lower; cursor <= upper; cursor = cursor.AddDays(1))
        {
            if (cursor.IsWorkingDay(service, territory, workingWeek))
                count++;
        }

        return count;
    }
}
