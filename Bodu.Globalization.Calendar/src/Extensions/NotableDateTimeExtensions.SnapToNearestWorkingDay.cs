// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.SnapToNearestWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns the date if it is a working day; otherwise the nearest working day, preserving the time-of-day and kind.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The nearest working day at the original time-of-day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateTime SnapToNearestWorkingDay(this DateTime date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date).SnapToNearestWorkingDay(service, territory, workingWeek));
}
