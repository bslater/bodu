// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateService.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Defines a service for managing, computing, and querying <see cref="NotableDate" /> instances produced from a set of
/// authored <see cref="NotableDateRule" /> sources.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are responsible for combining base <see cref="INotableDateRuleProvider" /> sources with optional
/// <see cref="INotableDateRuleOverrideProvider" /> layers, dispatching to the resolver and adjuster, caching results,
/// and applying any registered <see cref="INotableDateCollisionResolver" /> and
/// <see cref="INotableDateNameLocalizer" /> services to the output.
/// </para>
/// <para>
/// The canonical implementation is <see cref="NotableDateService" />, which supports lazy per-year caching, thread-safe
/// concurrent access, multi-day event spans, and composable <see cref="NotableDateFilter" /> predicates.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Construct the service using the built-in global rule set, then query notable dates for Australia:
/// </para>
/// <code>
///<![CDATA[
/// INotableDateService service = new NotableDateService();
///
/// // All notable dates for Australia in 2026, ordered by date:
/// IReadOnlyList<NotableDate> dates = service.GetNotableDates(2026, territoryCode: "AU");
///
/// // Only non-working public holidays in Australia:
/// NotableDateFilter filter = NotableDateFilter
///     .ForCategory(NotableDateCategory.Public)
///     .And(NotableDateFilter.IsNonWorkingDay());
///
/// IReadOnlyList<NotableDate> holidays = service.GetNotableDates(2026, filter, "AU");
///
/// // Test whether Christmas Day 2026 is a non-working day for New South Wales:
/// bool isNonWorking = service.IsNonWorkingDay(new DateTime(2026, 12, 25), "AU-NSW");
///]]>
/// </code>
/// </example>
public interface INotableDateService
{
    /// <summary>
    /// Gets the working-week pattern in effect for this service.
    /// </summary>
    /// <value>
    /// The <see cref="WeekPattern" /> whose selected days are treated as working days when classifying dates.
    /// </value>
    /// <returns>
    /// The configured working-week <see cref="WeekPattern" />. The default-implemented value is
    /// <see cref="WeekPattern.Weekdays" /> (Monday through Friday) for implementations that do not override this
    /// property.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The default implementation returns <see cref="WeekPattern.Weekdays" /> so that pre-existing
    /// <see cref="INotableDateService" /> implementors continue to compile and behave identically. Implementors that
    /// support configurable working weeks should override this property to surface their effective pattern.
    /// </para>
    /// </remarks>
    WeekPattern WorkingWeek => WeekPattern.Weekdays;

    /// <summary>
    /// Determines whether the supplied date falls on a weekend under the configured weekend definition.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <returns>
    /// <see langword="true" /> if the date is considered a weekend; otherwise <see langword="false" />.
    /// </returns>
    bool IsWeekend(DateTime date);

    /// <summary>
    /// Determines whether the supplied date is treated as a non-working day for the supplied territory and calendar
    /// context.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <param name="territoryCode">Optional territory scope (e.g. <c>"AU"</c>, <c>"AU-NSW"</c>).</param>
    /// <param name="calendarType">Optional calendar scope (e.g. <see cref="SysGlobal.GregorianCalendar" />).</param>
    /// <returns><see langword="true" /> if the date is a non-working day; otherwise <see langword="false" />.</returns>
    bool IsNonWorkingDay(DateTime date, string? territoryCode = null, Type? calendarType = null);

    /// <summary>
    /// Determines whether the supplied date is covered by a non-working notable date for the supplied territory and
    /// calendar context, ignoring the working-week dimension.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <param name="territoryCode">Optional territory scope (e.g. <c>"AU"</c>, <c>"AU-NSW"</c>).</param>
    /// <param name="calendarType">Optional calendar scope (e.g. <see cref="SysGlobal.GregorianCalendar" />).</param>
    /// <returns>
    /// <see langword="true" /> if a non-working notable date covers <paramref name="date" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method answers only the holiday side of "is this a non-working day" so that callers can compose it with a
    /// caller-supplied working-week pattern. The default implementation walks the result of
    /// <see cref="GetNotableDates(DateTime, string?, Type?)" /> and returns <see langword="true" /> when any covering
    /// notable date is flagged non-working. Concrete services may override this with a more efficient evaluation.
    /// </para>
    /// </remarks>
    bool IsHolidayNonWorkingDay(DateTime date, string? territoryCode = null, Type? calendarType = null)
    {
        foreach (NotableDate notable in GetNotableDates(date, territoryCode, calendarType))
        {
            if (notable.IsNonWorkingDay) return true;
        }

        return false;
    }

