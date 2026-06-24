// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.AddWorkingDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Advances the date by a signed number of working days.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="count">
    /// The number of working days to add; negative retreats, zero returns the date unchanged.
    /// </param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The resulting date.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// INotableDateService service = AmericasCalendarData.CreateService("US");
    ///
    /// // A T+2 settlement date that skips weekends and US holidays.
    /// DateOnly trade = new(2026, 7, 2);
    /// DateOnly settles = trade.AddWorkingDays(2, service, "US"); // jumps past July 4th observance
    ///]]>
    /// </code>
    /// </example>
    public static DateOnly AddWorkingDays(this DateOnly date, int count, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        if (count == 0)
            return date;

        int direction = count > 0 ? 1 : -1;
        int remaining = Math.Abs(count);
        DateOnly current = date;

        while (remaining > 0)
        {
            current = Step(current, direction, service, territory, workingWeek);
            remaining--;
        }

        return current;
    }
}
