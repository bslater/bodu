// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Resolves notable-date occurrences — public holidays, observances, and other named dates — for a requested territory
/// and a single day or an inclusive date range.
/// </summary>
/// <remarks>
/// <para>
/// An implementation draws its occurrences from a loaded <see cref="NotableDateResource" /> and applies the resource's
/// resolution policy: it calculates each rule's actual date, places any observed (substitute) date, resolves same-day
/// collisions, and emits the surviving <see cref="NotableDate" /> occurrences. The canonical implementation is
/// <see cref="NotableDateService" />.
/// </para>
/// <para>
/// <strong>Territory.</strong> The <c>territory</c> argument is a country code (<c>US</c>) or a subdivision
/// (<c>CA-ON</c>, <c>AU-NSW</c>). A subdivision sees both its own rules and the broader rules of its parent country,
/// with the most specific rule winning for a shared concept.
/// </para>
/// <para>
/// <strong>Inclusion by emitted date.</strong> An occurrence is included when its emitted (observed) date falls on the
/// requested day or within the requested range, so a single-day query and a range query covering the same dates return
/// consistent results.
/// </para>
/// <para>
/// <strong>When to use.</strong> Resolve a service from a data pack (for example the <c>AmericasCalendarData</c>
/// bundle) or from a document loaded with
/// <see cref="NotableDateResourceLoader" />, then query it directly; or call the working-day extension methods in
/// <c>Bodu.Extensions</c> (such as <c>IsWorkingDay</c> / <c>NextWorkingDay</c>), which delegate to this surface.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Resolve a service over a country's bundled rules.
/// INotableDateService service = AmericasCalendarData.CreateService("US");
///
/// // Every notable date emitted on a single day.
/// IReadOnlyList<NotableDate> newYear = service.Resolve(new DateOnly(2026, 1, 1), "US");
///
/// // Non-working public holidays across a whole year, restricted by a filter.
/// NotableDateFilter holidays = NotableDateFilter
///     .ForCategory(NotableDateCategory.PublicHoliday)
///     .And(NotableDateFilter.IsNonWorkingDay());
///
/// DateRange year = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
/// IReadOnlyList<NotableDate> publicHolidays = service.Resolve(year, "US", holidays);
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDateService" />
/// <seealso cref="NotableDate" />
/// <seealso cref="NotableDateFilter" />
/// <seealso cref="NotableDateResourceLoader" />
public interface INotableDateService
{
    /// <summary>
    /// Resolves the notable-date occurrences emitted on a single day for the requested territory.
    /// </summary>
    /// <param name="date">The day to resolve.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <returns>
    /// The occurrences whose emitted date equals <paramref name="date" />; empty when there are none.
    /// </returns>
    IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory);

    /// <summary>
    /// Resolves the notable-date occurrences emitted within an inclusive date range for the requested territory.
    /// </summary>
    /// <param name="range">The inclusive range of days to resolve.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <returns>The occurrences whose emitted date falls within the range, ordered by date then identity.</returns>
    IReadOnlyList<NotableDate> Resolve(DateRange range, string territory);

    /// <summary>
    /// Resolves the notable-date occurrences emitted on a single day for the requested territory, restricted to those
    /// matching the supplied filter.
    /// </summary>
    /// <param name="date">The day to resolve.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">The filter that emitted occurrences must satisfy.</param>
    /// <returns>The matching occurrences; empty when there are none.</returns>
    IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory, NotableDateFilter filter);

    /// <summary>
    /// Resolves the notable-date occurrences emitted within an inclusive date range for the requested territory,
    /// restricted to those matching the supplied filter.
    /// </summary>
    /// <param name="range">The inclusive range of days to resolve.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="filter">The filter that emitted occurrences must satisfy.</param>
    /// <returns>The matching occurrences, ordered by date then identity.</returns>
    IReadOnlyList<NotableDate> Resolve(DateRange range, string territory, NotableDateFilter filter);

    /// <summary>
    /// Gets the distinct territories any rule in the resource is scoped to.
    /// </summary>
    /// <returns>
    /// The scoped territory codes in case-insensitive ascending order; empty when every rule is global (unscoped).
    /// </returns>
    IReadOnlyList<string> GetSupportedTerritories();

    /// <summary>
    /// Gets the distinct calendar systems any rule in the resource is expressed in.
    /// </summary>
    /// <returns>The calendar systems in use, in ascending order.</returns>
    IReadOnlyList<CalendarSystem> GetSupportedCalendars();
}
