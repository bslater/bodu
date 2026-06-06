// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeOffsetExtensions.IsNotableDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateTimeOffsetExtensions
{
    /// <summary>
    /// Determines whether any notable date is emitted on the date for the territory.
    /// </summary>
    /// <param name="date">The value whose offset-local date is tested.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrence must satisfy.</param>
    /// <returns>
    /// <see langword="true" /> if at least one occurrence is emitted; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    public static bool IsNotableDate(this DateTimeOffset date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        DateOnly.FromDateTime(date.DateTime).IsNotableDate(service, territory, filter);
}
