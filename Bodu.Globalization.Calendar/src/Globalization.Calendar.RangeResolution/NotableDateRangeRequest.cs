// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangeRequest.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Describes a chronological window query against the notable-date range-resolution pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The request identifies the inclusive date range the caller cares about and the optional territory, calendar, and
/// filter context that should be applied during materialization and emission.
/// </para>
/// </remarks>
internal sealed record NotableDateRangeRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateRangeRequest" /> class.
    /// </summary>
    /// <param name="startDate">The inclusive start date of the requested window.</param>
    /// <param name="endDate">The inclusive end date of the requested window.</param>
    /// <param name="territoryCode">The optional territory context.</param>
    /// <param name="calendarType">The optional calendar context.</param>
    /// <param name="filter">The optional notable-date filter.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="endDate" /> is earlier than <paramref name="startDate" />.
    /// </exception>
    public NotableDateRangeRequest(
        DateTime startDate,
        DateTime endDate,
        string? territoryCode = null,
        Type? calendarType = null,
        NotableDateFilter? filter = null)
    {
        DateTime start = startDate.Date;
        DateTime end = endDate.Date;

        CalendarThrowHelper.ThrowIfEndDateBeforeStartDate(start, end, nameof(endDate));

        StartDate = start;
        EndDate = end;
        TerritoryCode = territoryCode;
        CalendarType = calendarType;
        Filter = filter;
    }

    /// <summary>
    /// Gets the inclusive start of the requested window.
    /// </summary>
    /// <returns>The window's start <see cref="DateTime" /> with the time-of-day component truncated.</returns>
    public DateTime StartDate { get; }

    /// <summary>
    /// Gets the inclusive end of the requested window.
    /// </summary>
    /// <returns>The window's end <see cref="DateTime" /> with the time-of-day component truncated.</returns>
    public DateTime EndDate { get; }

    /// <summary>
    /// Gets the optional territory context.
    /// </summary>
    /// <returns>The ISO 3166-style territory code, or <see langword="null" /> when the request is unscoped.</returns>
    public string? TerritoryCode { get; }

    /// <summary>
    /// Gets the optional calendar context.
    /// </summary>
    /// <returns>
    /// The CLR <see cref="Type" /> of the requesting calendar, or <see langword="null" /> when unscoped.
    /// </returns>
    public Type? CalendarType { get; }

    /// <summary>
    /// Gets the optional notable-date filter.
    /// </summary>
    /// <returns>
    /// The <see cref="NotableDateFilter" /> applied to the resolution result, or <see langword="null" /> when no filter
    /// is supplied.
    /// </returns>
    public NotableDateFilter? Filter { get; }
}
