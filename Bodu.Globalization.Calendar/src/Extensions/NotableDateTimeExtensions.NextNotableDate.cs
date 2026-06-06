// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.NextNotableDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns the earliest notable date emitted strictly after the date for the territory.
    /// </summary>
    /// <param name="date">The reference date whose date component is used.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrence must satisfy.</param>
    /// <returns>
    /// The next matching occurrence, or <see langword="null" /> when none exists up to the maximum year.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static NotableDate? NextNotableDate(this DateTime date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        DateOnly.FromDateTime(date).NextNotableDate(service, territory, filter);
}
