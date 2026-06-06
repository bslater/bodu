// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeOffsetExtensions.SnapToWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateTimeOffsetExtensions
{
    /// <summary>
    /// Returns the date if it is a working day; otherwise the next working day, preserving the time-of-day and offset.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The date or the next working day, at the original time-of-day and offset.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateTimeOffset SnapToWorkingDay(this DateTimeOffset date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date.DateTime).SnapToWorkingDay(service, territory, workingWeek));
}
