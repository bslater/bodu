// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.PreviousNotableDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns the most recent notable date emitted strictly before the date for the territory.
    /// </summary>
    /// <param name="date">The reference date whose date component is used.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrence must satisfy.</param>
    /// <returns>
    /// The previous matching occurrence, or <see langword="null" /> when none exists down to the minimum year.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static NotableDate? PreviousNotableDate(this DateTime date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        DateOnly.FromDateTime(date).PreviousNotableDate(service, territory, filter);
}