    /// <summary>
    /// Retrieves every notable date occurring within the supplied year.
    /// </summary>
    /// <param name="year">The year to query.</param>
    /// <param name="territoryCode">Optional territory scope.</param>
    /// <param name="calendarType">Optional calendar scope.</param>
    /// <returns>The notable dates ordered by anchor date.</returns>
    IReadOnlyList<NotableDate> GetNotableDates(int year, string? territoryCode = null, Type? calendarType = null);

    /// <summary>
    /// Retrieves notable dates occurring within the supplied year that satisfy the supplied <paramref name="filter" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The filter is applied in two stages. The primary gate is evaluated against each <see cref="NotableDateRule" />
    /// before its date is resolved; rules that fail the primary gate are skipped entirely. The secondary gate is then
    /// evaluated against each materialized <see cref="NotableDate" />, discarding dates that do not satisfy the full
    /// filter criteria. Filtered queries bypass the per-year cache so that unfiltered queries continue to return
    /// complete, cached results.
    /// </para>
    /// </remarks>
    /// <param name="year">The year to query.</param>
    /// <param name="filter">The filter to apply. Must not be <see langword="null" />.</param>
    /// <param name="territoryCode">Optional territory scope.</param>
    /// <param name="calendarType">Optional calendar scope.</param>
    /// <returns>The matching notable dates ordered by anchor date.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="filter" /> is <see langword="null" />.
    /// </exception>
    IReadOnlyList<NotableDate> GetNotableDates(int year, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null);

    /// <summary>
    /// Retrieves every notable date intersecting the supplied inclusive range. Multi-day spans whose anchor falls
    /// outside the range are still included if any day of the span lies within it.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="territoryCode">Optional territory scope.</param>
    /// <param name="calendarType">Optional calendar scope.</param>
    /// <returns>The notable dates ordered by anchor date.</returns>
    IReadOnlyList<NotableDate> GetNotableDates(DateTime startDate, DateTime endDate, string? territoryCode = null, Type? calendarType = null);

    /// <summary>
    /// Retrieves notable dates intersecting the supplied inclusive range that satisfy the supplied
    /// <paramref name="filter" />. Multi-day spans are included when any day of the span lies within the range.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The filter is applied in two stages. The primary gate is evaluated against each <see cref="NotableDateRule" />
    /// before its date is resolved; rules that fail the primary gate are skipped entirely. The secondary gate is then
    /// evaluated against each materialized <see cref="NotableDate" />, discarding dates that do not satisfy the full
    /// filter criteria. Filtered queries bypass the per-year cache so that unfiltered queries continue to return
    /// complete, cached results.
    /// </para>
    /// </remarks>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <param name="filter">The filter to apply. Must not be <see langword="null" />.</param>
    /// <param name="territoryCode">Optional territory scope.</param>
    /// <param name="calendarType">Optional calendar scope.</param>
    /// <returns>The matching notable dates ordered by anchor date.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="filter" /> is <see langword="null" />.
    /// </exception>
    IReadOnlyList<NotableDate> GetNotableDates(DateTime startDate, DateTime endDate, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null);

    /// <summary>
    /// Retrieves every notable date that contains the supplied day, including multi-day spans whose anchor lies on a
    /// previous day.
    /// </summary>
    /// <param name="date">The day to query.</param>
    /// <param name="territoryCode">Optional territory scope.</param>
    /// <param name="calendarType">Optional calendar scope.</param>
    /// <returns>The notable dates ordered by anchor date.</returns>
    IReadOnlyList<NotableDate> GetNotableDates(DateTime date, string? territoryCode = null, Type? calendarType = null);

    /// <summary>
    /// Retrieves notable dates that contain the supplied day and satisfy the supplied <paramref name="filter" />,
    /// including multi-day spans whose anchor lies on a previous day.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The filter is applied in two stages. The primary gate is evaluated against each <see cref="NotableDateRule" />
    /// before its date is resolved; rules that fail the primary gate are skipped entirely. The secondary gate is then
    /// evaluated against each materialized <see cref="NotableDate" />, discarding dates that do not satisfy the full
    /// filter criteria. Filtered queries bypass the per-year cache so that unfiltered queries continue to return
    /// complete, cached results.
    /// </para>
    /// </remarks>
    /// <param name="date">The day to query.</param>
    /// <param name="filter">The filter to apply. Must not be <see langword="null" />.</param>
    /// <param name="territoryCode">Optional territory scope.</param>
    /// <param name="calendarType">Optional calendar scope.</param>
    /// <returns>The matching notable dates ordered by anchor date.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="filter" /> is <see langword="null" />.
    /// </exception>
    IReadOnlyList<NotableDate> GetNotableDates(DateTime date, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null);

