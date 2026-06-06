// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.EnumerateNonWorkingDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Lazily enumerates the non-working days in the inclusive range, in ascending order, each at the start's
    /// time-of-day and kind.
    /// </summary>
    /// <param name="start">The inclusive start date.</param>
    /// <param name="end">The inclusive end date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The non-working days in the range at the start's time-of-day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IEnumerable<DateTime> EnumerateNonWorkingDays(this DateTime start, DateTime end, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        var time = TimeOnly.FromDateTime(start);
        DateTimeKind kind = start.Kind;

        return DateOnly.FromDateTime(start)
            .EnumerateNonWorkingDays(DateOnly.FromDateTime(end), service, territory, workingWeek)
            .Select(day => day.ToDateTime(time, kind));
    }
}
