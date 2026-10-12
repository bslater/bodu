// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.SnapToWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns the date if it is a working day; otherwise the next working day.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The date or the next working day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No working day is found within 4,000 days of the date (for example, when the working week has no working days).
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// INotableDateService service = AmericasCalendarData.CreateService("US");
    ///
    /// // Roll a scheduled due date forward to the next working day when it lands on a holiday.
    /// // Friday 3 July 2026 is the observed Independence Day, then a weekend: due is Monday 6 July 2026.
    /// DateOnly due = new DateOnly(2026, 7, 3).SnapToWorkingDay(service, "US");
    ///]]>
    /// </code>
    /// </example>
    public static DateOnly SnapToWorkingDay(this DateOnly date, INotableDateService service, string territory, DayOfWeekSet? workingWeek = null) =>
        date.IsWorkingDay(service, territory, workingWeek) ? date : Step(date, 1, service, territory, workingWeek);
}
