// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeOffsetExtensions.IsNonWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateTimeOffsetExtensions
{
    /// <summary>
    /// Determines whether the date is a non-working day for the territory.
    /// </summary>
    /// <param name="date">The value whose offset-local date is tested.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns><see langword="true" /> if the date is non-working; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static bool IsNonWorkingDay(this DateTimeOffset date, INotableDateService service, string territory, WeekPattern? workingWeek = null) =>
        DateOnly.FromDateTime(date.DateTime).IsNonWorkingDay(service, territory, workingWeek);
}
