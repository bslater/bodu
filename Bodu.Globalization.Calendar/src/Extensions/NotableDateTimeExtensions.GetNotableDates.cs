// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.GetNotableDates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Gets the notable dates emitted on the date for the territory.
    /// </summary>
    /// <param name="date">The date whose date component is resolved.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrences must satisfy.</param>
    /// <returns>The emitted occurrences; empty when there are none.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> GetNotableDates(this DateTime date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        DateOnly.FromDateTime(date).GetNotableDates(service, territory, filter);
}
