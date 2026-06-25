// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.GetNotableDatesInYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Resolves the notable dates emitted in the calendar year that contains the date for the territory.
    /// </summary>
    /// <param name="date">A date within the year to resolve.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">An optional filter the occurrences must satisfy.</param>
    /// <returns>The emitted occurrences in the year, ordered by date then identity.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// INotableDateService service = AmericasCalendarData.CreateService("US");
    ///
    /// // Every notable date in 2026, derived from any date within the year.
    /// IReadOnlyList<NotableDate> year = new DateOnly(2026, 1, 1).GetNotableDatesInYear(service, "US");
    ///]]>
    /// </code>
    /// </example>
    public static IReadOnlyList<NotableDate> GetNotableDatesInYear(this DateOnly date, INotableDateService service, string territory, NotableDateFilter? filter = null) =>
        new DateOnly(date.Year, 1, 1)
            .EnumerateNotableDates(new DateOnly(date.Year, 12, 31), service, territory, filter);
}
