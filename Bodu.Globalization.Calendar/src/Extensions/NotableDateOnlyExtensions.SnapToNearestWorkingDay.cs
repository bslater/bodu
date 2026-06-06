// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.SnapToNearestWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns the date if it is a working day; otherwise the nearest working day, preferring the forward direction on
    /// a tie.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The nearest working day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateOnly SnapToNearestWorkingDay(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        if (date.IsWorkingDay(service, territory, workingWeek))
            return date;

        DateOnly forward = Step(date, 1, service, territory, workingWeek);
        DateOnly backward = Step(date, -1, service, territory, workingWeek);

        return (forward.DayNumber - date.DayNumber) <= (date.DayNumber - backward.DayNumber) ? forward : backward;
    }
}
