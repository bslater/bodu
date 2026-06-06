// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.IsWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Determines whether the date is a working day for the territory.
    /// </summary>
    /// <param name="date">The date whose date component is tested.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns><see langword="true" /> if the date is a working day; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static bool IsWorkingDay(this DateTime date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        DateOnly.FromDateTime(date).IsWorkingDay(service, territory, workingWeek);
}
