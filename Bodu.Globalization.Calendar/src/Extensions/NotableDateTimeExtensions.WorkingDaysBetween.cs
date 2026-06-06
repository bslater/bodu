// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.WorkingDaysBetween.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Counts the working days in the inclusive range bounded by the two dates, regardless of their order.
    /// </summary>
    /// <param name="start">One end of the range.</param>
    /// <param name="end">The other end of the range.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The number of working days in the inclusive range.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static int WorkingDaysBetween(this DateTime start, DateTime end, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        DateOnly.FromDateTime(start).WorkingDaysBetween(DateOnly.FromDateTime(end), service, territory, workingWeek);
}
