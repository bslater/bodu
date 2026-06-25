// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.IsWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Determines whether the date is a working day.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns><see langword="true" /> if the date is a working day; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// INotableDateService service = AmericasCalendarData.CreateService("US");
    ///
    /// bool labourDay = new DateOnly(2026, 9, 7).IsWorkingDay(service, "US");   // false (Labor Day)
    /// bool ordinary = new DateOnly(2026, 9, 8).IsWorkingDay(service, "US");    // true
    ///]]>
    /// </code>
    /// </example>
    public static bool IsWorkingDay(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        !date.IsNonWorkingDay(service, territory, workingWeek);
}
