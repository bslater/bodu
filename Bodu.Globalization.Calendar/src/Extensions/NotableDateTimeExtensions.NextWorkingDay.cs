// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.NextWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns the first working day strictly after the date, preserving the time-of-day and kind.
    /// </summary>
    /// <param name="date">The starting date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The next working day at the original time-of-day.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static DateTime NextWorkingDay(this DateTime date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        WithTimeOf(date, DateOnly.FromDateTime(date).NextWorkingDay(service, territory, workingWeek));
}
