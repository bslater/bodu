// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.IsNonWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Determines whether the date is a non-working day: outside the working week, or a non-working notable date.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns><see langword="true" /> if the date is non-working; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// INotableDateService service = AsiaPacificCalendarData.CreateService("AU");
    ///
    /// bool anzacDay = new DateOnly(2024, 4, 25).IsNonWorkingDay(service, "AU");   // true (public holiday)
    /// bool friday = new DateOnly(2024, 4, 26).IsNonWorkingDay(service, "AU");     // false
    ///]]>
    /// </code>
    /// </example>
    public static bool IsNonWorkingDay(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        if (date.IsWeekend(workingWeek))
            return true;

        return service.Resolve(date, territory).Any(n => n.IsNonWorkingDay);
    }
}
