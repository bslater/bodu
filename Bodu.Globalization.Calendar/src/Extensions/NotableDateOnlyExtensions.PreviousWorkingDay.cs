// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.PreviousWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns the first working day strictly before the date.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The previous working day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// INotableDateService service = AsiaPacificCalendarData.CreateService("AU");
    ///
    /// // From Friday 26 April 2024 the previous open day skips Anzac Day back to Wednesday.
    /// DateOnly open = new DateOnly(2024, 4, 26).PreviousWorkingDay(service, "AU");   // 2024-04-24
    ///]]>
    /// </code>
    /// </example>
    public static DateOnly PreviousWorkingDay(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        Step(date, -1, service, territory, workingWeek);
}
