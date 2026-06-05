// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides convenience query extension methods over <see cref="INotableDateService" />, including a by-year overload
/// that resolves a full civil year.
/// </summary>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// INotableDateService service = AmericasCalendarData.CreateService("US");
///
/// // Resolve a whole civil year in one call.
/// IReadOnlyList<NotableDate> all = service.Resolve(2026, "US");
///
/// // Or restrict to non-working public holidays.
/// NotableDateFilter holidays = NotableDateFilter
///     .ForCategory(NotableDateCategory.PublicHoliday)
///     .And(NotableDateFilter.IsNonWorkingDay());
/// IReadOnlyList<NotableDate> publicHolidays = service.Resolve(2026, "US", holidays);
///]]>
/// </code>
/// </example>
/// <seealso cref="INotableDateService" />
/// <seealso cref="NotableDateFilter" />
/// <seealso href="../guides/calendar/notable-dates.html">Using NotableDateService (guide)</seealso>
public static class NotableDateServiceExtensions
{
    /// <summary>
    /// Resolves every notable-date occurrence emitted in a civil year for the requested territory.
    /// </summary>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="year">The civil year to resolve.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <returns>The occurrences emitted in the year, ordered by date then identity.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="year" /> is not a valid year.</exception>
    public static IReadOnlyList<NotableDate> Resolve(this INotableDateService service, int year, string territory)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);

        return service.Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory);
    }

    /// <summary>
    /// Resolves the notable-date occurrences emitted in a civil year for the requested territory, restricted to those
    /// matching the supplied filter.
    /// </summary>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="year">The civil year to resolve.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">The filter that emitted occurrences must satisfy.</param>
    /// <returns>The matching occurrences emitted in the year, ordered by date then identity.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" />, <paramref name="territory" />, or <paramref name="filter" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="year" /> is not a valid year.</exception>
    public static IReadOnlyList<NotableDate> Resolve(this INotableDateService service, int year, string territory, NotableDateFilter filter)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfNull(filter);

        return service.Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory, filter);
    }
}
