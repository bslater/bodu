// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.SnapToWorkingDayBackward.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns the date if it is a working day; otherwise the previous working day.
    /// </summary>
    /// <param name="date">The date to snap.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The date or the previous working day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateOnly SnapToWorkingDayBackward(this DateOnly date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        date.IsWorkingDay(service, territory, workingWeek) ? date : Step(date, -1, service, territory, workingWeek);
}
