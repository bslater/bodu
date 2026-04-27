// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateService.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Defines a service for managing, computing, and querying <see cref="NotableDate" /> instances produced from a set of authored
/// <see cref="NotableDateRule" /> sources.
/// </summary>
/// <remarks>
/// Implementations are responsible for combining base <see cref="INotableDateRuleProvider" /> sources with optional
/// <see cref="INotableDateRuleOverrideProvider" /> layers, dispatching to the resolver and adjuster, caching results, and applying any
/// registered <see cref="INotableDateCollisionResolver" /> and <see cref="INotableDateNameLocalizer" /> services to the output.
/// </remarks>
public interface INotableDateService
{
	/// <summary>
	/// Determines whether the supplied date falls on a weekend under the configured weekend definition.
	/// </summary>
	/// <param name="date">The date to evaluate.</param>
	/// <returns><see langword="true" /> if the date is considered a weekend; otherwise <see langword="false" />.</returns>
	bool IsWeekend(DateTime date);

	/// <summary>
	/// Determines whether the supplied date is treated as a non-working day for the supplied territory and calendar context.
	/// </summary>
	/// <param name="date">The date to evaluate.</param>
	/// <param name="territoryCode">Optional territory scope (e.g. <c>"AU"</c>, <c>"AU-NSW"</c>).</param>
	/// <param name="calendarType">Optional calendar scope (e.g. <see cref="SysGlobal.GregorianCalendar" />).</param>
	/// <returns><see langword="true" /> if the date is a non-working day; otherwise <see langword="false" />.</returns>
	bool IsNonWorkingDay(DateTime date, string? territoryCode = null, Type? calendarType = null);

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
	/// The filter is applied in two stages. The primary gate is evaluated against each <see cref="NotableDateRule" /> before its date
	/// is resolved; rules that fail the primary gate are skipped entirely. The secondary gate is then evaluated against each
	/// materialised <see cref="NotableDate" />, discarding dates that do not satisfy the full filter criteria. Filtered queries
	/// bypass the per-year cache so that unfiltered queries continue to return complete, cached results.
	/// </para>
	/// </remarks>
	/// <param name="year">The year to query.</param>
	/// <param name="filter">The filter to apply. Must not be <see langword="null" />.</param>
	/// <param name="territoryCode">Optional territory scope.</param>
	/// <param name="calendarType">Optional calendar scope.</param>
	/// <returns>The matching notable dates ordered by anchor date.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="filter" /> is <see langword="null" />.</exception>
	IReadOnlyList<NotableDate> GetNotableDates(int year, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null);

	/// <summary>
	/// Retrieves every notable date intersecting the supplied inclusive range. Multi-day spans whose anchor falls outside the range
	/// are still included if any day of the span lies within it.
	/// </summary>
	/// <param name="startDate">The inclusive start of the range.</param>
	/// <param name="endDate">The inclusive end of the range.</param>
	/// <param name="territoryCode">Optional territory scope.</param>
	/// <param name="calendarType">Optional calendar scope.</param>
	/// <returns>The notable dates ordered by anchor date.</returns>
	IReadOnlyList<NotableDate> GetNotableDates(DateTime startDate, DateTime endDate, string? territoryCode = null, Type? calendarType = null);

	/// <summary>
	/// Retrieves notable dates intersecting the supplied inclusive range that satisfy the supplied <paramref name="filter" />. Multi-day
	/// spans are included when any day of the span lies within the range.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The filter is applied in two stages. The primary gate is evaluated against each <see cref="NotableDateRule" /> before its date
	/// is resolved; rules that fail the primary gate are skipped entirely. The secondary gate is then evaluated against each
	/// materialised <see cref="NotableDate" />, discarding dates that do not satisfy the full filter criteria. Filtered queries
	/// bypass the per-year cache so that unfiltered queries continue to return complete, cached results.
	/// </para>
	/// </remarks>
	/// <param name="startDate">The inclusive start of the range.</param>
	/// <param name="endDate">The inclusive end of the range.</param>
	/// <param name="filter">The filter to apply. Must not be <see langword="null" />.</param>
	/// <param name="territoryCode">Optional territory scope.</param>
	/// <param name="calendarType">Optional calendar scope.</param>
	/// <returns>The matching notable dates ordered by anchor date.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="filter" /> is <see langword="null" />.</exception>
	IReadOnlyList<NotableDate> GetNotableDates(DateTime startDate, DateTime endDate, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null);

	/// <summary>
	/// Retrieves every notable date that contains the supplied day, including multi-day spans whose anchor lies on a previous day.
	/// </summary>
	/// <param name="date">The day to query.</param>
	/// <param name="territoryCode">Optional territory scope.</param>
	/// <param name="calendarType">Optional calendar scope.</param>
	/// <returns>The notable dates ordered by anchor date.</returns>
	IReadOnlyList<NotableDate> GetNotableDates(DateTime date, string? territoryCode = null, Type? calendarType = null);

	/// <summary>
	/// Retrieves notable dates that contain the supplied day and satisfy the supplied <paramref name="filter" />, including multi-day
	/// spans whose anchor lies on a previous day.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The filter is applied in two stages. The primary gate is evaluated against each <see cref="NotableDateRule" /> before its date
	/// is resolved; rules that fail the primary gate are skipped entirely. The secondary gate is then evaluated against each
	/// materialised <see cref="NotableDate" />, discarding dates that do not satisfy the full filter criteria. Filtered queries
	/// bypass the per-year cache so that unfiltered queries continue to return complete, cached results.
	/// </para>
	/// </remarks>
	/// <param name="date">The day to query.</param>
	/// <param name="filter">The filter to apply. Must not be <see langword="null" />.</param>
	/// <param name="territoryCode">Optional territory scope.</param>
	/// <param name="calendarType">Optional calendar scope.</param>
	/// <returns>The matching notable dates ordered by anchor date.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="filter" /> is <see langword="null" />.</exception>
	IReadOnlyList<NotableDate> GetNotableDates(DateTime date, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null);

	/// <summary>
	/// Discards all cached notable dates so that subsequent queries regenerate them from the underlying providers.
	/// </summary>
	/// <remarks>
	/// Call this method after mutating any <see cref="INotableDateRuleOverrideProvider" />, switching weekend definitions, or
	/// otherwise changing the inputs that feed rule resolution.
	/// </remarks>
	void Invalidate();

	/// <summary>
	/// Discards cached notable dates for the supplied year only.
	/// </summary>
	/// <param name="year">The year whose cache entries should be cleared.</param>
	void Invalidate(int year);
}
