// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.GetNotableDates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Gets the notable dates emitted on the date for the territory.
    /// </summary>
    /// <param name="date">The date to resolve.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrences must satisfy.</param>
    /// <returns>The emitted occurrences; empty when there are none.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// INotableDateService service = AmericasCalendarData.CreateService("US");
    ///
    /// IReadOnlyList<NotableDate> occurrences = new DateOnly(2026, 12, 25).GetNotableDates(service, "US");
    /// foreach (NotableDate notable in occurrences)
    ///     Console.WriteLine(notable.DisplayName);
    ///]]>
    /// </code>
    /// </example>
    public static IReadOnlyList<NotableDate> GetNotableDates(this DateOnly date, INotableDateService service, string territory, NotableDateFilter? filter = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        return filter is null ? service.Resolve(date, territory) : service.Resolve(date, territory, filter);
    }
}
