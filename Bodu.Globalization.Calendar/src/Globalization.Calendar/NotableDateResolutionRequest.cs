// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionRequest.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents an internal request to resolve notable dates for a chronological window.
/// </summary>
internal sealed record NotableDateResolutionRequest
{
    /// <summary>
    /// Initialises a new instance of the <see cref="NotableDateResolutionRequest" /> class.
    /// </summary>
    /// <param name="startDate">The inclusive start of the requested window.</param>
    /// <param name="endDate">The inclusive end of the requested window.</param>
    /// <param name="projection">The projection semantics requested by the caller.</param>
    /// <param name="territoryCode">The optional territory context.</param>
    /// <param name="calendarType">The optional calendar type context.</param>
    /// <param name="filter">The optional notable-date filter.</param>
    /// <exception cref="ArgumentException"><paramref name="endDate" /> is earlier than <paramref name="startDate" />.</exception>
    public NotableDateResolutionRequest(
        DateTime startDate,
        DateTime endDate,
        NotableDateResolutionProjection projection,
        string? territoryCode = null,
        Type? calendarType = null,
        NotableDateFilter? filter = null)
    {
        DateTime start = startDate.Date;
        DateTime end = endDate.Date;

        if (end < start)
            throw new ArgumentException("The end date must not be earlier than the start date.", nameof(endDate));

        StartDate = start;
        EndDate = end;
        Projection = projection;
        TerritoryCode = territoryCode;
        CalendarType = calendarType;
        Filter = filter;
    }

    /// <summary>
    /// Gets the inclusive start of the requested window.
    /// </summary>
    public DateTime StartDate { get; }

    /// <summary>
    /// Gets the inclusive end of the requested window.
    /// </summary>
    public DateTime EndDate { get; }

    /// <summary>
    /// Gets the projection semantics requested by the caller.
    /// </summary>
    public NotableDateResolutionProjection Projection { get; }

    /// <summary>
    /// Gets the optional territory context.
    /// </summary>
    public string? TerritoryCode { get; }

    /// <summary>
    /// Gets the optional calendar type context.
    /// </summary>
    public Type? CalendarType { get; }

    /// <summary>
    /// Gets the optional notable-date filter.
    /// </summary>
    public NotableDateFilter? Filter { get; }
}