// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.EnumerateNotableDates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Resolves the notable dates emitted within the inclusive range for the territory.
    /// </summary>
    /// <param name="start">The inclusive start date.</param>
    /// <param name="end">The inclusive end date.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrences must satisfy.</param>
    /// <returns>The emitted occurrences, ordered by date then identity.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> EnumerateNotableDates(this DateOnly start, DateOnly end, INotableDateService service, string territory, NotableDateFilter? filter = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        DateRange range = new(start, end);
        return filter is null ? service.Resolve(range, territory) : service.Resolve(range, territory, filter);
    }
}
