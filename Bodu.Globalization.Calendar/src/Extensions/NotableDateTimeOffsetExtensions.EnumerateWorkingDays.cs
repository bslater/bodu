// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeOffsetExtensions.EnumerateWorkingDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateTimeOffsetExtensions
{
    /// <summary>
    /// Lazily enumerates the working days in the inclusive range, in ascending order, each at the start's time-of-day
    /// and offset.
    /// </summary>
    /// <param name="start">The inclusive start date.</param>
    /// <param name="end">The inclusive end date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The working days in the range at the start's time-of-day and offset.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IEnumerable<DateTimeOffset> EnumerateWorkingDays(this DateTimeOffset start, DateTimeOffset end, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        var time = TimeOnly.FromTimeSpan(start.TimeOfDay);
        TimeSpan offset = start.Offset;

        return DateOnly.FromDateTime(start.DateTime)
            .EnumerateWorkingDays(DateOnly.FromDateTime(end.DateTime), service, territory, workingWeek)
            .Select(day => new DateTimeOffset(day.ToDateTime(time), offset));
    }
}
