// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.NextWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns the first working day strictly after the date.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The next working day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// INotableDateService service = AmericasCalendarData.CreateService("US");
    ///
    /// // The first business day after a holiday weekend.
    /// DateOnly resume = new DateOnly(2026, 7, 3).NextWorkingDay(service, "US");
    ///]]>
    /// </code>
    /// </example>
    public static DateOnly NextWorkingDay(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        Step(date, 1, service, territory, workingWeek);
}
