// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.SnapToWorkingDayBackward.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns the date if it is a working day; otherwise the previous working day.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The date or the previous working day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// INotableDateService service = AsiaPacificCalendarData.CreateService("AU");
    ///
    /// // A "no later than" contractual date falling on Anzac Day rolls back to Wednesday.
    /// DateOnly payday = new DateOnly(2024, 4, 25).SnapToWorkingDayBackward(service, "AU");   // 2024-04-24
    ///]]>
    /// </code>
    /// </example>
    public static DateOnly SnapToWorkingDayBackward(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        date.IsWorkingDay(service, territory, workingWeek) ? date : Step(date, -1, service, territory, workingWeek);
}
