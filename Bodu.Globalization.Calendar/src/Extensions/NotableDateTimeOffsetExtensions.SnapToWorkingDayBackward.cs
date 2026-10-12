// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeOffsetExtensions.SnapToWorkingDayBackward.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateTimeOffsetExtensions
{
    /// <summary>
    /// Returns the date if it is a working day; otherwise the previous working day, preserving the time-of-day and
    /// offset.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The date or the previous working day, at the original time-of-day and offset.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No working day is found within 4,000 days of the date (for example, when the working week has no working days).
    /// </exception>
    public static DateTimeOffset SnapToWorkingDayBackward(this DateTimeOffset date, INotableDateService service, string territory, DayOfWeekSet? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date.DateTime).SnapToWorkingDayBackward(service, territory, workingWeek));
}
