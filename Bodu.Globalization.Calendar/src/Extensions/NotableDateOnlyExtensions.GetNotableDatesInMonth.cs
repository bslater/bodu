// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.GetNotableDatesInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Resolves the notable dates emitted in the calendar month that contains the date for the territory.
    /// </summary>
    /// <param name="date">A date within the month to resolve.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrences must satisfy.</param>
    /// <returns>The emitted occurrences in the month, ordered by date then identity.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> GetNotableDatesInMonth(this DateOnly date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        new DateOnly(date.Year, date.Month, 1)
            .EnumerateNotableDates(new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)), service, territory, filter);
}