    /// <summary>
    /// Discards all cached notable dates so that subsequent queries regenerate them from the underlying providers.
    /// </summary>
    /// <remarks>
    /// Call this method after mutating any <see cref="INotableDateRuleOverrideProvider" />, switching weekend
    /// definitions, or otherwise changing the inputs that feed rule resolution.
    /// </remarks>
    void Invalidate();

    /// <summary>
    /// Discards cached notable dates for the supplied year only.
    /// </summary>
    /// <param name="year">The year whose cache entries should be cleared.</param>
    void Invalidate(int year);

    /// <summary>
    /// Re-queries every registered <see cref="INotableDateRuleOverrideProvider" /> and rebuilds the effective rule set
    /// so that subsequent queries observe the current additions and removals.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default implementation is a no-op suitable for services whose rules are immutable for the lifetime of the
    /// instance. The canonical <see cref="NotableDateService" /> overrides this method to re-snapshot every override
    /// provider's <see cref="INotableDateRuleOverrideProvider.GetAdditions" /> and
    /// <see cref="INotableDateRuleOverrideProvider.GetRemovals" /> contributions, rebuild the merged rule set, and clear
    /// all cached year results. Base rule providers are <em>not</em> re-queried; they are considered load-time inputs.
    /// </para>
    /// <para>
    /// Call this method after mutating a runtime-mutable override provider (for example,
    /// <see cref="MutableNotableDateRuleOverrideProvider" />) so that additions and removals authored after construction
    /// take effect.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// Wire a <see cref="MutableNotableDateRuleOverrideProvider" /> so the service auto-reloads when the provider
    /// changes:
    /// </para>
    /// <code>
    ///<![CDATA[
    /// var overrides = new MutableNotableDateRuleOverrideProvider();
    /// INotableDateService service = new NotableDateService(
    ///     ruleProviders: AsiaPacificCalendarData.CreateProviders(),
    ///     workingWeek: WeekPattern.MondayToFriday,
    ///     options: new NotableDateServiceOptions { OverrideProviders = new[] { overrides } });
    ///
    /// overrides.Changed += (_, _) => service.Reload();
    ///
    /// overrides.AddRule(new NotableDateRule
    /// {
    ///     Name = "Site Maintenance Day",
    ///     Strategy = DateResolutionStrategy.Fixed,
    ///     Category = NotableDateCategory.Observance,
    ///     Month = 3,
    ///     Day = 28,
    ///     IsNonWorkingDay = true,
    /// });
    /// // New rule is now visible to service.GetNotableDates(...).
    ///]]>
    /// </code>
    /// </example>
    void Reload()
    {
        Invalidate();
    }

    /// <summary>
    /// Returns the distinct set of territory codes referenced by the service's effective rule set.
    /// </summary>
    /// <returns>
    /// A read-only collection of every non-empty <see cref="NotableDateRule.TerritoryCode" /> that the loaded providers
    /// cover, sorted case-insensitively. Rules without an explicit territory (global rules) contribute no entry.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The default implementation returns an empty collection; concrete services should override it to project over
    /// their effective rule set. Consumers typically use the returned codes to drive territory pickers in UI or to
    /// enumerate query scopes programmatically.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    ///<![CDATA[
    /// foreach (string code in service.GetSupportedTerritories())
    /// {
    ///     Console.WriteLine($"{code}: {service.GetNotableDates(2026, code).Count} notable dates");
    /// }
    ///]]>
    /// </code>
    /// </example>
    IReadOnlyCollection<string> GetSupportedTerritories() => [];

    /// <summary>
    /// Returns the distinct set of calendar types referenced by the service's effective rule set.
    /// </summary>
    /// <returns>
    /// A read-only collection of every non-<see langword="null" /> <see cref="NotableDateRule.CalendarType" /> that the
    /// loaded providers reference. Rules without an explicit calendar scope contribute no entry.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The default implementation returns an empty collection; concrete services should override it to project over
    /// their effective rule set. Consumers typically use the returned types to expose calendar-specific filters
    /// (for example, separating Gregorian observances from <see cref="SysGlobal.HebrewCalendar" />-anchored ones).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    ///<![CDATA[
    /// IReadOnlyCollection<Type> calendars = service.GetSupportedCalendars();
    /// bool hasHebrewRules = calendars.Contains(typeof(System.Globalization.HebrewCalendar));
    ///]]>
    /// </code>
    /// </example>
    IReadOnlyCollection<Type> GetSupportedCalendars() => [];
}
